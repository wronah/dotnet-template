using Template.Domain.Entities;

namespace Template.Application.Abstracts;
public interface IAuthTokenProcessor
{
    (string jwtToken, DateTime expiresAtUtc) GenerateJwtToken(User user, IList<string> roles);
    string GenerateRefreshToken();
    void WriteAuthenticationTokenAsHttpOnlyCookie(string cookieName, string token, DateTime expiresAt);
}
