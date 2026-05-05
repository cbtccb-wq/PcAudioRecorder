@echo off
cd /d "%~dp0"

if exist "publish\PcAudioRecorder.exe" goto :run

echo ビルド中です。初回のみ少し時間がかかります...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
if errorlevel 1 (
    echo.
    echo ビルドに失敗しました。.NET 8 SDK がインストールされているか確認してください。
    pause
    exit /b 1
)

:run
start "" "publish\PcAudioRecorder.exe"
