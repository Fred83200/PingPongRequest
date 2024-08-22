using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;

namespace PingPong
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register Cosmos DB service
            builder.Services.AddSingleton<CosmosClient>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var endpointUri = configuration["CosmosDb__EndpointUri"];
                var primaryKey = configuration["CosmosDb__PrimaryKey"];
                return new CosmosClient(endpointUri, primaryKey);
            });

            var app = builder.Build();

            app.MapPost("/ping", () => Results.Ok("pong"));

            app.Run();
        }
    }
}
