using System.Management;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace NobleBrightness;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly TrayWindow _tray;
    private readonly BrightnessCoordinator _brightness;
    private readonly GlobalWheelHook _wheelHook;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _levelItem = new() { Enabled = false };
    private readonly ToolStripMenuItem _monitorMenu = new("Select monitor");
    private readonly ToolStripMenuItem _brightnessMenu = new("Set brightness");
    private readonly ToolStripMenuItem _startWithWindowsItem = new("Start with Windows");
    private readonly SynchronizationContext _uiContext;
    private bool _stopping;

    public TrayApplicationContext()
    {
        _uiContext = SynchronizationContext.Current ?? throw new InvalidOperationException("Windows Forms synchronization context was not created.");
        _tray = new TrayWindow(GetAppIcon(), ShowMenu);
        _brightness = new BrightnessCoordinator();
        _brightness.StateChanged += OnBrightnessStateChanged;

        _menu.Items.Add(_levelItem);
        _menu.Items.Add(_monitorMenu);
        _menu.Items.Add(_brightnessMenu);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_startWithWindowsItem);
        _menu.Items.Add("Exit", null, (_, _) => ExitSafely());
        _startWithWindowsItem.Click += (_, _) =>
        {
            StartupRegistration.SetEnabled(!StartupRegistration.IsEnabled);
            RefreshMenu();
        };
        _menu.Opening += (_, _) => RefreshMenu();

        RefreshTrayText();
        _wheelHook = new GlobalWheelHook(_tray.IsCursorOverIcon, delta => _brightness.ChangeBy(delta > 0 ? 5 : -5));
        _ = _brightness.InitializeAsync();
    }

    private void OnBrightnessStateChanged() => _uiContext.Post(_ =>
    {
        if (!_stopping) RefreshTrayText();
    }, null);

    private void RefreshTrayText()
    {
        var state = _brightness.Snapshot;
        _tray.Tooltip = $"Monitor ({state.Name}): {state.Percent}%";
        RefreshMenu();
    }

    private void RefreshMenu()
    {
        var state = _brightness.Snapshot;
        _levelItem.Text = $"Brightness: {state.Percent}% ({state.Name})";

        _monitorMenu.DropDownItems.Clear();
        foreach (var monitor in _brightness.Monitors)
        {
            var item = new ToolStripMenuItem(monitor.Name) { Checked = monitor.IsSelected };
            var id = monitor.Id;
            item.Click += (_, _) => _brightness.Select(id);
            _monitorMenu.DropDownItems.Add(item);
        }
        _monitorMenu.Enabled = _monitorMenu.DropDownItems.Count > 0;

        _brightnessMenu.DropDownItems.Clear();
        foreach (var value in new[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 })
        {
            var item = new ToolStripMenuItem($"{value}%") { Checked = state.Percent == value };
            item.Click += (_, _) => _brightness.SetCurrent(value);
            _brightnessMenu.DropDownItems.Add(item);
        }
        _brightnessMenu.DropDownItems.Add(new ToolStripSeparator());
        _brightnessMenu.DropDownItems.Add("Increase +5%", null, (_, _) => _brightness.ChangeBy(5));
        _brightnessMenu.DropDownItems.Add("Decrease -5%", null, (_, _) => _brightness.ChangeBy(-5));
        _brightnessMenu.Enabled = _brightness.IsReady;
        _startWithWindowsItem.Checked = StartupRegistration.IsEnabled;
    }

    private void ShowMenu(Point screenPoint)
    {
        RefreshMenu();
        NativeMethods.SetForegroundWindow(_tray.Handle);
        _menu.Show(screenPoint);
    }

    private void ExitSafely()
    {
        if (_stopping) return;
        _stopping = true;
        _wheelHook.Dispose();
        _brightness.Dispose();
        _tray.Dispose();
        _menu.Dispose();
        ExitThread();
    }

    private static Icon GetAppIcon()
    {
        try
        {
            using var stream = typeof(Program).Assembly.GetManifestResourceStream("NobleBrightness.wasd.ico");
            if (stream is not null)
            {
                using var rawIcon = new Icon(stream);
                return new Icon(rawIcon, SystemInformation.SmallIconSize);
            }
        }
        catch { }

        try
        {
            if (!string.IsNullOrEmpty(Environment.ProcessPath))
            {
                using var rawIcon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
                if (rawIcon is not null)
                {
                    return new Icon(rawIcon, SystemInformation.SmallIconSize);
                }
            }
        }
        catch { }
        return SystemIcons.Application;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_stopping) ExitSafely();
        base.Dispose(disposing);
    }
}

