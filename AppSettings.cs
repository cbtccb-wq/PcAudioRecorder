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
        public int SampleRate { get; set; } = 44100;
        public int BitDepth { get; set; } = 16;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PcAudioRecorder",
            "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* 読み込み失敗時はデフォルト設定を返す */ }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* 保存失敗は無視 */ }
        }
    }
}
