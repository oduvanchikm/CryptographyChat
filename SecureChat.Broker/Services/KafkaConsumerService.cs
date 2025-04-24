using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureChat.Common.Models;
using StackExchange.Redis;

namespace SecureChat.Broker.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConsumer<Null, ChatMessageEvent> _consumer;
    private readonly IDatabase _redisDb;
    private readonly ILogger<KafkaConsumerService> _logger;
    
    public KafkaConsumerService(
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        KafkaSerialization.JsonDeserializer<ChatMessageEvent> deserializer,
        ILogger<KafkaConsumerService> logger)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            GroupId = "chat-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _logger = logger;
        _redisDb = redis.GetDatabase();
        _consumer = new ConsumerBuilder<Null, ChatMessageEvent>(config)
            .SetValueDeserializer(deserializer)
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("chat-messages");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                
                if (result?.Message?.Value == null)
                {
                    _logger.LogWarning("Received null message from Kafka");
                    continue;
                }

                var message = result.Message.Value;
                _logger.LogInformation($"Received message for chat {message.ChatId}");

                var redisKey = $"chat:{message.ChatId}:messages";
                var serialized = JsonSerializer.Serialize(message);
                await _redisDb.ListRightPushAsync(redisKey, serialized);
                await _redisDb.ListTrimAsync(redisKey, -100, -1);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Kafka message");
                await Task.Delay(1000, stoppingToken); // Задержка при ошибках
            }
        }
    }
}