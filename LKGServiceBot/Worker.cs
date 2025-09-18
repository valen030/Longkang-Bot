using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LKGMusicBot;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using Victoria;

namespace LKGServiceBot
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _services;

        private readonly ConfigSetting _configSetting;
        private readonly DiscordSocketClient _discordClient;
        private readonly CommandService _commands;
        private readonly LavaNode<LavaPlayer<LavaTrack>, LavaTrack> _lavaNode;

        public Worker(ILogger<Worker> logger, DiscordSocketClient discordClient, CommandService commands, 
            IServiceProvider services, LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode)
        {
            _logger = logger;
            _discordClient = discordClient;
            _commands = commands;
            _services = services;
            _configSetting = GetConfigSetting();
            _lavaNode = lavaNode;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (!await Server.ServerStartup())
                {
                    _logger.LogInformation("Lavalink Server failed to startup.");
                    return;
                }

                if (_discordClient.ConnectionState == ConnectionState.Connecting || _discordClient.ConnectionState == ConnectionState.Connected)
                {
                    _logger.LogInformation($"Bot is already connecting or connected.");
                    return;
                }
                else
                {
                    await _discordClient.LoginAsync(TokenType.Bot, _configSetting.DiscordToken);
                    await _discordClient.StartAsync(); // Startup the client.
                }

                var bot = new MizuBot(_discordClient, _configSetting, _commands, _services, _lavaNode);
                await bot.InstallCommands();

                var timer = 1000;
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (timer == 1000 && _discordClient.ConnectionState == ConnectionState.Connected)
                        timer = 60000;

                    _logger.LogInformation($"{_discordClient.ConnectionState}");

                    await Task.Delay(timer, stoppingToken);
                }
            }
            finally
            {
                try
                {
                    _logger.LogInformation("Stopping Server...");
                    Server.ServerShutdown();

                    _logger.LogInformation("Stopping Discord client...");
                    if (_discordClient.ConnectionState == ConnectionState.Connected)
                    {
                        await _discordClient.LogoutAsync();
                        await _discordClient.StopAsync();
                    }
                }
                catch { }
            }
        }

        private ConfigSetting GetConfigSetting()
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var appSetting = config.GetSection("ConfigSetting").Get<ConfigSetting>();

                return appSetting ?? new ConfigSetting();
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "Error reading app settings");
                }
                return new ConfigSetting();
            }
        }
    }
}
