using System.Net;
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

builder.Services.AddSingleton<IProducer<int, ChatMessageEvent>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
        ClientId = Dns.GetHostName(),
        Acks = Acks.All,
        MessageTimeoutMs = 30000,
        RequestTimeoutMs = 10000,
        RetryBackoffMs = 1000,
        SocketTimeoutMs = 60000,
        BrokerAddressFamily = BrokerAddressFamily.V4,
        EnableDeliveryReports = true,
        
        TransactionalId = "secure-chat-producer-1"
    };
    
    var producer = new ProducerBuilder<int, ChatMessageEvent>(config)
        .SetValueSerializer(new KafkaProducer.JsonSerializer<ChatMessageEvent>())
        .Build();

    producer.InitTransactions(TimeSpan.FromSeconds(10));
    return producer;
});

builder.Services.AddSingleton<IConsumer<int, ChatMessageEvent>>(_ =>
    new ConsumerBuilder<int, ChatMessageEvent>(new ConsumerConfig
        {
            BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
            GroupId = builder.Configuration["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            SocketTimeoutMs = 60000,
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 300000,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            AllowAutoCreateTopics = true
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