using System.Text.Json;
using Confluent.Kafka;

namespace SecureChat.Broker;

public class KafkaSerialization
{
    public class JsonSerializer<T> : ISerializer<T>
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public byte[] Serialize(T data, SerializationContext context)
        {
            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(data, _options);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to serialize message", ex);
            }
        }
    }
    
    public class JsonDeserializer<T> : IDeserializer<T>
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(data, _options) ?? 
                       throw new InvalidOperationException("Deserialized message is null");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to deserialize message", ex);
            }
        }
    }
}