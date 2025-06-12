using Template.Domain.Requests;

namespace Template.Application.Abstracts;
public interface IAccountService
{
    Task RegisterAsync(RegisterRequest request);
    Task LoginAsync(LoginRequest request);
    Task RefreshTokenAsync(string? refreshToken);
}
