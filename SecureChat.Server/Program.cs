using Microsoft.EntityFrameworkCore;
using SecureChat.Database;
using SecureChat.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SecureChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), 
        b => b.MigrationsAssembly("SecureChat.Database")));

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<GreeterService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();