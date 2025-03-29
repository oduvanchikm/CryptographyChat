using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureChat.Common;
using SecureChat.Common.Models;
using SecureChat.Database;
using SecureChat.Server.Protos;
using AuthServiceBase = SecureChat.Server.Protos.AuthService.AuthServiceBase;

namespace SecureChat.Server.Services;

public class AuthService(
    IDbContextFactory<SecureChatDbContext> dbContextFactory,
    IConfiguration configuration,
    ILogger<AuthService> logger) : AuthServiceBase
{
    public override async Task<AuthResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        await using var contextDb = await dbContextFactory.CreateDbContextAsync();

        if (await contextDb.Users.AnyAsync(u => u.Email == request.Email))
        {
            logger.LogError($"User with email {request.Email} already exists.");
            return new AuthResponse { Message = "User was created" };
        }

        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = PasswordHelper.HashPassword(request.Password)
        };

        contextDb.Users.Add(user);
        await contextDb.SaveChangesAsync();

        logger.LogInformation($"User with email {request.Email} has been created.");

        return new AuthResponse { Message = "Registered" };
    }

    public override async Task<AuthResponse> Login(LoginRequest request, ServerCallContext context)
    {
        await using var contextDb = await dbContextFactory.CreateDbContextAsync();

        var user = await contextDb.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            logger.LogError($"User with email {request.Email} not found.");
            return new AuthResponse { Message = "Wrong password or email" };
        }

        var token = GenerateJwtToken(user);

        logger.LogInformation($"User with email {request.Email} has been logged in.");
        return new AuthResponse { Token = token, Message = "Authenticated" };
    }

    private string GenerateJwtToken(User user)
    {
        logger.LogInformation("start generating jwt");
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username)
        };

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        logger.LogInformation("jwt token generated");

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}