using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SecureChat.Broker.Services;

public class KafkaProducerService
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            MessageMaxBytes = 10 * 1024 * 1024,
            CompressionType = CompressionType.Snappy
        };

        _logger = logger;
        _topic = configuration["Kafka:Topic"];
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task SendMessage<T>(T message) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        try
        {
            _logger.LogInformation($"Sending message: {json}");
            var send = await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = json });
            _logger.LogInformation($"Message sent to {send.TopicPartitionOffset}");
        }
        catch (ProduceException<Null, string> e)
        {
            _logger.LogError("Kafka send error: {Reason}", e.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}