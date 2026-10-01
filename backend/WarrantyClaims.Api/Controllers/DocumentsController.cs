using System;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WarrantyClaims.Api.Data;

namespace WarrantyClaims.Api.Controllers
{
    [ApiController]
    [Route("api/claims/{claimId}/documents")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly WarrantyContext _context;

        public DocumentsController(IWebHostEnvironment env, WarrantyContext context)
        {
            _env = env;
            _context = context;
        }

        // Takes whatever filename the browser sends (IFormFile.FileName is entirely
        // client-controlled) and Path.Combine()s it directly into the uploads folder -
        // no Path.GetFileName() sanitization, no extension allow-list, no size cap, no
        // content-type check. A filename like "../../appsettings.json" or
        // "../Controllers/ClaimsController.cs" writes OUTSIDE wwwroot/uploads entirely
        // (CWE-22 path traversal). Even a "normal" upload ends up directly, publicly
        // downloadable, because wwwroot is served unauthenticated (see Startup.cs).
        [HttpPost]
        public IActionResult Upload(int claimId, IFormFile document)
        {
            var claim = _context.Claims.Find(claimId);
            if (claim == null) return NotFound();
            if (document == null || document.Length == 0) return BadRequest("No file.");

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var destPath = Path.Combine(uploadsDir, $"{claimId}_{document.FileName}");

            using (var stream = new FileStream(destPath, FileMode.Create))
            {
                document.CopyTo(stream);
            }

            claim.PhotoPath = $"{claimId}_{document.FileName}";
            _context.SaveChanges();

            return Ok(new { path = claim.PhotoPath });
        }
    }
}
