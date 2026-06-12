using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;

namespace PairCode.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    {
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(Guid userId, string action, string entityName, string entityId, string details)
    {
        var auditLog = new AuditLog(userId, action, entityName, entityId, details);
        await _auditLogRepository.AddAsync(auditLog);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLogDto>> GetAllLogsAsync()
    {
        var logs = await _auditLogRepository.GetAllAsync();
        return logs.Select(l => new AuditLogDto(
            l.Id, l.UserId, l.User?.Name ?? "Unknown", l.Action, l.EntityName, l.EntityId, l.Details, l.Timestamp
        ));
    }
}
