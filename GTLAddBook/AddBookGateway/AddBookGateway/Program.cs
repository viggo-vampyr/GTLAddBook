using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

var factory = new ConnectionFactory { HostName = "rabbitmq" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();


// Declare a queue for "add-book" operations
await channel.QueueDeclareAsync(queue: "add-book_queue",
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

// Create persistent properties for the message
var properties = new BasicProperties
{
    Persistent = true
};

// Create some book messages (both valid and invalid)
var validBook1 = new { Title = "Clean Code", Author = "Robert C. Martin", ISBN = "978-0132350884" };
var validBook2 = new { Title = "The Pragmatic Programmer", Author = "Andy Hunt", ISBN = "978-0201616224" };

var invalidBook1 = new { Title = "Refactoring", Author = "Martin Fowler" };  // Missing ISBN
var invalidBook2 = new { Title = "Code Complete", Author = 5, ISBN = "978-0735619678" }; // Invalid Author
var invalidBook3 = new { Title = 12, Author = "Kent Beck", ISBN = "978-0134757599" }; // Invalid Title

// Serialize and send the valid books

var bookJson = JsonSerializer.Serialize(validBook1);
var body = Encoding.UTF8.GetBytes(bookJson);
await channel.BasicPublishAsync(exchange: string.Empty,
                                routingKey: "add-book_queue",
                                mandatory: true,
                                basicProperties: properties,
                                body: body);

bookJson = JsonSerializer.Serialize(validBook2);
body = Encoding.UTF8.GetBytes(bookJson);
await channel.BasicPublishAsync(exchange: string.Empty,
                                routingKey: "add-book_queue",
                                mandatory: true,
                                basicProperties: properties,
                                body: body);

// Serialize and send the invalid books

bookJson = JsonSerializer.Serialize(invalidBook1);
body = Encoding.UTF8.GetBytes(bookJson);
await channel.BasicPublishAsync(exchange: string.Empty,
                                routingKey: "add-book_queue",
                                mandatory: true,
                                basicProperties: properties,
                                body: body);

bookJson = JsonSerializer.Serialize(invalidBook2);
body = Encoding.UTF8.GetBytes(bookJson);
await channel.BasicPublishAsync(exchange: string.Empty,
                                routingKey: "add-book_queue",
                                mandatory: true,
                                basicProperties: properties,
                                body: body);

bookJson = JsonSerializer.Serialize(invalidBook3);
body = Encoding.UTF8.GetBytes(bookJson);
await channel.BasicPublishAsync(exchange: string.Empty,
                                routingKey: "add-book_queue",
                                mandatory: true,
                                basicProperties: properties,
                                body: body);



Console.WriteLine(" [x] Sent books to the queue.");



// Method to serialize a book object and send it to the queue
//static async Task SendMessageToQueue(IModel channel, object book, IBasicProperties properties)
//{
//    var bookJson = JsonSerializer.Serialize(book);
//    var body = Encoding.UTF8.GetBytes(bookJson);
//    await channel.BasicPublishAsync(exchange: string.Empty,
//                                    routingKey: "add-book-queue",
//                                    mandatory: true,
//                                    basicProperties: properties,
//                                    body: body);
//}
