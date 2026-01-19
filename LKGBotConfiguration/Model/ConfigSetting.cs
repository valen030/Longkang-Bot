namespace LKGServiceBot
{
    public class AppSettingsWrapper
    {
        public ConfigSetting ConfigSetting { get; set; } = new ConfigSetting();
    }

    public class ConfigSetting
    {
        public string DiscordToken { get; set; }
        public char Prefix { get; set; }
        public string GameStatus { get; set; }
        public string ClientID { get; set; }
    }
}
