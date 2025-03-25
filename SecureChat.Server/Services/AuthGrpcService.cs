// using Grpc.Core;
// using SecureChat.Common.ViewModels;
//
// namespace SecureChat.Server.Services;
//
// public class AuthGrpcService : IAuthServiceBase
// {
//     private readonly AuthService _authService;
//
//     public AuthGrpcService(AuthGrpcService authService)
//     {
//         _authService = authService;
//     }
//
//     public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
//     {
//         var success = await _authService.RegisterAsync(request.Username, request.Password);
//         return new RegisterResponse
//         {
//             Success = success,
//             Message = success ? "User registered successfully." : "Username already exists."
//         };
//     }
//
//     public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
//     {
//         var user = await _authService.LoginAsync(request.Username, request.Password);
//         return new LoginResponse
//         {
//             Success = user != null,
//             Message = user != null ? "Login successful." : "Invalid username or password.",
//             UserId = user?.Id ?? 0,
//             Username = user?.Username ?? ""
//         };
//     }
// }
// }