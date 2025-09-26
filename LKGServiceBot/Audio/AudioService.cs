using Discord;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Text.Json;
using Victoria;
using Victoria.WebSocket.EventArgs;

namespace LKGServiceBot.Audio
{
    public class AudioService
    {
        private readonly LavaNode<LavaPlayer<LavaTrack>, LavaTrack> _lavaNode;
        private readonly DiscordSocketClient _socketClient;
        private readonly ILogger _logger;
        public readonly HashSet<ulong> VoteQueue;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;
        public readonly ConcurrentDictionary<ulong, ulong> TextChannels;

        public AudioService(LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode,
            DiscordSocketClient socketClient,
            ILogger<AudioService> logger)
        {
            _lavaNode = lavaNode;
            _socketClient = socketClient;
            _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
            _logger = logger;
            TextChannels = new ConcurrentDictionary<ulong, ulong>();
            VoteQueue = [];
            _lavaNode.OnWebSocketClosed += OnWebSocketClosedAsync;
            _lavaNode.OnStats += OnStatsAsync;
            _lavaNode.OnPlayerUpdate += OnPlayerUpdateAsync;
            _lavaNode.OnTrackEnd += OnTrackEndAsync;
            _lavaNode.OnTrackStart += OnTrackStartAsync;
            _ = MonitorPlayersAsync();
        }

        private async Task MonitorPlayersAsync()
        {
            while (true)
            {
                try
                {
                    // check all connected players
                    var players = await _lavaNode.GetPlayersAsync();
                    foreach (var player in players)
                    {
                        // if nothing is playing but queue has items
                        if (player.Track == null && player.GetQueue().Count > 0)
                            await player.SkipAsync(_lavaNode);
                    }
                }
                catch
                {
                    // ignore errors or log them
                }
                await Task.Delay(5000);
            }
        }

        private Task OnTrackStartAsync(TrackStartEventArg arg)
        {
            return SendAndLogMessageAsync(arg.GuildId,
                $"Now playing: {arg.Track.Title}");
        }

        private Task OnTrackEndAsync(TrackEndEventArg arg)
        {
            if (arg.Reason == Victoria.Enums.TrackEndReason.Replaced)
                return Task.CompletedTask;

            return SendAndLogMessageAsync(arg.GuildId, $"{arg.Track.Title} ended with reason: {arg.Reason}");
        }

        private Task OnPlayerUpdateAsync(PlayerUpdateEventArg arg)
        {
            _logger.LogInformation("Guild latency: {}", arg.Ping);
            return Task.CompletedTask;
        }

        private Task OnStatsAsync(StatsEventArg arg)
        {
            _logger.LogInformation("{}", JsonSerializer.Serialize(arg));
            return Task.CompletedTask;
        }

        private Task OnWebSocketClosedAsync(WebSocketClosedEventArg arg)
        {
            _logger.LogCritical("{}", JsonSerializer.Serialize(arg));
            return Task.CompletedTask;
        }

        private Task SendAndLogMessageAsync(ulong guildId, string message)
        {
            _logger.LogInformation(message);
            if (!TextChannels.TryGetValue(guildId, out var textChannelId))
            {
                return Task.CompletedTask;
            }

            return (_socketClient
                    .GetGuild(guildId)
                    .GetChannel(textChannelId) as ITextChannel)
                .SendMessageAsync(message);
        }
    }
}
