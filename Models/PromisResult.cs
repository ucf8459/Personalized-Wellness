using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class PromisResult
    {
        public int Id { get; set; }
        public int HealthProfileId { get; set; }
        public DateTime AssessmentDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Domain { get; set; } = string.Empty; // Pain, Fatigue, Depression, Physical Function, etc.
        
        public decimal TScore { get; set; } // Standard PROMIS T-Score
        public decimal? PercentileRank { get; set; }
        
        [StringLength(20)]
        public string? SeverityLevel { get; set; } // Normal, Mild, Moderate, Severe
        
        public int ItemsAnswered { get; set; }
        
        // Navigation property
        public HealthProfile HealthProfile { get; set; } = null!;
    }
}