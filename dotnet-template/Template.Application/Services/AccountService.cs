using Microsoft.AspNetCore.Identity;
using Template.Application.Abstracts;
using Template.Domain.Entities;
using Template.Domain.Exceptions;
using Template.Domain.Requests;

namespace Template.Application.Services;
public class AccountService : IAccountService
{
    private readonly IAuthTokenProcessor authTokenProcessor;
    private readonly UserManager<User> userManager;
    private readonly IUserRepository userRepository;

    public AccountService(IAuthTokenProcessor authTokenProcessor, UserManager<User> userManager, IUserRepository userRepository)
    {
        this.authTokenProcessor = authTokenProcessor;
        this.userManager = userManager;
        this.userRepository = userRepository;
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        var userExists = await userManager.FindByEmailAsync(request.Email) != null;

        if (userExists)
        {
            throw new UserAlreadyExistsException(email: request.Email);
        }

        var user = User.Create(email: request.Email, firstName: request.FirstName, lastName: request.LastName);
        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, request.Password);

        var result = await userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            throw new RegistrationFailedException(result.Errors.Select(x => x.Description));
        }
    }

    public async Task LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new LoginFailedException(request.Email);
        }

        await AssignTokens(user);
    }

    public async Task RefreshTokenAsync(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new RefreshTokenException("Refresh token is missing.");
        }

        var user = await userRepository.GetUserByRefreshTokenAsync(refreshToken);

        if (user == null)
        {
            throw new RefreshTokenException("Unable to retrieve user for refresh token");
        }

        if (user.RefreshTokenExpiresAtUtc < DateTime.UtcNow)
        {
            throw new RefreshTokenException("Refresh token is expired.");
        }

        await AssignTokens(user);
    }

    private async Task AssignTokens(User user)
    {
        var (jwtToken, jwtTokenExpiresAtUtc) = authTokenProcessor.GenerateJwtToken(user);
        var refreshTokenValue = authTokenProcessor.GenerateRefreshToken();

        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);

        user.RefreshToken = refreshTokenValue;
        user.RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;

        await userManager.UpdateAsync(user);

        authTokenProcessor.WriteAuthenticationTokenAsHttpOnlyCookie("ACCESS_TOKEN", jwtToken, jwtTokenExpiresAtUtc);
        authTokenProcessor.WriteAuthenticationTokenAsHttpOnlyCookie("REFRESH_TOKEN", user.RefreshToken, refreshTokenExpiresAtUtc);
    }
}
