# Odin desktop companion source

Odin is a portable Windows desktop companion written in C# using Windows Forms and the Windows desktop APIs. This folder contains the current application source snapshot (assembly version 3.1.0.0), including its behavior engine, renderer, updater, and existing checks. It is not a claim that this snapshot matches an earlier release executable byte for byte.

## Artwork

This is a source-only publication. Photographs, reference images, animation frames, icons, packed artwork, executables, and local project records are intentionally excluded. The original artwork is private and is not licensed or distributed with this source.

The normal application needs a compatible artwork pack embedded as `Odin.Assets`. Without that resource it will compile but will not display the companion. `app/Art.cs` defines the pack reader and manifest types: the pack has a little-endian `0x324E444F` header, a 32-bit UTF-8 JSON byte count, the version-2 JSON manifest, and PNG payloads indexed by offsets relative to the payload start. Frames are 1024 by 1024 pixels. No artwork is generated or downloaded by the build script.

## Build

Use 64-bit Windows with the .NET Framework C# compiler and the framework assemblies listed in `build.ps1`. No third-party packages are required.

```powershell
# Check compilation without supplying artwork:
.\build.ps1 -CompileOnly

# Build a runnable copy using your own compatible artwork pack:
.\build.ps1 -AssetPack 'D:\YourAssets\Odin.assets'
```

Output is written to `build/Odin.exe`. Build output and all artwork are ignored by Git.

## Desktop and network behavior

The companion uses cursor position, mouse-button state, Escape-key state, and visible-window geometry for interaction, menu dismissal, and perching. Settings are stored under the current user's local application data in `OdinCompanionV3`. The source does not include personal settings, credentials, or telemetry uploads.

The updater checks GitHub releases for `BurtFablehoff/Odin` and can download and execute a newer release. Automatic updates are enabled by default and can be disabled in the app menu. Downloads require an HTTPS release URL for that repository and a matching SHA-256 digest; the updater supports backup and rollback. These checks rely on the release repository remaining trustworthy.

## Existing checks

`app/Tests.cs` contains the existing checks. A compiled copy can run `--test-behavior <report.json>` and `--test-updater <report.json>` without artwork. `--self-test <report.json>` and the rendering/native checks require a compatible embedded artwork pack and an appropriate Windows desktop session.
