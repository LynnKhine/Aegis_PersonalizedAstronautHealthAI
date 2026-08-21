using Aegis.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Data;

public class AegisDbContext : DbContext
{
    public AegisDbContext(DbContextOptions<AegisDbContext> options) : base(options) { }

    public DbSet<Astronaut> Astronauts => Set<Astronaut>();
    public DbSet<BiometricReading> BiometricReadings => Set<BiometricReading>();
    public DbSet<PersonalBaseline> PersonalBaselines => Set<PersonalBaseline>();
    public DbSet<InterventionPlan> InterventionPlans => Set<InterventionPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Astronaut
        modelBuilder.Entity<Astronaut>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).IsRequired().HasMaxLength(200);
            e.Property(a => a.NASAId).IsRequired().HasMaxLength(50);
            e.HasIndex(a => a.NASAId).IsUnique();
        });

        // BiometricReading
        modelBuilder.Entity<BiometricReading>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.MetricType).HasConversion<string>();
            e.Property(r => r.Severity).HasConversion<string>();
            e.HasIndex(r => new { r.AstronautId, r.MetricType });
            e.HasIndex(r => r.RecordedAt);
            e.HasOne(r => r.Astronaut)
             .WithMany(a => a.BiometricReadings)
             .HasForeignKey(r => r.AstronautId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PersonalBaseline — one row per astronaut+metric pair
        modelBuilder.Entity<PersonalBaseline>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.MetricType).HasConversion<string>();
            e.HasIndex(b => new { b.AstronautId, b.MetricType }).IsUnique();
            e.HasOne(b => b.Astronaut)
             .WithMany(a => a.PersonalBaselines)
             .HasForeignKey(b => b.AstronautId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // InterventionPlan
        modelBuilder.Entity<InterventionPlan>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Summary).IsRequired();
            e.Property(p => p.ImmediateActionsJson).IsRequired();
            e.HasIndex(p => p.AstronautId);
            e.HasOne(p => p.Astronaut)
             .WithMany(a => a.InterventionPlans)
             .HasForeignKey(p => p.AstronautId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.TriggeredByReading)
             .WithOne()
             .HasForeignKey<InterventionPlan>(p => p.TriggeredByReadingId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
