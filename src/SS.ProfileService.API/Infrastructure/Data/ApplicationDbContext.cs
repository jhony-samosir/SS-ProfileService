using Microsoft.EntityFrameworkCore;
using SS.ProfileService.API.Domain.Entities;

namespace SS.ProfileService.API.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles => Set<Profile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("Profiles");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.PublicId)
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50);

            entity.Property(e => e.Address)
                .HasMaxLength(500);

            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(1000);

            entity.HasIndex(e => e.UserId)
                .IsUnique();
        });
    }
}
