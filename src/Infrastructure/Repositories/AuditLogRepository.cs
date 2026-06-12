using Microsoft.EntityFrameworkCore;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Infrastructure.Data;

namespace PairCode.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<AuditLog> AddAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        return Task.FromResult(auditLog);
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync() =>
        await _context.AuditLogs.Include(l => l.User)
            .OrderByDescending(l => l.Timestamp).ToListAsync();

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.AuditLogs.Include(l => l.User).OrderByDescending(l => l.Timestamp);
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId) =>
        await _context.AuditLogs.Include(l => l.User)
            .Where(l => l.UserId == userId).OrderByDescending(l => l.Timestamp).ToListAsync();

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string entityId) =>
        await _context.AuditLogs.Include(l => l.User)
            .Where(l => l.EntityName == entityName && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp).ToListAsync();
}
