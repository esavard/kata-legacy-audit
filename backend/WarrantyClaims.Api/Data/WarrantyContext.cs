using Microsoft.EntityFrameworkCore;
using WarrantyClaims.Api.Models;

namespace WarrantyClaims.Api.Data
{
    // Maps onto a database created by db/schema.sql - there are no EF Core migrations
    // in this repo. Migrations were tried once during the Azure port and abandoned
    // after a schema drift incident; since then, every schema change has been "run this
    // ALTER TABLE by hand against prod, then eventually update schema.sql to match (eventually)."
    public class WarrantyContext : DbContext
    {
        public WarrantyContext(DbContextOptions<WarrantyContext> options) : base(options) { }

        public DbSet<Claim> Claims { get; set; }
        public DbSet<Dealer> Dealers { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Claim>().ToTable("claims");
            modelBuilder.Entity<Dealer>().ToTable("dealers");
            modelBuilder.Entity<Manufacturer>().ToTable("manufacturers");
            modelBuilder.Entity<User>().ToTable("users");

            modelBuilder.Entity<Claim>(e =>
            {
                e.Property(c => c.ClaimNumber).HasColumnName("claim_number");
                e.Property(c => c.DealerId).HasColumnName("dealer_id");
                e.Property(c => c.ManufacturerCode).HasColumnName("manufacturer_code");
                e.Property(c => c.CustomerName).HasColumnName("customer_name");
                e.Property(c => c.CustomerPhone).HasColumnName("customer_phone");
                e.Property(c => c.CustomerEmail).HasColumnName("customer_email");
                e.Property(c => c.Vin).HasColumnName("vin");
                e.Property(c => c.VehicleModel).HasColumnName("vehicle_model");
                e.Property(c => c.VehicleYear).HasColumnName("vehicle_year");
                e.Property(c => c.VehicleMileageKm).HasColumnName("vehicle_mileage_km");
                e.Property(c => c.VehicleColor).HasColumnName("vehicle_color");
                e.Property(c => c.ProblemDescription).HasColumnName("problem_description");

                e.Property(c => c.Part1Name).HasColumnName("part1_name"); e.Property(c => c.Part1Number).HasColumnName("part1_number"); e.Property(c => c.Part1Qty).HasColumnName("part1_qty"); e.Property(c => c.Part1UnitPrice).HasColumnName("part1_unit_price");
                e.Property(c => c.Part2Name).HasColumnName("part2_name"); e.Property(c => c.Part2Number).HasColumnName("part2_number"); e.Property(c => c.Part2Qty).HasColumnName("part2_qty"); e.Property(c => c.Part2UnitPrice).HasColumnName("part2_unit_price");
                e.Property(c => c.Part3Name).HasColumnName("part3_name"); e.Property(c => c.Part3Number).HasColumnName("part3_number"); e.Property(c => c.Part3Qty).HasColumnName("part3_qty"); e.Property(c => c.Part3UnitPrice).HasColumnName("part3_unit_price");
                e.Property(c => c.Part4Name).HasColumnName("part4_name"); e.Property(c => c.Part4Number).HasColumnName("part4_number"); e.Property(c => c.Part4Qty).HasColumnName("part4_qty"); e.Property(c => c.Part4UnitPrice).HasColumnName("part4_unit_price");
                e.Property(c => c.Part5Name).HasColumnName("part5_name"); e.Property(c => c.Part5Number).HasColumnName("part5_number"); e.Property(c => c.Part5Qty).HasColumnName("part5_qty"); e.Property(c => c.Part5UnitPrice).HasColumnName("part5_unit_price");

                e.Property(c => c.LaborHours).HasColumnName("labor_hours");
                e.Property(c => c.LaborRate).HasColumnName("labor_rate");
                e.Property(c => c.Status).HasColumnName("status");
                e.Property(c => c.RejectionReason).HasColumnName("rejection_reason");
                e.Property(c => c.ApprovedByName).HasColumnName("approved_by_name");
                e.Property(c => c.EstimatePdfPath).HasColumnName("estimate_pdf_path");
                e.Property(c => c.InvoicePdfPath).HasColumnName("invoice_pdf_path");
                e.Property(c => c.PhotoPath).HasColumnName("photo_path");
                e.Property(c => c.InvoiceTotal).HasColumnName("invoice_total");
                e.Property(c => c.TaxRateUsed).HasColumnName("tax_rate_used");
                e.Property(c => c.SubmittedAt).HasColumnName("submitted_at");
                e.Property(c => c.ApprovedAt).HasColumnName("approved_at");
                e.Property(c => c.CompletedAt).HasColumnName("completed_at");
                e.Property(c => c.InvoicedAt).HasColumnName("invoiced_at");
                e.Property(c => c.PaidAt).HasColumnName("paid_at");
                e.Property(c => c.InternalNotes).HasColumnName("internal_notes");
                e.Property(c => c.ExtraFieldsJson).HasColumnName("extra_fields_json");
                e.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
                e.Property(c => c.UpdatedByUserId).HasColumnName("updated_by_user_id");
                e.Property(c => c.IsDeleted).HasColumnName("is_deleted");
                e.Property(c => c.CreatedAt).HasColumnName("created_at");
                e.Property(c => c.UpdatedAt).HasColumnName("updated_at");

                e.HasOne(c => c.Dealer).WithMany().HasForeignKey(c => c.DealerId);
            });

            modelBuilder.Entity<Dealer>(e =>
            {
                e.Property(d => d.Name).HasColumnName("name");
                e.Property(d => d.Address).HasColumnName("address");
                e.Property(d => d.Phone).HasColumnName("phone");
                e.Property(d => d.ManufacturerId).HasColumnName("manufacturer_id");
                e.Property(d => d.CreatedAt).HasColumnName("created_at");
                e.HasOne(d => d.Manufacturer).WithMany().HasForeignKey(d => d.ManufacturerId);
            });

            modelBuilder.Entity<Manufacturer>(e =>
            {
                e.Property(m => m.Code).HasColumnName("code");
                e.Property(m => m.DisplayName).HasColumnName("display_name");
                e.Property(m => m.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<User>(e =>
            {
                e.Property(u => u.Username).HasColumnName("username");
                e.Property(u => u.PasswordHash).HasColumnName("password_hash");
                e.Property(u => u.Role).HasColumnName("role");
                e.Property(u => u.FullName).HasColumnName("full_name");
                e.Property(u => u.DealerId).HasColumnName("dealer_id");
                e.Property(u => u.CreatedAt).HasColumnName("created_at");
                e.HasOne(u => u.Dealer).WithMany().HasForeignKey(u => u.DealerId);
            });
        }
    }
}
