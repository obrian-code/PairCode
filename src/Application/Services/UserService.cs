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
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepository, ITokenService tokenService, IAuditService auditService, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
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
        await _unitOfWork.SaveChangesAsync();

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
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        if (!VerifyBCryptPassword(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        if (dto.NewPassword.Length < 8)
            throw new InvalidOperationException("New password must be at least 8 characters");

        if (!dto.NewPassword.Any(char.IsUpper))
            throw new InvalidOperationException("New password must contain at least one uppercase letter");

        if (!dto.NewPassword.Any(char.IsDigit))
            throw new InvalidOperationException("New password must contain at least one digit");

        if (!dto.NewPassword.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException("New password must contain at least one special character");

        user.UpdatePasswordHash(BCryptPasswordHash(dto.NewPassword));
        await _userRepository.UpdateAsync(user);
        await _auditService.LogAsync(userId, "ChangePassword", nameof(User), userId.ToString(), "Password changed");
        await _unitOfWork.SaveChangesAsync();
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
