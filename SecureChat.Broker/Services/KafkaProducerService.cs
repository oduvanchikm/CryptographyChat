using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureChat.Common.Models;

namespace SecureChat.Broker.Services;

public class KafkaProducerService
{
    private readonly IProducer<int, ChatMessageEvent> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    
    public KafkaProducerService(
        IProducer<int, ChatMessageEvent> producer,
        ILogger<KafkaProducerService> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task SendMessage(ChatMessageEvent message)
    {
        try
        {
            var result = await _producer.ProduceAsync("chat-messages", 
                new Message<int, ChatMessageEvent> { Value = message });
            
            _logger.LogInformation($"Message sent to partition {result.Partition}, offset {result.Offset}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Kafka");
        }
    }
}