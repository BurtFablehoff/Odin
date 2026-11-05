# Windows installer build

Build the main application into `build/Odin.exe` first. With Inno Setup 7.1.1 available, run `ISCC.exe installer/Odin.iss` from this repository. The output is `build/Odin-Setup-3.1.1.exe`.

The installer uses a stable AppId, installs for the current user under local application data, creates a Start menu shortcut, offers an optional desktop shortcut, and registers an uninstaller. It does not request administrator privileges. Setup checks the main application's mutex and handles files in use. User settings remain in `OdinCompanionV3` when uninstalling.
