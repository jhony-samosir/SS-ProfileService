using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Domain.Entities;

namespace SS.ProfileService.API.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<InboxEvent> InboxEvents => Set<InboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserProfile Configuration
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            
            entity.Property(e => e.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.PublicId).IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.UserPublicId).HasColumnName("user_public_id").IsRequired();
            entity.HasIndex(e => e.UserPublicId).IsUnique();

            entity.Property(e => e.FullName).HasColumnName("full_name").IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(50);
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(1000);
            entity.Property(e => e.Bio).HasColumnName("bio").HasMaxLength(500);
            entity.Property(e => e.Gender).HasColumnName("gender").HasMaxLength(20);
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");

            // Audit
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).HasDefaultValue("System");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(100);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        // UserAddress Configuration
        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.ToTable("user_addresses");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();

            entity.Property(e => e.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.PublicId).IsUnique();

            entity.Property(e => e.UserProfileId).HasColumnName("user_profile_id").IsRequired();
            entity.Property(e => e.AddressLabel).HasColumnName("address_label").HasMaxLength(100).HasDefaultValue("Home");
            entity.Property(e => e.ReceiverName).HasColumnName("receiver_name").IsRequired().HasMaxLength(255);
            entity.Property(e => e.ReceiverPhone).HasColumnName("receiver_phone").IsRequired().HasMaxLength(50);
            entity.Property(e => e.StreetAddress).HasColumnName("street_address").IsRequired().HasMaxLength(500);
            entity.Property(e => e.City).HasColumnName("city").IsRequired().HasMaxLength(100);
            entity.Property(e => e.StateProvince).HasColumnName("state_province").IsRequired().HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasColumnName("postal_code").IsRequired().HasMaxLength(20);
            entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100).HasDefaultValue("Indonesia");
            
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(9, 6);
            entity.Property(e => e.IsDefault).HasColumnName("is_default").HasDefaultValue(false);

            // Audit
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).HasDefaultValue("System");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(100);

            entity.HasOne(e => e.UserProfile)
                .WithMany(p => p.Addresses)
                .HasForeignKey(e => e.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(e => e.DeletedAt == null);

            // Partial unique index constraint: only one default address per active profile
            entity.HasIndex(e => new { e.UserProfileId })
                .HasDatabaseName("uq_user_addresses_default_idx")
                .IsUnique()
                .HasFilter("is_default = true AND deleted_at IS NULL");
        });

        // OutboxEvent Configuration
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("outbox_events");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            
            entity.Property(e => e.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.HasIndex(e => e.PublicId).IsUnique();

            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(255);
            entity.Property(e => e.AggregateType).HasColumnName("aggregate_type").IsRequired().HasMaxLength(100);
            entity.Property(e => e.AggregateId).HasColumnName("aggregate_id").IsRequired().HasMaxLength(100);
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("PENDING");
            entity.Property(e => e.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");

            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("idx_outbox_events_status_created")
                .HasFilter("status = 'PENDING'");
        });

        // InboxEvent Configuration
        modelBuilder.Entity<InboxEvent>(entity =>
        {
            entity.ToTable("inbox_events");
            
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.MessageId).HasColumnName("message_id").HasMaxLength(255);
            entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired().HasMaxLength(255);
            entity.Property(e => e.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100);
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("PROCESSED");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
        });
    }
}
