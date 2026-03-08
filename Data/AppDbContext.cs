using Microsoft.EntityFrameworkCore;
using CompiaBackend.Models;

namespace CompiaBackend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>        Users        => Set<User>();
    public DbSet<Product>     Products     => Set<Product>();
    public DbSet<Order>       Orders       => Set<Order>();
    public DbSet<OrderItem>   OrderItems   => Set<OrderItem>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .Property(u => u.Role).HasDefaultValue("cliente");

        modelBuilder.Entity<Product>()
            .Property(p => p.Price).HasPrecision(10, 2);
        modelBuilder.Entity<Product>()
            .Property(p => p.OriginalPrice).HasPrecision(10, 2);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId);
        modelBuilder.Entity<Order>()
            .Property(o => o.Total).HasPrecision(10, 2);
        modelBuilder.Entity<Order>()
            .Property(o => o.ShippingPrice).HasPrecision(10, 2);

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId);
        modelBuilder.Entity<OrderItem>()
            .Property(i => i.UnitPrice).HasPrecision(10, 2);
    }
}