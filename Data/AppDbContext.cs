using DogWalkingApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogWalkingApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<WalkingOrder> WalkingOrders => Set<WalkingOrder>();
    public DbSet<BoardingOrder> BoardingOrders => Set<BoardingOrder>();
    public DbSet<OrderApplication> OrderApplications => Set<OrderApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Участники сделки
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PublicId).HasColumnName("public_id");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.AvatarUrl).HasColumnName("avatar_url");
            e.Property(x => x.Role).HasColumnName("role");
            e.Property(x => x.EmailVerified).HasColumnName("email_verified");
            e.Property(x => x.PhoneVerified).HasColumnName("phone_verified");
            e.Property(x => x.RatingAsSitter).HasColumnName("rating_as_sitter");
            e.Property(x => x.ReviewsAsSitter).HasColumnName("reviews_as_sitter");
            e.Property(x => x.RatingAsOwner).HasColumnName("rating_as_owner");
            e.Property(x => x.ReviewsAsOwner).HasColumnName("reviews_as_owner");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.PublicId).IsUnique();
        });

        // Питомцы
        modelBuilder.Entity<Pet>(e =>
        {
            e.ToTable("pets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PublicId).HasColumnName("public_id");
            e.Property(x => x.OwnerId).HasColumnName("owner_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Species).HasColumnName("species");
            e.Property(x => x.Breed).HasColumnName("breed");
            e.Property(x => x.BirthDate).HasColumnName("birth_date");
            e.Property(x => x.WeightKg).HasColumnName("weight_kg");
            e.Property(x => x.Sex).HasColumnName("sex");
            e.Property(x => x.Sterilized).HasColumnName("sterilized");
            e.Property(x => x.PhotoUrl).HasColumnName("photo_url");
            e.Property(x => x.MedicalNotes).HasColumnName("medical_notes");
            e.Property(x => x.Medications).HasColumnName("medications");
            e.Property(x => x.Vaccinated).HasColumnName("vaccinated");
            e.Property(x => x.VaccinationExpires).HasColumnName("vaccination_expires");
            e.Property(x => x.BehaviorNotes).HasColumnName("behavior_notes");
            e.Property(x => x.AggressionLevel).HasColumnName("aggression_level");
            e.Property(x => x.SpecialNeeds).HasColumnName("special_needs");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            e.HasIndex(x => x.PublicId).IsUnique();

            e.HasOne(x => x.Owner)
             .WithMany()
             .HasForeignKey(x => x.OwnerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Заказы
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PublicId).HasColumnName("public_id");
            e.Property(x => x.ServiceType).HasColumnName("service_type");
            e.Property(x => x.OwnerId).HasColumnName("owner_id");
            e.Property(x => x.SitterId).HasColumnName("sitter_id");
            e.Property(x => x.PetId).HasColumnName("pet_id");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.Area).HasColumnName("area").HasMaxLength(120);
            e.Property(x => x.LocationAddress).HasColumnName("location_address").HasMaxLength(300);
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
            e.Property(x => x.ApproximateLatitude).HasColumnName("approximate_latitude");
            e.Property(x => x.ApproximateLongitude).HasColumnName("approximate_longitude");
            e.Property(x => x.ReportText).HasColumnName("report_text").HasMaxLength(2000);
            e.Property(x => x.TotalPrice).HasColumnName("total_price");
            e.Property(x => x.PlatformFee).HasColumnName("platform_fee");
            e.Property(x => x.PaymentIntentId).HasColumnName("payment_intent_id");
            e.Property(x => x.PaymentStatus).HasColumnName("payment_status");
            e.Property(x => x.PaidAt).HasColumnName("paid_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
            e.Property(x => x.CancelReason).HasColumnName("cancel_reason");
            e.HasIndex(x => x.PublicId).IsUnique();

            e.HasOne(x => x.Owner)
             .WithMany()
             .HasForeignKey(x => x.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Sitter)
             .WithMany()
             .HasForeignKey(x => x.SitterId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Pet)
             .WithMany()
             .HasForeignKey(x => x.PetId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Прогулка
        modelBuilder.Entity<WalkingOrder>(e =>
        {
            e.ToTable("walking_orders");
            e.HasKey(x => x.OrderId);
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.StartTime).HasColumnName("start_time");
            e.Property(x => x.DurationMin).HasColumnName("duration_min");
            e.Property(x => x.MeetingPoint).HasColumnName("meeting_point");
            e.Property(x => x.PetsCount).HasColumnName("pets_count");
            e.Property(x => x.StartedAt).HasColumnName("started_at");
            e.Property(x => x.FinishedAt).HasColumnName("finished_at");

            e.HasOne(x => x.Order)
             .WithOne(x => x.WalkingDetails)
             .HasForeignKey<WalkingOrder>(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Передержка
        modelBuilder.Entity<BoardingOrder>(e =>
        {
            e.ToTable("boarding_orders");
            e.HasKey(x => x.OrderId);
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.CheckInAt).HasColumnName("check_in_at");
            e.Property(x => x.CheckOutAt).HasColumnName("check_out_at");
            e.Property(x => x.PetsCount).HasColumnName("pets_count");
            e.Property(x => x.ActualCheckIn).HasColumnName("actual_check_in");
            e.Property(x => x.ActualCheckOut).HasColumnName("actual_check_out");

            e.HasOne(x => x.Order)
             .WithOne(x => x.BoardingDetails)
             .HasForeignKey<BoardingOrder>(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderApplication>(e =>
        {
            e.ToTable("order_applications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.SitterId).HasColumnName("sitter_id");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => new { x.OrderId, x.SitterId }).IsUnique();
            e.HasIndex(x => x.SitterId);
            e.HasOne(x => x.Order)
             .WithMany(x => x.Applications)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Sitter)
             .WithMany()
             .HasForeignKey(x => x.SitterId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
