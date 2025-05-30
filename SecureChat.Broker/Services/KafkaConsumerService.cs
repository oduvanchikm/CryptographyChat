using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureChat.Common.Models;
using SecureChat.Common.ViewModels;
using StackExchange.Redis;

namespace SecureChat.Broker.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConsumer<Null, string> _consumer;
    private readonly string _topic;
    private readonly IDatabase _redisDb;
    private readonly IServer _server;
    private readonly ILogger<KafkaConsumerService> _logger;

    public KafkaConsumerService(IConfiguration configuration,
        IConnectionMultiplexer redis,
        ILogger<KafkaConsumerService> logger)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = "chat-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _logger = logger;
        _topic = configuration["Kafka:Topic"];
        _redisDb = redis.GetDatabase();
        _consumer = new ConsumerBuilder<Null, string>(config).Build();
        _consumer.Subscribe(_topic);
        _server = redis.GetServer(redis.GetEndPoints().First());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = _consumer.Consume(stoppingToken);
            var rawMessage = result.Message.Value;
            
            try
            {
                _logger.LogInformation($"Consumed message: {rawMessage}");

                var message = JsonSerializer.Deserialize<ChatMessageEvent>(rawMessage);
                if (message != null)
                {
                    var redisKey = $"chat:{message.ChatId}:messages";
                    var serialized = JsonSerializer.Serialize(message);

                    await _redisDb.ListRightPushAsync(redisKey, serialized);
                    await _redisDb.ListTrimAsync(redisKey, -100, -1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while consuming Kafka message");
            }

            try
            {
                var deleteEvent = JsonSerializer.Deserialize<ChatDeletedEvent>(rawMessage);
                if (deleteEvent != null)
                {
                    var keysToDelete = new List<string>
                    {
                        $"chat:{deleteEvent.ChatId}:messages",
                        $"chat:{deleteEvent.ChatId}:user:*:publicKey"
                    };

                    foreach (var keyPattern in keysToDelete)
                    {
                        await foreach (var key in _server.KeysAsync(pattern: keyPattern))
                        {
                            await _redisDb.KeyDeleteAsync(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while consuming Kafka message");
            }
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        base.Dispose();
    }
}