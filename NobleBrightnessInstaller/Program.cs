using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NobleBrightnessInstaller;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Length > 0 && args[0].Equals("/uninstall", StringComparison.OrdinalIgnoreCase))
        {
            PerformUninstall();
            return;
        }

        Application.Run(new InstallerForm());
    }

    private static void PerformUninstall()
    {
        try
        {
            // Kill running process
            foreach (var proc in Process.GetProcessesByName("NobleBrightness"))
            {
                try { proc.Kill(); proc.WaitForExit(3000); } catch { }
            }

            // Remove startup registry
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                runKey?.DeleteValue("NobleBrightness", false);
            }
            catch { }

            // Remove uninstall registry
            try
            {
                using var uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true);
                uninstallKey?.DeleteSubKeyTree("NobleBrightness", false);
            }
            catch { }

            // Remove shortcuts
            try
            {
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "Noble Brightness.lnk");
                if (File.Exists(startMenu)) File.Delete(startMenu);

                string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Noble Brightness.lnk");
                if (File.Exists(desktop)) File.Delete(desktop);
            }
            catch { }

            MessageBox.Show("Noble Brightness has been successfully uninstalled.", "Uninstall Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Self delete directory via cmd
            string targetDir = @"C:\Program Files\NobleBrightness";
            if (Directory.Exists(targetDir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c timeout /t 2 & rmdir /s /q \"{targetDir}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Uninstall error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public sealed class InstallerForm : Form
{
    private readonly TextBox _txtPath;
    private readonly CheckBox _chkStartup;
    private readonly CheckBox _chkDesktop;
    private readonly CheckBox _chkLaunch;
    private readonly Button _btnInstall;
    private readonly Button _btnCancel;
    private readonly ProgressBar _progress;
    private readonly Label _lblStatus;

    public InstallerForm()
    {
        Text = "Noble Brightness Setup";
        Size = new Size(500, 360);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // Try load icon
        try
        {
            using var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NobleBrightnessInstaller.wasd.ico");
            if (iconStream != null) Icon = new Icon(iconStream);
        }
        catch { }

        var lblHeader = new Label
        {
            Text = "Noble Brightness Setup",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Location = new Point(20, 15),
            AutoSize = true
        };

        var lblSubHeader = new Label
        {
            Text = "Lightweight Windows Tray Brightness Controller",
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.Gray,
            Location = new Point(22, 45),
            AutoSize = true
        };

        var lblDir = new Label
        {
            Text = "Installation Folder:",
            Location = new Point(20, 80),
            AutoSize = true
        };

        _txtPath = new TextBox
        {
            Text = @"C:\Program Files\NobleBrightness",
            Location = new Point(20, 102),
            Size = new Size(340, 23)
        };

        var btnBrowse = new Button
        {
            Text = "Browse...",
            Location = new Point(370, 100),
            Size = new Size(90, 26)
        };
        btnBrowse.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog { SelectedPath = _txtPath.Text };
            if (dlg.ShowDialog() == DialogResult.OK) _txtPath.Text = dlg.SelectedPath;
        };

        _chkStartup = new CheckBox
        {
            Text = "Start automatically when Windows starts",
            Checked = true,
            Location = new Point(20, 140),
            AutoSize = true
        };

        _chkDesktop = new CheckBox
        {
            Text = "Create Desktop shortcut",
            Checked = true,
            Location = new Point(20, 168),
            AutoSize = true
        };

        _chkLaunch = new CheckBox
        {
            Text = "Launch Noble Brightness after installation",
            Checked = true,
            Location = new Point(20, 196),
            AutoSize = true
        };

        _progress = new ProgressBar
        {
            Location = new Point(20, 230),
            Size = new Size(440, 20),
            Visible = false
        };

        _lblStatus = new Label
        {
            Text = "",
            Location = new Point(20, 255),
            AutoSize = true,
            ForeColor = Color.DarkBlue
        };

        _btnInstall = new Button
        {
            Text = "Install",
            Location = new Point(265, 280),
            Size = new Size(95, 30),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        _btnInstall.Click += OnInstallClicked;

        _btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(365, 280),
            Size = new Size(95, 30)
        };
        _btnCancel.Click += (_, _) => Close();

        Controls.AddRange([
            lblHeader, lblSubHeader, lblDir, _txtPath, btnBrowse,
            _chkStartup, _chkDesktop, _chkLaunch, _progress, _lblStatus,
            _btnInstall, _btnCancel
        ]);
    }

    private async void OnInstallClicked(object? sender, EventArgs e)
    {
        string installDir = _txtPath.Text.Trim();
        if (string.IsNullOrEmpty(installDir)) return;

        _btnInstall.Enabled = false;
        _btnCancel.Enabled = false;
        _progress.Visible = true;
        _progress.Style = ProgressBarStyle.Marquee;
        _lblStatus.Text = "Installing Noble Brightness...";

        try
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                // Kill running instance if updating
                foreach (var proc in Process.GetProcessesByName("NobleBrightness"))
                {
                    try { proc.Kill(); proc.WaitForExit(3000); } catch { }
                }

                Directory.CreateDirectory(installDir);

                // Extract executable
                string targetExe = Path.Combine(installDir, "NobleBrightness.exe");
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NobleBrightnessInstaller.NobleBrightnessPayload.exe"))
                {
                    if (stream == null) throw new InvalidOperationException("Payload resource not found.");
                    using var fs = new FileStream(targetExe, FileMode.Create, FileAccess.Write);
                    stream.CopyTo(fs);
                }

                // Copy self as Uninstall.exe
                string uninstallExe = Path.Combine(installDir, "Uninstall.exe");
                string currentInstaller = Environment.ProcessPath ?? string.Empty;
                if (!string.IsNullOrEmpty(currentInstaller) && File.Exists(currentInstaller))
                {
                    File.Copy(currentInstaller, uninstallExe, true);
                }

                // Create Start Menu Shortcut
                string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "Noble Brightness.lnk");
                CreateShortcut(startMenuPath, targetExe, "Windows System Tray Brightness Controller");

                // Create Desktop Shortcut
                if (_chkDesktop.Checked)
                {
                    string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Noble Brightness.lnk");
                    CreateShortcut(desktopPath, targetExe, "Windows System Tray Brightness Controller");
                }

                // Startup Registry
                if (_chkStartup.Checked)
                {
                    using var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    runKey?.SetValue("NobleBrightness", $"\"{targetExe}\"");
                }

                // Control Panel Add/Remove Programs Registry
                try
                {
                    using var uninstallKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\NobleBrightness");
                    if (uninstallKey != null)
                    {
                        uninstallKey.SetValue("DisplayName", "Noble Brightness");
                        uninstallKey.SetValue("DisplayVersion", "1.0.0");
                        uninstallKey.SetValue("Publisher", "Noble");
                        uninstallKey.SetValue("DisplayIcon", targetExe);
                        uninstallKey.SetValue("UninstallString", $"\"{uninstallExe}\" /uninstall");
                        uninstallKey.SetValue("InstallLocation", installDir);
                        uninstallKey.SetValue("NoModify", 1);
                        uninstallKey.SetValue("NoRepair", 1);
                    }
                }
                catch { }

                // Launch app if checked
                if (_chkLaunch.Checked)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExe,
                        UseShellExecute = true
                    });
                }
            });

            MessageBox.Show("Noble Brightness has been successfully installed!", "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Installation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _btnInstall.Enabled = true;
            _btnCancel.Enabled = true;
            _progress.Visible = false;
            _lblStatus.Text = "Installation failed.";
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string description)
    {
        try
        {
            Type? shellType = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));
            if (shellType == null) return;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Description = description;
            shortcut.Save();
        }
        catch { }
    }
}
