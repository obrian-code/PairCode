using FluentAssertions;
using Moq;
using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Application.Services;
using PairCode.Domain.Entities;
using PairCode.Domain.Enums;

namespace PairCode.Application.Tests.Services;

public class RoomServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepo = new();
    private readonly Mock<IParticipantRepository> _participantRepo = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RoomService _sut;

    public RoomServiceTests()
    {
        _sut = new RoomService(_roomRepo.Object, _participantRepo.Object, _auditService.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateRoomAsync_ShouldReturnRoomDto()
    {
        var userId = Guid.NewGuid();
        _participantRepo.Setup(x => x.GetParticipantsCountByRoomIdAsync(It.IsAny<Guid>())).ReturnsAsync(0);

        var result = await _sut.CreateRoomAsync(new CreateRoomDto("Test Room"), userId);

        result.Should().NotBeNull();
        result.Name.Should().Be("Test Room");
        result.Status.Should().Be("Created");
        result.CreatedBy.Should().Be(userId);
        _roomRepo.Verify(x => x.AddAsync(It.IsAny<Room>()), Times.Once);
        _auditService.Verify(x => x.LogAsync(userId, "CreateRoom", nameof(Room), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetRoomByIdAsync_ShouldReturnRoom_WhenExists()
    {
        var roomId = Guid.NewGuid();
        var room = new Room("Existing Room", Guid.NewGuid());
        _roomRepo.Setup(x => x.GetByIdAsync(roomId)).ReturnsAsync(room);
        _participantRepo.Setup(x => x.GetParticipantsCountByRoomIdAsync(roomId)).ReturnsAsync(2);

        var result = await _sut.GetRoomByIdAsync(roomId);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Existing Room");
    }

    [Fact]
    public async Task GetRoomByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        _roomRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Room?)null);

        var result = await _sut.GetRoomByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CloseRoomAsync_ShouldCloseRoom_WhenOwner()
    {
        var userId = Guid.NewGuid();
        var room = new Room("Test", userId);
        room.Open();
        _roomRepo.Setup(x => x.GetByIdAsync(room.Id)).ReturnsAsync(room);

        await _sut.CloseRoomAsync(room.Id, userId);

        _roomRepo.Verify(x => x.UpdateAsync(It.Is<Room>(r => r.Status == RoomStatus.Finished)), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CloseRoomAsync_ShouldThrow_WhenNotOwner()
    {
        var room = new Room("Test", Guid.NewGuid());
        room.Open();
        _roomRepo.Setup(x => x.GetByIdAsync(room.Id)).ReturnsAsync(room);

        var act = () => _sut.CloseRoomAsync(room.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteRoomAsync_ShouldSoftDelete_WhenOwner()
    {
        var userId = Guid.NewGuid();
        var room = new Room("Test", userId);
        _roomRepo.Setup(x => x.GetByIdAsync(room.Id)).ReturnsAsync(room);

        await _sut.DeleteRoomAsync(room.Id, userId);

        room.IsDeleted.Should().BeTrue();
        room.DeletedAt.Should().NotBeNull();
        _roomRepo.Verify(x => x.UpdateAsync(It.Is<Room>(r => r.IsDeleted)), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteRoomAsync_ShouldThrow_WhenNotOwner()
    {
        var room = new Room("Test", Guid.NewGuid());
        _roomRepo.Setup(x => x.GetByIdAsync(room.Id)).ReturnsAsync(room);

        var act = () => _sut.DeleteRoomAsync(room.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task JoinRoomByCodeAsync_ShouldAddParticipant_WhenRoomAvailable()
    {
        var userId = Guid.NewGuid();
        var room = new Room("Test", Guid.NewGuid());
        _roomRepo.Setup(x => x.GetByAccessCodeAsync(room.AccessCode)).ReturnsAsync(room);
        _participantRepo.Setup(x => x.GetActiveParticipantAsync(room.Id, userId)).ReturnsAsync((Participant?)null);
        _participantRepo.Setup(x => x.GetParticipantsCountByRoomIdAsync(room.Id)).ReturnsAsync(1);

        var result = await _sut.JoinRoomByCodeAsync(room.AccessCode, userId);

        result.Should().NotBeNull();
        _participantRepo.Verify(x => x.AddAsync(It.IsAny<Participant>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task JoinRoomByCodeAsync_ShouldThrow_WhenAlreadyJoined()
    {
        var userId = Guid.NewGuid();
        var room = new Room("Test", Guid.NewGuid());
        _roomRepo.Setup(x => x.GetByAccessCodeAsync(room.AccessCode)).ReturnsAsync(room);
        _participantRepo.Setup(x => x.GetActiveParticipantAsync(room.Id, userId))
            .ReturnsAsync(new Participant(userId, room.Id));

        var act = () => _sut.JoinRoomByCodeAsync(room.AccessCode, userId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Already joined this room");
    }

    [Fact]
    public async Task UpdateRoomAsync_ShouldUpdateName_WhenOwner()
    {
        var userId = Guid.NewGuid();
        var room = new Room("Old Name", userId);
        _roomRepo.Setup(x => x.GetByIdAsync(room.Id)).ReturnsAsync(room);

        await _sut.UpdateRoomAsync(room.Id, "New Name", userId);

        room.Name.Should().Be("New Name");
        _roomRepo.Verify(x => x.UpdateAsync(It.Is<Room>(r => r.Name == "New Name")), Times.Once);
        _auditService.Verify(x => x.LogAsync(userId, "UpdateRoom", nameof(Room), room.Id.ToString(), "Room renamed to New Name"), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateRoomAsync_ShouldThrow_WhenNotOwner()
    {
        var room = new Room("Old Name", Guid.NewGuid());
        _roomRepo.Setup(x => x.GetByIdAsync(room.Id)).ReturnsAsync(room);

        var act = () => _sut.UpdateRoomAsync(room.Id, "New Name", Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetPagedRoomsAsync_ShouldReturnPagedResult()
    {
        var rooms = new List<Room> { new("Room 1", Guid.NewGuid()), new("Room 2", Guid.NewGuid()) };
        _roomRepo.Setup(x => x.GetPagedAsync(1, 20)).ReturnsAsync((rooms, 2));
        _participantRepo.Setup(x => x.GetParticipantsCountByRoomIdAsync(It.IsAny<Guid>())).ReturnsAsync(0);

        var result = await _sut.GetPagedRoomsAsync(1, 20);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }
}
