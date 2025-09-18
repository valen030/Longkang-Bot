using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Victoria;
using Victoria.Rest.Search;

namespace LKGServiceBot.Audio;

public sealed class AudioModule(
    LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode,
    AudioService audioService)
    : ModuleBase<SocketCommandContext> {
    private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);
    
    [Command("Join")]
    public async Task JoinAsync() {
        var voiceState = Context.User as IVoiceState;
        if (voiceState?.VoiceChannel == null) {
            await ReplyAsync("You must be connected to a voice channel!");
            return;
        }
        
        try {
            await lavaNode.JoinAsync(voiceState.VoiceChannel);
            await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}!");
            
            audioService.TextChannels.TryAdd(Context.Guild.Id, Context.Channel.Id);
        }
        catch (Exception exception) {
            await ReplyAsync(exception.ToString());
        }
    }
    
    [Command("Leave")]
    public async Task LeaveAsync() {
        var voiceChannel = (Context.User as IVoiceState).VoiceChannel;
        if (voiceChannel == null) {
            await ReplyAsync("Not sure which voice channel to disconnect from.");
            return;
        }
        
        try {
            await lavaNode.LeaveAsync(voiceChannel);
            await ReplyAsync($"I've left {voiceChannel.Name}!");
        }
        catch (Exception exception) {
            await ReplyAsync(exception.Message);
        }
    }
    
    [Command("Play")]
    public async Task PlayAsync([Remainder] string searchQuery) {
        if (string.IsNullOrWhiteSpace(searchQuery)) {
            await ReplyAsync("Please provide search terms.");
            return;
        }
        
        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
        if (player == null) {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null) {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }
            
            try {
                player = await lavaNode.JoinAsync(voiceState.VoiceChannel);
                await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}!");
                audioService.TextChannels.TryAdd(Context.Guild.Id, Context.Channel.Id);
            }
            catch (Exception exception) {
                await ReplyAsync(exception.Message);
            }
        }
        
        var searchResponse = await lavaNode.LoadTrackAsync(searchQuery);
        if (searchResponse.Type is SearchType.Empty or SearchType.Error) {
            await ReplyAsync($"I wasn't able to find anything for `{searchQuery}`.");
            return;
        }
        
        var track = searchResponse.Tracks.FirstOrDefault();
        if (player.Track != null)
        {
            player.GetQueue().Enqueue(track);
            await ReplyAsync($"Added {track.Title} to queue.");
        }
        else
        {
            await player.PlayAsync(lavaNode, track);
            return;
        }
    }

    [Command("PlayList")]
    public async Task PlayListAsync([Remainder] string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            await ReplyAsync("Please provide search terms.");
            return;
        }

        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
        if (player == null)
        {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            try
            {
                player = await lavaNode.JoinAsync(voiceState.VoiceChannel);
                await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}!");
                audioService.TextChannels.TryAdd(Context.Guild.Id, Context.Channel.Id);
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        var searchResponse = await lavaNode.LoadTrackAsync(searchQuery);
        if (searchResponse.Type is SearchType.Empty or SearchType.Error)
        {
            await ReplyAsync($"I wasn't able to find anything for `{searchQuery}`.");
            return;
        }

        var tracks = searchResponse.Tracks;
        var count = 0;

        await ReplyAsync($"Adding {tracks.Count} song added to the queue.");

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

        await ReplyAsync($"Total {count} song added to the queue.");
    }

    [Command("Pause"), RequirePlayer]
    public async Task PauseAsync() {
        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
        if (player.IsPaused && player.Track != null) {
            await ReplyAsync("I cannot pause when I'm not playing anything!");
            return;
        }
        
        try {
            await player.PauseAsync(lavaNode);
            await ReplyAsync($"Paused: {player.Track.Title}");
        }
        catch (Exception exception) {
            await ReplyAsync(exception.Message);
        }
    }
    
    [Command("Resume"), RequirePlayer]
    public async Task ResumeAsync() {
        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
        if (!player.IsPaused && player.Track != null) {
            await ReplyAsync("I cannot resume when I'm not playing anything!");
            return;
        }
        
        try {
            await player.ResumeAsync(lavaNode, player.Track);
            await ReplyAsync($"Resumed: {player.Track.Title}");
        }
        catch (Exception exception) {
            await ReplyAsync(exception.Message);
        }
    }
    
    [Command("Stop"), RequirePlayer]
    public async Task StopAsync() {
        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
        if (!player.State.IsConnected || player.Track == null) {
            await ReplyAsync("Woah, can't stop won't stop.");
            return;
        }
        
        try {
            await player.StopAsync(lavaNode, player.Track);
            await ReplyAsync("No longer playing anything.");
        }
        catch (Exception exception) {
            await ReplyAsync(exception.Message);
        }
    }
    
    [Command("Skip"), RequirePlayer]
    public async Task SkipAsync() {
        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
        if (!player.State.IsConnected) {
            await ReplyAsync("Woaaah there, I can't skip when nothing is playing.");
            return;
        }
        
        try {
            var (skipped, currenTrack) = await player.SkipAsync(lavaNode);
        }
        catch (Exception exception) {
            await ReplyAsync(exception.Message);
        }
    }

    [Command("Queue")]
    public async Task QueueAsync()
    {
        var player = await lavaNode.TryGetPlayerAsync(Context.Guild.Id);
        if (player == null)
        {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await ReplyAsync("You must be connected to a voice channel!");
                return;
            }

            try
            {
                player = await lavaNode.JoinAsync(voiceState.VoiceChannel);
                await ReplyAsync($"Joined {voiceState.VoiceChannel.Name}!");
                audioService.TextChannels.TryAdd(Context.Guild.Id, Context.Channel.Id);
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        if (player.GetQueue().Count == 0)
            await ReplyAsync("Nothing in the queue");
        else
        {
            var queueSnapshot = player.GetQueue().ToList(); // make a local copy
            var maxLength = 2000;
            var sb = new StringBuilder();
            int count = 1;

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

            // send remaining content
            if (sb.Length > 0)
                await ReplyAsync(sb.ToString());
        }
    }
}
