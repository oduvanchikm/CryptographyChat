using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SecureChat.Broker.Services;
using SecureChat.Common.Models;
using SecureChat.Server.Interfaces;
using SecureChat.Server.Services;
using StackExchange.Redis;
using Test;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationManager();
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var encryptionLogger = loggerFactory.CreateLogger<EncryptionService>();
        var kafkaLogger = loggerFactory.CreateLogger<KafkaProducerService>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Kafka:BootstrapServers", "localhost:9092" },
                { "Kafka:Topic", "test-topic" }
            }!)
            .Build();

        var redisConnection = await ConnectionMultiplexer.ConnectAsync(configuration["Kafka:BootstrapServers"]!);

        var encryptionService = new EncryptionService(redisConnection, encryptionLogger);
        var kafkaProducer = new KafkaProducerService(config, kafkaLogger);

        var chat = new Chats
        {
            Algorithm = "RC5", 
            Padding = "PKCS7",
            ModeCipher = "CBC"
        };

        int chatId = 123;
        int senderId = 42;

        var pubKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await redisConnection.GetDatabase()
            .StringSetAsync($"chat:{chatId}:user:{senderId}:publicKey", pubKey);

        var tester = new EncryptionKafkaRedisTester(
            kafkaProducer,
            redisConnection,
            encryptionService,
            chat,
            chatId,
            senderId
        );

        await tester.RunTestAsync("Hello, this is a test message!");
    }
}
