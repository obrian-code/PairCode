using PairCode.Domain.Entities;

namespace PairCode.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
