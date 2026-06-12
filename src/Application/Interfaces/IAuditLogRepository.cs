using PairCode.Domain.Entities;

namespace PairCode.Application.Interfaces;

public interface IAuditLogRepository
{
    Task<AuditLog> AddAsync(AuditLog auditLog);
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string entityId);
}
