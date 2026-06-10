using System;
using System.IO;
using System.Text.Json;

namespace PcAudioRecorder
{
    public class AppSettings
    {
        public string OutputDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public string OutputFormat { get; set; } = "WAV";
        public int Mp3Bitrate { get; set; } = 192;
        public string? SelectedRenderDeviceId { get; set; }

        public static string DefaultSettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PcAudioRecorder",
            "settings.json");

        public static AppSettings Load(string? settingsPath = null)
        {
            var path = settingsPath ?? DefaultSettingsPath;

            try
            {
                if (!File.Exists(path))
                    return new AppSettings();

                var json = File.ReadAllText(path);
                return Normalize(JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings());
            }
            catch (Exception ex)
            {
                AppLogger.Shared?.Warn("設定ファイルを読み込めなかったため、既定設定で起動します。", ex);
                return new AppSettings();
            }
        }

        public void Save(string? settingsPath = null)
        {
            var path = settingsPath ?? DefaultSettingsPath;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonSerializer.Serialize(Normalize(this), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                AppLogger.Shared?.Warn("設定ファイルを保存できませんでした。", ex);
            }
        }

        private static AppSettings Normalize(AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.OutputDirectory))
                settings.OutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            if (settings.OutputFormat != "MP3")
                settings.OutputFormat = "WAV";

            settings.Mp3Bitrate = settings.Mp3Bitrate switch
            {
                128 => 128,
                320 => 320,
                _ => 192
            };

            if (string.IsNullOrWhiteSpace(settings.SelectedRenderDeviceId))
                settings.SelectedRenderDeviceId = null;

            return settings;
        }
    }
}
