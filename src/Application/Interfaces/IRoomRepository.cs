using PairCode.Domain.Entities;

namespace PairCode.Application.Interfaces;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id);
    Task<Room?> GetByAccessCodeAsync(string accessCode);
    Task<IEnumerable<Room>> GetAllAsync();
    Task<IEnumerable<Room>> GetActiveRoomsAsync();
    Task<IEnumerable<Room>> GetFinishedRoomsAsync();
    Task<Room> AddAsync(Room room);
    Task UpdateAsync(Room room);
    Task DeleteAsync(Room room);
}
