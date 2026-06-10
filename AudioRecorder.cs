using System;
using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Lame;
using NAudio.Wave;

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
        private LameMP3FileWriter? _mp3Writer;
        private MMDevice? _activeRenderDevice;
        private string? _outputFilePath;
        private string _format = "WAV";

        public RecordingState State { get; private set; } = RecordingState.Idle;

        public event Action<float>? VolumeChanged;
        public event Action<string>? StatusChanged;
        public event Action? RecordingStopped;
        public event Action<Exception>? ErrorOccurred;

        public void Start(RecordingOptions options)
        {
            if (State != RecordingState.Idle)
                throw new InvalidOperationException("すでに録音中です。");

            _outputFilePath = options.OutputFilePath;
            _format = options.OutputFormat;

            try
            {
                _activeRenderDevice = AudioDeviceService.GetRenderDeviceOrDefault(options.RenderDeviceId, AppLogger.Shared);
                _capture = _activeRenderDevice == null
                    ? new WasapiLoopbackCapture()
                    : new WasapiLoopbackCapture(_activeRenderDevice);

                if (_format == "WAV")
                {
                    _waveWriter = new WaveFileWriter(options.OutputFilePath, _capture.WaveFormat);
                    _capture.DataAvailable += OnDataAvailableWav;
                }
                else
                {
                    _mp3Writer = new LameMP3FileWriter(options.OutputFilePath, _capture.WaveFormat, options.Mp3Bitrate);
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
            if (_waveWriter == null || _capture == null)
                return;

            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            VolumeChanged?.Invoke(CalculateRms(e.Buffer, e.BytesRecorded, _capture.WaveFormat.BitsPerSample));
        }

        private void OnDataAvailableMp3(object? sender, WaveInEventArgs e)
        {
            if (_mp3Writer == null || _capture == null)
                return;

            _mp3Writer.Write(e.Buffer, 0, e.BytesRecorded);
            VolumeChanged?.Invoke(CalculateRms(e.Buffer, e.BytesRecorded, _capture.WaveFormat.BitsPerSample));
        }

        private void OnCaptureStopped(object? sender, StoppedEventArgs e)
        {
            try
            {
                if (e.Exception != null)
                {
                    State = RecordingState.Idle;
                    ErrorOccurred?.Invoke(e.Exception);
                    return;
                }

                FlushAndDisposeWriters();

                State = RecordingState.Idle;
                StatusChanged?.Invoke("保存完了: " + Path.GetFileName(_outputFilePath));
                RecordingStopped?.Invoke();
            }
            catch (Exception ex)
            {
                State = RecordingState.Idle;
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                Cleanup();
            }
        }

        private void FlushAndDisposeWriters()
        {
            _waveWriter?.Flush();
            _waveWriter?.Dispose();
            _waveWriter = null;

            _mp3Writer?.Flush();
            _mp3Writer?.Dispose();
            _mp3Writer = null;
        }

        private static float CalculateRms(byte[] buffer, int bytesRecorded, int bitsPerSample)
        {
            if (bitsPerSample == 32)
            {
                double sum = 0;
                var sampleCount = bytesRecorded / 4;
                for (var i = 0; i + 3 < bytesRecorded; i += 4)
                {
                    var sample = BitConverter.ToSingle(buffer, i);
                    sum += sample * sample;
                }

                return sampleCount > 0 ? (float)Math.Sqrt(sum / sampleCount) : 0f;
            }

            if (bitsPerSample == 16)
            {
                double sum = 0;
                var sampleCount = bytesRecorded / 2;
                for (var i = 0; i + 1 < bytesRecorded; i += 2)
                {
                    var sample = BitConverter.ToInt16(buffer, i);
                    var normalized = sample / 32768.0;
                    sum += normalized * normalized;
                }

                return sampleCount > 0 ? (float)Math.Sqrt(sum / sampleCount) : 0f;
            }

            return 0f;
        }

        private void Cleanup()
        {
            FlushAndDisposeWriters();

            if (_capture != null)
            {
                _capture.DataAvailable -= OnDataAvailableWav;
                _capture.DataAvailable -= OnDataAvailableMp3;
                _capture.RecordingStopped -= OnCaptureStopped;
                _capture.Dispose();
                _capture = null;
            }

            _activeRenderDevice?.Dispose();
            _activeRenderDevice = null;
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
