using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class UserTreatment
    {
        public int Id { get; set; }
        public int HealthProfileId { get; set; }
        public int TreatmentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        [StringLength(200)]
        public string? Dosage { get; set; }
        
        [StringLength(100)]
        public string? Frequency { get; set; }
        
        public int? ResponseRating { get; set; } // 1-5 user-reported effectiveness
        
        [StringLength(1000)]
        public string? SideEffectsNoted { get; set; }
        
        public bool ProviderSupervised { get; set; }
        
        // Navigation properties
        public HealthProfile HealthProfile { get; set; } = null!;
        public Treatment Treatment { get; set; } = null!;
    }
}