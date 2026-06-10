namespace PcAudioRecorder
{
    public class RecordingOptions
    {
        public required string OutputFilePath { get; init; }
        public required string OutputFormat { get; init; }
        public int Mp3Bitrate { get; init; } = 192;
        public string? RenderDeviceId { get; init; }
    }
}
