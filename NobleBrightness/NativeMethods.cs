using System.Runtime.InteropServices;

namespace NobleBrightness;

internal static class NativeMethods
{
    internal const int WM_APP = 0x8000;
    internal const uint WM_MOUSEWHEEL = 0x020A;
    internal const uint WM_RBUTTONUP = 0x0205;
    internal const uint WM_CONTEXTMENU = 0x007B;
    internal const int WH_MOUSE_LL = 14;
    internal const int HC_ACTION = 0;
    internal const uint NIM_ADD = 0x00000000;
    internal const uint NIM_MODIFY = 0x00000001;
    internal const uint NIM_DELETE = 0x00000002;
    internal const uint NIM_SETVERSION = 0x00000004;
    internal const uint NIF_MESSAGE = 0x00000001;
    internal const uint NIF_ICON = 0x00000002;
    internal const uint NIF_TIP = 0x00000004;
    internal const uint NIF_SHOWTIP = 0x00000080;
    internal const uint NIF_GUID = 0x00000020;
    internal const uint NOTIFYICON_VERSION_4 = 4;
    internal const uint MONITOR_DEFAULTTOPRIMARY = 1;
    internal const byte VCP_BRIGHTNESS = 0x10;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Shell_NotifyIcon(uint message, ref NOTIFYICONDATA data);

    [DllImport("shell32.dll")]
    internal static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, out RECT iconLocation);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    internal static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFOEX monitorInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumDisplayDevices(string deviceName, uint deviceNumber, ref DISPLAY_DEVICE displayDevice, uint flags);

    [DllImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr monitor, out uint count);

    [DllImport("dxva2.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr monitor, uint count, [Out] PHYSICAL_MONITOR[] physicalMonitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyPhysicalMonitors(uint count, [In] PHYSICAL_MONITOR[] physicalMonitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr physicalMonitor, byte vcpCode, out uint typeCode, out uint currentValue, out uint maximumValue);

    [DllImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetVCPFeature(IntPtr physicalMonitor, byte vcpCode, uint newValue);

    internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
    internal delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NOTIFYICONDATA
    {
        internal uint cbSize;
        internal IntPtr hWnd;
        internal uint uID;
        internal uint uFlags;
        internal uint uCallbackMessage;
        internal IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] internal string szTip;
        internal uint dwState;
        internal uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] internal string szInfo;
        internal uint uVersionOrTimeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] internal string szInfoTitle;
        internal uint dwInfoFlags;
        internal Guid guidItem;
        internal IntPtr hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NOTIFYICONIDENTIFIER
    {
        internal uint cbSize;
        internal IntPtr hWnd;
        internal uint uID;
        internal Guid guidItem;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT { internal int X; internal int Y; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT { internal int Left; internal int Top; internal int Right; internal int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        internal POINT pt;
        internal uint mouseData;
        internal uint flags;
        internal uint time;
        internal IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct PHYSICAL_MONITOR
    {
        internal IntPtr hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] internal string szPhysicalMonitorDescription;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct MONITORINFOEX
    {
        internal int cbSize;
        internal RECT rcMonitor;
        internal RECT rcWork;
        internal uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] internal string szDevice;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DISPLAY_DEVICE
    {
        internal int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] internal string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] internal string DeviceString;
        internal uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] internal string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] internal string DeviceKey;
    }
}
