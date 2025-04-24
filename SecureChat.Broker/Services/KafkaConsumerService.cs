using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecureChat.Common.Models;

namespace SecureChat.Broker.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConsumer<int, ChatMessageEvent> _consumer;
    private readonly ILogger<KafkaConsumerService> _logger;
    
    public KafkaConsumerService(
        IConsumer<int, ChatMessageEvent> consumer,
        ILogger<KafkaConsumerService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); 
        
        _consumer.Subscribe("chat-messages");
        
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    if (result?.Message?.Value == null) continue;
                    
                    _logger.LogInformation($"Received message for chat {result.Message.Value.ChatId}");
                }
                catch (ConsumeException e)
                {
                    _logger.LogError(e, $"Consume error: {e.Error.Reason}");
                    if (e.Error.IsFatal)
                    {
                        _consumer.Unsubscribe();
                        await Task.Delay(5000, stoppingToken);
                        _consumer.Subscribe("chat-messages");
                    }
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }
}