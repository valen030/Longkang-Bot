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

        /// <summary>
        /// Attempts to parse a time value from a string in various flexible formats.
        /// </summary>
        /// <remarks>If the input is a decimal value less than 1, it is interpreted as hundredths of a
        /// second (e.g., "0.23" becomes 23 seconds). If the input is an integer, it is interpreted as seconds. Standard
        /// time formats with colons (such as "mm:ss" or "hh:mm:ss") are also supported.</remarks>
        /// <param name="input">The input string representing a time value. Supported formats include decimal seconds (e.g., "0.23"),
        /// integer seconds (e.g., "23"), and colon-separated time (e.g., "mm:ss" or "hh:mm:ss"). Leading and trailing
        /// whitespace is ignored.</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="TimeSpan"/> value if parsing succeeded; otherwise,
        /// <see cref="TimeSpan.Zero"/>.</param>
        /// <returns>true if the input string was successfully parsed into a <see cref="TimeSpan"/>; otherwise, false.</returns>
        public static bool TryParseFlexibleTime(string input, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            input = input.Trim();

            // 1. Decimal seconds (e.g., "0.23" is 23 seconds)
            if (input.Contains('.') && double.TryParse(input, out double dbl))
            {
                int minutes = (int)Math.Floor(dbl);
                int seconds = (int)Math.Round((dbl - minutes) * 100); // take fractional part as seconds
                result = new TimeSpan(0, 0, minutes * 60 + seconds);
                return true;
            }

            // 2. Pure integer seconds (e.g., "23")
            if (int.TryParse(input, out int sec))
            {
                result = TimeSpan.FromSeconds(sec);
                return true;
            }

            // 3. Colon-separated mm:ss or hh:mm:ss
            if (TimeSpan.TryParse(input, out TimeSpan ts))
            {
                result = ts;
                return true;
            }

            return false; // invalid
        }
    }
}
