using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarrantyClaims.Api.Data;

namespace WarrantyClaims.Api.Controllers
{
    // Backs the manufacturer dropdown on the "New Claim" / dealer admin screens.
    // Returns every active row in the manufacturers table, which is exactly the
    // problem: Kestrel/Solenne/Northgate show up right alongside Bramwell, looking equally
    // supported, when only Bramwell has real rules behind it anywhere else in the app
    // (see Services/ManufacturerRulesService.cs). This endpoint is the UI-level half
    // of the "fake multi-manufacturer" story.
    [ApiController]
    [Route("api/manufacturers")]
    [Authorize]
    public class ManufacturersController : ControllerBase
    {
        private readonly WarrantyContext _context;
        public ManufacturersController(WarrantyContext context) { _context = context; }

        [HttpGet]
        public IActionResult List()
        {
            var manufacturers = _context.Manufacturers.Where(m => m.IsActive);
            return Ok(manufacturers);
        }
    }
}
