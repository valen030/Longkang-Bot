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

            _socketClient.UserVoiceStateUpdated += UserVoiceStateUpdatedAsync;
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
            //_logger.LogCritical("{}", JsonSerializer.Serialize(arg));
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

        private async Task UserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            if (user.IsBot) return;

            // Get guild
            var guild = before.VoiceChannel?.Guild ?? after.VoiceChannel?.Guild;
            if (guild == null) return;

            // Get the player
            var player = await _lavaNode.TryGetPlayerAsync(guild.Id);
            if (player == null) return;

            // Get bot's current channel
            var botUser = guild.GetUser(_socketClient.CurrentUser.Id);
            var botChannel = botUser?.VoiceChannel;
            if (botChannel == null) return;

            // Check if user left the bot's channel
            if (before.VoiceChannel?.Id != botChannel.Id) return;

            // Count humans excluding bots and the leaving user
            int humans = botChannel.Users.Count(u => !u.IsBot && u.Id != user.Id);
            if (humans > 0)
            {
                // Cancel any existing timer
                if (_disconnectTokens.TryRemove(guild.Id, out var existingCts))
                    existingCts.Cancel();
                return;
            }

            // Start a 1-minute disconnect timer
            var cts = new CancellationTokenSource();

            if (_disconnectTokens.TryAdd(guild.Id, cts))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);

                        // Re-get the bot's current channel after delay
                        botUser = guild.GetUser(_socketClient.CurrentUser.Id);
                        botChannel = botUser?.VoiceChannel;
                        if (botChannel == null) return;

                        // Count humans again
                        humans = guild.Users
                            .Where(u => !u.IsBot)
                            .Count(u => u.VoiceChannel?.Id == botChannel.Id);

                        if (humans == 0)
                        {
                            await SendAndLogMessageAsync(player.GuildId, ConstMessage.LEFT_VOICE_CHANNEL);
                            await _lavaNode.LeaveAsync(botChannel);
                        }
                    }
                    catch (TaskCanceledException) { }
                    finally
                    {
                        _disconnectTokens.TryRemove(guild.Id, out _);
                    }
                });
            }
        }

        #region Loop Management

        /// <summary>
        /// Toggles the loop state for the specified guild asynchronously. If the guild does not have a loop state set,
        /// it is enabled.
        /// </summary>
        /// <param name="guildId">The unique identifier of the guild for which to toggle the loop state.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if looping is
        /// enabled after the toggle; otherwise, <see langword="false"/>.</returns>
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
        /// Asynchronously determines whether loop mode is enabled for the specified guild.
        /// </summary>
        /// <param name="guildId">The unique identifier of the guild to check for loop mode status.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if loop
        /// mode is enabled for the specified guild; otherwise, <see langword="false"/>.</returns>
        public static Task<bool> IsLoopAsync(ulong guildId)
        {
            return Task.FromResult(GuildLoop.TryGetValue(guildId, out var value) && value);
        }

        #endregion
    }
}
