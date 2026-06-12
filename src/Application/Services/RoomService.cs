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

    public RoomService(
        IRoomRepository roomRepository,
        IParticipantRepository participantRepository,
        IAuditService auditService)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _auditService = auditService;
    }

    public async Task<RoomDto> CreateRoomAsync(CreateRoomDto dto, Guid userId)
    {
        var room = new Room(dto.Name, userId);
        await _roomRepository.AddAsync(room);
        await _auditService.LogAsync(userId, "CreateRoom", nameof(Room), room.Id.ToString(), $"Room {room.Name} created");

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

    public async Task UpdateRoomAsync(Guid id, UpdateRoomDto dto, Guid userId)
    {
        var room = await _roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");

        room.UpdateName(dto.Name);
        await _roomRepository.UpdateAsync(room);
        await _auditService.LogAsync(userId, "UpdateRoom", nameof(Room), id.ToString(), "Room updated");
    }

    public async Task DeleteRoomAsync(Guid id, Guid userId)
    {
        var room = await _roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");

        await _roomRepository.DeleteAsync(room);
        await _auditService.LogAsync(userId, "DeleteRoom", nameof(Room), id.ToString(), "Room deleted");
    }

    public async Task OpenRoomAsync(Guid id, Guid userId)
    {
        var room = await _roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");

        room.Open();
        await _roomRepository.UpdateAsync(room);
        await _auditService.LogAsync(userId, "OpenRoom", nameof(Room), id.ToString(), "Room opened");
    }

    public async Task CloseRoomAsync(Guid id, Guid userId)
    {
        var room = await _roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");

        room.Close();
        await _roomRepository.UpdateAsync(room);
        await _auditService.LogAsync(userId, "CloseRoom", nameof(Room), id.ToString(), "Room closed");
    }

    public async Task CancelRoomAsync(Guid id, Guid userId)
    {
        var room = await _roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Room not found");

        room.Cancel();
        await _roomRepository.UpdateAsync(room);
        await _auditService.LogAsync(userId, "CancelRoom", nameof(Room), id.ToString(), "Room cancelled");
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
