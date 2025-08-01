using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WellnessPlatform.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Organization { get; set; }

        [StringLength(20)]
        public string? LicenseNumber { get; set; } // For healthcare providers

        public UserRole Role { get; set; } = UserRole.Patient;

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public HealthProfile? HealthProfile { get; set; }

        // Computed properties
        public string FullName => $"{FirstName} {LastName}";
        public string DisplayName => Role == UserRole.Provider ? $"{FullName} ({Organization})" : FullName;
        public string RoleDisplayName => Role switch
        {
            UserRole.Patient => "Patient",
            UserRole.Provider => "Healthcare Provider",
            UserRole.Admin => "Administrator",
            _ => "Unknown"
        };
    }

    public enum UserRole
    {
        Patient,
        Provider,
        Admin
    }
} 