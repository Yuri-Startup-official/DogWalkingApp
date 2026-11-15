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
    public DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UserSession>().HasIndex(session => session.TokenHash).IsUnique();
    }
}