using Microsoft.EntityFrameworkCore;

namespace InnerSky.WebApi;

public sealed class InnerSkyDbContext(DbContextOptions<InnerSkyDbContext> options) : DbContext(options)
{
    public DbSet<EmotionProfileEntity> EmotionProfiles => Set<EmotionProfileEntity>();
    public DbSet<EmotionProfileComponentEntity> EmotionProfileComponents => Set<EmotionProfileComponentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmotionProfileEntity>(entity =>
        {
            entity.ToTable("EmotionProfiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedUtc).HasColumnType("datetime2(0)").IsRequired();
            entity.HasMany(x => x.Components)
                .WithOne(x => x.Profile)
                .HasForeignKey(x => x.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmotionProfileComponentEntity>(entity =>
        {
            entity.ToTable("EmotionProfileComponents", table =>
                table.HasCheckConstraint("CK_EmotionProfileComponents_IntensityLevel", "[IntensityLevel] BETWEEN 0 AND 2"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityColumn();
            entity.Property(x => x.EmotionId).HasMaxLength(32).IsRequired();
            entity.Property(x => x.IntensityLevel).IsRequired();
        });
    }
}
