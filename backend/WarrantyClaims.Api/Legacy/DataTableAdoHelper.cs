using System;
using System.Data;
using System.Text;
using Npgsql;

namespace WarrantyClaims.Api.Legacy
{
    // Leftover from the WinForms days, when every data access was a DataTable filled
    // by a DataAdapter. When the app was ported to the web, most of this got replaced
    // by EF Core - except the CSV export feature, which "already worked" so nobody
    // rewrote it. It's the one place in the whole backend still using raw ADO.NET
    // DataTables, and it's a good illustration of what "half-migrated" actually looks
    // like in a real codebase: two completely different data access styles, coexisting,
    // years apart.
    public static class DataTableAdoHelper
    {
        public static string ExportClaimsToCsv(string connectionString, int dealerId)
        {
            var dt = new DataTable();
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                // parameterized, at least - this one code path got a security review
                // once, in 2021, after a different incident. nothing else in the
                // codebase got the same treatment.
                using var cmd = new NpgsqlCommand(
                    "SELECT claim_number, customer_name, vin, status, invoice_total FROM claims WHERE dealer_id = @dealerId AND is_deleted = false",
                    conn);
                cmd.Parameters.AddWithValue("dealerId", dealerId);
                using var adapter = new NpgsqlDataAdapter(cmd);
                adapter.Fill(dt);
            }

            var sb = new StringBuilder();
            sb.AppendLine("ClaimNumber,CustomerName,VIN,Status,InvoiceTotal");
            foreach (DataRow row in dt.Rows)
            {
                // no CSV-escaping of values containing commas/quotes - a customer name
                // like `Smith, "The Boss", Jr.` will shift columns when opened in Excel.
                sb.AppendLine($"{row["claim_number"]},{row["customer_name"]},{row["vin"]},{row["status"]},{row["invoice_total"]}");
            }
            return sb.ToString();
        }
    }
}
