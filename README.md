# PC Audio Recorder

PC上で再生されている音声をWASAPI Loopbackで録音し、WAV / MP3形式で保存するWindowsデスクトップアプリです。

## 動作環境
- Windows 10 / Windows 11
- .NET 8.0

## 機能
- 🎙 WASAPI Loopback による再生音の録音
- 💾 WAV（無圧縮PCM）または MP3（128 / 192 / 320kbps）で保存
- ⏱ 録音時間リアルタイム表示
- 📊 音量レベルバー
- 📁 保存先・ファイル名の自由指定
- ⚙️ 設定をJSONファイルで永続化

## ビルド方法

```bash
dotnet build
dotnet run
```

## 配布用 .exe の作成（ダブルクリック起動）

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

生成された `publish\PcAudioRecorder.exe` をダブルクリックすれば .NET インストール不要で起動できます。

### デスクトップショートカットの作成

```powershell
powershell -ExecutionPolicy Bypass -File create_shortcut.ps1
```

デスクトップに「PC Audio Recorder」ショートカットが作成されます。

## 技術構成
- C# + WPF (.NET 8)
- [NAudio](https://github.com/naudio/NAudio) 2.2.1
- [NAudio.Lame](https://github.com/Corey-M/NAudio.Lame) 2.1.0 (MP3エンコード)

## ライセンス
MIT
