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
        await _context.Rooms.FindAsync(id);

    public async Task<Room?> GetByAccessCodeAsync(string accessCode) =>
        await _context.Rooms.FirstOrDefaultAsync(r => r.AccessCode == accessCode);

    public async Task<IEnumerable<Room>> GetAllAsync() =>
        await _context.Rooms.OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<IEnumerable<Room>> GetActiveRoomsAsync() =>
        await _context.Rooms.Where(r => r.Status == RoomStatus.Active).ToListAsync();

    public async Task<IEnumerable<Room>> GetFinishedRoomsAsync() =>
        await _context.Rooms.Where(r => r.Status == RoomStatus.Finished).ToListAsync();

    public async Task<Room> AddAsync(Room room)
    {
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task UpdateAsync(Room room)
    {
        _context.Rooms.Update(room);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Room room)
    {
        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();
    }
}
