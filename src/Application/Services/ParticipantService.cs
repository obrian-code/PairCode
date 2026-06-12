using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;

namespace PairCode.Application.Services;

public class ParticipantService
{
    private readonly IParticipantRepository _participantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public ParticipantService(
        IParticipantRepository participantRepository,
        IUserRepository userRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _participantRepository = participantRepository;
        _userRepository = userRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ParticipantDto>> GetRoomParticipantsAsync(Guid roomId)
    {
        var participants = await _participantRepository.GetByRoomIdAsync(roomId);
        return participants.Select(MapToDto);
    }

    public async Task<IEnumerable<ParticipantDto>> GetUserHistoryAsync(Guid userId)
    {
        var participants = await _participantRepository.GetByUserIdAsync(userId);
        return participants.Select(MapToDto);
    }

    public async Task LeaveRoomAsync(Guid roomId, Guid userId)
    {
        var participant = await _participantRepository.GetActiveParticipantAsync(roomId, userId)
            ?? throw new InvalidOperationException("Not a participant in this room");

        participant.MarkLeft();
        await _participantRepository.UpdateAsync(participant);
        await _auditService.LogAsync(userId, "LeaveRoom", nameof(Room), roomId.ToString(), "User left room");
        await _unitOfWork.SaveChangesAsync();
    }

    private ParticipantDto MapToDto(Participant participant)
    {
        return new ParticipantDto(
            participant.Id,
            participant.UserId,
            new UserInfoDto(participant.User.Id, participant.User.Name, participant.User.Email, participant.User.Role.ToString()),
            participant.JoinedAt,
            participant.LeftAt
        );
    }
}
