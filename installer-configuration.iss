; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "My Manager Extension"
#define MyAppNameForFile "My-Manager-Extension"
#define MyAppVersion "1.0.0.0"                   
#define MyAppVersionForFile "1-0-0-0"
#define MyAppPublisher "Takuma Otake"
#define MyAppExeName "MyManagerExtension.exe"
#define CurrentYear GetDateTimeString('yyyy', '', '')
#define dotNETInstallerExeName "windowsdesktop-runtime-6.0.7-win-x64.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{5FE3CD7C-982A-454B-9AF5-1393DF4E58DB}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName}
AppPublisher=Open Source Developer, {#MyAppPublisher}
DefaultDirName={commonpf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile={#SourcePath}\LICENSE
OutputBaseFilename={#MyAppNameForFile}_{#MyAppVersionForFile}_Setup
Compression=lzma
SolidCompression=yes                                
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
ShowLanguageDialog=auto
VersionInfoVersion={#MyAppVersion}
VersionInfoCopyright=Copyright (c) {#CurrentYear} {#MyAppPublisher}
OutputDir=Installers

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "armenian"; MessagesFile: "compiler:Languages\Armenian.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "bulgarian"; MessagesFile: "compiler:Languages\Bulgarian.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "corsican"; MessagesFile: "compiler:Languages\Corsican.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "icelandic"; MessagesFile: "compiler:Languages\Icelandic.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "slovak"; MessagesFile: "compiler:Languages\Slovak.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Files]
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\Microsoft.Windows.SDK.NET.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\My Manager Extension.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\My Manager Extension.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\My Manager Extension.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\My Manager Extension.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\WinRT.Runtime.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\Microsoft.Toolkit.Uwp.Notifications.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\Microsoft.Web.WebView2.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "My Manager Extension\bin\Release\net6.0-windows10.0.17763.0\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion createallsubdirs recursesubdirs
Source: "{#dotNETInstallerExeName}"; DestDir: "{tmp}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
Root: "HKLM"; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue

[Run]
Filename: "{tmp}\{#dotNETInstallerExeName}"; Parameters: "-install -quiet"

[UninstallRun]
Filename: "PowerShell"; Parameters: "-Command ""Start-Process -FilePath 'powershell' -Argument '-command taskkill -f -t -im ''{#MyAppExeName}''' -Verb RunAs; Start-Sleep -Seconds 3"""; Flags: runhidden; RunOnceId: "TerminateApplication"

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Shared"

[Code]
var ErrorCode: Integer;
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssDone then
  begin
    ShellExec('', ExpandConstant('"{app}\{#MyAppExeName}"'), '--prepare-data', '', SW_SHOW, ewNoWait, ErrorCode);
  end;
end;
