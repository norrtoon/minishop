using Microsoft.EntityFrameworkCore;
using MiniShop.Models;

namespace MiniShop.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Name).HasMaxLength(100).IsRequired();
            e.Property(u => u.Role).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.Name).IsUnique();
            e.Property(c => c.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.ImageUrl).HasMaxLength(500);
            e.Property(p => p.Price).HasConversion<double>();

            e.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.Address).HasMaxLength(300).IsRequired();
            e.Property(o => o.Comment).HasMaxLength(500);
            e.Property(o => o.Total).HasConversion<double>();
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(o => o.User)
             .WithMany(u => u.Orders)
             .HasForeignKey(o => o.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            e.Property(i => i.UnitPrice).HasConversion<double>();

            e.HasOne(i => i.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.Product)
             .WithMany()
             .HasForeignKey(i => i.ProductId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "Rating BETWEEN 1 AND 5"));
            e.Property(r => r.Text).HasMaxLength(1000);
            e.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();

            e.HasOne(r => r.Product)
             .WithMany(p => p.Reviews)
             .HasForeignKey(r => r.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
