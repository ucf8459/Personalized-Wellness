using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class PromisResult
    {
        public int Id { get; set; }
        public int HealthProfileId { get; set; }
        [DataType(DataType.Date)]
        [Display(Name = "Assessment Date")]
        public DateTime AssessmentDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Domain { get; set; } = string.Empty; // Pain, Fatigue, Depression, Physical Function, etc.
        
        [Range(20, 80, ErrorMessage = "T-score must be between 20 and 80")]
        [Display(Name = "T-Score")]
        public decimal TScore { get; set; } // Standard PROMIS T-Score
        
        [Range(0, 100, ErrorMessage = "Percentile rank must be between 0 and 100")]
        [Display(Name = "Percentile Rank")]
        public decimal? PercentileRank { get; set; }
        
        [StringLength(20)]
        public string? SeverityLevel { get; set; } // Normal, Mild, Moderate, Severe
        
        [Range(1, 50, ErrorMessage = "Items answered must be between 1 and 50")]
        [Display(Name = "Items Answered")]
        public int ItemsAnswered { get; set; }
        
        // Navigation property
        public HealthProfile HealthProfile { get; set; } = null!;
    }
}