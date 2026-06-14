namespace PairCode.Application.DTOs;

public record RegisterUserDto(string Name, string Email, string Password, string Role);

public record LoginDto(string Email, string Password);

public record AuthResponseDto(string Token, string Name, string Email, string Role, Guid UserId, string? RefreshToken = null);

public record UserProfileDto(Guid Id, string Name, string Email, string Role, DateTime CreatedAt);

public record UpdateProfileDto(string Name);

public record ChangePasswordDto(string CurrentPassword, string NewPassword);

public record CreateRoomDto(string Name);

public record RoomDto(Guid Id, string Name, string AccessCode, string Status, DateTime CreatedAt, Guid CreatedBy, int ParticipantCount);

public record JoinRoomDto(string AccessCode);

public record ChatMessageDto(Guid Id, Guid UserId, UserInfoDto User, string Message, DateTime SentAt);

public record SendMessageDto(string Message);

public record SharedDocumentDto(string Content, int Version, DateTime UpdatedAt);

public record UpdateDocumentDto(string Content);

public record ParticipantDto(Guid Id, Guid UserId, UserInfoDto User, DateTime JoinedAt, DateTime? LeftAt);

public record UserInfoDto(Guid Id, string Name, string Email, string Role);

public record DashboardDto(
    int ActiveRooms,
    int FinishedRooms,
    int ActiveParticipants,
    int TotalParticipants,
    int TotalMessages,
    int TotalSessions
);

public record AuditLogDto(Guid Id, Guid UserId, string UserName, string Action, string EntityName, string EntityId, string Details, DateTime Timestamp);
public record ForgotPasswordDto(string Email);
public record ResetPasswordDto(string Email, string Token, string NewPassword);
public record RefreshTokenDto(string RefreshToken);
public record RefreshTokenResponseDto(string Token, string RefreshToken);

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class BreadcrumbItem
{
    public string Label { get; }
    public string? Url { get; }

    public BreadcrumbItem(string label, string? url = null)
    {
        Label = label;
        Url = url;
    }
}
