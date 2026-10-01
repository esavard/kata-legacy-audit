using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace WarrantyClaims.Api
{
    // Old-style Startup.cs hosting, kept exactly as it was in the ASP.NET Core 2.2 days
    // when this was ported from the WinForms desktop app. Every yearly TFM bump since
    // (netcoreapp2.2 -> 3.1 -> ... -> net8.0, to keep Azure App Service happy) touched
    // this file as little as possible. Nobody has rewritten it as a minimal-API
    // Program.cs even though that's been the default template for years now.
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
