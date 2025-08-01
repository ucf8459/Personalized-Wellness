using Microsoft.AspNetCore.Identity;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services
{
    public class AuthorizationService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthorizationService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> CanAccessHealthProfileAsync(string userId, int healthProfileId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            // Admins can access all profiles
            if (user.Role == UserRole.Admin) return true;

            // Providers can access profiles they're assigned to (future enhancement)
            if (user.Role == UserRole.Provider) return true;

            // Patients can only access their own profile
            return user.HealthProfile?.Id == healthProfileId;
        }

        public async Task<bool> CanViewBiomarkersAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsActive == true;
        }

        public async Task<bool> CanEditBiomarkersAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsActive == true && user.Role != UserRole.Patient;
        }

        public async Task<bool> CanViewTreatmentsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsActive == true;
        }

        public async Task<bool> CanManageTreatmentsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsActive == true && (user.Role == UserRole.Provider || user.Role == UserRole.Admin);
        }

        public async Task<bool> CanViewAdminPanelAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsActive == true && user.Role == UserRole.Admin;
        }

        public async Task<bool> CanManageUsersAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsActive == true && user.Role == UserRole.Admin;
        }

        public async Task<bool> CanViewAnalyticsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsActive == true && (user.Role == UserRole.Provider || user.Role == UserRole.Admin);
        }

        public async Task<bool> CanExportDataAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsActive == true && (user.Role == UserRole.Provider || user.Role == UserRole.Admin);
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            var permissions = new List<string>();

            // Base permissions for all active users
            if (user.IsActive)
            {
                permissions.AddRange(new[] { "ViewOwnProfile", "ViewBiomarkers", "ViewTreatments", "EnterData" });
            }

            // Provider permissions
            if (user.Role == UserRole.Provider)
            {
                permissions.AddRange(new[] { "ManageTreatments", "ViewAnalytics", "ExportData", "ViewPatientProfiles" });
            }

            // Admin permissions
            if (user.Role == UserRole.Admin)
            {
                permissions.AddRange(new[] { "ManageUsers", "ViewAdminPanel", "ManageTreatments", "ViewAnalytics", "ExportData", "ViewAllProfiles", "SystemConfiguration" });
            }

            return permissions;
        }
    }
} 