using Microsoft.EntityFrameworkCore;
using WellnessPlatform.Data;
using WellnessPlatform.Models;

namespace WellnessPlatform.Services
{
    public interface ISecureDataService
    {
        Task<T> SaveEncryptedDataAsync<T>(T entity, string userId) where T : class;
        Task<T?> GetEncryptedDataAsync<T>(int id, string userId) where T : class;
        Task<List<T>> GetEncryptedDataListAsync<T>(string userId) where T : class;
        Task<bool> DeleteEncryptedDataAsync<T>(int id, string userId) where T : class;
        Task MigrateExistingDataToEncryptedAsync();
    }

    public class SecureDataService : ISecureDataService
    {
        private readonly WellnessContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly IAuditService _auditService;

        public SecureDataService(WellnessContext context, IEncryptionService encryptionService, IAuditService auditService)
        {
            _context = context;
            _encryptionService = encryptionService;
            _auditService = auditService;
        }

        public async Task<T> SaveEncryptedDataAsync<T>(T entity, string userId) where T : class
        {
            var entityType = typeof(T).Name;
            var entityId = GetEntityId(entity);

            // Encrypt sensitive fields
            var encryptedEntity = EncryptSensitiveFields(entity, userId);

            // Save to database
            _context.Set<T>().Add(encryptedEntity);
            await _context.SaveChangesAsync();

            // Log the audit event
            await _auditService.LogActionAsync(
                action: "CREATE",
                entityType: entityType,
                entityId: entityId?.ToString(),
                newValues: encryptedEntity,
                reason: "Secure data creation",
                isSensitiveData: true,
                additionalContext: "Encrypted data storage"
            );

            return encryptedEntity;
        }

        public async Task<T?> GetEncryptedDataAsync<T>(int id, string userId) where T : class
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null) return null;

            // Decrypt sensitive fields
            var decryptedEntity = DecryptSensitiveFields(entity, userId);

            // Log the audit event
            await _auditService.LogActionAsync(
                action: "READ",
                entityType: typeof(T).Name,
                entityId: id.ToString(),
                reason: "Secure data retrieval",
                isSensitiveData: true,
                additionalContext: "Decrypted data access"
            );

            return decryptedEntity;
        }

        public async Task<List<T>> GetEncryptedDataListAsync<T>(string userId) where T : class
        {
            var entities = await _context.Set<T>().ToListAsync();
            
            // Decrypt sensitive fields for all entities
            var decryptedEntities = entities.Select(e => DecryptSensitiveFields(e, userId)).ToList();

            // Log the audit event
            await _auditService.LogActionAsync(
                action: "READ",
                entityType: typeof(T).Name,
                reason: "Bulk secure data retrieval",
                isSensitiveData: true,
                additionalContext: $"Retrieved {decryptedEntities.Count} records"
            );

            return decryptedEntities;
        }

        public async Task<bool> DeleteEncryptedDataAsync<T>(int id, string userId) where T : class
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity == null) return false;

            // Log before deletion
            await _auditService.LogActionAsync(
                action: "DELETE",
                entityType: typeof(T).Name,
                entityId: id.ToString(),
                oldValues: entity,
                reason: "Secure data deletion",
                isSensitiveData: true,
                additionalContext: "Permanent deletion"
            );

            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task MigrateExistingDataToEncryptedAsync()
        {
            // Migrate biomarker results
            var biomarkerResults = await _context.BiomarkerResults.ToListAsync();
            foreach (var result in biomarkerResults)
            {
                if (!_encryptionService.IsEncrypted(result.BiomarkerName))
                {
                    result.BiomarkerName = _encryptionService.EncryptData(result.BiomarkerName);
                    result.Units = _encryptionService.EncryptData(result.Units);
                    result.Notes = _encryptionService.EncryptData(result.Notes);
                }
            }

            // Migrate PROMIS results
            var promisResults = await _context.PromisResults.ToListAsync();
            foreach (var result in promisResults)
            {
                if (!_encryptionService.IsEncrypted(result.Domain))
                {
                    result.Domain = _encryptionService.EncryptData(result.Domain);
                    result.Notes = _encryptionService.EncryptData(result.Notes);
                }
            }

            // Migrate lifestyle metrics
            var lifestyleMetrics = await _context.LifestyleMetrics.ToListAsync();
            foreach (var metric in lifestyleMetrics)
            {
                if (!_encryptionService.IsEncrypted(metric.Notes))
                {
                    metric.Notes = _encryptionService.EncryptData(metric.Notes);
                }
            }

            await _context.SaveChangesAsync();

            // Log the migration
            await _auditService.LogActionAsync(
                action: "UPDATE",
                entityType: "System",
                reason: "Data encryption migration",
                isSensitiveData: true,
                additionalContext: "Migrated existing data to encrypted format"
            );
        }

        private T EncryptSensitiveFields<T>(T entity, string userId) where T : class
        {
            if (entity is BiomarkerResult biomarker)
            {
                biomarker.BiomarkerName = _encryptionService.EncryptData(biomarker.BiomarkerName, userId);
                biomarker.Units = _encryptionService.EncryptData(biomarker.Units, userId);
                biomarker.Notes = _encryptionService.EncryptData(biomarker.Notes, userId);
            }
            else if (entity is PromisResult promis)
            {
                promis.Domain = _encryptionService.EncryptData(promis.Domain, userId);
                promis.Notes = _encryptionService.EncryptData(promis.Notes, userId);
            }
            else if (entity is LifestyleMetric lifestyle)
            {
                lifestyle.Notes = _encryptionService.EncryptData(lifestyle.Notes, userId);
            }
            else if (entity is HealthProfile healthProfile)
            {
                healthProfile.Notes = _encryptionService.EncryptData(healthProfile.Notes, userId);
            }

            return entity;
        }

        private T DecryptSensitiveFields<T>(T entity, string userId) where T : class
        {
            if (entity is BiomarkerResult biomarker)
            {
                biomarker.BiomarkerName = _encryptionService.DecryptData(biomarker.BiomarkerName, userId);
                biomarker.Units = _encryptionService.DecryptData(biomarker.Units, userId);
                biomarker.Notes = _encryptionService.DecryptData(biomarker.Notes, userId);
            }
            else if (entity is PromisResult promis)
            {
                promis.Domain = _encryptionService.DecryptData(promis.Domain, userId);
                promis.Notes = _encryptionService.DecryptData(promis.Notes, userId);
            }
            else if (entity is LifestyleMetric lifestyle)
            {
                lifestyle.Notes = _encryptionService.DecryptData(lifestyle.Notes, userId);
            }
            else if (entity is HealthProfile healthProfile)
            {
                healthProfile.Notes = _encryptionService.DecryptData(healthProfile.Notes, userId);
            }

            return entity;
        }

        private int? GetEntityId<T>(T entity) where T : class
        {
            var idProperty = typeof(T).GetProperty("Id");
            return idProperty?.GetValue(entity) as int?;
        }
    }
} 