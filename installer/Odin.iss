#ifndef StartupValueName
  #define StartupValueName "Odin"
#endif
#ifndef InstallerAppId
  #define InstallerAppId "{{AB8BA377-1739-4D1D-9671-FE9D30ED755C}"
#endif
#ifndef InstallerName
  #define InstallerName "Odin"
#endif
#ifndef OutputName
  #define OutputName "Odin-Setup-3.2.0"
#endif
[Setup]
AppId={#InstallerAppId}
AppName=Odin
AppVersion=3.2.0
AppVerName=Odin 3.2.0
AppPublisher=BurtFablehoff
AppPublisherURL=https://github.com/BurtFablehoff/Odin
AppSupportURL=https://github.com/BurtFablehoff/Odin/issues
AppUpdatesURL=https://github.com/BurtFablehoff/Odin/releases/latest
DefaultDirName={localappdata}\Programs\Odin
DefaultGroupName={#InstallerName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableProgramGroupPage=yes
AppMutex=Local\OdinCompanionPortableV3
CloseApplications=yes
RestartApplications=no
UninstallDisplayName=Odin
UninstallDisplayIcon={app}\Odin.exe
OutputDir=..\build
OutputBaseFilename={#OutputName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=3.2.0.0

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\build\Odin.exe"; DestDir: "{app}"

[Icons]
Name: "{group}\{#InstallerName}"; Filename: "{app}\Odin.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\{#InstallerName}"; Filename: "{app}\Odin.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Odin.exe"; Description: "Launch Odin"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#StartupValueName}', StartupCommand) then
      if CompareText(StartupCommand, '"' + ExpandConstant('{app}\Odin.exe') + '"') = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#StartupValueName}');
  end;
end;
