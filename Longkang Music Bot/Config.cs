using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LKGMusicBot
{
    public class ConfigModel
    {
        public string ApiKey { get; set; }
        public string DiscordToken { get; set; }
        public char Prefix { get; set; }
        public string DownloadPath { get; set; }
    }

    public class Config
    {
        private static ConfigModel myConfig;

        public static ConfigModel Instance { get { return myConfig; } }

        public static void GetOrCreate()
        {
            myConfig = new ConfigModel
            {
                DiscordToken = "MTAwMzkyODkzNzg3NDg2NjI1OA.GiIYRV.5Ft0Hx_4UnBgRQ2DPCmbtfdVN1skEN0Qoy9Wqk",
                Prefix = '!'
            };
        }
    }
}
