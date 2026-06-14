using Microsoft.Extensions.Caching.Memory;
using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;

namespace PairCode.Application.Services;

public class DashboardService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IMemoryCache _cache;

    public DashboardService(
        IRoomRepository roomRepository,
        IParticipantRepository participantRepository,
        IChatMessageRepository messageRepository,
        IMemoryCache cache)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _cache = cache;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        if (_cache.TryGetValue<DashboardDto>("DashboardData", out var cached))
            return cached!;

        var activeRooms = await _roomRepository.GetActiveRoomsAsync();
        var finishedRooms = await _roomRepository.GetFinishedRoomsAsync();
        var activeParticipants = await _participantRepository.GetActiveParticipantsCountAsync();
        var totalParticipants = await _participantRepository.GetTotalParticipantsCountAsync();
        var totalMessages = await _messageRepository.GetTotalMessagesCountAsync();
        var totalSessions = totalParticipants;

        var result = new DashboardDto(
            activeRooms.Count(),
            finishedRooms.Count(),
            activeParticipants,
            totalParticipants,
            totalMessages,
            totalSessions
        );

        _cache.Set("DashboardData", result, TimeSpan.FromSeconds(60));
        return result;
    }
}
