using Grpc.Net.Client;
using SecureChat.Server.Protos;

namespace SecureChat.Client.Services;

public class AuthService : IDisposable
{
    private readonly Server.Protos.AuthService.AuthServiceClient _client;
    private readonly GrpcChannel _channel;
    
    public AuthService(string serverAddress)
    {
        if (string.IsNullOrWhiteSpace(serverAddress))
            throw new ArgumentException("Server address cannot be null or empty", nameof(serverAddress));

        try
        {
            _channel = GrpcChannel.ForAddress(serverAddress);
            _client = new Server.Protos.AuthService.AuthServiceClient(_channel);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create gRPC channel", ex);
        }
    }
    
    public async Task<AuthResult> RegisterAsync(string email, string username, string password)
    {
        try
        {
            var response = await _client.RegisterAsync(new RegisterRequest
            {
                Email = email,
                Username = username,
                Password = password
            });

            return new AuthResult
            {
                Success = response.Message == "Registered",
                Message = response.Message
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _client.LoginAsync(new LoginRequest
            {
                Email = email,
                Password = password
            });

            return new LoginResult
            {
                Success = !string.IsNullOrEmpty(response.Token),
                Token = response.Token,
                Message = response.Message
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string Token { get; set; }
    public string Message { get; set; }
}