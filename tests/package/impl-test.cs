using Xunit;
using System.Net.Http;
using System.Threading.Tasks;

public class PingPongTest
{
    [Fact]
    public async Task Ping_Should_Return_Pong()
    {
        await using var application = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();
        var client = application.CreateClient();

        var response = await client.PostAsync("/ping", null);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal("Pong", content);
    }
}