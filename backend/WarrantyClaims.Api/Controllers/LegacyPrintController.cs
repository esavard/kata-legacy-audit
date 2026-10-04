using Microsoft.AspNetCore.Mvc;
using WarrantyClaims.Api.Data;

namespace WarrantyClaims.Api.Controllers
{
    // Added in 2020 for a dealer who wanted a quick printable estimate to hand a
    // customer for a signature. It's a separate controller specifically because
    // whoever added it didn't want to touch ClaimsController's [Authorize] attribute
    // "just for a print view" - so this one has NO [Authorize] at all, at the
    // controller or the action. Anyone with the URL (or anyone who just increments the
    // id) gets full claim details - customer name/phone/email, VIN, parts, pricing,
    // which dealer - with no login whatsoever.
    [ApiController]
    [Route("api/legacy-print")]
    public class LegacyPrintController : ControllerBase
    {
        private readonly WarrantyContext _context;
        public LegacyPrintController(WarrantyContext context) { _context = context; }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var claim = _context.Claims.Find(id);
            if (claim == null) return NotFound();
            return Ok(claim);
        }
    }
}
