using System;
using System.IO;
using Xunit;

namespace PcAudioRecorder.Tests
{
    public class RecordingFileServiceTests
    {
        [Fact]
        public void BuildSafeFileName_UsesGeneratedNameWhenBlank()
        {
            var fileName = RecordingFileService.BuildSafeFileName("   ", "wav");

            Assert.StartsWith("Recording_", fileName);
            Assert.EndsWith(".wav", fileName);
        }

        [Fact]
        public void BuildSafeFileName_ReplacesInvalidPathCharacters()
        {
            var fileName = RecordingFileService.BuildSafeFileName(@"folder\bad:name?.mp3", "mp3");

            Assert.Equal("folder_bad_name.mp3", fileName);
        }

        [Fact]
        public void BuildSafeFileName_AddsSelectedExtension()
        {
            var fileName = RecordingFileService.BuildSafeFileName("meeting", "wav");

            Assert.Equal("meeting.wav", fileName);
        }

        [Fact]
        public void BuildSafeFileName_ReplacesDifferentExtension()
        {
            var fileName = RecordingFileService.BuildSafeFileName("meeting.mp3", "wav");

            Assert.Equal("meeting.wav", fileName);
        }

        [Fact]
        public void GetAvailablePath_AddsSequenceWhenFileExists()
        {
            using var temp = new TemporaryDirectory();
            var firstPath = Path.Combine(temp.Path, "Recording.wav");
            var secondPath = Path.Combine(temp.Path, "Recording_001.wav");
            File.WriteAllText(firstPath, string.Empty);

            var availablePath = RecordingFileService.GetAvailablePath(firstPath);

            Assert.Equal(secondPath, availablePath);
        }

        [Fact]
        public void BuildSafeOutputPath_CombinesSanitizedNameAndSequence()
        {
            using var temp = new TemporaryDirectory();
            File.WriteAllText(Path.Combine(temp.Path, "bad_name.wav"), string.Empty);

            var outputPath = RecordingFileService.BuildSafeOutputPath(temp.Path, "bad:name.mp3", "wav");

            Assert.Equal(Path.Combine(temp.Path, "bad_name_001.wav"), outputPath);
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
