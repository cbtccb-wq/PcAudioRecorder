# デスクトップにショートカットを作成するスクリプト
$exePath = Join-Path $PSScriptRoot "PcAudioRecorder.exe"
$shortcutPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath("Desktop"), "PC Audio Recorder.lnk")

$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $PSScriptRoot
$shortcut.Description = "PC再生音録音ソフト"
$shortcut.Save()

Write-Host "デスクトップにショートカットを作成しました: $shortcutPath" -ForegroundColor Green
