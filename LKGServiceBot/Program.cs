using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LKGServiceBot;
using LKGServiceBot.Audio;
using System.Net;
using Victoria;

var builder = Host.CreateApplicationBuilder(args);

// Configure Discord client
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
}));

// Register Victoria and services
builder.Services
    .AddSingleton<CommandService>()
    .AddLavaNode(x =>
    {
        x.SelfDeaf = true;
        x.Hostname = "127.0.0.1";
        x.Port = 2333;
        x.Authorization = "youshallnotpass";
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
