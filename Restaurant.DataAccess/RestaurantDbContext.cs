using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Restaurant.Models;

namespace Restaurant.DataAccess;

public class RestaurantDbContext : IdentityDbContext<ApplicationUser>
{
    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<CategoryPrintStation> CategoryPrintStations { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<PrintStation> PrintStations { get; set; }
    public DbSet<Printer> Printers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Cashbox> Cashboxes { get; set; }
    public DbSet<CashDrawerEntry> CashDrawerEntries { get; set; }
    public DbSet<CashShift> CashShifts { get; set; }
    public DbSet<Addon> Addons { get; set; }
    public DbSet<RestaurantInfo> Restaurants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TransactionReceipt>().ToTable("__TransactionReceipts");
        modelBuilder.Entity<Product>().Property(p => p.IsAvailable).HasDefaultValue(true);
        modelBuilder.Entity<PaymentMethod>().Property(p => p.IsActive).HasDefaultValue(true);

        // Configure CategoryPrintStation composite key and relations
        modelBuilder.Entity<CategoryPrintStation>()
            .HasKey(cps => new { cps.CategoryId, cps.PrintStationId });

        modelBuilder.Entity<CategoryPrintStation>()
            .HasOne(cps => cps.Category)
            .WithMany(c => c.CategoryPrintStations)
            .HasForeignKey(cps => cps.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CategoryPrintStation>()
            .HasOne(cps => cps.PrintStation)
            .WithMany(ps => ps.CategoryPrintStations)
            .HasForeignKey(cps => cps.PrintStationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Order relationship to User
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderNumber)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.ClientRequestId)
            .IsUnique(false)
            .HasFilter("[ClientRequestId] IS NOT NULL");

        modelBuilder.Entity<Order>().HasIndex(o => o.Date);
        modelBuilder.Entity<CashDrawerEntry>().HasIndex(e => new { e.CashboxId, e.Id });
        modelBuilder.Entity<CashDrawerEntry>().HasIndex(e => new { e.Date, e.EntryType });
        modelBuilder.Entity<Order>().HasIndex(o => new { o.UserId, o.ClientRequestId }).IsUnique()
            .HasFilter("[ClientRequestId] IS NOT NULL");

        // Configure CashDrawerEntry relationships
        modelBuilder.Entity<CashDrawerEntry>()
            .HasIndex(e => new { e.UserId, e.ClientRequestId }).IsUnique()
            .HasFilter("[ClientRequestId] IS NOT NULL");
        modelBuilder.Entity<CashDrawerEntry>()
            .HasOne(e => e.CashShift).WithMany(s => s.Entries)
            .HasForeignKey(e => e.CashShiftId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashShift>()
            .HasIndex(s => new { s.UserId, s.ClientRequestId }).IsUnique();
        modelBuilder.Entity<CashShift>()
            .HasIndex(s => new { s.SourceCashboxId, s.PreviousShiftId }).IsUnique();
        modelBuilder.Entity<CashShift>().HasOne(s => s.SourceCashbox).WithMany()
            .HasForeignKey(s => s.SourceCashboxId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashShift>().HasOne(s => s.TargetCashbox).WithMany()
            .HasForeignKey(s => s.TargetCashboxId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CashShift>().HasOne(s => s.User).WithMany()
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CashDrawerEntry>()
            .HasOne(cde => cde.Cashbox)
            .WithMany(cb => cb.CashDrawerEntries)
            .HasForeignKey(cde => cde.CashboxId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CashDrawerEntry>()
            .HasOne(cde => cde.PaymentMethod)
            .WithMany(pm => pm.CashDrawerEntries)
            .HasForeignKey(cde => cde.PaymentMethodId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CashDrawerEntry>()
            .HasOne(cde => cde.Order)
            .WithMany(o => o.CashDrawerEntries)
            .HasForeignKey(cde => cde.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CashDrawerEntry>()
            .HasOne(cde => cde.User)
            .WithMany()
            .HasForeignKey(cde => cde.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure OrderItem relationships
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent multiple cascade paths in SQL Server for OrderItem -> Product
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Printer -> PrintStation
        modelBuilder.Entity<Printer>()
            .HasIndex(p => new { p.PrintStationId, p.PrinterType, p.PrinterName })
            .IsUnique();
        modelBuilder.Entity<Printer>()
            .HasOne(p => p.PrintStation)
            .WithMany(ps => ps.Printers)
            .HasForeignKey(p => p.PrintStationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Product -> Category
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Restaurant -> User relationship
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Restaurant)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RestaurantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
