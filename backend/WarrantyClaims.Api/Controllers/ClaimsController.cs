using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WarrantyClaims.Api.Data;
using WarrantyClaims.Api.Services;
using ClaimModel = WarrantyClaims.Api.Models.Claim;

namespace WarrantyClaims.Api.Controllers
{
    // The God controller. Everything about a claim's lifecycle - CRUD, workflow
    // transitions, tax/total math, PDF triggers - lives here instead of being split
    // into a domain layer + application services, same as its WinForms-era ancestor
    // where all of this lived in one form's code-behind (FrmClaimEdit.cs, if you're
    // ever shown the original desktop source - see git history/backups if anyone finds
    // them). The web port kept the shape, just moved it into a controller.
    [ApiController]
    [Route("api/claims")]
    [Authorize]
    public class ClaimsController : ControllerBase
    {
        private const decimal TaxRate = 0.13m; // flat HST - fine for the Ontario-only Bramwell dealers we have TODAY

        private readonly WarrantyContext _context;
        private readonly ManufacturerRulesService _rulesService;
        private static readonly log4net.ILog Log4 = log4net.LogManager.GetLogger(typeof(ClaimsController));

        public ClaimsController(WarrantyContext context, ManufacturerRulesService rulesService)
        {
            _context = context;
            _rulesService = rulesService;
        }

        // GET /api/claims - scoped to the caller's dealer via the JWT "dealerId" claim...
        // for THIS endpoint. GetById below does not apply the same scoping - see there.
        [HttpGet]
        public IActionResult List()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var dealerIdClaim = User.FindFirst("dealerId")?.Value;

            IQueryable<ClaimModel> query = _context.Claims.Where(c => !c.IsDeleted);

            if (role == "dealer" && int.TryParse(dealerIdClaim, out var dealerId))
            {
                query = query.Where(c => c.DealerId == dealerId);
            }
            // role == "manufacturer"/"admin" sees everything - intentional (that's the
            // approval queue), but note GetById() below gives that same "everything"
            // access to a plain dealer account too, which is NOT intentional.

            var claims = query.OrderByDescending(c => c.Id).ToList();
            return Ok(claims);
        }

