using Newtonsoft.Json.Schema;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using NJsonSchema;
using static System.Runtime.InteropServices.JavaScript.JSType;

var factory = new ConnectionFactory { HostName = "rabbitmq" }; //enten local, rabbitmq eller host.docker.internal efter behov
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "add-book_queue", durable: true, exclusive: false,
    autoDelete: false, arguments: null);

await channel.QueueDeclareAsync(queue: "translated-book_queue", durable: true, exclusive: false,
autoDelete: false, arguments: null);

await channel.QueueDeclareAsync(queue: "invalid_queue", durable: true, exclusive: false,
    autoDelete: false, arguments: null);

await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

var schemaJson = File.ReadAllText("add-book-schema.json");
var schema = NJsonSchema.JsonSchema.FromJsonAsync(schemaJson).Result;

Console.WriteLine(" [*] Waiting for messages.");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    try
    {
        // Validate the incoming message against the schema
        var errors = schema.Validate(message);
        if (errors.Count > 0)
        {
            Console.WriteLine(" [!] Validation failed:");
            foreach (var error in errors)
            {
                Console.WriteLine($" - {error.Path}: {error.Kind}");
                string errorReason = $" - {error.Path}: {error.Kind}";

                // Optionally re-publish to a dead-letter or invalid queue for further processing or retrying later
                var errorproperties = new BasicProperties
                {
                    Persistent = true
                };

                var errorBody = Encoding.UTF8.GetBytes(message + errorReason);
                await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "invalid_queue", mandatory: true,
                basicProperties: errorproperties, body: errorBody);

                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);

            }

            return;
        }

        var originalMessage = JsonSerializer.Deserialize<Message>(message);
        Console.WriteLine($" [x] Received: Title={originalMessage.Title}, Author={originalMessage.Author}, ISBN={originalMessage.ISBN}");

        var transformedMessage = new TranslatedMessage
        {
            Title = originalMessage.Title,
            Author = originalMessage.Author,
            ISBN = originalMessage.ISBN
        };

        var transformedMessageJson = JsonSerializer.Serialize(transformedMessage);

        var properties = new BasicProperties
        {
            Persistent = true
        };
        var outputBody = Encoding.UTF8.GetBytes(transformedMessageJson);

        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "translated-book_queue",
            mandatory: true, basicProperties: properties, body: outputBody);

        await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($" [!] Error processing message: {ex.Message}");


        // Optionally, you could try re-publishing the message to the same queue or another retry queue here.
    }
};



await channel.BasicConsumeAsync("add-book_queue", autoAck: false, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();


public class Message
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
}

public class TranslatedMessage
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string ISBN { get; set; }
}

