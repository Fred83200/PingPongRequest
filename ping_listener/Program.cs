using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Explicitly set Kestrel to listen on port 80 as the app service targets this port when sending the request
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80); // Listen on port 80
});

var app = builder.Build();

app.MapGet("/ping", () => "pong");

app.Run();
