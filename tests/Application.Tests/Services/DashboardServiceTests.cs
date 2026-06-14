using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Application.Services;
using PairCode.Domain.Entities;

namespace PairCode.Application.Tests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepo = new();
    private readonly Mock<IParticipantRepository> _participantRepo = new();
    private readonly Mock<IChatMessageRepository> _messageRepo = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _sut = new DashboardService(_roomRepo.Object, _participantRepo.Object, _messageRepo.Object, _cache);
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldReturnDashboardDto()
    {
        _roomRepo.Setup(x => x.GetActiveRoomsAsync()).ReturnsAsync(new List<Room> { new("Active", Guid.NewGuid()) });
        _roomRepo.Setup(x => x.GetFinishedRoomsAsync()).ReturnsAsync(new List<Room>());
        _participantRepo.Setup(x => x.GetActiveParticipantsCountAsync()).ReturnsAsync(5);
        _participantRepo.Setup(x => x.GetTotalParticipantsCountAsync()).ReturnsAsync(10);
        _messageRepo.Setup(x => x.GetTotalMessagesCountAsync()).ReturnsAsync(100);

        var result = await _sut.GetDashboardAsync();

        result.Should().NotBeNull();
        result.ActiveRooms.Should().Be(1);
        result.FinishedRooms.Should().Be(0);
        result.ActiveParticipants.Should().Be(5);
        result.TotalParticipants.Should().Be(10);
        result.TotalMessages.Should().Be(100);
        result.TotalSessions.Should().Be(10);
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldReturnCached_OnSecondCall()
    {
        _roomRepo.Setup(x => x.GetActiveRoomsAsync()).ReturnsAsync(new List<Room>());
        _roomRepo.Setup(x => x.GetFinishedRoomsAsync()).ReturnsAsync(new List<Room>());
        _participantRepo.Setup(x => x.GetActiveParticipantsCountAsync()).ReturnsAsync(0);
        _participantRepo.Setup(x => x.GetTotalParticipantsCountAsync()).ReturnsAsync(0);
        _messageRepo.Setup(x => x.GetTotalMessagesCountAsync()).ReturnsAsync(0);

        var first = await _sut.GetDashboardAsync();
        var second = await _sut.GetDashboardAsync();

        _roomRepo.Verify(x => x.GetActiveRoomsAsync(), Times.Once);
        first.Should().BeEquivalentTo(second);
    }
}
