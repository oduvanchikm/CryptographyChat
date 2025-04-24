using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureChat.Common.Models;

namespace SecureChat.Broker.Services;

public class KafkaProducerService
{
    private readonly IProducer<Null, ChatMessageEvent> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    
    public KafkaProducerService(
        IConfiguration configuration,
        KafkaSerialization.JsonSerializer<ChatMessageEvent> serializer,
        ILogger<KafkaProducerService> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            MessageTimeoutMs = 5000,
            Acks = Acks.All
        };
        
        _logger = logger;
        _producer = new ProducerBuilder<Null, ChatMessageEvent>(config)
            .SetValueSerializer(serializer)
            .Build();
    }

    public async Task SendMessage(ChatMessageEvent message)
    {
        try
        {
            var result = await _producer.ProduceAsync("chat-messages", 
                new Message<Null, ChatMessageEvent> { Value = message });
            
            _logger.LogInformation($"Message sent to partition {result.Partition}, offset {result.Offset}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Kafka");
        }
    }
}