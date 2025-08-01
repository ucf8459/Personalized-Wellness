using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class LifestyleMetric
    {
        public int Id { get; set; }
        public int HealthProfileId { get; set; }
        public DateTime RecordDate { get; set; }
        
        public decimal? SleepHours { get; set; }
        public int? SleepQuality { get; set; } // 1-10 scale
        public int? ExerciseMinutes { get; set; }
        public int? ExerciseIntensity { get; set; } // 1-10 scale
        public int? StressLevel { get; set; } // 1-10 scale
        public int? EnergyLevel { get; set; } // 1-10 scale
        public int? MoodRating { get; set; } // 1-10 scale
        public decimal? Weight { get; set; }
        public decimal? BodyFatPercentage { get; set; }
        
        // Navigation property
        public HealthProfile HealthProfile { get; set; } = null!;
    }
}