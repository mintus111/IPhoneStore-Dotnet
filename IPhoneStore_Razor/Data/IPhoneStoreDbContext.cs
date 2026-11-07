using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IPhoneStore_Razor.Models.Entities;

namespace IPhoneStore_Razor.Data;

public class IPhoneStoreDbContext : IdentityDbContext<ApplicationUser>
{
    public IPhoneStoreDbContext(DbContextOptions<IPhoneStoreDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; } = null!;
    public virtual DbSet<Customer> Customers { get; set; } = null!;
    public virtual DbSet<Product> Products { get; set; } = null!;
    public virtual DbSet<Order> Orders { get; set; } = null!;
    public virtual DbSet<OrderDetail> OrderDetails { get; set; } = null!;

    // Các DbSet nghiệp vụ mới chuẩn V2.0
    public virtual DbSet<ProductImei> ProductImeis { get; set; } = null!;
    public virtual DbSet<Warranty> Warranties { get; set; } = null!;
    public virtual DbSet<WarrantyClaim> WarrantyClaims { get; set; } = null!;
    public virtual DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // One-to-one or one-to-optional: ApplicationUser -> Customer
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.User)
            .WithOne(u => u.Customer)
            .HasForeignKey<Customer>(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Product -> Category (Cascade delete)
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Order -> Customer (Cascade delete)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderDetail -> Order (Cascade delete)
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderDetail -> Product (Restrict delete)
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Product)
            .WithMany(p => p.OrderDetails)
            .HasForeignKey(od => od.ProductId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // OrderDetail -> ProductImei (Optional)
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.ProductImei)
            .WithMany()
            .HasForeignKey(od => od.ProductImeiId)
            .OnDelete(DeleteBehavior.SetNull);

        // ProductImei: Unique IMEI
        modelBuilder.Entity<ProductImei>()
            .HasIndex(pi => pi.Imei)
            .IsUnique();

        // ProductImei -> Product (Cascade delete)
        modelBuilder.Entity<ProductImei>()
            .HasOne(pi => pi.Product)
            .WithMany(p => p.ProductImeis)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductImei -> Order (Optional)
        modelBuilder.Entity<ProductImei>()
            .HasOne(pi => pi.Order)
            .WithMany(o => o.ProductImeis)
            .HasForeignKey(pi => pi.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // Warranty -> Customer
        modelBuilder.Entity<Warranty>()
            .HasOne(w => w.Customer)
            .WithMany(c => c.Warranties)
            .HasForeignKey(w => w.CustomerId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // Warranty -> Product
        modelBuilder.Entity<Warranty>()
            .HasOne(w => w.Product)
            .WithMany(p => p.Warranties)
            .HasForeignKey(w => w.ProductId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // Warranty -> OrderDetail
        modelBuilder.Entity<Warranty>()
            .HasOne(w => w.OrderDetail)
            .WithMany()
            .HasForeignKey(w => w.OrderDetailId)
            .OnDelete(DeleteBehavior.SetNull);

        // WarrantyClaim -> Warranty (Cascade delete)
        modelBuilder.Entity<WarrantyClaim>()
            .HasOne(wc => wc.Warranty)
            .WithMany(w => w.WarrantyClaims)
            .HasForeignKey(wc => wc.WarrantyId)
            .OnDelete(DeleteBehavior.Cascade);

        // AuditLog -> ApplicationUser
        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
