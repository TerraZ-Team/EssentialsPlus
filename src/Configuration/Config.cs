using System.IO;
using TShockAPI.Configuration;
using TShockAPI;

namespace EssentialsPlus.Configuration
{
    static class Config
    {
        private static readonly string _configFilePath = Path.Combine(TShock.SavePath, "Essentials.json");
        private static ConfigFile<ConfigSettings> _config = new();
        public static ConfigSettings Settings => _config.Settings;

        public static void Save()
        {
            _config.Write(_configFilePath);
        }

        public static void Reload()
        {
            _config.Read(_configFilePath, out bool incomplete);
            if (incomplete)
            {
                Settings.BackPositionHistory = 10;
                Settings.CommandHistory = 10;
                Save();
            }
        }
    }
}
