using System;
using System.IO;
using System.Threading;
using NAudio.Wave;
using NAudio.Lame;

namespace PcAudioRecorder
{
    public enum RecordingState
    {
        Idle,
        Recording,
        Saving
    }

    public class AudioRecorder : IDisposable
    {
        private WasapiLoopbackCapture? _capture;
        private WaveFileWriter? _waveWriter;
        private MemoryStream? _memoryStream;
        private string? _outputFilePath;
        private string _format = "WAV";
        private int _mp3Bitrate = 192;

        public RecordingState State { get; private set; } = RecordingState.Idle;

        // イベント
        public event Action<float>? VolumeChanged;
        public event Action<string>? StatusChanged;
        public event Action? RecordingStopped;
        public event Action<Exception>? ErrorOccurred;

        public void Start(string outputFilePath, string format, int mp3Bitrate = 192)
        {
            if (State != RecordingState.Idle)
                throw new InvalidOperationException("すでに録音中です。");

            _outputFilePath = outputFilePath;
            _format = format;
            _mp3Bitrate = mp3Bitrate;

            try
            {
                _capture = new WasapiLoopbackCapture();

                if (format == "WAV")
                {
                    _waveWriter = new WaveFileWriter(outputFilePath, _capture.WaveFormat);
                    _capture.DataAvailable += OnDataAvailableWav;
                }
                else // MP3
                {
                    _memoryStream = new MemoryStream();
                    _capture.DataAvailable += OnDataAvailableMp3;
                }

                _capture.RecordingStopped += OnCaptureStopped;
                _capture.StartRecording();
                State = RecordingState.Recording;
                StatusChanged?.Invoke("録音中...");
            }
            catch (Exception ex)
            {
                Cleanup();
                ErrorOccurred?.Invoke(ex);
                throw;
            }
        }

        public void Stop()
        {
            if (State != RecordingState.Recording)
                return;

            State = RecordingState.Saving;
            StatusChanged?.Invoke("保存中...");
            _capture?.StopRecording();
        }

        private void OnDataAvailableWav(object? sender, WaveInEventArgs e)
        {
            if (_waveWriter == null) return;
            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);

            // 音量計算（RMS）
            float rms = CalculateRms(e.Buffer, e.BytesRecorded, _capture!.WaveFormat.BitsPerSample);
            VolumeChanged?.Invoke(rms);
        }

        private void OnDataAvailableMp3(object? sender, WaveInEventArgs e)
        {
            if (_memoryStream == null) return;
            _memoryStream.Write(e.Buffer, 0, e.BytesRecorded);

            float rms = CalculateRms(e.Buffer, e.BytesRecorded, _capture!.WaveFormat.BitsPerSample);
            VolumeChanged?.Invoke(rms);
        }

        private void OnCaptureStopped(object? sender, StoppedEventArgs e)
        {
            try
            {
                if (e.Exception != null)
                {
                    ErrorOccurred?.Invoke(e.Exception);
                    Cleanup();
                    return;
                }

                if (_format == "WAV")
                {
                    _waveWriter?.Flush();
                    _waveWriter?.Dispose();
                    _waveWriter = null;
                }
                else // MP3
                {
                    SaveMp3();
                }

                State = RecordingState.Idle;
                StatusChanged?.Invoke("保存完了: " + Path.GetFileName(_outputFilePath));
                RecordingStopped?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                Cleanup();
            }
        }

        private void SaveMp3()
        {
            if (_memoryStream == null || _capture == null || _outputFilePath == null) return;

            _memoryStream.Position = 0;
            using var rawReader = new RawSourceWaveStream(_memoryStream, _capture.WaveFormat);
            using var mp3Writer = new LameMP3FileWriter(
                _outputFilePath,
                _capture.WaveFormat,
                _mp3Bitrate);

            var buffer = new byte[4096];
            int read;
            while ((read = rawReader.Read(buffer, 0, buffer.Length)) > 0)
                mp3Writer.Write(buffer, 0, read);
        }

        private float CalculateRms(byte[] buffer, int bytesRecorded, int bitsPerSample)
        {
            if (bitsPerSample == 32)
            {
                // 32bit float
                double sum = 0;
                int sampleCount = bytesRecorded / 4;
                for (int i = 0; i < bytesRecorded; i += 4)
                {
                    float sample = BitConverter.ToSingle(buffer, i);
                    sum += sample * sample;
                }
                return sampleCount > 0 ? (float)Math.Sqrt(sum / sampleCount) : 0f;
            }
            else
            {
                // 16bit PCM
                double sum = 0;
                int sampleCount = bytesRecorded / 2;
                for (int i = 0; i < bytesRecorded; i += 2)
                {
                    short sample = BitConverter.ToInt16(buffer, i);
                    double normalized = sample / 32768.0;
                    sum += normalized * normalized;
                }
                return sampleCount > 0 ? (float)Math.Sqrt(sum / sampleCount) : 0f;
            }
        }

        private void Cleanup()
        {
            _waveWriter?.Dispose();
            _waveWriter = null;
            _memoryStream?.Dispose();
            _memoryStream = null;
            _capture?.Dispose();
            _capture = null;
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
