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
        public static string FormatSearchQuery(string input)
        {
            if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
                return input;

            return $"ytsearch:{input}";
        }

        public static bool IsUserInVoiceChannel(IVoiceState voiceState)
        {
            return voiceState?.VoiceChannel != null;
        }

        public static string Bold(string text)
        {
            return $"**{text}**";
        }

        public static string Italic(string text)
        {
            return $"*{text}*";
        }

        public static string Underline(string text)
        {
            return $"__{text}__";
        }

        public static string InlineCode(string text)
        {
            return $"`{text}`";
        }
    }
}
