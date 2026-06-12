using Microsoft.EntityFrameworkCore;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Infrastructure.Data;

namespace PairCode.Infrastructure.Repositories;

public class ParticipantRepository : IParticipantRepository
{
    private readonly AppDbContext _context;

    public ParticipantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Participant?> GetByIdAsync(Guid id) =>
        await _context.Participants.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Participant>> GetByRoomIdAsync(Guid roomId) =>
        await _context.Participants.Include(p => p.User)
            .Where(p => p.RoomId == roomId).OrderBy(p => p.JoinedAt).ToListAsync();

    public async Task<IEnumerable<Participant>> GetByUserIdAsync(Guid userId) =>
        await _context.Participants.Include(p => p.User)
            .Where(p => p.UserId == userId).OrderByDescending(p => p.JoinedAt).ToListAsync();

    public async Task<Participant?> GetActiveParticipantAsync(Guid roomId, Guid userId) =>
        await _context.Participants
            .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId && p.LeftAt == null);

    public async Task<Participant> AddAsync(Participant participant)
    {
        _context.Participants.Add(participant);
        await _context.SaveChangesAsync();
        return participant;
    }

    public async Task UpdateAsync(Participant participant)
    {
        _context.Participants.Update(participant);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetActiveParticipantsCountAsync() =>
        await _context.Participants.CountAsync(p => p.LeftAt == null);

    public async Task<int> GetTotalParticipantsCountAsync() =>
        await _context.Participants.CountAsync();

    public async Task<int> GetParticipantsCountByRoomIdAsync(Guid roomId) =>
        await _context.Participants.CountAsync(p => p.RoomId == roomId);
}
