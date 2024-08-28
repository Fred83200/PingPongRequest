using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Microsoft.Azure.Cosmos;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Linq;
using Moq.Protected;

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

                // Remove the existing HttpClient registration for PingService
                var httpClientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHttpClientFactory));
                if (httpClientDescriptor != null)
                {
                    services.Remove(httpClientDescriptor);
                }

                // Create a mock CosmosClient
                var mockCosmosClient = new Mock<CosmosClient>();

                // Mock the database and container interactions
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

                // Mock the CreateItemAsync and UpsertItemAsync methods
                var mockItemResponse = new Mock<ItemResponse<PingRecord>>();
                mockItemResponse.SetupGet(r => r.Resource).Returns(new PingRecord { Id = Guid.NewGuid().ToString(), Timestamp = DateTime.UtcNow, Pong = "pong" });

                mockContainer
                    .Setup(c => c.CreateItemAsync(It.IsAny<PingRecord>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockItemResponse.Object);

                mockContainer
                    .Setup(c => c.UpsertItemAsync(It.IsAny<PingRecord>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(mockItemResponse.Object);

                // Register the mock CosmosClient
                services.AddSingleton(mockCosmosClient.Object);

                // Mock HttpClient for PingService
                var mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

                mockHttpMessageHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>()
                    )
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("pong"),
                    });

                var mockHttpClientFactory = new Mock<IHttpClientFactory>();
                mockHttpClientFactory
                    .Setup(_ => _.CreateClient(It.IsAny<string>()))
                    .Returns(new HttpClient(mockHttpMessageHandler.Object));

                // Register the mocked HttpClientFactory
                services.AddSingleton(mockHttpClientFactory.Object);
            });
        }
    }

    public class PingPongTest : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public PingPongTest(CustomWebApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
        }

        [Fact]
        public async Task Ping_Should_Return_Pong_With_Valid_UserId_And_Timestamp()
        {
            var client = _factory.CreateClient();

            var response = await client.PostAsync("/ping", null);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            // Deserialize the response content
            var result = JsonConvert.DeserializeObject<PingResponse>(content);

            Assert.NotNull(result);
            Assert.Equal("pong", result.Message);
            Assert.False(string.IsNullOrEmpty(result.UserId));
            Assert.True(result.Timestamp > DateTime.MinValue);

            _output.WriteLine($"Ping returned: {result.Message}, UserId: {result.UserId}, Timestamp: {result.Timestamp}");
        }
    }

    public class PingResponse
    {
        public string Message { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
