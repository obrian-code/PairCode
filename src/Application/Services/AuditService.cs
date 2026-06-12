using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;

namespace PairCode.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogAsync(Guid userId, string action, string entityName, string entityId, string details)
    {
        var auditLog = new AuditLog(userId, action, entityName, entityId, details);
        await _auditLogRepository.AddAsync(auditLog);
    }

    public async Task<IEnumerable<AuditLogDto>> GetAllLogsAsync()
    {
        var logs = await _auditLogRepository.GetAllAsync();
        return logs.Select(l => new AuditLogDto(
            l.Id, l.UserId, l.User?.Name ?? "Unknown", l.Action, l.EntityName, l.EntityId, l.Details, l.Timestamp
        ));
    }
}

public record AuditLogDto(Guid Id, Guid UserId, string UserName, string Action, string EntityName, string EntityId, string Details, DateTime Timestamp);
