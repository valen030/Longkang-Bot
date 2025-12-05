using Discord;
using Discord.Commands;
using Discord.Interactions;

using LKGServiceBot.Helper;

public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly CommandService _commandService;

    public InteractionModule(CommandService commandService)
    {
        _commandService = commandService;
    }

    [SlashCommand("help", "Show all the commands.")]
    public async Task HelpAsync()
    {
        var builder = new EmbedBuilder()
            .WithTitle("HELP")
            .WithColor(Color.Blue);

        foreach (var module in _commandService.Modules)
        {
            string description = "";
            foreach (var cmd in module.Commands)
            {
                string name = cmd.Name;

                if (cmd.Aliases.Count > 1)
                {
                    var aliases = string.Join(", ", cmd.Aliases);
                    name += $" ({GeneralHelper.InlineCode(aliases)})";
                }

                string summary = cmd.Summary ?? "No description";

                description += $"{GeneralHelper.Bold(name)} — {summary}\n";
            }

            if (!string.IsNullOrWhiteSpace(description))
                builder.AddField(GeneralHelper.Underline("List Of Commands"), description);
        }

        // Slash commands must use RespondAsync
        await RespondAsync(embed: builder.Build(), ephemeral: true); // ephemeral: only user sees it
    }
}