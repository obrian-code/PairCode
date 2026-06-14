using FluentAssertions;
using Moq;
using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Application.Services;
using PairCode.Domain.Entities;

namespace PairCode.Application.Tests.Services;

public class AuditServiceTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepo = new();
    private readonly AuditService _sut;

    public AuditServiceTests()
    {
        _sut = new AuditService(_auditLogRepo.Object);
    }

    [Fact]
    public async Task LogAsync_ShouldAddAuditLog()
    {
        var userId = Guid.NewGuid();

        await _sut.LogAsync(userId, "TestAction", "User", userId.ToString(), "Test details");

        _auditLogRepo.Verify(x => x.AddAsync(It.Is<AuditLog>(l =>
            l.UserId == userId &&
            l.Action == "TestAction" &&
            l.EntityName == "User" &&
            l.EntityId == userId.ToString() &&
            l.Details == "Test details"
        )), Times.Once);
    }

    [Fact]
    public async Task GetAllLogsAsync_ShouldReturnAllLogs()
    {
        var logs = new List<AuditLog>
        {
            new(Guid.NewGuid(), "Action1", "Room", "1", "Details1"),
            new(Guid.NewGuid(), "Action2", "User", "2", "Details2")
        };
        _auditLogRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(logs);

        var result = await _sut.GetAllLogsAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedLogsAsync_ShouldReturnPagedResult()
    {
        var userId = Guid.NewGuid();
        var logs = new List<AuditLog>
        {
            new(userId, "CreateRoom", "Room", "1", "Created room")
        };
        _auditLogRepo.Setup(x => x.GetPagedAsync(1, 10)).ReturnsAsync((logs, 1));

        var result = await _sut.GetPagedLogsAsync(1, 10);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
    }
}
