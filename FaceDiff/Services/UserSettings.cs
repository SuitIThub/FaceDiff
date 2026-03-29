using System;
using System.IO;
using System.Text.Json;

namespace FaceDiff.Services
{
    public class UserSettings
    {
        public string BaseFolderPath { get; set; }
        public string ComparisonFolderPath { get; set; }
        public string BaseFilter { get; set; }
        public string RegexPattern { get; set; }
        public double OvalScale { get; set; } = 1.3;
        public string DestinationPath { get; set; }
        public double Threshold { get; set; } = 10;
        public int ViewportWidth { get; set; } = 1920;
        public int ViewportHeight { get; set; } = 1080;

        public string SuppressedUpdateVersion { get; set; }

        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FaceDiff");

        private static readonly string SettingsPath =
            Path.Combine(SettingsDir, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static UserSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new UserSettings();
                }
            }
            catch
            {
            }
            return new UserSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
            }
        }
    }
}
