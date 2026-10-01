using System;

namespace WarrantyClaims.Api.Models
{
    // Dealer is a real, normalized table - the system genuinely supports several Ford
    // dealerships today, so this part of the "multi-tenant" story is true. What's NOT
    // true yet is manufacturer independence - see Manufacturer.cs and
    // ManufacturerRulesService for where the "multi-manufacturer" story is cosmetic.
    public class Dealer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }

        // FK to Manufacturer exists (added when the dropdown was built), but almost
        // nothing downstream actually branches on it correctly - see
        // ManufacturerRulesService.GetRulesFor().
        public int ManufacturerId { get; set; }
        public Manufacturer Manufacturer { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
