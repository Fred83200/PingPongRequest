using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.Azure.Cosmos;
using Xunit;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Threading;

namespace PingPong.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing CosmosClient registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CosmosClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Create a mock CosmosClient
                var mockCosmosClient = new Mock<CosmosClient>();

                // Mock the database and container interactions here
                var mockDatabase = new Mock<Database>(MockBehavior.Strict);
                var mockContainer = new Mock<Container>(MockBehavior.Strict);

                // Mock the response for CreateDatabaseIfNotExistsAsync
                var mockDatabaseResponse = new Mock<DatabaseResponse>();
                mockDatabaseResponse.Setup(r => r.Database).Returns(mockDatabase.Object);

                mockCosmosClient
                    .Setup(c => c.CreateDatabaseIfNotExistsAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockDatabaseResponse.Object);

                // Mock the response for CreateContainerIfNotExistsAsync
                var mockContainerResponse = new Mock<ContainerResponse>();
                mockContainerResponse.Setup(r => r.Container).Returns(mockContainer.Object);

                mockDatabase
                    .Setup(db => db.CreateContainerIfNotExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<RequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockContainerResponse.Object);

                // Mock the CreateItemAsync method
                var mockItemResponse = new Mock<ItemResponse<PingRecord>>();
                mockItemResponse.SetupGet(r => r.Resource).Returns(new PingRecord { Id = Guid.NewGuid().ToString(), Timestamp = DateTime.UtcNow });

                mockContainer
                    .Setup(c => c.CreateItemAsync(It.IsAny<PingRecord>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockItemResponse.Object);

                // Register the mock CosmosClient
                services.AddSingleton(mockCosmosClient.Object);
            });
        }
    }

    public class PingPongTest
    {
        [Fact]
        public async Task Ping_Should_Return_Pong_With_Valid_UserId_And_Timestamp()
        {
            await using var application = new CustomWebApplicationFactory();
            var client = application.CreateClient();

            var response = await client.PostAsync("/ping", null);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            // Deserialize the response content
            var result = JsonConvert.DeserializeObject<PingResponse>(content);

            Assert.NotNull(result);
            Assert.Equal("pong", result.Message);
            Assert.False(string.IsNullOrEmpty(result.UserId));
            Assert.True(result.Timestamp > DateTime.MinValue);
        }
    }

    public class PingResponse
    {
        public string Message { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
