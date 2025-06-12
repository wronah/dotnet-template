using Microsoft.EntityFrameworkCore;
using Template.Application.Abstracts;
using Template.Domain.Entities;

namespace Template.Infrastructure.Repositories;
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);

        return user;
    }
}
