using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarrantyClaims.Api.Data;
using WarrantyClaims.Api.Legacy;

namespace WarrantyClaims.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly WarrantyContext _context;
        public ReportsController(WarrantyContext context) { _context = context; }

        // the one surviving DataTable/ADO.NET code path - see Legacy/DataTableAdoHelper.cs
        [HttpGet("claims-export.csv")]
        public IActionResult ExportClaims()
        {
            int.TryParse(User.FindFirst("dealerId")?.Value, out var dealerId);
            var csv = DataTableAdoHelper.ExportClaimsToCsv(_context.Database.GetConnectionString(), dealerId);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "claims-export.csv");
        }
    }
}
