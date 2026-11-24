using Microsoft.EntityFrameworkCore;

namespace DogWalkingApp.Api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Dog> Dogs { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderApplication> OrderApplications { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UserSession>().HasIndex(session => session.TokenHash).IsUnique();
        modelBuilder.Entity<OrderApplication>()
            .HasIndex(application => new { application.OrderId, application.WalkerId })
            .IsUnique();
        modelBuilder.Entity<Order>().Property(order => order.Area).HasMaxLength(120);
        modelBuilder.Entity<Order>().Property(order => order.LocationAddress).HasMaxLength(300);
        modelBuilder.Entity<Order>().Property(order => order.Description).HasMaxLength(2000);
    }
}
