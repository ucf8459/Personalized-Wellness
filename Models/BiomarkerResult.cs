using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class BiomarkerResult
    {
        public int Id { get; set; }
        public int HealthProfileId { get; set; }
        public DateTime TestDate { get; set; }
        
        [Required]
        [StringLength(100)]
        public string BiomarkerName { get; set; } = string.Empty;
        
        public decimal Value { get; set; }
        public decimal? ReferenceRangeMin { get; set; }
        public decimal? ReferenceRangeMax { get; set; }
        public decimal? OptimalRangeMin { get; set; }
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