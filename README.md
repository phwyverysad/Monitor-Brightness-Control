# 🖥️ Monitor Brightness Control (Noble Brightness)

[![Framework](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows)](https://microsoft.com)
[![Language](https://img.shields.io/badge/Language-C%23-239120?logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)

Lightweight, high-performance Windows System Tray brightness controller. Adjust your primary display brightness effortlessly by hovering over the tray icon and using your mouse scroll wheel.

---

## ✨ Features (คุณสมบัติเด่น)

* 🖱️ **Scroll-to-Adjust**: Hover the system tray icon and scroll the mouse wheel to change brightness in 5% increments.
* 🖥️ **Dual Hardware Support**:
  * **DDC/CI** for external desktop monitors.
  * **WMI (Windows Management Instrumentation)** fallback for laptop panels and internal displays.
* ⚡ **Ultra Lightweight & Fast**: Optimized C# implementation with minimal CPU/RAM footprint (~3.2 MB installer package).
* 🔒 **Safe Mouse Hooking**: The low-level mouse hook checks the physical bounds of the tray icon before acting, ensuring unrelated document/browser scrolling is never intercepted.
* ⚙️ **Context Menu**: Right-click the tray icon to select active monitors or jump directly to specific brightness levels (0% – 100%).
* 📦 **Standalone Installer**: Includes a lightweight Windows Installer with auto-startup options and clean Control Panel uninstallation.

---

## 🚀 Installation & Usage (การติดตั้งและการใช้งาน)

### Requirements
* **Operating System**: Windows 10 / 11 (64-bit)
* **Monitor**: DDC/CI enabled in the monitor's On-Screen Display (OSD) menu for external displays.

### Quick Start
1. Download `Setup_NobleBrightness.exe` from the latest release.
2. Run the installer to install the application into `C:\Program Files\NobleBrightness`.
3. The app will launch automatically in your System Tray (bottom right of your screen).
4. Hover over the tray icon and scroll your mouse wheel up/down to adjust brightness.

---

## 🛠️ Building from Source (การคอมไพล์ด้วยตนเอง)

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. Build & Run Application
```powershell
# Restore and run
dotnet restore .\NobleBrightness\NobleBrightness.csproj
dotnet run --project .\NobleBrightness\NobleBrightness.csproj
```

### 2. Publish Standalone / Lightweight Binary
```powershell
# Framework-dependent small binary
dotnet publish .\NobleBrightness\NobleBrightness.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# Self-contained single file (no .NET required on target machine)
dotnet publish .\NobleBrightness\NobleBrightness.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

### 3. Build Setup Installer Package
```powershell
dotnet publish .\NobleBrightnessInstaller\NobleBrightnessInstaller.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

---

## 📐 Architecture & Technical Details

### Safe Low-Level Mouse Wheel Hook
The application registers a low-level mouse hook (`WH_MOUSE_LL`). When a scroll event occurs, it calls `Shell_NotifyIconGetRect` to query Explorer for the exact physical screen boundaries of the tray icon. If the cursor is outside the icon, the hook immediately passes the event down the hook chain without consuming it.

### Smooth DDC/CI & WMI Execution
DDC/CI hardware I/O and WMI queries run asynchronously off the UI thread. Mouse wheel scroll bursts are coalesced using an 80 ms debounce window in a background worker channel. Tooltips and context menus update instantaneously from in-memory state while physical monitor hardware commands execute smoothly in the background.

---

## 📄 License
This project is open-source under the MIT License.
