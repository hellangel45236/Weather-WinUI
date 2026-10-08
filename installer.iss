; Script generated for Inno Setup 6
; Weather App - WinUI 3 Modern Weather Application

#define MyAppName "Weather App"
#define MyAppVersion "3.0.2"
#define MyAppPublisher "Weather WinUI Team"
#define MyAppExeName "WeatherApp.exe"

[Setup]
; Basic setup info
AppId={{E68F5B8B-9321-4F43-9871-A263C7B652D1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\WeatherApp
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=D:\Project\Weather WinUI\Output
OutputBaseFilename=WeatherApp_Setup_v3.0.2
SetupIconFile=D:\Project\Weather WinUI\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/normal
SolidCompression=no
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
VersionInfoVersion=3.0.2.0
VersionInfoCompany=Weather WinUI Team
VersionInfoDescription=Weather App Modern WinUI 3 Application
VersionInfoCopyright=Copyright (C) 2026 Weather WinUI Team
VersionInfoProductName=Weather App
VersionInfoProductVersion=3.0.2.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "D:\Project\Weather WinUI\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "D:\Project\Weather WinUI\publish\Assets\Fonts\fa-solid-900.ttf"; DestDir: "{autofonts}"; FontInstall: "Font Awesome 6 Free Solid"; Flags: onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent; WorkingDir: "{app}"
