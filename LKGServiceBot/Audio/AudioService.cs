using Discord;
using Discord.WebSocket;

using LKGServiceBot.Helper;

using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using Victoria;
using Victoria.Rest.Search;
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
        public static readonly ConcurrentDictionary<ulong, bool> GuildLoop = new();

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
            _lavaNode.OnTrackException += OnTrackException;
            _lavaNode.OnTrackStuck += OnTrackStuck;
        }

        private async Task OnTrackStartAsync(TrackStartEventArg arg)
        {
            var players = await _lavaNode.GetPlayersAsync();
            var player = players.FirstOrDefault(p => p.GuildId == arg.GuildId);

            // if nothing is playing but queue has items
            if (player.Track != null && player.GetQueue().Count > 0 && !player.IsPaused)
            {
                await SendAndLogMessageAsync(player.GuildId, string.Format(ConstMessage.TRACK_PLAYING, 
                    GeneralHelper.InlineCode(player.Track.Title)));
            }
        }

        private async Task OnTrackEndAsync(TrackEndEventArg arg)
        {
            try
            {
                var players = await _lavaNode.GetPlayersAsync();
                var player = players.FirstOrDefault(p => p.GuildId == arg.GuildId);

                if (player == null) return;
                var isLoop = await IsLoopAsync(player.GuildId);

                if (isLoop)
                {
                    // Replay the same track
                    var searchResponse = await _lavaNode.LoadTrackAsync(arg.Track.Url);
                    var newTrack = searchResponse.Tracks.FirstOrDefault();
                    await player.PlayAsync(_lavaNode, newTrack);
                }

                // if nothing is playing but queue has items
                if (player.GetQueue().Count > 0 && player.Track == null)
                {
                    await player.SkipAsync(_lavaNode, TimeSpan.FromSeconds(1));
                }
            }
            catch
            {
                // ignore errors or log them
            }

            return;
        }

        private async Task OnTrackException(TrackExceptionEventArg arg)
        {
            var players = await _lavaNode.GetPlayersAsync();
            var player = players.FirstOrDefault(p => p.GuildId == arg.GuildId);

            if (!string.IsNullOrEmpty(arg.Exception.Message))
                await SendAndLogMessageAsync(player.GuildId, $"{arg.Track.Title} throwing an exception. Message : {arg.Exception.Message}");
        }

        private async Task OnTrackStuck(TrackStuckEventArg arg)
        {
            var players = await _lavaNode.GetPlayersAsync();
            var player = players.FirstOrDefault(p => p.GuildId == arg.GuildId);

            await SendAndLogMessageAsync(player.GuildId, $"{arg.Track.Title} was stuck");
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

        #region Loop Management

        /// <summary>
        /// Toggles loop for a guild asynchronously:
        /// - If no record exists, adds it with true
        /// - If record exists, toggles its value
        /// Returns the new state
        /// </summary>
        public static Task<bool> ToggleLoopAsync(ulong guildId)
        {
            return Task.FromResult(
                GuildLoop.AddOrUpdate(
                    guildId,
                    addValue: true,                 // If not found, add with true
                    updateValueFactory: (key, oldValue) => !oldValue // Toggle current value
                )
            );
        }

        /// <summary>
        /// Gets current loop state for a guild asynchronously
        /// </summary>
        public static Task<bool> IsLoopAsync(ulong guildId)
        {
            return Task.FromResult(GuildLoop.TryGetValue(guildId, out var value) && value);
        }

        #endregion
    }
}
