using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace PcAudioRecorder
{
    public partial class MainWindow : Window
    {
        private readonly AudioRecorder _recorder = new AudioRecorder();
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly AppLogger _logger;
        private AppSettings _settings;
        private TimeSpan _elapsed = TimeSpan.Zero;
        private string? _currentOutputPath;
        private bool _isInitializing = true;

        public MainWindow()
        {
            InitializeComponent();

            _logger = new AppLogger(Dispatcher);
            LogList.ItemsSource = _logger.Entries;
            _settings = AppSettings.Load();

            InitializeUI();
            SetupRecorderEvents();

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _logger.Info("アプリを起動しました。");
        }

        private void InitializeUI()
        {
            OutputDirText.Text = _settings.OutputDirectory;
            FileNameText.Text = RecordingFileService.GenerateFileName(GetExtension());
            LoadDeviceList();

            FormatCombo.SelectedIndex = _settings.OutputFormat switch
            {
                "MP3" when _settings.Mp3Bitrate == 128 => 1,
                "MP3" when _settings.Mp3Bitrate == 192 => 2,
                "MP3" when _settings.Mp3Bitrate == 320 => 3,
                _ => 0
            };

            _isInitializing = false;
        }

        private void LoadDeviceList()
        {
            var devices = AudioDeviceService.GetRenderDevices(_logger);
            DeviceCombo.ItemsSource = devices;

            var selectedDevice = devices.FirstOrDefault(device => device.Id == _settings.SelectedRenderDeviceId);
            if (selectedDevice == null)
            {
                if (!string.IsNullOrWhiteSpace(_settings.SelectedRenderDeviceId))
                {
                    _logger.Warn("保存されていた再生デバイスが見つかりません。既定の再生デバイスを選択します。");
                    _settings.SelectedRenderDeviceId = null;
                }

                selectedDevice = devices.First();
            }

            DeviceCombo.SelectedItem = selectedDevice;
        }

        private void SetupRecorderEvents()
        {
            _recorder.VolumeChanged += vol =>
            {
                Dispatcher.Invoke(() => VolumeBar.Value = Math.Min(vol * 3.0, 1.0));
            };

            _recorder.StatusChanged += msg =>
            {
                Dispatcher.Invoke(() => StatusText.Text = msg);
            };

            _recorder.RecordingStopped += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    _timer.Stop();
                    SetButtonState(false);
                    VolumeBar.Value = 0;
                    _logger.Info("録音を保存しました: " + (_currentOutputPath ?? "保存先不明"));
                    _currentOutputPath = null;
                });
            };

            _recorder.ErrorOccurred += ex =>
            {
                Dispatcher.Invoke(() =>
                {
                    _timer.Stop();
                    SetButtonState(false);
                    VolumeBar.Value = 0;
                    StatusText.Text = "エラー: " + ex.Message;
                    _logger.Error("録音処理でエラーが発生しました。", ex);
                    System.Windows.MessageBox.Show(
                        "エラーが発生しました:\n" + ex.Message,
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            };
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var outputDirectory = OutputDirText.Text;
                RecordingFileService.ValidateOutputDirectory(outputDirectory);

                var outputPath = RecordingFileService.BuildSafeOutputPath(
                    outputDirectory,
                    FileNameText.Text,
                    GetExtension());

                _elapsed = TimeSpan.Zero;
                TimerText.Text = "00:00:00";
                SetButtonState(true);

                var options = new RecordingOptions
                {
                    OutputFilePath = outputPath,
                    OutputFormat = _settings.OutputFormat,
                    Mp3Bitrate = _settings.Mp3Bitrate,
                    RenderDeviceId = (DeviceCombo.SelectedItem as RenderDeviceInfo)?.Id
                };

                _currentOutputPath = outputPath;
                _logger.Info($"録音を開始します: {outputPath}");
                _recorder.Start(options);
                _timer.Start();

                StatusText.Text = "録音中: " + Path.GetFileName(outputPath);
                FileNameText.Text = RecordingFileService.GenerateFileName(GetExtension());
            }
            catch (Exception ex)
            {
                SetButtonState(false);
                StatusText.Text = "録音開始エラー: " + ex.Message;
                _logger.Error("録音を開始できませんでした。", ex);
                System.Windows.MessageBox.Show(
                    "録音を開始できませんでした:\n" + ex.Message,
                    "エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.Info("録音停止を要求しました。");
            _recorder.Stop();
            StopButton.IsEnabled = false;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "録音ファイルの保存先を選択してください",
                SelectedPath = OutputDirText.Text,
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                OutputDirText.Text = dialog.SelectedPath;
                _settings.OutputDirectory = dialog.SelectedPath;
                _settings.Save();
                _logger.Info("保存先を変更しました: " + dialog.SelectedPath);
            }
        }

        private void DeviceCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            _settings.SelectedRenderDeviceId = (DeviceCombo.SelectedItem as RenderDeviceInfo)?.Id;
            _settings.Save();
            _logger.Info("録音対象を変更しました: " + ((DeviceCombo.SelectedItem as RenderDeviceInfo)?.Name ?? "既定の再生デバイス"));
        }

        private void FormatCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (FormatCombo.SelectedItem is not System.Windows.Controls.ComboBoxItem item)
                return;

            var tag = item.Tag?.ToString() ?? "WAV";
            if (tag == "WAV")
            {
                _settings.OutputFormat = "WAV";
                _settings.Mp3Bitrate = 192;
            }
            else
            {
                _settings.OutputFormat = "MP3";
                _settings.Mp3Bitrate = tag switch
                {
                    "MP3_128" => 128,
                    "MP3_320" => 320,
                    _ => 192
                };
            }

            _settings.Save();

            if (FileNameText != null)
                FileNameText.Text = RecordingFileService.GenerateFileName(GetExtension());

            if (!_isInitializing)
                _logger.Info($"保存形式を変更しました: {_settings.OutputFormat} {(_settings.OutputFormat == "MP3" ? _settings.Mp3Bitrate + "kbps" : string.Empty)}");
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _elapsed = _elapsed.Add(TimeSpan.FromSeconds(1));
            TimerText.Text = _elapsed.ToString(@"hh\:mm\:ss");
        }

        private void SetButtonState(bool isRecording)
        {
            RecordButton.IsEnabled = !isRecording;
            StopButton.IsEnabled = isRecording;
            FormatCombo.IsEnabled = !isRecording;
            DeviceCombo.IsEnabled = !isRecording;
            BrowseButton.IsEnabled = !isRecording;
            FileNameText.IsEnabled = !isRecording;

            TimerText.Foreground = isRecording
                ? System.Windows.Media.Brushes.OrangeRed
                : System.Windows.Media.Brushes.White;
        }

        private string GetExtension()
        {
            return _settings.OutputFormat == "MP3" ? "mp3" : "wav";
        }

        protected override void OnClosed(EventArgs e)
        {
            _recorder.Dispose();
            _settings.Save();
            _logger.Info("アプリを終了しました。");
            base.OnClosed(e);
        }
    }
}
