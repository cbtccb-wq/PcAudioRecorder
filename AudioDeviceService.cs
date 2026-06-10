using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;

namespace PcAudioRecorder
{
    public class RenderDeviceInfo
    {
        public RenderDeviceInfo(string? id, string name, bool isDefault)
        {
            Id = id;
            Name = name;
            IsDefault = isDefault;
        }

        public string? Id { get; }
        public string Name { get; }
        public bool IsDefault { get; }

        public override string ToString() => Name;
    }

    public static class AudioDeviceService
    {
        public static IReadOnlyList<RenderDeviceInfo> GetRenderDevices(AppLogger? logger = null)
        {
            var devices = new List<RenderDeviceInfo>
            {
                new RenderDeviceInfo(null, "既定の再生デバイス", true)
            };

            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var defaultDeviceId = TryGetDefaultDeviceId(enumerator);
                var activeDevices = enumerator
                    .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .OrderBy(device => device.FriendlyName)
                    .ToList();

                foreach (var device in activeDevices)
                {
                    var suffix = device.ID == defaultDeviceId ? " (現在の既定)" : string.Empty;
                    devices.Add(new RenderDeviceInfo(device.ID, device.FriendlyName + suffix, device.ID == defaultDeviceId));
                }
            }
            catch (Exception ex)
            {
                logger?.Warn("再生デバイスの一覧を取得できませんでした。既定の再生デバイスを使用します。", ex);
            }

            return devices;
        }

        public static MMDevice? GetRenderDeviceOrDefault(string? requestedDeviceId, AppLogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(requestedDeviceId))
                return null;

            try
            {
                var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                var device = devices.FirstOrDefault(item => item.ID == requestedDeviceId);

                if (device == null)
                {
                    enumerator.Dispose();
                    logger?.Warn("選択されていた再生デバイスが見つからないため、既定の再生デバイスを使用します。");
                    return null;
                }

                return device;
            }
            catch (Exception ex)
            {
                logger?.Warn("選択された再生デバイスを開けないため、既定の再生デバイスを使用します。", ex);
                return null;
            }
        }

        private static string? TryGetDefaultDeviceId(MMDeviceEnumerator enumerator)
        {
            try
            {
                using var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                return defaultDevice.ID;
            }
            catch
            {
                return null;
            }
        }
    }
}
