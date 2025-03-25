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

public class AuthService : AuthServiceBase
{
    private readonly SecureChatDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AuthService(SecureChatDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public override async Task<AuthResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
            return new AuthResponse { Message = "User was created" };

        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = PasswordHelper.HashPassword(request.Password)
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new AuthResponse { Message = "Registered" };
    }
    
    public override async Task<AuthResponse> Login(LoginRequest request, ServerCallContext context)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            return new AuthResponse { Message = "Wrong password or email" };
        
        var token = GenerateJwtToken(user);

        return new AuthResponse { Token = token, Message = "Authenticated" };
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
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

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}