using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using SecureChat.Server.Controllers;
using SecureChat.Server.Interfaces;
using SecureChat.Common.ViewModels;
using SecureChat.Database;
using Microsoft.EntityFrameworkCore;

namespace CryptographyChat.Tests;

public class AuthControllerTests
{
    private AuthController CreateController(DbContextOptions<SecureChatDbContext> options)
    {
        var contextFactoryMock = new Mock<IDbContextFactory<SecureChatDbContext>>();
        contextFactoryMock.Setup(x => x
                .CreateDbContext())
            .Returns(new SecureChatDbContext(options));

        var loggerMock = new Mock<ILogger<AuthController>>();
        var userServiceMock = new Mock<IUserService>();

        return new AuthController(userServiceMock.Object, contextFactoryMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsOk_WhenRegistrationIsSuccessful()
    {
        var option = new DbContextOptionsBuilder<SecureChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var controller = CreateController(option);

        var request = new RegisterRequest
        {
            Email = "test@test.com",
            Password = "test",
            Username = "test"
        };

        var result = await controller.RegisterAsync(request);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Registration Successful", okResult.Value);
    }

    [Fact]
    public async Task LoginAsync_ReturnsUnauthorized_WhenUserNotFound()
    {
        var option = new DbContextOptionsBuilder<SecureChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var controller = CreateController(option);

        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "test"
        };

        var result = await controller.LoginAsync(request);
        Assert.IsType<UnauthorizedResult>(result);
    }
}