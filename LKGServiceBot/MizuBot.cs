using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using Victoria;

namespace LKGServiceBot
{
    public class MizuBot
    {
        private readonly IServiceProvider _services;

        private readonly ConfigSetting _configSetting;
        private readonly DiscordSocketClient _discordClient;
        private readonly CommandService _commands;
        private readonly InteractionService _interactions;
        private readonly LavaNode<LavaPlayer<LavaTrack>, LavaTrack> _lavaNode;

        public MizuBot(DiscordSocketClient discordClient, ConfigSetting configSetting, CommandService commands, InteractionService interactions,
            IServiceProvider services, LavaNode<LavaPlayer<LavaTrack>, LavaTrack> lavaNode)
        {
            _discordClient = discordClient;
            _configSetting = configSetting;
            _commands = commands;
            _interactions = interactions;
            _services = services;
            _lavaNode = lavaNode;
        }

        /// <summary>
        /// Initializes and installs command and interaction modules for the Discord client, enabling message and
        /// interaction handling.
        /// </summary>
        /// <remarks>This method sets up event handlers for message reception, connection state changes,
        /// logging, and interactions. Commands and interactions are discovered and loaded from the entry assembly. The
        /// method performs no action if the Discord client is not logged in.</remarks>
        /// <returns>A task that represents the asynchronous operation of installing commands and event handlers.</returns>
        public async Task InstallCommands()
        {
            // Before we install commands, we should check if everything was set up properly. Check if logged in.
            if (_discordClient.LoginState != LoginState.LoggedIn) return;

            // Hook the MessageReceived Event into our Command Handler
            _discordClient.MessageReceived += MessageReceived;

            // Add tasks to send Messages, and userJoined to appropriate places
            _discordClient.Ready += Ready;
            _discordClient.Connected += Connected;
            _discordClient.Disconnected += Disconnected;
            _discordClient.Log += Log;
            _discordClient.InteractionCreated += InteractionHandler;

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task MessageReceived(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command, based on if it starts with the prefix char or a mention prefix
            if (!(message.HasCharPrefix(_configSetting.Prefix, ref argPos) || message.HasMentionPrefix(_discordClient.CurrentUser, ref argPos)))
            {
                // If it isn't a command, decide what to do with it here. 
                // TODO: Add any special handlers here.
                return;
            }

            // Create a Command Context
            var context = new SocketCommandContext(_discordClient, message);

            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess) // If failed, write error to chat.
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        private async Task Ready()
        {
            await _discordClient.SetGameAsync(_configSetting.GameStatus);
            await _interactions.RegisterCommandsGloballyAsync();

            if (!_lavaNode.IsConnected)
            {
                await _lavaNode.ConnectAsync();
            }
        }

        /// <summary>
        /// This function is used for any client logging.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());

            return Task.CompletedTask;
        }

        /// <summary>
        /// This function is called, when the client is fully connected.
        /// </summary>
        /// <returns></returns>
        private Task Connected()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// This function is called, when the client suddenly disconnects.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task Disconnected(Exception arg)
        {
            return Task.CompletedTask;
        }

        private async Task InteractionHandler(SocketInteraction interaction)
        {
            var ctx = new SocketInteractionContext(_discordClient, interaction);
            await _interactions.ExecuteCommandAsync(ctx, _services);
        }
    }
}
