using System.Text.Json;
using Confluent.Kafka;

namespace SecureChat.Broker;

public class KafkaProducer
{
    public class JsonSerializer<T> : ISerializer<T>
    {
        public byte[] Serialize(T data, SerializationContext context)
            => JsonSerializer.SerializeToUtf8Bytes(data);
    }

    public class JsonDeserializer<T> : IDeserializer<T>
    {
        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
            => JsonSerializer.Deserialize<T>(data)!;
    }
    
    // builder.Services.AddSingleton<IProducer<int, ChatMessageEvent>>(_ => 
    // new ProducerBuilder<int, ChatMessageEvent>(new ProducerConfig
    // {
    //     BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
    //     CompressionType = CompressionType.Gzip,
    //     Acks = Acks.All
    // })
    // .SetValueSerializer(new KafkaProducer.JsonSerializer<ChatMessageEvent>())
    // .Build());
    //
    // builder.Services.AddSingleton<IConsumer<int, ChatMessageEvent>>(_ =>
    // new ConsumerBuilder<int, ChatMessageEvent>(new ConsumerConfig
    // {
    //     BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
    //     GroupId = "chat-service-group",
    //     AutoOffsetReset = AutoOffsetReset.Earliest,
    //     EnableAutoCommit = false
    // })
    // .SetValueDeserializer(new KafkaProducer.JsonDeserializer<ChatMessageEvent>())
    // .Build());
}