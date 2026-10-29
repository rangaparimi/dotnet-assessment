using AssetManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AssetAllocation> AssetAllocations => Set<AssetAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Assets
        modelBuilder.Entity<Asset>(e => 
        //building the structure for the asset table and providing constraints for the fields
        {
            e.HasIndex(a => a.AssetTag).IsUnique();
            e.Property(a => a.AssetTag).HasMaxLength(50).IsRequired();
            e.Property(a => a.Name).HasMaxLength(100).IsRequired();
            e.Property(a => a.Category).HasMaxLength(50).IsRequired();
            e.Property(a => a.SerialNumber).HasMaxLength(100);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        });

        // Employees
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.EmployeeCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.Property(x => x.Department).HasMaxLength(50).IsRequired();
        });

        // AssetAllocations (used for both allocation and return)
        modelBuilder.Entity<AssetAllocation>(e =>
        {
            e.Property(x => x.ReturnCondition).HasMaxLength(20);
            e.Property(x => x.Remarks).HasMaxLength(500);

            // Safety net: an asset can have only ONE open allocation (ReturnedDate IS NULL).
            // Stops two simultaneous requests from allocating the same asset twice.
            e.HasIndex(x => x.AssetId, "IX_AssetAllocations_AssetId_Open")
             .IsUnique()
             .HasFilter("[ReturnedDate] IS NULL");

            // Normal index for looking up an asset's full history
            e.HasIndex(x => x.AssetId, "IX_AssetAllocations_AssetId");

            // Restrict: deleting an asset/employee must never wipe allocation history
            e.HasOne(x => x.Asset)
             .WithMany(a => a.Allocations)
             .HasForeignKey(x => x.AssetId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Employee)
             .WithMany(emp => emp.Allocations)
             .HasForeignKey(x => x.EmployeeId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
