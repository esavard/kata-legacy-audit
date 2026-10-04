using System;
using System.IO;
using System.Text;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using WarrantyClaims.Api.Data;
using WarrantyClaims.Api.Services;

namespace WarrantyClaims.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // connection string lives straight in appsettings.json, committed to the
            // repo - see the comment there. same story as the Jwt:Secret below.
            services.AddDbContext<WarrantyContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("WarrantyDb")));

            services.AddScoped<ManufacturerRulesService>();
            services.AddScoped<JwtTokenService>();

            // kept Newtonsoft for the whole pipeline because "the Angular app expects
            // the old date format" - see csproj comment. Nobody has ever written down
            // which endpoint actually needed that.
            services.AddControllers()
                .AddNewtonsoftJson(opts =>
                {
                    opts.SerializerSettings.DateFormatString = "yyyy-MM-dd";
                    // NOTE: TypeNameHandling is NOT set globally - but see
                    // ClaimsController.ImportExtraFields(), which turns it on manually
                    // per-call for "flexible" input. that's the dangerous one.
                });

            // CORS wide open for the Angular SPA, which during the Azure port ended up
            // hosted on a different subdomain than the API and nobody wanted to deal
            // with configuring an allow-list "for now". That was 2019.
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            // JWT secret is a hardcoded string from appsettings.json (committed). Issuer
            // and audience validation are both off because the SPA and API were briefly
            // on different hostnames during a migration test and turning validation back
            // on "broke login" - a TODO to revisit this has been open since 2019.
            var jwtSecret = Configuration["Jwt:Secret"];
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ClockSkew = TimeSpan.FromHours(12) // generous, because of clock drift on an old Azure VM once. never revisited.
                    };
                });

            services.AddAuthorization();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // log4net config loaded manually since this predates the Microsoft.Extensions.Logging
            // integration being set up properly - half the codebase logs through ILogger,
            // half calls LogManager.GetLogger(...) directly (see Controllers). see
            // log4net.config for where this ends up writing (hint: wwwroot).
            XmlConfigurator.Configure(LogManager.GetRepository(), new FileInfo("log4net.config"));

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // UseStaticFiles() runs BEFORE UseAuthentication()/UseAuthorization() below,
            // which means everything under wwwroot/ - including wwwroot/uploads (user
            // documents) and wwwroot/logs (the log4net output, see log4net.config) - is
            // served to anyone, logged in or not. This was fine when uploads/ was empty.
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
