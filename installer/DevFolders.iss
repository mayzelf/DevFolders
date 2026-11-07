; Per-user installer: no admin rights needed. Build with
;   iscc /DAppVersion=1.0.0 /DPublishDir=..\publish installer\DevFolders.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif

[Setup]
AppId={{6F3B2C1E-8D4A-4E7B-9C2F-1A5D7E9B3C40}
AppName=DevFolders
AppVersion={#AppVersion}
AppPublisher=mayzelf
DefaultDirName={localappdata}\Programs\DevFolders
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=DevFolders-Setup-{#AppVersion}
SetupIconFile=..\src\DevFolders\Assets\DevFolders.ico
UninstallDisplayIcon={app}\DevFolders.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{autoprograms}\DevFolders"; Filename: "{app}\DevFolders.exe"

[Run]
Filename: "{app}\DevFolders.exe"; Parameters: "install"; Flags: runhidden waituntilterminated
Filename: "{app}\DevFolders.exe"; Description: "Open DevFolders"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "{app}\DevFolders.exe"; Parameters: "uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "RemoveMenu"
