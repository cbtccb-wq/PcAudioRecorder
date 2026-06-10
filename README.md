# PC Audio Recorder

PCで再生中の音声を WASAPI Loopback で録音し、WAV または MP3 形式で保存する Windows デスクトップアプリです。

## 動作環境

- Windows 10 / Windows 11
- .NET 8.0

## 主な機能

- PCで再生中の音声を録音
- 録音対象の再生デバイスを選択
- WAV または MP3 128 / 192 / 320kbps で保存
- 録音時間と音量レベルをリアルタイム表示
- 保存先とファイル名を指定
- 同名ファイルは自動連番で保存
- 履歴表示とログファイル出力
- ウィンドウを常に最前面に表示（切り替え可能）
- 設定を JSON ファイルで保存

## ビルドと実行

```bash
dotnet build
dotnet run --project PcAudioRecorder.csproj
```

## テスト

```bash
dotnet test
```

## 配布用 exe の作成

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

生成された `publish\PcAudioRecorder.exe` を実行すると、.NET ランタイムを別途インストールせずに起動できます。

## デスクトップショートカットの作成

```powershell
powershell -ExecutionPolicy Bypass -File create_shortcut.ps1
```

デスクトップに `PC Audio Recorder` のショートカットを作成します。

## ログと設定

- 設定: `%AppData%\PcAudioRecorder\settings.json`
- ログ: `%AppData%\PcAudioRecorder\logs\app-yyyyMMdd.log`

## 技術構成

- C# + WPF (.NET 8)
- NAudio 2.2.1
- NAudio.Lame 2.1.0
- xUnit

## ライセンス

MIT
