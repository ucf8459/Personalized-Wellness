using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class HealthProfile
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        public DateTime DateCreated { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Navigation properties
        public List<BiomarkerResult> BiomarkerResults { get; set; } = new();
        public List<PromisResult> PromisResults { get; set; } = new();
        public List<UserTreatment> Treatments { get; set; } = new();
        public List<LifestyleMetric> LifestyleMetrics { get; set; } = new();
    }
}