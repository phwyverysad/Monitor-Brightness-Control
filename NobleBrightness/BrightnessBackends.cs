using System.Management;
using System.Runtime.InteropServices;

namespace NobleBrightness;

internal interface IBrightnessBackend : IDisposable
{
    string Id { get; }
    string Name { get; }
    int ReadPercent();
    void WritePercent(int percent);
}

internal static class BrightnessBackendFactory
{
    public static IReadOnlyList<IBrightnessBackend> Discover()
    {
        var names = MonitorNameResolver.ReadFriendlyNames();
        var backends = new List<IBrightnessBackend>();

        // A physical DDC/CI monitor can be attached to every active Windows display.
        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
        {
            foreach (var backend in DdcCiBrightnessBackend.CreateForDisplay(monitor, names))
            {
                try
                {
                    _ = backend.ReadPercent(); // Reject monitors that advertise but do not implement DDC/CI brightness.
                    backends.Add(backend);
                }
                catch { backend.Dispose(); }
            }
            return true;
        }, IntPtr.Zero);

        // Internal/laptop panels usually use this API instead of DDC/CI.
        // WMI is optional. Many external monitors return WBEM_E_NOT_SUPPORTED for
        // WmiMonitorBrightness even though the DDC/CI monitors above are healthy.
        // Never discard already-discovered DDC/CI displays in that case.
        try
        {
            foreach (var backend in WmiBrightnessBackend.CreateAll(names))
            {
                try
                {
                    _ = backend.ReadPercent();
                    backends.Add(backend);
                }
                catch { backend.Dispose(); }
            }
        }
        catch (ManagementException) { }

        if (backends.Count == 0)
            throw new InvalidOperationException("No Windows-controllable monitor was found.");
        return backends;
    }
}

internal sealed class DdcCiBrightnessBackend : IBrightnessBackend
{
    private readonly IntPtr _physicalMonitor;
    private readonly string _name;
    private uint _maximumVcp;
    private bool _disposed;

    private DdcCiBrightnessBackend(IntPtr physicalMonitor, string id, string name)
    {
        _physicalMonitor = physicalMonitor;
        Id = id;
        _name = name;
    }

    public string Id { get; }
    public string Name => _name;

    public static IEnumerable<DdcCiBrightnessBackend> CreateForDisplay(IntPtr displayMonitor, IReadOnlyDictionary<string, string> names)
    {
        if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(displayMonitor, out var count) || count == 0)
            yield break;

        var monitors = new NativeMethods.PHYSICAL_MONITOR[count];
        if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(displayMonitor, count, monitors))
            yield break;

        var pnpId = MonitorNameResolver.GetPnpId(displayMonitor);
        for (var index = 0; index < monitors.Length; index++)
        {
            var physical = monitors[index];
            var fallback = string.IsNullOrWhiteSpace(physical.szPhysicalMonitorDescription)
                ? "External display"
                : physical.szPhysicalMonitorDescription.Trim();
            var name = MonitorNameResolver.ResolveFriendlyName(pnpId, names, fallback);
            yield return new DdcCiBrightnessBackend(physical.hPhysicalMonitor, $"ddc:{pnpId}:{index}", name);
        }
    }

    public int ReadPercent()
    {
        ThrowIfDisposed();
        if (!NativeMethods.GetVCPFeatureAndVCPFeatureReply(_physicalMonitor, NativeMethods.VCP_BRIGHTNESS, out _, out var current, out var maximum) || maximum == 0)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "DDC/CI brightness read failed.");
        _maximumVcp = maximum;
        return Math.Clamp((int)Math.Round(current * 100d / maximum), 0, 100);
    }

    public void WritePercent(int percent)
    {
        ThrowIfDisposed();
        if (_maximumVcp == 0) _ = ReadPercent();
        var value = (uint)Math.Round(Math.Clamp(percent, 0, 100) * _maximumVcp / 100d);
        if (!NativeMethods.SetVCPFeature(_physicalMonitor, NativeMethods.VCP_BRIGHTNESS, value))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "DDC/CI brightness write failed.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NativeMethods.DestroyPhysicalMonitors(1, new[] { new NativeMethods.PHYSICAL_MONITOR { hPhysicalMonitor = _physicalMonitor } });
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DdcCiBrightnessBackend));
    }
}

