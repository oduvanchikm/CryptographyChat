using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureChat.Common;
using SecureChat.Common.Models;
using SecureChat.Common.ViewModels;
using SecureChat.Database;

namespace SecureChat.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IDbContextFactory<SecureChatDbContext> _dbContextFactory;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IDbContextFactory<SecureChatDbContext> dbContextFactory, ILogger<AuthController> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest registerRequest)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        
        _logger.LogInformation("[ RegisterController ] : Start registration method");

        try
        {
            _logger.LogInformation("[ RegisterController ] : Email " + registerRequest.Email + ", Password " + registerRequest.Password);
        
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await context.Users.AnyAsync(u => u.Email == registerRequest.Email))
            {
                _logger.LogInformation("[ RegisterController ] : Email is already taken");
                return BadRequest("Email is already taken");
            }
        
            string email = registerRequest.Email;
            string password = registerRequest.Password;
            string username = registerRequest.Username;
        
            Console.WriteLine(email);
            Console.WriteLine(email);
            Console.WriteLine(username);
        
            var passwordHash = PasswordHelper.HashPassword(password);

            var newUser = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                Username = username,
            };

            context.Users.Add(newUser);
            await context.SaveChangesAsync();
        
            _logger.LogInformation("[ RegisterController ] : Registration Successful");
            return Ok("Registration Successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest loginRequest)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        
        _logger.LogInformation("[ LoginController ] : Start login method");
        _logger.LogInformation("[ LoginController ] : Email " + loginRequest.Email + ", Password " + loginRequest.Password);
    
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

        if (user == null || user.PasswordHash != PasswordHelper.HashPassword(loginRequest.Password))
        {
            _logger.LogWarning("[ LoginController ] : Authentication failed: Invalid email or password.");
            return Unauthorized();
        }
        
        bool isPasswordValid = user.PasswordHash == PasswordHelper.HashPassword(loginRequest.Password);

        if (!isPasswordValid)
        {
            _logger.LogWarning("[ LoginController ] : Authentication failed: Invalid password.");
            return Unauthorized();
        }
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        _logger.LogInformation("[ LoginController ] : User logged in");
        
        return Ok(new { 
            message = "Login Successful", 
            username = user.Username,
            userId = user.Id
        });
    }
}