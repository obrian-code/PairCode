using Microsoft.EntityFrameworkCore;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Infrastructure.Data;

namespace PairCode.Infrastructure.Repositories;

public class SharedDocumentRepository : ISharedDocumentRepository
{
    private readonly AppDbContext _context;

    public SharedDocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SharedDocument?> GetByRoomIdAsync(Guid roomId) =>
        await _context.SharedDocuments.FirstOrDefaultAsync(d => d.RoomId == roomId);

    public Task<SharedDocument> AddAsync(SharedDocument document)
    {
        _context.SharedDocuments.Add(document);
        return Task.FromResult(document);
    }

    public Task UpdateAsync(SharedDocument document)
    {
        _context.SharedDocuments.Update(document);
        return Task.CompletedTask;
    }
}
