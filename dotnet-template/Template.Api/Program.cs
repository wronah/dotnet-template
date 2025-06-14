using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using Template.Api.Handlers;
using Template.Application.Abstracts;
using Template.Application.Services;
using Template.Domain.Entities;
using Template.Domain.Requests;
using Template.Infrastructure;
using Template.Infrastructure.Options;
using Template.Infrastructure.Processors;
using Template.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.JwtOptionsKey));

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DbConnectionString"));
});

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.local.json");
}

builder.Services.AddScoped<IAuthTokenProcessor, AuthTokenProcessor>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddCookie().AddGoogle(options =>
{
    var clientId = builder.Configuration["Authentication:Google:ClientId"];

    if(clientId == null)
    {
        throw new ArgumentNullException(nameof(clientId));
    }

    var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

    if (clientSecret == null)
    {
        throw new ArgumentNullException(nameof(clientSecret));
    }

    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;


}).AddJwtBearer(options => 
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.JwtOptionsKey).Get<JwtOptions>() ?? throw new ArgumentNullException(nameof(JwtOptions));

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["ACCESS_TOKEN"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Template");
    });
}

app.UseExceptionHandler(_ => { });
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/account/register", async (RegisterRequest request, IAccountService accountService) =>
{
    await accountService.RegisterAsync(request);

    return Results.Ok();
});

app.MapPost("/api/account/login", async (LoginRequest request, IAccountService accountService) =>
{
    await accountService.LoginAsync(request);

    return Results.Ok();
});

app.MapPost("/api/account/refresh", async (HttpContext httpContext, IAccountService accountService) =>
{
    var refreshToken = httpContext.Request.Cookies["REFRESH_TOKEN"];

    await accountService.RefreshTokenAsync(refreshToken);

    return Results.Ok();
});

app.MapGet("/api/account/login/google", ([FromQuery] string returnUrl, LinkGenerator linkGenerator, SignInManager<User> signInManager, HttpContext context) =>
{
    var properties = signInManager.ConfigureExternalAuthenticationProperties(provider: "Google", redirectUrl: linkGenerator.GetPathByName(context, "GoogleLoginCallback") + $"?returnUrl={returnUrl}");

    return Results.Challenge(properties, authenticationSchemes: ["Google"]);
});

app.MapGet("/api/account/login/google/callback", async ([FromQuery] string returnUrl, HttpContext context, IAccountService accountService) =>
{
    var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

    if (!result.Succeeded)
    {
        return Results.Unauthorized();
    }

    await accountService.LoginWithGoogleAsync(result.Principal);

    return Results.Redirect(returnUrl);

}).WithName("GoogleLoginCallback");

app.MapGet("/api/movies", () => Results.Ok(new List<string> { "Matrix" })).RequireAuthorization();

app.Run();
