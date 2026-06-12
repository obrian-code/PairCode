using PairCode.Application.DTOs;
using PairCode.Application.Interfaces;
using PairCode.Domain.Entities;
using PairCode.Domain.Enums;

namespace PairCode.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;

    public UserService(IUserRepository userRepository, ITokenService tokenService, IAuditService auditService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _auditService = auditService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
            throw new InvalidOperationException("Email already registered");

        var role = dto.Role switch
        {
            "Admin" => UserRole.Admin,
            "Interviewer" => UserRole.Interviewer,
            "Candidate" => UserRole.Candidate,
            _ => throw new InvalidOperationException("Invalid role")
        };

        var passwordHash = BCryptPasswordHash(dto.Password);
        var user = new User(dto.Name, dto.Email, passwordHash, role);

        await _userRepository.AddAsync(user);
        await _auditService.LogAsync(user.Id, "Register", nameof(User), user.Id.ToString(), $"User {user.Name} registered");

        var token = _tokenService.GenerateToken(user);
        return new AuthResponseDto(token, user.Name, user.Email, user.Role.ToString(), user.Id);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !VerifyBCryptPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        var token = _tokenService.GenerateToken(user);
        return new AuthResponseDto(token, user.Name, user.Email, user.Role.ToString(), user.Id);
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return new UserProfileDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        user.UpdateProfile(dto.Name);
        await _userRepository.UpdateAsync(user);
        await _auditService.LogAsync(userId, "UpdateProfile", nameof(User), userId.ToString(), "Profile updated");
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        if (!VerifyBCryptPassword(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        if (dto.NewPassword.Length < 6)
            throw new InvalidOperationException("New password must be at least 6 characters");

        user.UpdatePasswordHash(BCryptPasswordHash(dto.NewPassword));
        await _userRepository.UpdateAsync(user);
        await _auditService.LogAsync(userId, "ChangePassword", nameof(User), userId.ToString(), "Password changed");
    }

    private static string BCryptPasswordHash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyBCryptPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
