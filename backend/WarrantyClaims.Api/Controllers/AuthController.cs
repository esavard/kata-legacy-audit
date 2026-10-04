using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using WarrantyClaims.Api.Data;
using WarrantyClaims.Api.Services;

namespace WarrantyClaims.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly WarrantyContext _context;
        private readonly JwtTokenService _jwtTokenService;
        private readonly ILogger<AuthController> _logger;
        // half the codebase logs through ILogger (the "modern" Azure-port half), half
        // through log4net directly (the half nobody touched) - this file does both.
        private static readonly log4net.ILog Log4 = log4net.LogManager.GetLogger(typeof(AuthController));

        public AuthController(WarrantyContext context, JwtTokenService jwtTokenService, ILogger<AuthController> logger)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Login was moved OFF EntityFrameworkCore and onto a raw Npgsql command
            // "for performance" during a 2019 troubleshooting session (EF wasn't
            // actually the bottleneck - the real one was an unrelated N+1 query
            // elsewhere that never got fixed). The raw SQL here concatenates the
            // username directly into the query text instead of parameterizing it -
            // textbook SQL injection, e.g. username = admin' --  bypasses the password
            // check entirely by commenting it out.
            var passwordHash = Sha1Unsalted(request.Password ?? "");
            var sql = $"SELECT id, username, password_hash, role, full_name, dealer_id " +
                      $"FROM users WHERE username = '{request.Username}' AND password_hash = '{passwordHash}'";

            using var conn = new NpgsqlConnection(_context.Database.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);

            int? userId = null; string username = null, role = null, fullName = null; int? dealerId = null;

            try
            {
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    userId = reader.GetInt32(0);
                    username = reader.GetString(1);
                    role = reader.GetString(3);
                    fullName = reader.IsDBNull(4) ? null : reader.GetString(4);
                    dealerId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5);
                }
            }
            catch (Exception ex)
            {
                // logs the raw SQL (including whatever was injected) alongside the
                // exception - into wwwroot/logs/app.log, which is served unauthenticated
                // (see Startup.cs / log4net.config).
                Log4.Error("Login query failed. SQL: " + sql, ex);
                return StatusCode(500, "Login failed: " + ex.Message);
            }

            if (userId == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            var user = new Models.User
            {
                Id = userId.Value,
                Username = username,
                Role = role,
                FullName = fullName,
                DealerId = dealerId
            };

            var token = _jwtTokenService.GenerateToken(user);
            return Ok(new { token, role, fullName, dealerId });
        }

        private static string Sha1Unsalted(string input)
        {
            // SHA-1, no salt. Chosen in 2019 under the (even then outdated) belief that
            // "MD5 is the broken one, SHA-1 is fine." See sql/seed equivalent (db/seed.sql)
            // for how seed passwords were hashed the same way.
            using var sha1 = SHA1.Create();
            var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
