using System;
using System.IO;
using System.Linq;

namespace PcAudioRecorder
{
    public static class RecordingFileService
    {
        public const long MinimumFreeSpaceBytes = 500L * 1024 * 1024;

        public static string GenerateFileName(string extension, DateTime? timestamp = null)
        {
            var date = timestamp ?? DateTime.Now;
            return $"Recording_{date:yyyyMMdd_HHmmss}.{NormalizeExtension(extension)}";
        }

        public static string BuildSafeOutputPath(string outputDirectory, string? requestedFileName, string extension)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("保存先フォルダーが指定されていません。", nameof(outputDirectory));

            if (!Directory.Exists(outputDirectory))
                throw new DirectoryNotFoundException("保存先フォルダーが見つかりません。");

            var normalizedExtension = NormalizeExtension(extension);
            var safeFileName = BuildSafeFileName(requestedFileName, normalizedExtension);
            var candidatePath = Path.Combine(outputDirectory, safeFileName);
            return GetAvailablePath(candidatePath);
        }

        public static void ValidateOutputDirectory(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new InvalidOperationException("保存先フォルダーが指定されていません。");

            if (!Directory.Exists(outputDirectory))
                throw new DirectoryNotFoundException("保存先フォルダーが見つかりません。");

            var root = Path.GetPathRoot(outputDirectory);
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("保存先フォルダーのドライブを確認できません。");

            var driveInfo = new DriveInfo(root);
            if (!driveInfo.IsReady)
                throw new IOException("保存先ドライブを利用できません。");

            if (driveInfo.AvailableFreeSpace < MinimumFreeSpaceBytes)
                throw new IOException("ディスクの空き容量が500MB未満です。保存先を変更するか空き容量を増やしてください。");

            var probePath = Path.Combine(outputDirectory, $".write-test-{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(probePath, string.Empty);
            }
            finally
            {
                if (File.Exists(probePath))
                    File.Delete(probePath);
            }
        }

        public static string BuildSafeFileName(string? requestedFileName, string extension)
        {
            var normalizedExtension = NormalizeExtension(extension);
            var sourceName = string.IsNullOrWhiteSpace(requestedFileName)
                ? GenerateFileName(normalizedExtension)
                : requestedFileName.Trim();

            var invalidChars = Path.GetInvalidFileNameChars()
                .Concat(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar })
                .Distinct()
                .ToArray();

            var sanitized = new string(sourceName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
            sanitized = sanitized.Trim();

            var baseName = Path.GetFileNameWithoutExtension(sanitized).Trim(' ', '.', '_');
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = Path.GetFileNameWithoutExtension(GenerateFileName(normalizedExtension));

            return $"{baseName}.{normalizedExtension}";
        }

        public static string GetAvailablePath(string candidatePath)
        {
            if (!File.Exists(candidatePath))
                return candidatePath;

            var directory = Path.GetDirectoryName(candidatePath) ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(candidatePath);
            var extension = Path.GetExtension(candidatePath);

            for (var index = 1; index <= 9999; index++)
            {
                var nextPath = Path.Combine(directory, $"{baseName}_{index:000}{extension}");
                if (!File.Exists(nextPath))
                    return nextPath;
            }

            throw new IOException("利用可能な保存ファイル名を作成できませんでした。");
        }

        private static string NormalizeExtension(string extension)
        {
            var value = extension.Trim().TrimStart('.').ToLowerInvariant();
            if (value is not ("wav" or "mp3"))
                throw new ArgumentException("対応していない保存形式です。", nameof(extension));

            return value;
        }
    }
}
