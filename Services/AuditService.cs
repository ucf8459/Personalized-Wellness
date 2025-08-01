using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WellnessPlatform.Data;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(string action, string entityType, string? entityId = null, 
            object? oldValues = null, object? newValues = null, string? reason = null, 
            bool isSensitiveData = false, string? additionalContext = null);
        Task<List<AuditLog>> GetAuditLogsAsync(string userId, DateTime? startDate = null, 
            DateTime? endDate = null, string? entityType = null);
        Task<List<AuditLog>> GetSensitiveDataAccessAsync(string userId, DateTime? startDate = null, 
            DateTime? endDate = null);
    }

    public class AuditService : IAuditService
    {
        private readonly WellnessContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(WellnessContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActionAsync(string action, string entityType, string? entityId = null, 
            object? oldValues = null, object? newValues = null, string? reason = null, 
            bool isSensitiveData = false, string? additionalContext = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.Identity?.Name ?? "System";
            
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                Reason = reason,
                IsSensitiveData = isSensitiveData,
                SessionId = httpContext?.Session?.Id,
                AdditionalContext = additionalContext
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(string userId, DateTime? startDate = null, 
            DateTime? endDate = null, string? entityType = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(a => a.EntityType == entityType);

            return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
        }

        public async Task<List<AuditLog>> GetSensitiveDataAccessAsync(string userId, DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            var query = _context.AuditLogs.Where(a => a.IsSensitiveData);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(a => a.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(a => a.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Timestamp <= endDate.Value);

            return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
        }

        private string? GetClientIpAddress(HttpContext? httpContext)
        {
            if (httpContext == null) return null;

            // Check for forwarded headers (for proxy scenarios)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
} 