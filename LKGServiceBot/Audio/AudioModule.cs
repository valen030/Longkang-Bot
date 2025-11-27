using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Discord;
using Discord.Commands;

using Victoria;
using Victoria.Rest;
using Victoria.Rest.Search;

namespace LKGServiceBot.Audio;

public sealed class AudioModule(LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode, AudioService audioService) : ModuleBase<SocketCommandContext>
{

    private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);

    #region Channel Commands

    [Command("Join")]
    [Alias("j", "JOIN", "Join")]
    [Summary(text: "Join the channel.")]
    public async Task JoinAsync()
    {
        try
        {
            await ValidationAsync();
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Leave")]
    [Alias("l", "LEAVE", "Leave")]
    [Summary(text: "Leave the channel.")]
    public async Task LeaveAsync()
    {
        try
        {
            await ValidationAsync(false);

            var voiceState = Context.User as IVoiceState;
            var voiceChannel = voiceState.VoiceChannel;

            await ReplyAsync("I've left the channel");
            await lavaNode.LeaveAsync(voiceChannel);
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    #endregion

    #region Track Commands

    [Command("Play")]
    [Alias("p", "PLAY", "Play")]
    [Summary(text: "Play a song.")]
    public async Task PlayAsync([Remainder] string searchQuery)
    {
        try
        {
            await ValidationAsync();

            var track = await SearchTrack(searchQuery);
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player.Track != null)
            {
                player.GetQueue().Enqueue(track);
                await ReplyAsync($"Added '{track.Title}' to the queue.");
            }
            else
                await player.PlayAsync(lavaNode, track);
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("PlayList")]
    [Alias("pl", "PLAYLIST", "PlayList")]
    [Summary(text: "Play a list of song.")]
    public async Task PlayListAsync([Remainder] string searchQuery)
    {
        try
        {
            await ValidationAsync();

            var count = 0;
            var tracks = await SearchMultiTrack(searchQuery);
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            foreach (var track in tracks)
            {
                if (player.Track == null)
                {
                    await player.PlayAsync(lavaNode, track);
                    player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id); // refresh the 
                }
                else
                {
                    // Add to queue
                    player.GetQueue().Enqueue(track);
                    count++;
                }
            }

            await ReplyAsync($"Total of {count} songs added to the queue.");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Pause"), RequirePlayer]
    [Alias("pa", "PAUSE", "Pause")]
    [Summary(text: "Pause current song.")]
    public async Task PauseAsync()
    {
        try
        {
            await ValidationAsync();

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player.IsPaused && player.Track != null)
            {
                await ReplyAsync("Current track is already paused.");
                return;
            }

            await player.PauseAsync(lavaNode);
            await ReplyAsync($"Track paused : {player.Track.Title}");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Resume"), RequirePlayer]
    [Alias("r", "RESUME", "Resume")]
    [Summary(text: "Resume current song.")]
    public async Task ResumeAsync()
    {
        try
        {
            await ValidationAsync();

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player.Track == null || !player.IsPaused)
            {
                await ReplyAsync("Nothing to resume.");
                return;
            }

            await player.ResumeAsync(lavaNode, player.Track);
            await ReplyAsync($"Current track resumed.");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Stop"), RequirePlayer]
    [Alias("st", "STOP", "Stop")]
    [Summary(text: "Stop current song.")]
    public async Task StopAsync()
    {
        try
        {
            await ValidationAsync();

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player.Track == null)
            {
                await ReplyAsync("Nothing to stop.");
                return;
            }

            await player.StopAsync(lavaNode, player.Track);
            await ReplyAsync("Track stopped.");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Skip"), RequirePlayer]
    [Alias("s", "SKIP", "Skip")]
    [Summary(text: "Play next song.")]
    public async Task SkipAsync()
    {
        try
        {
            await ValidationAsync();

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            await player.StopAsync(lavaNode, player.Track);  // forcibly ends current track

            var (skipped, currenTrack) = await player.SkipAsync(lavaNode); // to update player
            await player.PlayAsync(lavaNode, currenTrack); // starts the next track

            await player.ResumeAsync(lavaNode, currenTrack); // resume it if it was paused
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("PlaySkip"), RequirePlayer]
    [Alias("ps", "PS", "playskip")]
    [Summary(text: "Skip current track and play the music.")]
    public async Task PlaySkipAsync([Remainder] string searchQuery)
    {
        try
        {
            await ValidationAsync();

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            var track = await SearchTrack(searchQuery);

            await player.StopAsync(lavaNode, player.Track); // forcibly ends current track
            await player.PlayAsync(lavaNode, track);
            await player.ResumeAsync(lavaNode, track);
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("CurrentTrack"), RequirePlayer]
    [Alias("ct", "CT", "Ct")]
    [Summary(text: "Current track.")]
    public async Task CurrentTrackAsync()
    {
        try
        {
            await ValidationAsync();

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            var track = player.Track;
            if (track != null)
                await ReplyAsync($"Current Track: {track.Title} | Duration: {track.Duration}");
            else
                await ReplyAsync("No Track Playing.");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Loop")]
    [Alias("loop", "LOOP", "Loop")]
    [Summary(text: "Loop current track.")]
    public async Task LoopAsync()
    {
        try
        {
            await ValidationAsync();

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            var isLoop = !await AudioService.IsLoopAsync(Context.Guild.Id);
            await AudioService.ToggleLoopAsync(Context.Guild.Id);
            await ReplyAsync($"Loop is {(isLoop ? "enabled" : "disabled")}");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    #endregion

    #region Queue Commands

    [Command("Queue")]
    [Alias("q", "QUEUE", "Queue")]
    [Summary(text: "Show the queue.")]
    public async Task QueueAsync()
    {
        try
        {
            await ValidationAsync();

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            if (player.GetQueue().Count == 0)
                await ReplyAsync("No track in the queue");
            else
            {
                var queueSnapshot = player.GetQueue().ToList(); // make a local copy
                var maxLength = 2000;
                var sb = new StringBuilder();
                var count = 1;

                foreach (var item in queueSnapshot)
                {
                    string line = $"{count++} - {item.Title}\n";

                    if (sb.Length + line.Length > maxLength)
                    {
                        await ReplyAsync(sb.ToString());
                        sb.Clear();
                    }

                    sb.Append(line);
                }

                if (sb.Length > 0)
                    await ReplyAsync(sb.ToString());
            }
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Shuffle")]
    [Alias("shuffle", "SHUFFLE", "sf")]
    [Summary(text: "Show the queue.")]
    public async Task ShuffleAsync()
    {
        try
        {
            await ValidationAsync();
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            if (player.GetQueue().Count == 0)
                await ReplyAsync("Nothing in the queue");
            else
            {
                var queueSnapshot = player.GetQueue().ToList(); // make a local copy

                var rng = new Random();
                for (int i = queueSnapshot.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (queueSnapshot[i], queueSnapshot[j]) = (queueSnapshot[j], queueSnapshot[i]);
                }

                // Clear original queue
                var queue = player.GetQueue();
                queue.Clear();

                // Put them back in shuffled order
                foreach (var track in queueSnapshot)
                {
                    if (player.Track == null)
                    {
                        await player.PlayAsync(lavaNode, track);
                        player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id); // refresh the 
                    }
                    else
                    {
                        // Add to queue
                        queue.Enqueue(track);

                    }
                }

                await ReplyAsync($"Queue was shuffled.");
            }
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Clear")]
    [Alias("c", "CLEAR", "Clear")]
    [Summary(text: "Clear the queue.")]
    public async Task ClearAsync()
    {
        try
        {
            await ValidationAsync();
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            if (player.GetQueue().Count == 0)
                await ReplyAsync("Nothing in the queue");
            else
            {
                var queue = player.GetQueue();
                queue.Clear();

                await ReplyAsync("Queue cleared");
            }
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Remove")]
    [Alias("r", "REMOVE", "R")]
    [Summary(text: "Remove a track in the queue.")]
    public async Task RemoveAsync([Remainder] string number)
    {
        try
        {
            await ValidationAsync();
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            if (player.GetQueue().Count == 0)
            {
                await ReplyAsync("Nothing in the queue");
                return;
            }

            if (!int.TryParse(number, out int trackNumber))
            {
                await ReplyAsync("Please enter a valid number.");
                return;
            }

            var index = trackNumber - 1;
            var queue = player.GetQueue();
            if (index < 0 || index >= queue.Count)
            {
                await ReplyAsync("That track number does not exist in the queue!");
                return;
            }

            var removedTrack = queue.ToList()[index];
            player.GetQueue().RemoveAt(index);
            await ReplyAsync($"Removed **#{trackNumber}**: `{removedTrack.Title}` from the queue.");
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    #endregion

    #region Helper Methods

    public async Task ValidationAsync(bool autoJoin = true)
    {
        // must be in VoiceChannel
        var voiceState = Context.User as IVoiceState;
        AudioHelper.IsUserInVoiceChannel(voiceState);
        
        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
        var isConnected = player != null && player.State.IsConnected;

        // join the channel
        if (autoJoin && !isConnected)
        {
            await lavaNode.JoinAsync(voiceState.VoiceChannel);
            await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}!");
            audioService.TextChannels.TryAdd(Context.Guild.Id, Context.Channel.Id);
        }
    }

    public async Task<LavaTrack> SearchTrack(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            throw new Exception("Please provide search terms.");

        searchQuery = AudioHelper.FormatSearchQuery(searchQuery);

        var searchResponse = await lavaNode.LoadTrackAsync(searchQuery);
        if (searchResponse.Type is SearchType.Empty or SearchType.Error || searchResponse.Tracks.Count == 0)
            throw new Exception($"Track not found for `{searchQuery}`.");

        return searchResponse.Tracks.FirstOrDefault();
    }

    public async Task<IReadOnlyCollection<LavaTrack>> SearchMultiTrack(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            throw new Exception("Please provide search terms.");

        searchQuery = AudioHelper.FormatSearchQuery(searchQuery);

        var searchResponse = await lavaNode.LoadTrackAsync(searchQuery);
        if (searchResponse.Type is SearchType.Empty or SearchType.Error || searchResponse.Tracks.Count == 0)
            throw new Exception($"Track not found for `{searchQuery}`.");

        return searchResponse.Tracks;
    }

    #endregion

}
