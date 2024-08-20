using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PingPong.Tests
{
    public class PingPongTest
    {
        [Fact]
        public async Task Ping_Should_Return_Pong()
        {
            await using var application = new WebApplicationFactory<Program>();
            var client = application.CreateClient();

            var response = await client.PostAsync("/ping", null);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal("pong", content.Trim('"'));
        }
    }
}