using System.Text.Json;

using LKGServiceBot;

using YamlDotNet.RepresentationModel;

namespace LKGBotConfiguration.Helper
{
    public class SettingsHelper
    {
        private readonly string _workerFolder;
        private readonly string _appsettingFile;

        public SettingsHelper(string workerFolder)
        {
            _workerFolder = workerFolder;
            _appsettingFile = Path.Combine(workerFolder, "appsettings.json");
        }

        public ConfigSetting Load()
        {
            if (!File.Exists(_appsettingFile))
            {
                // Return default if file not exist
                return new ConfigSetting();
            }

            string json = File.ReadAllText(_appsettingFile);
            var deserialize = JsonSerializer.Deserialize<AppSettingsWrapper>(json);
            return deserialize.ConfigSetting ?? new ConfigSetting();
        }

        public void Save(ConfigSetting settings)
        {
            if (!File.Exists(_appsettingFile))
                throw new FileNotFoundException("appsettings.json not found.");

            // Load entire JSON
            string json = File.ReadAllText(_appsettingFile);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.Clone();

            // Convert root to mutable dictionary
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(root.GetRawText())
                       ?? new Dictionary<string, object>();

            // Replace only ConfigSetting
            dict["ConfigSetting"] = settings;

            // Serialize back with indentation
            string updatedJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(_appsettingFile, updatedJson);
        }
    }
}
