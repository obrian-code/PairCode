using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;

namespace PairCode.Application.Services;

public class DashboardService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IChatMessageRepository _messageRepository;

    public DashboardService(
        IRoomRepository roomRepository,
        IParticipantRepository participantRepository,
        IChatMessageRepository messageRepository)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var activeRooms = await _roomRepository.GetActiveRoomsAsync();
        var finishedRooms = await _roomRepository.GetFinishedRoomsAsync();
        var activeParticipants = await _participantRepository.GetActiveParticipantsCountAsync();
        var totalParticipants = await _participantRepository.GetTotalParticipantsCountAsync();
        var totalMessages = await _messageRepository.GetTotalMessagesCountAsync();
        var totalSessions = totalParticipants;

        return new DashboardDto(
            activeRooms.Count(),
            finishedRooms.Count(),
            activeParticipants,
            totalParticipants,
            totalMessages,
            totalSessions
        );
    }
}
