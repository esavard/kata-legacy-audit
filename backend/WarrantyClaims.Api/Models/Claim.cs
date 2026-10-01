using System;

namespace WarrantyClaims.Api.Models
{
    // THE table. Dealer got its own row in the schema because the app genuinely needed
    // to support several real dealerships - but Vehicle, Customer, the parts list, the
    // invoice and the approval are all still flattened into this one entity, exactly
    // like the Excel sheet ("Warranty Tracking.xlsx") this was modeled on back when it
    // was still a WinForms app talking to a local Access/SQL Server file. Porting it to
    // Postgres/EF Core in 2019 kept the exact same shape - "why would we redesign the
    // database, the app already works."
    public class Claim
    {
        public int Id { get; set; }
        public string ClaimNumber { get; set; }

        public int DealerId { get; set; }
        public Dealer Dealer { get; set; }

        // manufacturer is ALSO denormalized onto the claim (copied from the dealer's
        // manufacturer at creation time) instead of just following Dealer.ManufacturerId -
        // a leftover from the WinForms version where this was a plain string typed by
        // hand on the form. Nobody reconciles the two if a dealer's manufacturer changes
        // (which has never happened, so nobody's noticed the duplication is pointless).
        public string ManufacturerCode { get; set; }

        // customer - never logs in, has no table of its own
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        // vehicle
        public string Vin { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleYear { get; set; }       // text, not int - see dirty data notes in kata-solution
        public string VehicleMileageKm { get; set; }
        public string VehicleColor { get; set; }

        public string ProblemDescription { get; set; }  // rendered via ng-bind-html on the frontend - see ClaimDetailController.js

        // parts - 5 repeating slots, ported verbatim from the WinForms form's five
        // hardcoded "Part N" group boxes. "6th part? put it in the notes" is still the
        // actual guidance given to dealer staff.
        public string Part1Name { get; set; } public string Part1Number { get; set; } public int? Part1Qty { get; set; } public decimal? Part1UnitPrice { get; set; }
        public string Part2Name { get; set; } public string Part2Number { get; set; } public int? Part2Qty { get; set; } public decimal? Part2UnitPrice { get; set; }
        public string Part3Name { get; set; } public string Part3Number { get; set; } public int? Part3Qty { get; set; } public decimal? Part3UnitPrice { get; set; }
        public string Part4Name { get; set; } public string Part4Number { get; set; } public int? Part4Qty { get; set; } public decimal? Part4UnitPrice { get; set; }
        public string Part5Name { get; set; } public string Part5Number { get; set; } public int? Part5Qty { get; set; } public decimal? Part5UnitPrice { get; set; }

        public decimal? LaborHours { get; set; }
        public decimal? LaborRate { get; set; }

        public string Status { get; set; }             // free text state machine - see kata-solution for the full (undocumented) list, including the 'aproved' typo rows
        public string RejectionReason { get; set; }
        public string ApprovedByName { get; set; }      // free text, not a user id FK

        public string EstimatePdfPath { get; set; }
        public string InvoicePdfPath { get; set; }
        public string PhotoPath { get; set; }

        public decimal? InvoiceTotal { get; set; }
        public string TaxRateUsed { get; set; }

        // dates as free text - carried over unchanged from the WinForms app's text-field
        // date pickers, which didn't enforce a format consistently across Windows
        // locales. fmt_date_any()-equivalent parsing pain lives in ClaimsController now.
        public string SubmittedAt { get; set; }
        public string ApprovedAt { get; set; }
        public string CompletedAt { get; set; }
        public string InvoicedAt { get; set; }
        public string PaidAt { get; set; }

        // ad hoc "extra fields" bag - free text, semicolon-separated key:value pairs by
        // convention (nothing enforces the convention). See
        // ClaimsController.ImportExtraFields() for the (dangerous) structured version of
        // this that was added later and accepts arbitrary JSON instead.
        public string InternalNotes { get; set; }
        public string ExtraFieldsJson { get; set; }

        public int CreatedByUserId { get; set; }
        public int? UpdatedByUserId { get; set; }
        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