        // GET /api/claims/{id} - no ownership/tenant check at all. Any authenticated
        // account, dealer or manufacturer, from ANY dealership, can fetch ANY claim by
        // incrementing the id. IDOR, and with several real Bramwell dealers in production
        // now (unlike the single-dealer days), this actually leaks one dealer's
        // customer data (name/phone/email, VIN) to a completely unrelated dealer.
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim == null) return NotFound();
            return Ok(new { claim, estimatedTotal = CalculateGrandTotal(claim) });
        }

        // GET /api/claims/search?q=... - built with FromSqlRaw and plain string
        // interpolation instead of FromSqlInterpolated or a parameterized query. EF
        // Core's FromSqlRaw does NOT parameterize an interpolated string for you (that's
        // what FromSqlInterpolated is for) - this is one of the most common real-world
        // EF Core SQL injection mistakes, and it's sitting right here.
        [HttpGet("search")]
        public IActionResult Search([FromQuery] string q)
        {
            q ??= "";
            var sql = $"SELECT * FROM claims WHERE is_deleted = false AND " +
                      $"(customer_name ILIKE '%{q}%' OR vin ILIKE '%{q}%' OR claim_number ILIKE '%{q}%')";
            try
            {
                var results = _context.Claims.FromSqlRaw(sql).ToList();
                return Ok(results);
            }
            catch (Exception ex)
            {
                Log4.Error("Search query failed. SQL: " + sql, ex);
                return StatusCode(500, "Search failed: " + ex.Message);
            }
        }

        public class NewClaimRequest
        {
            public string Vin { get; set; }
            public string VehicleModel { get; set; }
            public string VehicleYear { get; set; }
            public string VehicleMileageKm { get; set; }
            public string VehicleColor { get; set; }
            public string CustomerName { get; set; }
            public string CustomerPhone { get; set; }
            public string CustomerEmail { get; set; }
            public string ProblemDescription { get; set; }
            public decimal? LaborHours { get; set; }
            public List<PartLine> Parts { get; set; }
        }

        public class PartLine
        {
            public string Name { get; set; }
            public string Number { get; set; }
            public int? Qty { get; set; }
            public decimal? UnitPrice { get; set; }
        }

        // No request validation beyond what [ApiController]'s automatic model binding
        // gives you for free (which is nothing here, since nothing is marked
        // [Required]). ProblemDescription is stored as-is; the frontend is the only
        // thing standing between this and a stored XSS payload (see
        // ClaimDetailController.js's ng-bind-html usage).
        [HttpPost]
        public IActionResult Create([FromBody] NewClaimRequest req)
        {
            var dealerIdClaim = User.FindFirst("dealerId")?.Value;
            if (!int.TryParse(dealerIdClaim, out var dealerId)) return BadRequest("No dealer on account.");

            var dealer = _context.Dealers.Include(d => d.Manufacturer).FirstOrDefault(d => d.Id == dealerId);
            if (dealer == null) return BadRequest("Unknown dealer.");

            var rules = _rulesService.GetRulesFor(dealer.Manufacturer?.Code);

            var claim = new ClaimModel
            {
                ClaimNumber = GenerateClaimNumber(dealer.Name),
                DealerId = dealer.Id,
                ManufacturerCode = dealer.Manufacturer?.Code, // copied once, never reconciled again - see Models/Claim.cs
                CustomerName = req.CustomerName,
                CustomerPhone = req.CustomerPhone,
                CustomerEmail = req.CustomerEmail,
                Vin = req.Vin,
                VehicleModel = req.VehicleModel,
                VehicleYear = req.VehicleYear,
                VehicleMileageKm = req.VehicleMileageKm,
                VehicleColor = req.VehicleColor,
                ProblemDescription = req.ProblemDescription,
                LaborHours = req.LaborHours ?? 0,
                LaborRate = rules.DefaultLaborRate,
                Status = "draft",
                CreatedByUserId = CurrentUserId(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            ApplyParts(claim, req.Parts);

            _context.Claims.Add(claim);
            _context.SaveChanges();

            return Ok(claim);
        }

        [HttpPost("{id}/submit")]
        public IActionResult Submit(int id)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim == null) return NotFound();
            claim.Status = "submitted";
            claim.SubmittedAt = DateTime.UtcNow.ToString("yyyy-MM-dd");
            claim.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();
            return Ok(claim);
        }

        [HttpPost("{id}/approve")]
        public IActionResult Approve(int id)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim == null) return NotFound();
            claim.Status = "approved";
            claim.ApprovedAt = DateTime.UtcNow.ToString("yyyy-MM-dd");
            claim.ApprovedByName = User.FindFirst(ClaimTypes.Name)?.Value;
            claim.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();
            return Ok(claim);
        }

        [HttpPost("{id}/reject")]
        public IActionResult Reject(int id, [FromBody] RejectRequest req)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim == null) return NotFound();
            claim.Status = "rejected";
            claim.RejectionReason = req?.Reason; // not escaped/sanitized - rendered via ng-bind-html on the frontend too
            claim.ApprovedAt = DateTime.UtcNow.ToString("yyyy-MM-dd");
            claim.ApprovedByName = User.FindFirst(ClaimTypes.Name)?.Value;
            claim.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();
            return Ok(claim);
        }

        public class RejectRequest { public string Reason { get; set; } }

        [HttpPost("{id}/mark-invoiced")]
        public IActionResult MarkInvoiced(int id)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim == null) return NotFound();

            // SECOND copy of the total math, rounding subtotal/tax independently
            // instead of once at the end like CalculateGrandTotal() below - can differ
            // by a cent from what GetById() showed the dealer a moment earlier. A THIRD
            // copy lives in the PDF generator (see Legacy/InvoicePdfGenerator.cs), and a
            // FOURTH in the Angular frontend (ClaimDetailController.js).
            var partsTotal = Math.Round(PartsTotal(claim), 2);
            var laborTotal = Math.Round(LaborTotal(claim), 2);
            var subtotal = Math.Round(partsTotal + laborTotal, 2);
            var tax = Math.Round(subtotal * TaxRate, 2);
            var total = subtotal + tax;

            claim.Status = "invoiced";
            claim.InvoicedAt = DateTime.UtcNow.ToString("yyyy-MM-dd");
            claim.InvoiceTotal = total;
            claim.TaxRateUsed = "13% HST";
            claim.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return Ok(claim);
        }

        // Added later, when a dealer asked for "a way to attach some extra info we
        // didn't think of" without waiting for a schema change. Accepts arbitrary JSON
        // and deserializes it with TypeNameHandling.All so the "extra fields" can carry
        // typed objects - this is the textbook Newtonsoft.Json insecure deserialization
        // pattern (CWE-502): a crafted payload naming a gadget type in $type can lead to
        // remote code execution on deserialization, well beyond "just extra fields".
        [HttpPost("{id}/extra-fields")]
        public IActionResult ImportExtraFields(int id, [FromBody] object rawBody)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim == null) return NotFound();

            var json = rawBody?.ToString() ?? "{}";
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All // <- the dangerous part
            };
            var deserialized = JsonConvert.DeserializeObject(json, settings);

            claim.ExtraFieldsJson = JsonConvert.SerializeObject(deserialized);
            claim.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return Ok(claim);
        }

        [HttpGet("{id}/invoice-pdf")]
        public IActionResult InvoicePdf(int id)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim == null) return NotFound();
            var bytes = Legacy.InvoicePdfGenerator.Generate(claim); // THIRD copy of the total math - see that file
            return File(bytes, "application/pdf", $"invoice_{claim.ClaimNumber}.pdf");
        }

        // ---- helpers - the FIRST copy of the total math, used by GetById() above ----

        private decimal PartsTotal(ClaimModel c)
        {
            decimal total = 0;
            total += (c.Part1Qty ?? 0) * (c.Part1UnitPrice ?? 0);
            total += (c.Part2Qty ?? 0) * (c.Part2UnitPrice ?? 0);
            total += (c.Part3Qty ?? 0) * (c.Part3UnitPrice ?? 0);
            total += (c.Part4Qty ?? 0) * (c.Part4UnitPrice ?? 0);
            total += (c.Part5Qty ?? 0) * (c.Part5UnitPrice ?? 0);
            return total;
        }

        private decimal LaborTotal(ClaimModel c) => (c.LaborHours ?? 0) * (c.LaborRate ?? 0);

        private decimal CalculateGrandTotal(ClaimModel c)
        {
            var subtotal = PartsTotal(c) + LaborTotal(c);
            return subtotal + subtotal * TaxRate; // tax applied to the whole sum, NOT rounded per-step - differs from MarkInvoiced() above
        }

        private void ApplyParts(ClaimModel claim, List<PartLine> parts)
        {
            if (parts == null) return;
            var setters = new Action<PartLine>[]
            {
                p => { claim.Part1Name = p.Name; claim.Part1Number = p.Number; claim.Part1Qty = p.Qty; claim.Part1UnitPrice = p.UnitPrice; },
                p => { claim.Part2Name = p.Name; claim.Part2Number = p.Number; claim.Part2Qty = p.Qty; claim.Part2UnitPrice = p.UnitPrice; },
                p => { claim.Part3Name = p.Name; claim.Part3Number = p.Number; claim.Part3Qty = p.Qty; claim.Part3UnitPrice = p.UnitPrice; },
                p => { claim.Part4Name = p.Name; claim.Part4Number = p.Number; claim.Part4Qty = p.Qty; claim.Part4UnitPrice = p.UnitPrice; },
                p => { claim.Part5Name = p.Name; claim.Part5Number = p.Number; claim.Part5Qty = p.Qty; claim.Part5UnitPrice = p.UnitPrice; },
            };
            // max 5 - "6th part? put it in the notes field" is still the actual
            // guidance given to dealer staff, straight from the WinForms form's five
            // hardcoded "Part N" group boxes.
            for (int i = 0; i < parts.Count && i < 5; i++) setters[i](parts[i]);
        }

        private string GenerateClaimNumber(string dealerName)
        {
            var prefix = new string((dealerName ?? "DLR").Where(char.IsLetter).ToArray());
            prefix = prefix.Length >= 3 ? prefix.Substring(0, 3).ToUpperInvariant() : "DLR";
            return $"{prefix}-{DateTime.UtcNow:yyMMdd}-{new Random().Next(100, 999)}";
        }

        private int CurrentUserId()
        {
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id);
            return id;
        }
    }
}
