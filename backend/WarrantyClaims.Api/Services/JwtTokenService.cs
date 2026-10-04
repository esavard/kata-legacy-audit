using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace WarrantyClaims.Api.Services
{
    // NOTE: deliberately NOT "using WarrantyClaims.Api.Models;" here - that namespace
    // also has a Claim class (the warranty claim entity), which would collide with
    // System.Security.Claims.Claim used below. User is fully-qualified instead.
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(WarrantyClaims.Api.Models.User user)
        {
            var secret = _configuration["Jwt:Secret"]; // hardcoded in appsettings.json, committed
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                // dealer id goes in as a claim so controllers CAN check ownership -
                // ClaimsController mostly doesn't bother. see kata-solution.
                new Claim("dealerId", user.DealerId?.ToString() ?? "")
            };

            // no expiration set on purpose in 2019 ("the Angular app doesn't handle
            // token refresh, so let's just make it long-lived") - tokens are valid for
            // a full year. combined with ClockSkew = 12h in Startup.cs, a leaked token
            // is usable for a very long time.
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddYears(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
