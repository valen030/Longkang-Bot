using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Discord;
using Discord.Commands;

using LKGServiceBot.Helper;

using Victoria;
using Victoria.Rest;
using Victoria.Rest.Search;

namespace LKGServiceBot.Audio;

public sealed class AudioModule(LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode, AudioService audioService,
    CommandService command) : ModuleBase<SocketCommandContext>
{
    #region Channel Commands

    [Command("Join")]
    [Alias("j")]
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
    [Alias("quit", "go")]
    [Summary(text: "Leave the channel.")]
    public async Task LeaveAsync()
    {
        try
        {
            if (!await ValidationAsync(false)) return;

            var voiceState = Context.User as IVoiceState;
            var voiceChannel = voiceState.VoiceChannel;

            await ReplyAsync(ConstMessage.LEFT_VOICE_CHANNEL);
            await lavaNode.LeaveAsync(voiceChannel);
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Help")]
    [Alias("h", "Command")]
    [Summary("List all the commands.")]
    public async Task HelpAsync()
    {
        try
        {
            if (!await ValidationAsync()) return;

            var builder = new EmbedBuilder()
            .WithTitle("HELP")
            .WithColor(Color.Blue);

            foreach (var module in command.Modules)
            {
                string description = "";

                foreach (var cmd in module.Commands)
                {
                    // Check if command is valid in the current context (permissions etc)
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (!result.IsSuccess) continue;

                    // Command name
                    string name = $"{cmd.Name}";

                    // Aliases
                    if (cmd.Aliases.Count > 1)
                    {
                        var aliases = string.Join(", ", cmd.Aliases);
                        name += $" ({GeneralHelper.InlineCode(aliases)})";
                    }

                    // Summary (if exists)
                    string summary = cmd.Summary ?? "No description";

                    description += $"{GeneralHelper.Bold(name)} — {summary}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                    builder.AddField("List Of Commands", description);
            }

            await ReplyAsync(embed: builder.Build());
        }
        catch (Exception ex)
        {
            await ReplyAsync(ex.Message);
        }
    }

    #endregion

    #region Track Commands

    [Command("Play")]
    [Alias("p")]
    [Summary(text: "Play a song.")]
    public async Task PlayAsync([Remainder] string searchQuery)
    {
        try
        {
            if (!await ValidationAsync()) return;

            var track = await SearchTrack(searchQuery);
            if (track == null) return;

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player.Track != null)
            {
                player.GetQueue().Enqueue(track);
                await ReplyAsync(string.Format(ConstMessage.TRACK_ADDED_TO_QUEUE, 
                    GeneralHelper.InlineCode(track.Title)));
            }
            else
                await player.PlayAsync(lavaNode, track);
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Playlist")]
    [Alias("pl")]
    [Summary(text: "Play a list of song.")]
    public async Task PlayListAsync([Remainder] string searchQuery)
    {
        try
        {
            if (!await ValidationAsync()) return;

            var count = 0;
            var tracks = await SearchMultiTrack(searchQuery);
            if (tracks == null) return;

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

            await ReplyAsync(string.Format(ConstMessage.QUEUE_TOTAL, count));
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Pause")]
    [Alias("pa", "wait")]
    [Summary(text: "Pause current song.")]
    public async Task PauseAsync()
    {
        try
        {
            if (!await ValidationAsync()) return;

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if ((player.IsPaused && player.Track != null) || player.Track == null)
            {
                await ReplyAsync(string.Format(ConstMessage.NOTHING_ACTION, "pause"));
                return;
            }

            await player.PauseAsync(lavaNode);
            await ReplyAsync(string.Format(ConstMessage.TRACK_PAUSED, 
                GeneralHelper.InlineCode(player.Track.Title)));
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Resume")]
    [Alias("con", "continue")]
    [Summary(text: "Resume current song.")]
    public async Task ResumeAsync()
    {
        try
        {
            if (!await ValidationAsync()) return;

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            if (player.Track == null || !player.IsPaused)
            {
                await ReplyAsync(string.Format(ConstMessage.NOTHING_ACTION, "resume"));
                return;
            }

            await player.ResumeAsync(lavaNode, player.Track);
            await ReplyAsync(ConstMessage.TRACK_RESUMED);
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    //[Command("Stop")]
    //[Alias("st")]
    //[Summary(text: "Stop current song.")]
    //public async Task StopAsync()
    //{
    //    try
    //    {
    //        if (!await ValidationAsync()) return;

    //        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
    //        if (player.Track == null)
    //        {
    //            await ReplyAsync(string.Format(ConstMessage.NOTHING_ACTION, "stop"));
    //            return;
    //        }

    //        await player.StopAsync(lavaNode, player.Track);
    //        await ReplyAsync(ConstMessage.TRACK_STOPPED);
    //    }
    //    catch (Exception exception)
    //    {
    //        await ReplyAsync(exception.Message);
    //    }
    //}

    [Command("Skip")]
    [Alias("s", "next")]
    [Summary(text: "Play next song.")]
    public async Task SkipAsync()
    {
        try
        {
            if (!await ValidationAsync()) return;

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

    [Command("PlaySkip")]
    [Alias("ps")]
    [Summary(text: "Skip current track and play the requested track.")]
    public async Task PlaySkipAsync([Remainder] string searchQuery)
    {
        try
        {
            if (!await ValidationAsync()) return;

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            var track = await SearchTrack(searchQuery);
            if (track == null) return;

            await player.StopAsync(lavaNode, player.Track); // forcibly ends current track
            await player.PlayAsync(lavaNode, track);
            await player.ResumeAsync(lavaNode, track);
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Now")]
    [Alias("ct", "current")]
    [Summary(text: "Current track.")]
    public async Task CurrentTrackAsync()
    {
        try
        {
            if (!await ValidationAsync()) return;

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
            var track = player.Track;
            if (track != null)
                await ReplyAsync(string.Format(ConstMessage.TRACK_PLAYING, 
                    GeneralHelper.InlineCode($"{track.Title} | Duration: {track.Duration}")));
            else
                await ReplyAsync(ConstMessage.TRACK_EMPTY);
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Loop")]
    [Alias("cycle")]
    [Summary(text: "Enable or disable loop on current track.")]
    public async Task LoopAsync()
    {
        try
        {
            if (!await ValidationAsync()) return;

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            var isLoop = !await AudioService.IsLoopAsync(Context.Guild.Id);
            await AudioService.ToggleLoopAsync(Context.Guild.Id);
            await ReplyAsync(isLoop ? ConstMessage.TRACK_LOOP_ENABLED : ConstMessage.TRACK_LOOP_DISABLED);
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    #endregion

    #region Queue Commands

    [Command("Queue")]
    [Alias("q", "l", "list")]
    [Summary(text: "Show the queue.")]
    public async Task QueueAsync()
    {
        try
        {
            if (!await ValidationAsync()) return;

            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            if (player.GetQueue().Count == 0)
                await ReplyAsync(ConstMessage.QUEUE_EMPTY);
            else
            {
                var queueSnapshot = player.GetQueue().ToList();
                const int maxDescriptionLength = 4000; // slightly below 4096 to be safe
                var embedBuilder = new EmbedBuilder()
                    .WithTitle("Queue")
                    .WithColor(Color.Blue);

                var sb = new StringBuilder();
                int count = 1;

                foreach (var track in queueSnapshot)
                {
                    string line = $"{count++} - {track.Title}\n";

                    // If adding this line exceeds max description, send the current embed and start a new one
                    if (sb.Length + line.Length > maxDescriptionLength)
                    {
                        embedBuilder.WithDescription(sb.ToString());
                        await ReplyAsync(embed: embedBuilder.Build());

                        sb.Clear();
                        embedBuilder = new EmbedBuilder()
                            .WithTitle("Queue (cont.)")
                            .WithColor(Color.Blue);
                    }

                    sb.Append(line);
                }

                // Send remaining tracks
                if (sb.Length > 0)
                {
                    embedBuilder.WithDescription(sb.ToString());
                    await ReplyAsync(embed: embedBuilder.Build());
                }
            }
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Shuffle")]
    [Alias("mix", "sf")]
    [Summary(text: "Shuffle the queue.")]
    public async Task ShuffleAsync()
    {
        try
        {
            if (!await ValidationAsync()) return;
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            if (player.GetQueue().Count == 0)
                await ReplyAsync(ConstMessage.QUEUE_EMPTY);
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

                await ReplyAsync(ConstMessage.QUEUE_SHUFFLED);
            }
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Clear")]
    [Alias("c")]
    [Summary(text: "Clear the queue.")]
    public async Task ClearAsync()
    {
        try
        {
            if (!await ValidationAsync()) return;
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            if (player.GetQueue().Count == 0)
                await ReplyAsync(ConstMessage.QUEUE_EMPTY);
            else
            {
                var queue = player.GetQueue();
                queue.Clear();

                await ReplyAsync(ConstMessage.QUEUE_CLEARED);
            }
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Remove")]
    [Alias("r")]
    [Summary(text: "Remove a track from the queue.")]
    public async Task RemoveAsync([Remainder] string number)
    {
        try
        {
            if (!await ValidationAsync()) return;
            var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);

            if (player.GetQueue().Count == 0)
            {
                await ReplyAsync(ConstMessage.QUEUE_EMPTY);
                return;
            }

            if (!int.TryParse(number, out int trackNumber))
            {
                await ReplyAsync(ConstMessage.INVALID_NUMBER);
                return;
            }

            var index = trackNumber - 1;
            var queue = player.GetQueue();
            if (index < 0 || index >= queue.Count)
            {
                await ReplyAsync(string.Format(ConstMessage.QUEUE_TRACK_NOT_FOUND, trackNumber));
                return;
            }

            var removedTrack = queue.ToList()[index];
            player.GetQueue().RemoveAt(index);
            await ReplyAsync(string.Format(ConstMessage.QUEUE_TRACK_REMOVED, 
                $"{GeneralHelper.Bold("#" + trackNumber)} : {GeneralHelper.InlineCode(removedTrack.Title)}"));
        }
        catch (Exception exception)
        {
            await ReplyAsync(exception.Message);
        }
    }

    #endregion

    #region Helper Methods

    public async Task<bool> ValidationAsync(bool autoJoin = true)
    {
        // must be in VoiceChannel
        var voiceState = Context.User as IVoiceState;
        if (!GeneralHelper.IsUserInVoiceChannel(voiceState))
        {
            await ReplyAsync(ConstMessage.USER_NOT_IN_VOICE_CHANNEL);
            return false;
        }

        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
        var isConnected = player != null && player.State.IsConnected;

        // join the channel
        if (autoJoin && !isConnected)
        {
            await lavaNode.JoinAsync(voiceState.VoiceChannel);
            await ReplyAsync(string.Format(ConstMessage.JOINED_VOICE_CHANNEL, voiceState.VoiceChannel.Name));
            audioService.TextChannels.TryAdd(Context.Guild.Id, Context.Channel.Id);
        }

        return true;
    }

    public async Task<LavaTrack> SearchTrack(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            await ReplyAsync(ConstMessage.INVALID_SEARCH);
            return null;
        }

        searchQuery = GeneralHelper.FormatSearchQuery(searchQuery);

        var searchResponse = await lavaNode.LoadTrackAsync(searchQuery);
        if (searchResponse.Type is SearchType.Empty or SearchType.Error || searchResponse.Tracks.Count == 0)
        {
            await ReplyAsync(ConstMessage.TRACK_NOT_FOUND);
            return null;
        }


        return searchResponse.Tracks.FirstOrDefault();
    }

    public async Task<IReadOnlyCollection<LavaTrack>> SearchMultiTrack(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            await ReplyAsync(ConstMessage.INVALID_SEARCH);
            return null;
        }

        searchQuery = GeneralHelper.FormatSearchQuery(searchQuery);

        var searchResponse = await lavaNode.LoadTrackAsync(searchQuery);
        if (searchResponse.Type is SearchType.Empty or SearchType.Error || searchResponse.Tracks.Count == 0)
        {
            await ReplyAsync(ConstMessage.TRACK_NOT_FOUND);
            return null;
        }

        return searchResponse.Tracks;
    }

    #endregion

}
