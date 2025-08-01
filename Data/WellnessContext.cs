using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WellnessPlatform.Models;

namespace WellnessPlatform.Data
{
    public class WellnessContext : IdentityDbContext
    {
        public WellnessContext(DbContextOptions<WellnessContext> options) : base(options)
        {
        }
        
        public DbSet<HealthProfile> HealthProfiles { get; set; }
        public DbSet<BiomarkerResult> BiomarkerResults { get; set; }
        public DbSet<PromisResult> PromisResults { get; set; }
        public DbSet<Treatment> Treatments { get; set; }
        public DbSet<UserTreatment> UserTreatments { get; set; }
        public DbSet<LifestyleMetric> LifestyleMetrics { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Configure decimal precision for biomarker values
            builder.Entity<BiomarkerResult>()
                .Property(b => b.Value)
                .HasPrecision(10, 3);
                
            builder.Entity<BiomarkerResult>()
                .Property(b => b.ReferenceRangeMin)
                .HasPrecision(10, 3);
                
            builder.Entity<BiomarkerResult>()
                .Property(b => b.ReferenceRangeMax)
                .HasPrecision(10, 3);
                
            builder.Entity<BiomarkerResult>()
                .Property(b => b.OptimalRangeMin)
                .HasPrecision(10, 3);
                
            builder.Entity<BiomarkerResult>()
                .Property(b => b.OptimalRangeMax)
                .HasPrecision(10, 3);
            
            // Configure decimal precision for PROMIS scores
            builder.Entity<PromisResult>()
                .Property(p => p.TScore)
                .HasPrecision(5, 2);
                
            builder.Entity<PromisResult>()
                .Property(p => p.PercentileRank)
                .HasPrecision(5, 2);
            
            // Configure decimal precision for lifestyle metrics
            builder.Entity<LifestyleMetric>()
                .Property(l => l.SleepHours)
                .HasPrecision(3, 1);
                
            builder.Entity<LifestyleMetric>()
                .Property(l => l.Weight)
                .HasPrecision(5, 1);
                
            builder.Entity<LifestyleMetric>()
                .Property(l => l.BodyFatPercentage)
                .HasPrecision(4, 1);
            
            // Configure relationships
            builder.Entity<BiomarkerResult>()
                .HasOne(b => b.HealthProfile)
                .WithMany(h => h.BiomarkerResults)
                .HasForeignKey(b => b.HealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<PromisResult>()
                .HasOne(p => p.HealthProfile)
                .WithMany(h => h.PromisResults)
                .HasForeignKey(p => p.HealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<UserTreatment>()
                .HasOne(ut => ut.HealthProfile)
                .WithMany(h => h.Treatments)
                .HasForeignKey(ut => ut.HealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<UserTreatment>()
                .HasOne(ut => ut.Treatment)
                .WithMany(t => t.UserTreatments)
                .HasForeignKey(ut => ut.TreatmentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Entity<LifestyleMetric>()
                .HasOne(l => l.HealthProfile)
                .WithMany(h => h.LifestyleMetrics)
                .HasForeignKey(l => l.HealthProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}