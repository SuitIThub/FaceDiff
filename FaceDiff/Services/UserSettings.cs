using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FaceDiff.Services
{
    public class UserSettings
    {
        public Dictionary<string, string> TemplateParameters { get; set; }

        public string BaseFolderPath { get; set; }
        public string ComparisonFolderPath { get; set; }
        public string BaseFilter { get; set; }
        public string RegexPattern { get; set; }
        public double OvalScale { get; set; } = 1.3;
        public string DestinationPath { get; set; }
        public double Threshold { get; set; } = 10;
        public bool RequireConfirmPerBaseImage { get; set; }
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
                    var s = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
                    if (s == null)
                        s = new UserSettings();
                    if (s.TemplateParameters == null)
                        s.TemplateParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    return s;
                }
            }
            catch
            {
            }
            return new UserSettings
            {
                TemplateParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
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
