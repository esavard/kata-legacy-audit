using System;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ClaimModel = WarrantyClaims.Api.Models.Claim;

namespace WarrantyClaims.Api.Legacy
{
    // iTextSharp (pre-iText7) - see the csproj comment for why this is still here.
    // Written in the old Document/PdfWriter style that was standard advice circa 2015-
    // 2018, and never revisited. This is the THIRD copy of the total/tax math (see
    // ClaimsController for the other two, and ClaimDetailController.js on the frontend
    // for the fourth) - it prefers claim.InvoiceTotal if one was already stored instead
    // of recomputing, so an old claim can show a "wrong" total on its PDF forever if an
    // earlier calculation bug ever wrote a bad value for it.
    public static class InvoicePdfGenerator
    {
        private const decimal TaxRate = 0.13m;

        public static byte[] Generate(ClaimModel claim)
        {
            using var ms = new MemoryStream();
            var doc = new Document(PageSize.LETTER, 36, 36, 36, 36);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            doc.Add(new Paragraph("Warranty Repair Invoice (Bill to: Manufacturer Warranty Dept.)"));
            doc.Add(new Paragraph($"Claim #: {claim.ClaimNumber}"));
            doc.Add(new Paragraph($"Vehicle: {claim.VehicleModel} {claim.VehicleYear} - VIN {claim.Vin}"));
            doc.Add(Chunk.NEWLINE);

            var table = new PdfPTable(4);
            table.AddCell("Part"); table.AddCell("Part #"); table.AddCell("Qty"); table.AddCell("Unit Price");
            AddPartRow(table, claim.Part1Name, claim.Part1Number, claim.Part1Qty, claim.Part1UnitPrice);
            AddPartRow(table, claim.Part2Name, claim.Part2Number, claim.Part2Qty, claim.Part2UnitPrice);
            AddPartRow(table, claim.Part3Name, claim.Part3Number, claim.Part3Qty, claim.Part3UnitPrice);
            AddPartRow(table, claim.Part4Name, claim.Part4Number, claim.Part4Qty, claim.Part4UnitPrice);
            AddPartRow(table, claim.Part5Name, claim.Part5Number, claim.Part5Qty, claim.Part5UnitPrice);
            doc.Add(table);

            doc.Add(Chunk.NEWLINE);
            doc.Add(new Paragraph($"Labor: {claim.LaborHours ?? 0} hrs @ ${claim.LaborRate ?? 0}/hr"));

            // prefers the stored total over recomputing - see comment above
            decimal total;
            if (claim.InvoiceTotal.HasValue)
            {
                total = claim.InvoiceTotal.Value;
            }
            else
            {
                var partsTotal = (claim.Part1Qty ?? 0) * (claim.Part1UnitPrice ?? 0)
                               + (claim.Part2Qty ?? 0) * (claim.Part2UnitPrice ?? 0)
                               + (claim.Part3Qty ?? 0) * (claim.Part3UnitPrice ?? 0)
                               + (claim.Part4Qty ?? 0) * (claim.Part4UnitPrice ?? 0)
                               + (claim.Part5Qty ?? 0) * (claim.Part5UnitPrice ?? 0);
                var laborTotal = (claim.LaborHours ?? 0) * (claim.LaborRate ?? 0);
                var subtotal = partsTotal + laborTotal;
                total = subtotal + subtotal * TaxRate;
            }

            doc.Add(new Paragraph($"TOTAL DUE FROM MANUFACTURER: ${total:F2}") { SpacingBefore = 8 });

            doc.Close();
            return ms.ToArray();
        }

        private static void AddPartRow(PdfPTable table, string name, string number, int? qty, decimal? price)
        {
            if (string.IsNullOrEmpty(name)) return;
            table.AddCell(name ?? "");
            table.AddCell(number ?? "");
            table.AddCell((qty ?? 0).ToString());
            table.AddCell($"${price ?? 0:F2}");
        }
    }
}
