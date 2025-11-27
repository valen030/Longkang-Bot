using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;

using Victoria;

namespace LKGServiceBot.Audio
{
    public class AudioHelper
    {
        public static string FormatSearchQuery(string input)
        {
            if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
            {
                return input;
            }
            return $"ytsearch:{input}";
        }

        public static void IsUserInVoiceChannel(IVoiceState voiceState)
        {
            if (voiceState?.VoiceChannel == null)
                throw new Exception("You must be connected to a voice channel to execute command!");
        }
    }
}