internal sealed class WmiBrightnessBackend : IBrightnessBackend
{
    private readonly string _instanceName;

    private WmiBrightnessBackend(string instanceName, string name)
    {
        _instanceName = instanceName;
        Id = $"wmi:{instanceName}";
        Name = name;
    }

    public string Id { get; }
    public string Name { get; }

    public static IEnumerable<WmiBrightnessBackend> CreateAll(IReadOnlyDictionary<string, string> names)
    {
        using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT InstanceName FROM WmiMonitorBrightness WHERE Active = TRUE");
        using var results = searcher.Get();
        foreach (var monitor in results.Cast<ManagementObject>())
        {
            if (monitor["InstanceName"] is not string instanceName) continue;
            var pnpId = MonitorNameResolver.PnpToken(instanceName);
            yield return new WmiBrightnessBackend(instanceName,
                MonitorNameResolver.ResolveFriendlyName(pnpId, names, "Internal display"));
        }
    }

    public int ReadPercent()
    {
        using var searcher = new ManagementObjectSearcher("root\\WMI", $"SELECT CurrentBrightness FROM WmiMonitorBrightness WHERE InstanceName = '{Escape(_instanceName)}'");
        using var results = searcher.Get();
        var value = results.Cast<ManagementObject>().FirstOrDefault()?["CurrentBrightness"];
        return value is byte brightness ? brightness : throw new InvalidOperationException("Could not read WMI brightness.");
    }

    public void WritePercent(int percent)
    {
        using var searcher = new ManagementObjectSearcher("root\\WMI", $"SELECT * FROM WmiMonitorBrightnessMethods WHERE InstanceName = '{Escape(_instanceName)}'");
        using var results = searcher.Get();
        var methods = results.Cast<ManagementObject>().FirstOrDefault()
            ?? throw new InvalidOperationException("WMI brightness setter is not available.");
        methods.InvokeMethod("WmiSetBrightness", new object[] { 1u, (byte)Math.Clamp(percent, 0, 100) });
    }

    public void Dispose() { }
    private static string Escape(string value) => value.Replace("'", "''");
}

/// <summary>Gets EDID's user-friendly model name and matches it to the active display's PNP identifier.</summary>
internal static class MonitorNameResolver
{
    public static IReadOnlyDictionary<string, string> ReadFriendlyNames()
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT InstanceName, UserFriendlyName FROM WmiMonitorID WHERE Active = TRUE");
            using var results = searcher.Get();
            foreach (var monitor in results.Cast<ManagementObject>())
            {
                if (monitor["InstanceName"] is not string instanceName || monitor["UserFriendlyName"] is not ushort[] codes) continue;
                var name = new string(codes.TakeWhile(c => c != 0).Select(c => (char)c).ToArray()).Trim();
                if (!string.IsNullOrWhiteSpace(name)) names[PnpToken(instanceName)] = name;
            }
        }
        catch (ManagementException) { }
        return names;
    }

    public static string GetPnpId(IntPtr monitor)
    {
        var info = new NativeMethods.MONITORINFOEX { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return "UNKNOWN";
        var device = new NativeMethods.DISPLAY_DEVICE { cb = Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>() };
        return NativeMethods.EnumDisplayDevices(info.szDevice, 0, ref device, 0)
            ? PnpToken(device.DeviceID)
            : "UNKNOWN";
    }

    public static string ResolveFriendlyName(string pnpId, IReadOnlyDictionary<string, string> names, string fallback)
    {
        if (names.TryGetValue(pnpId, out var name)) return name;
        // Some graphics drivers return an incomplete DISPLAY_DEVICE.DeviceID even
        // though Windows successfully exposes the EDID name. A single active EDID
        // is unambiguous, so use it rather than showing "Generic PnP Monitor".
        return names.Count == 1 ? names.Values.First() : fallback;
    }

    public static string PnpToken(string instanceOrDeviceId)
    {
        var parts = instanceOrDeviceId.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : instanceOrDeviceId;
    }
}
