using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;

using Victoria;

namespace LKGServiceBot.Helper
{
    public class GeneralHelper
    {
        /// <summary>
        /// Formats a search query string for use with YouTube APIs, returning the input as-is if it is a well-formed
        /// absolute URI.
        /// </summary>
        /// <remarks>Use this method to ensure that search queries are correctly formatted for YouTube API
        /// endpoints that accept either direct URLs or search terms. If the input is not a valid absolute URI, it will
        /// be treated as a search term.</remarks>
        /// <param name="input">The search query or absolute URI to format. If the value is a well-formed absolute URI, it will be returned
        /// unchanged; otherwise, it will be prefixed for YouTube search.</param>
        /// <returns>A string containing either the original absolute URI or the input prefixed with "ytsearch:" for YouTube
        /// search queries.</returns>
        public static string FormatSearchQuery(string input)
        {
            if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
                return input;

            return $"ytsearch:{input}";
        }

        /// <summary>
        /// Determines whether the specified user is currently connected to a voice channel.
        /// </summary>
        /// <param name="voiceState">The voice state representing the user's current voice connection. Cannot be null.</param>
        /// <returns>true if the user is connected to a voice channel; otherwise, false.</returns>
        public static bool IsUserInVoiceChannel(IVoiceState voiceState)
        {
            return voiceState?.VoiceChannel != null;
        }

        /// <summary>
        /// Formats the specified text as bold using Markdown syntax.
        /// </summary>
        /// <param name="text">The text to format as bold. If null or empty, the result will contain only the Markdown bold markers.</param>
        /// <returns>A string containing the input text surrounded by double asterisks (**), suitable for Markdown bold
        /// formatting.</returns>
        public static string Bold(string text)
        {
            return $"**{text}**";
        }

        /// <summary>
        /// Formats the specified text with Markdown italic syntax.
        /// </summary>
        /// <param name="text">The text to be formatted as italic. If null or empty, the result will contain only the Markdown italic
        /// markers.</param>
        /// <returns>A string containing the input text wrapped in asterisks for Markdown italic formatting.</returns>
        public static string Italic(string text)
        {
            return $"*{text}*";
        }

        /// <summary>
        /// Returns the specified text surrounded by double underscores, commonly used to indicate underlined text in
        /// Markdown formatting.
        /// </summary>
        /// <remarks>This method does not validate or escape the input text. In Markdown, double
        /// underscores are interpreted as underlined or bold text depending on the renderer.</remarks>
        /// <param name="text">The text to be underlined. If null, the method returns "____".</param>
        /// <returns>A string containing the input text wrapped with double underscores. For example, passing "example" returns
        /// "__example__".</returns>
        public static string Underline(string text)
        {
            return $"__{text}__";
        }

        /// <summary>
        /// Formats the specified text as inline code by surrounding it with backticks.
        /// </summary>
        /// <param name="text">The text to be formatted as inline code. Cannot be null.</param>
        /// <returns>A string containing the input text surrounded by backticks, suitable for use in Markdown or similar markup
        /// languages.</returns>
        public static string InlineCode(string text)
        {
            return $"`{text}`";
        }
    }
}
