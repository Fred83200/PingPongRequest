using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;

namespace PingPong
{
    public class Program
    {
        private static readonly string databaseId = "PingPongDb";
        private static readonly string containerId = "Pings";
        private static readonly int maxRetries = 5;
        private static readonly TimeSpan delay = TimeSpan.FromSeconds(5);

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Hardcoded Cosmos DB configuration
            string endpointUri = "https://cosmosdb-emulator:8081";
            string primaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            // Configure HttpClient to ignore SSL certificate validation
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            var cosmosClientOptions = new CosmosClientOptions
            {
                HttpClientFactory = () => new HttpClient(httpClientHandler),
                ConnectionMode = ConnectionMode.Gateway // Use Gateway mode for HTTP connections
            };

            // Register Cosmos DB service with retry mechanism
            builder.Services.AddSingleton<CosmosClient>(sp =>
            {
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        Console.WriteLine($"Attempt {attempt + 1} to connect to Cosmos DB Emulator...");
                        return new CosmosClient(endpointUri, primaryKey, cosmosClientOptions);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to connect to Cosmos DB Emulator: {ex.Message}");
                        if (attempt == maxRetries - 1)
                        {
                            throw;
                        }
                        Task.Delay(delay).Wait();
                    }
                }
                throw new Exception("Could not connect to Cosmos DB Emulator after multiple attempts.");
            });

            var app = builder.Build();

            app.MapPost("/ping", async (HttpContext context, CosmosClient cosmosClient) =>
            {
                var userId = Guid.NewGuid().ToString();
                var timestamp = DateTime.UtcNow;

                var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
                var container = await database.Database.CreateContainerIfNotExistsAsync(containerId, "/id");

                // Ensure the id property is set
                var pingRecord = new PingRecord
                {
                    Id = userId,
                    Timestamp = timestamp
                };

                await container.Container.CreateItemAsync(pingRecord, new PartitionKey(pingRecord.Id));

                return Results.Ok(new { message = "pong", userId, timestamp });
            });

            app.Run();
        }
    }

    public class PingRecord
    {
        [JsonProperty("id")] // Add this to ensure the property is serialized correctly
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
