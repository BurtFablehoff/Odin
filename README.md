# Odin desktop companion

A portable Windows desktop cat companion.

## Download and run

Open [the latest release](https://github.com/BurtFablehoff/Odin/releases/latest).

- **Odin-Setup-3.1.1.exe** installs Odin for your Windows user, adds a Start menu shortcut, offers a desktop shortcut, and provides an uninstall entry.
- **Odin.exe** is the portable version. Save it in a writable folder and double-click it.

The finished artwork is embedded in either download. No source files or separate artwork are needed to run Odin. Windows 10 or later, x64-compatible, is required for the installer.

Right-click Odin or his system tray icon for settings, size, monitor controls, updates and Quit. The menu and tray tooltip show the current version. Small is the default size. Odin remains above normal windows without taking keyboard focus.

## Version 3.1.1

Odin chooses his own behavior. The main menu does not contain commands to make him pose or perform actions. The four resting states are Sitting, Loaf, Laying down awake, and Sleeping. Each holds its body pose while eligible expressions occur independently.

| Feature | Frames |
| --- | ---: |
| Sitting gaze | 19 |
| Loaf gaze | 12 |
| Awake laying gaze | 12 |
| Eye expressions for each awake resting state | 3 |
| Running left | 18 |
| Running right | 16 |
| Jumping left | 16 |
| Jumping right | 16 |

Loaf uses a smaller consistent scale. Running has separate size corrections and tail-aware monitor margins. Running in-betweens preserve the original poses and cycle duration. The jumping sequences include the reviewed frame-order corrections.

Window perches can start only from Sitting or awake Laying. Subtle occasional tail flicks are available while Sitting, awake Laying, or Sleeping; the broken loaf tail flick is excluded. Walking and belly-up content are excluded from this release.

## Updates and settings

Main-channel settings are stored in the current user's local application data under `OdinCompanionV3`. Automatic updates check this repository's GitHub Releases and verify download size, SHA-256, product identity and version before installation. Backups, startup health checks and rollback are supported. Disable Automatic updates in the menu to update manually.

To update manually, quit Odin and replace the portable EXE, or run the newer installer. Separate development copies use their own settings and do not update themselves.

## Source and artwork

The C# source and installer recipe are included for inspection. Original reference photos and videos remain private and are not published. The artwork pack is omitted from the source tree to keep it small; the released executable embeds the finished art.

On 64-bit Windows with the .NET Framework C# compiler, run `./build.ps1 -CompileOnly` to check compilation. A runnable build needs a compatible artwork pack: `./build.ps1 -AssetPack <path-to-Odin.assets>`. The reader and manifest format are defined in `app/Art.cs`. No third-party application runtime packages are required. See `installer/README.md` to build the installer.

Development menu code is conditional on `ODIN_DEV`; the main build does not define it. Production and development builds have separate identities and settings.

## Checks and desktop behavior

The application supports `--self-test <report.json>`, `--test-updater <report.json>`, `--menu-test <report.json>`, and `--smoke-test <report.json>`. Rendering tests need a Windows desktop session.

Odin uses cursor position, mouse-button state, Escape-key state and visible-window geometry for interaction. There are no telemetry uploads. Network access is used for GitHub release checks and downloads.
