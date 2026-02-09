using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services
{
    public interface IAuditService
    {
        Task<List<AuditLog>> GetAuditLogsAsync(string entityType, string entityId);
        Task<List<AuditLog>> GetAuditLogsByUserAsync(string userId, int pageSize = 50);
        Task<int> GetAuditLogCountAsync(string entityType, string entityId);
        Task LogViewAsync(string entityType, string entityId, string? userId, string? ipAddress, string? userAgent);
        Task LogCustomFieldChangeAsync(Guid patientId, string fieldLabel, string? oldValue, string? newValue, string? userId, string? ipAddress);
        Task LogChangeAsync(string entityType, string entityId, string fieldName, string? oldValue, string? newValue, string? userId, string? ipAddress);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(string entityType, string entityId)
        {
            return await _context.AuditLogs
                .Include(a => a.ChangedByUser)
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetAuditLogsByUserAsync(string userId, int pageSize = 50)
        {
            return await _context.AuditLogs
                .Include(a => a.ChangedByUser)
                .Where(a => a.ChangedByUserId == userId)
                .OrderByDescending(a => a.ChangedAt)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetAuditLogCountAsync(string entityType, string entityId)
        {
            return await _context.AuditLogs
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .CountAsync();
        }

        public async Task LogViewAsync(string entityType, string entityId, string? userId, string? ipAddress, string? userAgent)
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = "Viewed",
                FieldName = "Record",
                OldValue = null,
                NewValue = null,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task LogCustomFieldChangeAsync(Guid patientId, string fieldLabel, string? oldValue, string? newValue, string? userId, string? ipAddress)
        {
            var auditLog = new AuditLog
            {
                EntityType = "Patient",
                EntityId = patientId.ToString(),
                Action = "Modified",
                FieldName = $"Custom Field: {fieldLabel}",
                OldValue = oldValue ?? "(empty)",
                NewValue = newValue ?? "(empty)",
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                IpAddress = ipAddress,
                UserAgent = null
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task LogChangeAsync(string entityType, string entityId, string fieldName, string? oldValue, string? newValue, string? userId, string? ipAddress)
        {
            // Only log if values actually changed
            if (oldValue == newValue) return;

            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = "Modified",
                FieldName = fieldName,
                OldValue = oldValue ?? "(empty)",
                NewValue = newValue ?? "(empty)",
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                IpAddress = ipAddress,
                UserAgent = null
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
