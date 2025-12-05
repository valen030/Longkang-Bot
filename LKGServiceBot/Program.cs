using System.Net;

using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using LKGServiceBot;
using LKGServiceBot.Audio;

using Victoria;
using Victoria.WebSocket.Internal;

var builder = Host.CreateApplicationBuilder(args);

// Configure Discord client
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
}));

// Register Victoria and services
builder.Services
    .AddSingleton<CommandService>()
    .AddSingleton(x =>
    {
        var client = x.GetRequiredService<DiscordSocketClient>();
        return new InteractionService(client.Rest);
    })
    .AddLavaNode(x =>
    {
        x.SelfDeaf = true;
        x.Hostname = "127.0.0.1";
        x.Port = 2333;
        x.Authorization = "youshallnotpass";
        x.SocketConfiguration = new WebSocketConfiguration
        {
            BufferSize = 8192
        };
    })
    .AddSingleton<AudioService>()
    .AddLogging(x =>
    {
        x.ClearProviders();
        x.AddConsole();
        x.SetMinimumLevel(LogLevel.Trace);
    })
    .AddHostedService<Worker>();

var host = builder.Build();
host.Run();
