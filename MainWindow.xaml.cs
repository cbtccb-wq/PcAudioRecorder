using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace PcAudioRecorder
{
    public partial class MainWindow : Window
    {
        private AudioRecorder _recorder = new AudioRecorder();
        private AppSettings _settings = AppSettings.Load();
        private DispatcherTimer _timer = new DispatcherTimer();
        private TimeSpan _elapsed = TimeSpan.Zero;

        public MainWindow()
        {
            InitializeComponent();
            InitializeUI();
            SetupRecorderEvents();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        private void InitializeUI()
        {
            OutputDirText.Text = _settings.OutputDirectory;
            FileNameText.Text = GenerateFileName(_settings.OutputFormat == "WAV" ? "wav" : "mp3");

            // フォーマット選択を設定から復元
            FormatCombo.SelectedIndex = _settings.OutputFormat switch
            {
                "MP3" when _settings.Mp3Bitrate == 128 => 1,
                "MP3" when _settings.Mp3Bitrate == 192 => 2,
                "MP3" when _settings.Mp3Bitrate == 320 => 3,
                _ => 0
            };
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
                    System.Windows.MessageBox.Show("エラーが発生しました:\n" + ex.Message, "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            };
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 出力ディレクトリ確認
                var dir = OutputDirText.Text;
                if (!Directory.Exists(dir))
                {
                    System.Windows.MessageBox.Show("保存先フォルダが見つかりません。", "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ディスク容量確認（最低500MB必要）
                var driveInfo = new DriveInfo(Path.GetPathRoot(dir)!);
                if (driveInfo.AvailableFreeSpace < 500L * 1024 * 1024)
                {
                    StatusText.Text = "警告: ディスク容量が不足しています。";
                }

                string fileName = FileNameText.Text.Trim();
                if (string.IsNullOrEmpty(fileName))
                    fileName = GenerateFileName(GetExtension());
                if (!fileName.EndsWith("." + GetExtension(), StringComparison.OrdinalIgnoreCase))
                    fileName += "." + GetExtension();

                string outputPath = Path.Combine(dir, fileName);

                _elapsed = TimeSpan.Zero;
                TimerText.Text = "00:00:00";
                SetButtonState(true);

                _recorder.Start(outputPath, _settings.OutputFormat, _settings.Mp3Bitrate);
                _timer.Start();

                // 次のデフォルトファイル名を更新
                FileNameText.Text = GenerateFileName(GetExtension());
            }
            catch (Exception ex)
            {
                SetButtonState(false);
                StatusText.Text = "録音開始エラー: " + ex.Message;
                System.Windows.MessageBox.Show("録音を開始できませんでした:\n" + ex.Message, "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _recorder.Stop();
            StopButton.IsEnabled = false;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "録音ファイルの保存先を選択してください",
                SelectedPath = OutputDirText.Text
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputDirText.Text = dialog.SelectedPath;
                _settings.OutputDirectory = dialog.SelectedPath;
                _settings.Save();
            }
        }

        private void FormatCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (FormatCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            {
                var tag = item.Tag?.ToString() ?? "WAV";
                if (tag == "WAV")
                {
                    _settings.OutputFormat = "WAV";
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
                    FileNameText.Text = GenerateFileName(GetExtension());
            }
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
            BrowseButton.IsEnabled = !isRecording;
            FileNameText.IsEnabled = !isRecording;

            if (isRecording)
            {
                TimerText.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
            else
            {
                TimerText.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private string GenerateFileName(string ext)
        {
            return $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
        }

        private string GetExtension()
        {
            return _settings.OutputFormat == "MP3" ? "mp3" : "wav";
        }

        protected override void OnClosed(EventArgs e)
        {
            _recorder.Dispose();
            _settings.Save();
            base.OnClosed(e);
        }
    }
}