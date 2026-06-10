using System;
using System.IO;
using Xunit;

namespace PcAudioRecorder.Tests
{
    public class AppSettingsTests
    {
        [Fact]
        public void Load_ReturnsDefaultsWhenJsonIsBroken()
        {
            using var temp = new TemporaryDirectory();
            var settingsPath = Path.Combine(temp.Path, "settings.json");
            File.WriteAllText(settingsPath, "{ broken json");

            var settings = AppSettings.Load(settingsPath);

            Assert.Equal("WAV", settings.OutputFormat);
            Assert.Equal(192, settings.Mp3Bitrate);
            Assert.False(string.IsNullOrWhiteSpace(settings.OutputDirectory));
        }

        [Fact]
        public void Load_IgnoresOldUnusedProperties()
        {
            using var temp = new TemporaryDirectory();
            var settingsPath = Path.Combine(temp.Path, "settings.json");
            File.WriteAllText(settingsPath, """
                {
                  "OutputDirectory": "C:\\Music",
                  "OutputFormat": "MP3",
                  "Mp3Bitrate": 320,
                  "SampleRate": 44100,
                  "BitDepth": 16,
                  "SelectedRenderDeviceId": "device-1"
                }
                """);

            var settings = AppSettings.Load(settingsPath);

            Assert.Equal("C:\\Music", settings.OutputDirectory);
            Assert.Equal("MP3", settings.OutputFormat);
            Assert.Equal(320, settings.Mp3Bitrate);
            Assert.Equal("device-1", settings.SelectedRenderDeviceId);
        }

        [Fact]
        public void Save_WritesNormalizedSettings()
        {
            using var temp = new TemporaryDirectory();
            var settingsPath = Path.Combine(temp.Path, "settings.json");
            var settings = new AppSettings
            {
                OutputDirectory = temp.Path,
                OutputFormat = "AAC",
                Mp3Bitrate = 999,
                SelectedRenderDeviceId = " "
            };

            settings.Save(settingsPath);
            var loaded = AppSettings.Load(settingsPath);

            Assert.Equal("WAV", loaded.OutputFormat);
            Assert.Equal(192, loaded.Mp3Bitrate);
            Assert.Null(loaded.SelectedRenderDeviceId);
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PcAudioRecorderTests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, true);
            }
        }
    }
}