/// <summary>Native Shell icon host; Shell_NotifyIconGetRect returns this exact icon's physical bounds.</summary>
internal sealed class TrayWindow : NativeWindow, IDisposable
{
    private const int CallbackMessage = NativeMethods.WM_APP + 71;
    private const uint IconId = 1;
    private readonly Guid _iconGuid = new("84EB31A8-FA13-4B8D-9960-7B70D0B9B9BE");
    private readonly Action<Point> _showMenu;
    private bool _added;

    public TrayWindow(Icon icon, Action<Point> showMenu)
    {
        _showMenu = showMenu;
        CreateHandle(new CreateParams { Caption = "NobleBrightness.TrayHost" });
        AddIcon(icon);
    }

    public string Tooltip { set => ModifyIcon(value); }

    public bool IsCursorOverIcon(Point cursor)
    {
        var id = CreateIdentifier();
        return NativeMethods.Shell_NotifyIconGetRect(ref id, out var rect) == 0 &&
               cursor.X >= rect.Left && cursor.X < rect.Right && cursor.Y >= rect.Top && cursor.Y < rect.Bottom;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == CallbackMessage)
        {
            var notification = unchecked((uint)m.LParam.ToInt64()) & 0xffff;
            if (notification is NativeMethods.WM_RBUTTONUP or NativeMethods.WM_CONTEXTMENU)
            {
                NativeMethods.GetCursorPos(out var point);
                _showMenu(new Point(point.X, point.Y));
            }
        }
        base.WndProc(ref m);
    }

    private void AddIcon(Icon icon)
    {
        var data = CreateData();
        data.uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_GUID | NativeMethods.NIF_SHOWTIP;
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = icon.Handle;
        data.szTip = "Monitor (Detecting...): 50%";
        _added = NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref data);
        if (!_added)
        {
            data.uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_SHOWTIP;
            data.hIcon = SystemIcons.Application.Handle;
            _added = NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref data);
        }
        if (!_added) throw new InvalidOperationException("Could not add the notification icon.");
        data.uVersionOrTimeout = NativeMethods.NOTIFYICON_VERSION_4;
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_SETVERSION, ref data);
    }

    private void ModifyIcon(string text)
    {
        if (!_added) return;
        var data = CreateData();
        data.uFlags = NativeMethods.NIF_TIP | NativeMethods.NIF_GUID | NativeMethods.NIF_SHOWTIP;
        data.szTip = text.Length <= 127 ? text : text[..127];
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_MODIFY, ref data);
    }

    private NativeMethods.NOTIFYICONIDENTIFIER CreateIdentifier() => new()
    {
        cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONIDENTIFIER>(), hWnd = Handle, uID = IconId, guidItem = _iconGuid
    };
    private NativeMethods.NOTIFYICONDATA CreateData() => new()
    {
        cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(), hWnd = Handle, uID = IconId, guidItem = _iconGuid
    };
    public void Dispose()
    {
        if (_added)
        {
            var data = CreateData(); data.uFlags = NativeMethods.NIF_GUID;
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref data); _added = false;
        }
        DestroyHandle();
    }
}

/// <summary>Observer only: never consumes Windows' original wheel event.</summary>
internal sealed class GlobalWheelHook : IDisposable
{
    private readonly NativeMethods.HookProc _callback;
    private readonly Func<Point, bool> _isOverIcon;
    private readonly Action<int> _onWheel;
    private readonly IntPtr _hook;

    public GlobalWheelHook(Func<Point, bool> isOverIcon, Action<int> onWheel)
    {
        _isOverIcon = isOverIcon; _onWheel = onWheel; _callback = HookCallback;
        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _callback, IntPtr.Zero, 0);
        if (_hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }
    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code == NativeMethods.HC_ACTION && unchecked((uint)wParam.ToInt64()) == NativeMethods.WM_MOUSEWHEEL)
        {
            var info = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            if (_isOverIcon(new Point(info.pt.X, info.pt.Y)))
            {
                var delta = unchecked((short)(info.mouseData >> 16));
                if (delta != 0) _onWheel(delta);
            }
        }
        return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
    }
    public void Dispose() { if (_hook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_hook); }
}

