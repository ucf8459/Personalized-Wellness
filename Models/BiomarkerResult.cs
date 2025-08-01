using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class BiomarkerResult
    {
        public int Id { get; set; }
        public int HealthProfileId { get; set; }
        [DataType(DataType.Date)]
        [Display(Name = "Test Date")]
        public DateTime TestDate { get; set; }
        
        [Required]
        [StringLength(100)]
        public string BiomarkerName { get; set; } = string.Empty;
        
        [Range(0.01, 10000, ErrorMessage = "Biomarker value must be between 0.01 and 10,000")]
        public decimal Value { get; set; }
        
        [Range(0.01, 10000, ErrorMessage = "Reference range minimum must be between 0.01 and 10,000")]
        public decimal? ReferenceRangeMin { get; set; }
        
        [Range(0.01, 10000, ErrorMessage = "Reference range maximum must be between 0.01 and 10,000")]
        public decimal? ReferenceRangeMax { get; set; }
        
        [Range(0.01, 10000, ErrorMessage = "Optimal range minimum must be between 0.01 and 10,000")]
        public decimal? OptimalRangeMin { get; set; }
        
        [Range(0.01, 10000, ErrorMessage = "Optimal range maximum must be between 0.01 and 10,000")]
        public decimal? OptimalRangeMax { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Units { get; set; } = string.Empty;
        
        public BiomarkerStatus Status { get; set; }
        
        // Navigation property
        public HealthProfile HealthProfile { get; set; } = null!;
        
        // Calculated properties
        public bool IsInOptimalRange => OptimalRangeMin.HasValue && OptimalRangeMax.HasValue && 
                                       Value >= OptimalRangeMin && Value <= OptimalRangeMax;
        
        public string StatusColor => Status switch
        {
            BiomarkerStatus.Low => "text-danger",
            BiomarkerStatus.High => "text-danger", 
            BiomarkerStatus.Normal => "text-warning",
            BiomarkerStatus.Optimal => "text-success",
            _ => "text-muted"
        };
    }
    
    public enum BiomarkerStatus
    {
        Low,
        Normal,
        High,
        Optimal
    }
}