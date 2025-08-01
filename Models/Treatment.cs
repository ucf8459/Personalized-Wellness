using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class Treatment
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public TreatmentCategory Category { get; set; }
        public EvidenceLevel EvidenceLevel { get; set; }
        public RegulatoryStatus RegulatoryStatus { get; set; }
        
        [StringLength(500)]
        public string? Mechanism { get; set; }
        
        [StringLength(200)]
        public string? TypicalDosage { get; set; }
        
        [StringLength(500)]
        public string? MonitoringRequired { get; set; }
        
        [StringLength(1000)]
        public string? CommonSideEffects { get; set; }
        
        [StringLength(1000)]
        public string? Contraindications { get; set; }
        
        [StringLength(100)]
        public string? CostRange { get; set; }
        
        [Range(1, 5, ErrorMessage = "Safety rating must be between 1 and 5")]
        [Display(Name = "Safety Rating")]
        public int SafetyRating { get; set; } // 1-5 scale
        
        // Navigation properties
        public List<UserTreatment> UserTreatments { get; set; } = new();
        
        // Display properties
        public string EvidenceBadge => EvidenceLevel switch
        {
            EvidenceLevel.SystematicReviews => "🥇 GOLD",
            EvidenceLevel.RandomizedTrials => "🥈 SILVER", 
            EvidenceLevel.CohortStudies => "🥉 BRONZE",
            EvidenceLevel.CaseReports => "🧪 EXPERIMENTAL",
            EvidenceLevel.PreclinicalOnly => "🔬 RESEARCH",
            _ => "❓ UNKNOWN"
        };
    }
    
    public enum TreatmentCategory
    {
        Supplement,
        Peptide,
        Medication,
        Lifestyle
    }
    
    public enum EvidenceLevel
    {
        SystematicReviews = 1,    // Meta-analyses, systematic reviews of RCTs
        RandomizedTrials = 2,     // Individual RCTs, high-quality cohort studies
        CohortStudies = 3,        // Observational studies, expert consensus
        CaseReports = 4,          // Case reports, limited human data
        PreclinicalOnly = 5       // Animal studies, theoretical only
    }
    
    public enum RegulatoryStatus
    {
        FDAApproved,
        OffLabel,
        Experimental,
        Restricted,
        Banned
    }
}