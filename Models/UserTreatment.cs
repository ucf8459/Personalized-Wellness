using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class UserTreatment
    {
        public int Id { get; set; }
        public int HealthProfileId { get; set; }
        public int TreatmentId { get; set; }
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }
        
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
        
        [StringLength(200)]
        public string? Dosage { get; set; }
        
        [StringLength(100)]
        public string? Frequency { get; set; }
        
        [Range(1, 5, ErrorMessage = "Response rating must be between 1 and 5")]
        [Display(Name = "Response Rating")]
        public int? ResponseRating { get; set; } // 1-5 user-reported effectiveness
        
        [StringLength(1000)]
        public string? SideEffectsNoted { get; set; }
        
        public bool ProviderSupervised { get; set; }
        
        // Navigation properties
        public HealthProfile HealthProfile { get; set; } = null!;
        public Treatment Treatment { get; set; } = null!;
    }
}