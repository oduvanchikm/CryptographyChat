using System.Net;
using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SecureChat.Broker;
using SecureChat.Broker.Services;
using SecureChat.Common.Models;
using SecureChat.Database;
using SecureChat.Server.Interfaces;
using SecureChat.Server.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContextFactory<SecureChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true));
});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("Redis:Connection")));

// Services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<KafkaConsumerService>();

// Kafka Producer
builder.Services.AddSingleton<IProducer<int, ChatMessageEvent>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = "kafka:9092",
        ClientId = Dns.GetHostName(),
        Acks = Acks.All,
        MessageTimeoutMs = 30000,

        Debug = "all",
        LogQueue = true,
        LogThreadName = true,

        SocketTimeoutMs = 60000,
        ReconnectBackoffMs = 1000,
        ReconnectBackoffMaxMs = 10000
    };

    var producer = new ProducerBuilder<int, ChatMessageEvent>(config)
        .SetValueSerializer(new KafkaSerialization.JsonSerializer<ChatMessageEvent>())
        .SetLogHandler((_, message) =>
            Console.WriteLine($"Kafka Producer: {message.Level} {message.Facility} {message.Message}"))
        .SetErrorHandler((_, error) =>
            Console.WriteLine($"Kafka Producer Error: {error.Code} {error.Reason}"))
        .Build();

    return producer;
});

// Kafka Consumer
builder.Services.AddSingleton<IConsumer<int, ChatMessageEvent>>(_ =>
    new ConsumerBuilder<int, ChatMessageEvent>(new ConsumerConfig
        {
            BootstrapServers = "kafka:9092",
            GroupId = "secure-chat-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,

            Debug = "all",
            LogQueue = true,
            LogThreadName = true,

            SocketTimeoutMs = 60000,
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 300000
        })
        .SetValueDeserializer(new KafkaSerialization.JsonDeserializer<ChatMessageEvent>())
        .SetLogHandler((_, message) =>
            Console.WriteLine($"Kafka Consumer: {message.Level} {message.Facility} {message.Message}"))
        .SetErrorHandler((_, error) =>
            Console.WriteLine($"Kafka Consumer Error: {error.Code} {error.Reason}"))
        .Build());

// API и аутентификация
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();