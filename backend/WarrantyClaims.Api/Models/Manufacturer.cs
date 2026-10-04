namespace WarrantyClaims.Api.Models
{
    // This table is the entire reason the UI has a manufacturer dropdown. It was added
    // when a second dealer group (not just Bramwell) was briefly a sales prospect, and
    // someone added the lookup table and the dropdown so the demo would "look" ready
    // for other manufacturers. The deal never closed. Kestrel/Solenne/Northgate rows exist and
    // the dropdown lets you pick them - but see ManufacturerRulesService: only "BRAM"
    // has real rules behind it. Everything else silently runs Bramwell's rules anyway.
    public class Manufacturer
    {
        public int Id { get; set; }
        public string Code { get; set; }          // 'BRAM', 'KESTREL', 'SOLENNE', 'NORTHGATE'
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }
    }
}