/// <summary>Cached UI state plus a single debounced hardware worker shared by all discovered displays.</summary>
internal sealed class BrightnessCoordinator : IDisposable
{
    private readonly object _gate = new();
    private readonly Channel<bool> _wake = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly CancellationTokenSource _stop = new();
    private readonly List<ManagedMonitor> _monitors = [];
    private BrightnessState _state = new("Detecting...", 50);
    private string? _selectedId;
    private int? _desiredPercent;
    private bool _initialized;

    public event Action? StateChanged;
    public BrightnessState Snapshot { get { lock (_gate) return _state; } }
    public bool IsReady { get { lock (_gate) return _initialized; } }
    public IReadOnlyList<MonitorMenuState> Monitors
    {
        get { lock (_gate) return _monitors.Select(m => new MonitorMenuState(m.Backend.Id, m.Backend.Name, m.Backend.Id == _selectedId)).ToArray(); }
    }

    public async Task InitializeAsync()
    {
        try
        {
            var discovered = await Task.Run(BrightnessBackendFactory.Discover, _stop.Token).ConfigureAwait(false);
            var monitors = await Task.Run(() => discovered.Select(b => new ManagedMonitor(b, b.ReadPercent())).ToArray(), _stop.Token).ConfigureAwait(false);
            lock (_gate)
            {
                _monitors.AddRange(monitors);
                var selected = _monitors[0];
                _selectedId = selected.Backend.Id;
                _state = new BrightnessState(selected.Backend.Name, selected.Percent);
                _initialized = true;
            }
            StateChanged?.Invoke();
            _ = Task.Run(WorkerAsync);
        }
        catch (Exception)
        {
            lock (_gate) _state = new BrightnessState("Unsupported display", 0);
            StateChanged?.Invoke();
        }
    }

    public void ChangeBy(int amount) => SetCurrent(CheckedAdd(amount));
    private int CheckedAdd(int amount)
    {
        lock (_gate) return Math.Clamp((_desiredPercent ?? _state.Percent) + amount, 0, 100);
    }
    public void SetCurrent(int percent)
    {
        lock (_gate)
        {
            if (!_initialized) return;
            _desiredPercent = Math.Clamp(percent, 0, 100);
            _state = _state with { Percent = _desiredPercent.Value };
        }
        StateChanged?.Invoke(); _wake.Writer.TryWrite(true);
    }
    public void Select(string monitorId)
    {
        lock (_gate)
        {
            var monitor = _monitors.FirstOrDefault(m => m.Backend.Id == monitorId);
            if (!_initialized || monitor is null || _selectedId == monitorId) return;
            _selectedId = monitorId; _desiredPercent = null;
            _state = new BrightnessState(monitor.Backend.Name, monitor.Percent);
        }
        StateChanged?.Invoke();
    }

    private async Task WorkerAsync()
    {
        try
        {
            while (await _wake.Reader.WaitToReadAsync(_stop.Token).ConfigureAwait(false))
            {
                while (_wake.Reader.TryRead(out _)) { }
                await Task.Delay(80, _stop.Token).ConfigureAwait(false);
                while (_wake.Reader.TryRead(out _)) { }
                int target; ManagedMonitor monitor;
                lock (_gate)
                {
                    if (_desiredPercent is null || _selectedId is null) continue;
                    target = _desiredPercent.Value; _desiredPercent = null;
                    monitor = _monitors.First(m => m.Backend.Id == _selectedId);
                }
                try { monitor.Backend.WritePercent(target); lock (_gate) monitor.Percent = target; }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or ManagementException or InvalidOperationException) { }
                lock (_gate) if (_desiredPercent is not null) _wake.Writer.TryWrite(true);
            }
        }
        catch (OperationCanceledException) { }
    }
    public void Dispose()
    {
        _stop.Cancel();
        lock (_gate) foreach (var monitor in _monitors) monitor.Backend.Dispose();
        _stop.Dispose();
    }
}

internal readonly record struct BrightnessState(string Name, int Percent);
internal sealed class ManagedMonitor(IBrightnessBackend backend, int percent)
{
    public IBrightnessBackend Backend { get; } = backend;
    public int Percent { get; set; } = percent;
}
internal readonly record struct MonitorMenuState(string Id, string Name, bool IsSelected);

/// <summary>Per-user auto-start; no administrator access or scheduled task is required.</summary>
internal static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "NobleBrightness";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true)
            ?? throw new InvalidOperationException("Could not open the Windows startup registry key.");
        if (enabled)
            key.SetValue(ValueName, $"\"{Application.ExecutablePath}\"", RegistryValueKind.String);
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
