using Template.Domain.Entities;

namespace Template.Application.Abstracts;
public interface IUserRepository
{
    Task<User?> GetUserByRefreshTokenAsync(string refreshToken);
}
