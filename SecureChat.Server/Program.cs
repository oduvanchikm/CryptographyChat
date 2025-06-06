using System.Net;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
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

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetValue<string>("Redis:Connection")));

// Services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();

builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<KafkaConsumerService>();

// Kafka Producer
builder.Services.AddSingleton<IProducer<int, ChatMessageEvent>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = "kafka:9092",

        MessageMaxBytes = 10485760,
        CompressionType = CompressionType.Gzip,
        QueueBufferingMaxMessages = 100000,
        QueueBufferingMaxKbytes = 1024000,

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
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "SecureChat.Auth";
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// Migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SecureChatDbContext>();

    var pendingMigrations = dbContext.Database.GetPendingMigrations();

    if (pendingMigrations.Any())
    {
        dbContext.Database.Migrate();
    }
}

app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var kafkaConfig = new AdminClientConfig
{
    BootstrapServers = "kafka:9092"
};

using var adminClient = new AdminClientBuilder(kafkaConfig).Build();

try
{
    var topicSpec = new TopicSpecification
    {
        Name = "chat-messages",
        NumPartitions = 1,
        ReplicationFactor = 1,
        Configs = new Dictionary<string, string>
        {
            { "max.message.bytes", "1048576" }
        }
    };

    await adminClient.CreateTopicsAsync(new[] { topicSpec });
}
catch (CreateTopicsException ex)
{
    if (ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
    {
        Console.WriteLine("Kafka topic 'chat-messages' already exists.");
    }
    else
    {
        Console.WriteLine($"Error creating Kafka topic: {ex.Results[0].Error.Reason}");
        throw;
    }
}

app.Run();