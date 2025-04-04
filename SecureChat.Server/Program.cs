using System.Text;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureChat.Broker;
using SecureChat.Common.Models;
using SecureChat.Database;
using SecureChat.Server.Interfaces;
using SecureChat.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<SecureChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("SecureChat.Database")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:3001")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true));
});

builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddSingleton<IProducer<int, ChatMessageEvent>>(_ =>
    new ProducerBuilder<int, ChatMessageEvent>(new ProducerConfig
        {
            BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
            CompressionType = CompressionType.Gzip,
            Acks = Acks.All
        })
        .SetValueSerializer(new KafkaProducer.JsonSerializer<ChatMessageEvent>())
        .Build());

builder.Services.AddSingleton<IConsumer<int, ChatMessageEvent>>(_ =>
    new ConsumerBuilder<int, ChatMessageEvent>(new ConsumerConfig
        {
            BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
            GroupId = "chat-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        })
        .SetValueDeserializer(new KafkaProducer.JsonDeserializer<ChatMessageEvent>())
        .Build());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();