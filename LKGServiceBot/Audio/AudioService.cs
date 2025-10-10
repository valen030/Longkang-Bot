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
        }

        private async Task OnTrackStartAsync(TrackStartEventArg arg)
        {
            var players = await _lavaNode.GetPlayersAsync();
            var player = players.FirstOrDefault(p => p.GuildId == arg.GuildId);

            // if nothing is playing but queue has items
            if (player.Track != null && player.GetQueue().Count > 0 && !player.IsPaused)
            {
                await SendAndLogMessageAsync(player.GuildId, $"Now playing: {player.Track.Title}");
            }
        }

        private async Task OnTrackEndAsync(TrackEndEventArg arg)
        {
            try
            {
                // check all connected players
                var players = await _lavaNode.GetPlayersAsync();
                foreach (var player in players)
                {
                    // if nothing is playing but queue has items
                    if (player.GetQueue().Count > 0 && player.Track == null)
                    {
                        await player.SkipAsync(_lavaNode, TimeSpan.FromSeconds(1));
                    }
                }
            }
            catch
            {
                // ignore errors or log them
            }

            return;
        }

        private Task OnPlayerUpdateAsync(PlayerUpdateEventArg arg)
        {
            //_logger.LogInformation("Guild latency: {}", arg.Ping);
            return Task.CompletedTask;
        }

        private Task OnStatsAsync(StatsEventArg arg)
        {
            //_logger.LogInformation("{}", JsonSerializer.Serialize(arg));
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
