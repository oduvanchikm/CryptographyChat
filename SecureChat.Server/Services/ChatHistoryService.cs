using Confluent.Kafka;
using SecureChat.Broker;
using SecureChat.Common.Models;

namespace SecureChat.Server.Services;

public class ChatHistoryService
{
    private readonly IConfiguration _configuration;
    private readonly IDeserializer<ChatMessageEvent> _deserializer;

    public ChatHistoryService(
        IConfiguration configuration,
        KafkaSerialization.JsonDeserializer<ChatMessageEvent> deserializer)
    {
        _configuration = configuration;
        _deserializer = deserializer;
    }

    public List<ChatMessageEvent> GetChatHistory(int chatId, int maxCount)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = $"chat-history-reader-{chatId}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<int, ChatMessageEvent>(config)
            .SetValueDeserializer(_deserializer)
            .Build();

        var messages = new List<ChatMessageEvent>();
        consumer.Subscribe("chat-messages");

        try
        {
            consumer.Assign(consumer.Assignment.Select(tp => 
                new TopicPartitionOffset(tp, Offset.Beginning)).ToList());

            var startTime = DateTime.UtcNow;
            while (messages.Count < maxCount && (DateTime.UtcNow - startTime).TotalSeconds < 10)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result == null) continue;

                if (result.Message.Value.ChatId == chatId)
                {
                    messages.Add(result.Message.Value);
                }
            }
        }
        finally
        {
            consumer.Close();
        }

        return messages;
    }
}