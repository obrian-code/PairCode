using PairCode.Domain.Entities;

namespace PairCode.Application.Interfaces;

public interface IParticipantRepository
{
    Task<Participant?> GetByIdAsync(Guid id);
    Task<IEnumerable<Participant>> GetByRoomIdAsync(Guid roomId);
    Task<IEnumerable<Participant>> GetByUserIdAsync(Guid userId);
    Task<Participant?> GetActiveParticipantAsync(Guid roomId, Guid userId);
    Task<Participant> AddAsync(Participant participant);
    Task UpdateAsync(Participant participant);
    Task<int> GetActiveParticipantsCountAsync();
    Task<int> GetTotalParticipantsCountAsync();
    Task<int> GetParticipantsCountByRoomIdAsync(Guid roomId);
}
