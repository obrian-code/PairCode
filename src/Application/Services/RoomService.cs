using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Domain.Enums;

namespace PairCode.Application.Services;

public class RoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public RoomService(
        IRoomRepository roomRepository,
        IParticipantRepository participantRepository,
        IAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RoomDto> CreateRoomAsync(CreateRoomDto dto, Guid userId)
    {
        var room = new Room(dto.Name, userId);
        await _roomRepository.AddAsync(room);
        await _auditService.LogAsync(userId, "CreateRoom", nameof(Room), room.Id.ToString(), $"Room {room.Name} created");
        await _unitOfWork.SaveChangesAsync();

        return await MapToDto(room);
    }

    public async Task<RoomDto?> GetRoomByIdAsync(Guid id)
    {
        var room = await _roomRepository.GetByIdAsync(id);
        return room == null ? null : await MapToDto(room);
    }

    public async Task<RoomDto?> GetRoomByAccessCodeAsync(string code)
    {
        var room = await _roomRepository.GetByAccessCodeAsync(code);
        return room == null ? null : await MapToDto(room);
    }

    public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync()
    {
        var rooms = await _roomRepository.GetAllAsync();
        var dtos = new List<RoomDto>();
        foreach (var room in rooms)
            dtos.Add(await MapToDto(room));
        return dtos;
    }

    public async Task DeleteRoomAsync(Guid id, Guid userId)
    {
        var room = await _roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");

        RequireOwnership(room, userId);
        await _roomRepository.DeleteAsync(room);
        await _auditService.LogAsync(userId, "DeleteRoom", nameof(Room), id.ToString(), "Room deleted");
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task OpenRoomAsync(Guid id, Guid userId)
    {
        var room = await _roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");

        RequireOwnership(room, userId);
        room.Open();
        await _roomRepository.UpdateAsync(room);
        await _auditService.LogAsync(userId, "OpenRoom", nameof(Room), id.ToString(), "Room opened");
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CloseRoomAsync(Guid id, Guid userId)
    {
        var room = await _roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");

        RequireOwnership(room, userId);
        room.Close();
        await _roomRepository.UpdateAsync(room);
        await _auditService.LogAsync(userId, "CloseRoom", nameof(Room), id.ToString(), "Room closed");
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CancelRoomAsync(Guid id, Guid userId)
    {
        var room = await _roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");

        RequireOwnership(room, userId);
        room.Cancel();
        await _roomRepository.UpdateAsync(room);
        await _auditService.LogAsync(userId, "CancelRoom", nameof(Room), id.ToString(), "Room cancelled");
        await _unitOfWork.SaveChangesAsync();
    }

    private static void RequireOwnership(Room room, Guid userId)
    {
        if (room.CreatedBy != userId)
            throw new UnauthorizedAccessException("Only the room creator can perform this action");
    }

    public async Task<RoomDto> JoinRoomByCodeAsync(string accessCode, Guid userId)
    {
        var room = await _roomRepository.GetByAccessCodeAsync(accessCode)
            ?? throw new InvalidOperationException("Room not found");

        if (room.Status != RoomStatus.Created && room.Status != RoomStatus.Active)
            throw new InvalidOperationException("Room is not available");

        var existing = await _participantRepository.GetActiveParticipantAsync(room.Id, userId);
        if (existing != null)
            throw new InvalidOperationException("Already joined this room");

        var participant = new Participant(userId, room.Id);
        await _participantRepository.AddAsync(participant);
        await _auditService.LogAsync(userId, "JoinRoom", nameof(Room), room.Id.ToString(), $"User joined room {room.Name}");
        await _unitOfWork.SaveChangesAsync();

        return await MapToDto(room);
    }

    private async Task<RoomDto> MapToDto(Room room)
    {
        var participantCount = await _participantRepository.GetParticipantsCountByRoomIdAsync(room.Id);
        return new RoomDto(
            room.Id,
            room.Name,
            room.AccessCode,
            room.Status.ToString(),
            room.CreatedAt,
            room.CreatedBy,
            participantCount
        );
    }
}
