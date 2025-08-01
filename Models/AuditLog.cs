using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string Action { get; set; } = string.Empty; // CREATE, READ, UPDATE, DELETE
        
        [Required]
        public string EntityType { get; set; } = string.Empty; // HealthProfile, BiomarkerResult, etc.
        
        public string? EntityId { get; set; } // ID of the affected entity
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public string? IpAddress { get; set; }
        
        public string? UserAgent { get; set; }
        
        public string? OldValues { get; set; } // JSON serialized old values
        
        public string? NewValues { get; set; } // JSON serialized new values
        
        public string? Reason { get; set; } // Reason for the change
        
        public bool IsSensitiveData { get; set; } = false; // Flag for PHI data
        
        public string? SessionId { get; set; } // For session tracking
        
        public string? AdditionalContext { get; set; } // Any additional context
    }
} 