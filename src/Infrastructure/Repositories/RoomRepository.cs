using Microsoft.EntityFrameworkCore;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Domain.Enums;
using PairCode.Infrastructure.Data;

namespace PairCode.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _context;

    public RoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Room?> GetByIdAsync(Guid id) =>
        await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

    public async Task<Room?> GetByAccessCodeAsync(string accessCode) =>
        await _context.Rooms.FirstOrDefaultAsync(r => r.AccessCode == accessCode && !r.IsDeleted);

    public async Task<IEnumerable<Room>> GetAllAsync() =>
        await _context.Rooms.Where(r => !r.IsDeleted).OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<(IEnumerable<Room> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Rooms.Where(r => !r.IsDeleted).OrderByDescending(r => r.CreatedAt);
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<IEnumerable<Room>> GetActiveRoomsAsync() =>
        await _context.Rooms.Where(r => r.Status == RoomStatus.Active && !r.IsDeleted).ToListAsync();

    public async Task<IEnumerable<Room>> GetFinishedRoomsAsync() =>
        await _context.Rooms.Where(r => r.Status == RoomStatus.Finished && !r.IsDeleted).ToListAsync();

    public Task<Room> AddAsync(Room room)
    {
        _context.Rooms.Add(room);
        return Task.FromResult(room);
    }

    public Task UpdateAsync(Room room)
    {
        _context.Rooms.Update(room);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Room room)
    {
        _context.Rooms.Remove(room);
        return Task.CompletedTask;
    }
}
