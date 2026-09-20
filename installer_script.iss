; Script generated for Inno Setup 6
; MeroDokan Cafe POS Billing System Installer

#define MyAppName "Mero Dokan Cafe"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Mero Dokan Technologies"
#define MyAppURL "https://merodokan.com"
#define MyAppExeName "MeroDokanCafe.exe"
#define MySourceDir "publish"

[Setup]
AppId={{E84B9123-28C5-4F3A-9D6B-8B2379EB981A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; Output installer executable name and location
OutputDir=Installer_Output
OutputBaseFilename=MeroDokanCafe_Setup_v1.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=app_icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
CloseApplications=yes
RestartApplications=no
VersionInfoVersion=1.0.0.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Mero Dokan Cafe Setup
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Copy all published files and subdirectories, excluding pdb files and dbconfig.txt
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,dbconfig.txt"; Flags: ignoreversion recursesubdirs createallsubdirs
; Copy dbconfig.txt only if it does not already exist so user database configuration is preserved across updates
Source: "{#MySourceDir}\dbconfig.txt"; DestDir: "{app}"; Flags: onlyifdoesntexist uninsneveruninstall

[Dirs]
Name: "{app}"; Permissions: users-modify

[Icons]
; Start Menu and Desktop Shortcuts with custom logo icon
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Option to launch application upon installation finish
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Helper function to check if .NET Framework 4.8 or later is installed
function IsDotNet48Installed(): Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  // 528040 is the Release DWORD for .NET Framework 4.8 on Windows 10 May 2019 Update and all subsequent platforms
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
  begin
    if Release >= 528040 then
      Result := True;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if not IsDotNet48Installed() then
  begin
    if MsgBox('Mero Dokan Cafe requires Microsoft .NET Framework 4.8 or newer.' + #13#10#13#10 +
              'It appears that .NET Framework 4.8 is not currently installed on this system.' + #13#10#13#10 +
              'Would you like to open the Microsoft website to download .NET Framework 4.8 now?',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
  end;
end;
