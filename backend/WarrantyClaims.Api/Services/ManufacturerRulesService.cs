namespace WarrantyClaims.Api.Services
{
    // This class is the entire "multi-manufacturer" story, and it's fake. The dropdown
    // in the Angular app and the Manufacturers table (see Models/Manufacturer.cs) both
    // suggest Kestrel/Solenne/Northgate are real options alongside Bramwell. They're not: only BRAM
    // has real tax/labor-rate rules below. Every other code falls through to the
    // default branch, which is a verbatim copy of Bramwell's numbers with a comment saying
    // "placeholder, fix before onboarding a real non-Bramwell dealer" - dated 2019.
    //
    // Nobody has hit this yet because every real dealer in production today is a Bramwell
    // dealer. The day a Kestrel or Solenne dealer actually onboards, their claims will be
    // silently taxed and rated using Bramwell's numbers, with no error, no warning, nothing
    // in a log - just quietly wrong invoices.
    public class ManufacturerRulesService
    {
        public ManufacturerRules GetRulesFor(string manufacturerCode)
        {
            switch ((manufacturerCode ?? "").ToUpperInvariant())
            {
                case "BRAM":
                    return new ManufacturerRules
                    {
                        DefaultLaborRate = 110.00m,
                        ReimbursementSlaDays = 30,
                        RequiresDealerRepEmail = true
                    };

                case "KESTREL":
                case "SOLENNE":
                case "NORTHGATE":
                default:
                    // TODO (2019, mp): placeholder, fix before onboarding a real
                    // non-Bramwell dealer. using Bramwell's numbers for now so the demo works.
                    return new ManufacturerRules
                    {
                        DefaultLaborRate = 110.00m,
                        ReimbursementSlaDays = 30,
                        RequiresDealerRepEmail = true
                    };
            }
        }
    }

    public class ManufacturerRules
    {
        public decimal DefaultLaborRate { get; set; }
        public int ReimbursementSlaDays { get; set; }
        public bool RequiresDealerRepEmail { get; set; }
    }
}
