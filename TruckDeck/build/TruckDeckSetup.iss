; TruckDeck Windows installer - compile with Inno Setup 6 (iscc.exe)
; Run: .\build\build_installer.ps1

#define MyAppName "TruckDeck"
#define MyAppVersion "1.6.5.2"
#define MyAppPublisher "TruckDeck"
#define MyAppExeName "TruckDeck.exe"
#ifndef ReleaseDir
#define ReleaseDir "..\..\TruckDeck_build_1.6.5.2\release"
#endif
#ifndef OutputDir
#define OutputDir "..\..\TruckDeck_build_1.6.5.2"
#endif

[Setup]
AppId={{A7B3C9D1-4E5F-6789-ABCD-EF0123456789}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={code:GetDefaultInstallDir}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=TruckDeck-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=..\TruckDeck.Server\Resources\app.ico
LicenseFile=INSTALL.txt
InfoBeforeFile=INSTALL.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"
Name: "installmod"; Description: "Install TruckDeck NAV mod to Documents\Euro Truck Simulator 2\mod"; GroupDescription: "Optional:"; Flags: checkedonce
Name: "launch"; Description: "Launch TruckDeck after setup"; GroupDescription: "Optional:"; Flags: checkedonce

[Files]
Source: "{#ReleaseDir}\TruckDeck\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#ReleaseDir}\Extras\*"; DestDir: "{app}\Extras"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\TruckDeck Dashboard"; Filename: "http://127.0.0.1:25555/"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Parameters: "-install -frominstaller"; WorkingDir: "{app}"; Description: "Complete plugin and firewall setup"; Flags: runhidden waituntilterminated
Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent runascurrentuser; Tasks: launch

[UninstallRun]
Filename: "{app}\{#MyAppExeName}"; Parameters: "-uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "TruckDeckCleanup"

[Code]
var
  Ets2Page, AtsPage: TInputDirWizardPage;
  DetectedEts2, DetectedAts: String;
  ChosenEts2, ChosenAts: String;

function PosFrom(const SubStr, S: String; Start: Integer): Integer;
var
  Tail: String;
begin
  Tail := Copy(S, Start, MaxInt);
  Result := Pos(SubStr, Tail);
  if Result > 0 then
    Result := Result + Start - 1;
end;

function UnescapeVdfPath(const S: String): String;
begin
  Result := S;
  StringChangeEx(Result, '\\', '\', True);
end;

function RootExists(const Path: String): Boolean;
begin
  Result := (Path <> '') and DirExists(Path);
end;

function AddUniqueRoot(var Roots: TArrayOfString; const Path: String): Boolean;
var
  I: Integer;
  P: String;
begin
  Result := False;
  if not RootExists(Path) then
    Exit;
  P := RemoveBackslashUnlessRoot(Path);
  for I := 0 to GetArrayLength(Roots) - 1 do
    if CompareText(Roots[I], P) = 0 then
      Exit;
  SetArrayLength(Roots, GetArrayLength(Roots) + 1);
  Roots[GetArrayLength(Roots) - 1] := P;
  Result := True;
end;

function TryExtractSecondQuoted(const S: String; var OutVal: String): Boolean;
var
  I, QuoteCount, P2: Integer;
begin
  Result := False;
  OutVal := '';
  QuoteCount := 0;
  for I := 1 to Length(S) do
  begin
    if S[I] = '"' then
    begin
      Inc(QuoteCount);
      if QuoteCount = 2 then
      begin
        P2 := PosFrom('"', S, I + 1);
        if P2 > I then
        begin
          OutVal := Copy(S, I + 1, P2 - I - 1);
          Result := True;
          Exit;
        end;
      end;
    end;
  end;
end;

function LooksLikeDrivePath(const S: String): Boolean;
begin
  Result := (Length(S) >= 3) and ((S[2] = ':') and ((S[3] = '\') or (S[3] = '/')));
end;

procedure ParseLibraryFoldersVdf(const VdfPath: String; var Roots: TArrayOfString);
var
  Lines: TArrayOfString;
  I: Integer;
  Line, Val: String;
begin
  if not LoadStringsFromFile(VdfPath, Lines) then
    Exit;
  for I := 0 to GetArrayLength(Lines) - 1 do
  begin
    Line := Trim(Lines[I]);
    if (Pos('"path"', Line) > 0) and TryExtractSecondQuoted(Line, Val) then
      AddUniqueRoot(Roots, UnescapeVdfPath(Val))
    else if TryExtractSecondQuoted(Line, Val) and LooksLikeDrivePath(UnescapeVdfPath(Val)) then
      AddUniqueRoot(Roots, UnescapeVdfPath(Val));
  end;
end;

procedure CollectSteamLibraryRoots(var Roots: TArrayOfString);
var
  SteamPath: String;
  I, Drive: Integer;
  Lib, Candidate: String;
begin
  SetArrayLength(Roots, 0);
  if RegQueryStringValue(HKCU, 'Software\Valve\Steam', 'SteamPath', SteamPath) then
    AddUniqueRoot(Roots, SteamPath);

  for I := 0 to GetArrayLength(Roots) - 1 do
    ParseLibraryFoldersVdf(AddBackslash(Roots[I]) + 'steamapps\libraryfolders.vdf', Roots);

  for Drive := Ord('C') to Ord('Z') do
  begin
    Lib := Chr(Drive) + ':\SteamLibrary';
    AddUniqueRoot(Roots, Lib);
    Lib := Chr(Drive) + ':\Program Files (x86)\Steam';
    AddUniqueRoot(Roots, Lib);
    Lib := Chr(Drive) + ':\Program Files\Steam';
    AddUniqueRoot(Roots, Lib);
  end;
end;

function IsValidGamePath(const Path: String): Boolean;
begin
  Result := (Path <> '') and FileExists(AddBackslash(Path) + 'base.scs') and DirExists(AddBackslash(Path) + 'bin');
end;

function DetectGamePath(const GameFolder: String): String;
var
  Roots: TArrayOfString;
  I: Integer;
  Candidate: String;
begin
  Result := '';
  CollectSteamLibraryRoots(Roots);
  for I := 0 to GetArrayLength(Roots) - 1 do
  begin
    Candidate := AddBackslash(Roots[I]) + 'steamapps\common\' + GameFolder;
    if IsValidGamePath(Candidate) then
    begin
      Result := Candidate;
      Exit;
    end;
  end;
end;

procedure AutofillGamePage(Page: TInputDirWizardPage; const GameFolder, KnownDetected: String);
var
  Path, Found: String;
begin
  Path := Trim(Page.Values[0]);
  if Path <> '' then
    Exit;
  Found := KnownDetected;
  if Found = '' then
    Found := DetectGamePath(GameFolder);
  if Found <> '' then
    Page.Values[0] := Found;
end;

procedure UpdateGamePageHint(Page: TInputDirWizardPage; const GameLabel, Detected: String);
begin
  if Detected <> '' then
    Page.SubCaptionLabel.Caption :=
      'Auto-detected ' + GameLabel + ': ' + Detected + #13#10 +
      'Leave blank if you do not own ' + GameLabel + '.'
  else
    Page.SubCaptionLabel.Caption :=
      GameLabel + ' was not found via Steam libraries.' + #13#10 +
      'Browse manually, or leave blank if you do not own ' + GameLabel + '.';
end;

function GetDefaultInstallDir(Param: String): String;
var
  Ets2, Ats: String;
begin
  Ets2 := DetectGamePath('Euro Truck Simulator 2');
  if IsValidGamePath(Ets2) then
  begin
    Result := AddBackslash(Ets2) + 'Telemetry Server';
    Exit;
  end;
  Ats := DetectGamePath('American Truck Simulator');
  if IsValidGamePath(Ats) then
  begin
    Result := AddBackslash(Ats) + 'Telemetry Server';
    Exit;
  end;
  Result := ExpandConstant('{autopf}\TruckDeck');
end;

procedure InitializeWizard;
begin
  DetectedEts2 := DetectGamePath('Euro Truck Simulator 2');
  DetectedAts := DetectGamePath('American Truck Simulator');

  Ets2Page := CreateInputDirPage(wpSelectDir,
    'Euro Truck Simulator 2', 'Select your ETS2 installation folder.',
    'Searching Steam libraries for ETS2...',
    True, 'New Folder');
  Ets2Page.Add('ETS2 folder:');
  if DetectedEts2 <> '' then
    Ets2Page.Values[0] := DetectedEts2;

  AtsPage := CreateInputDirPage(Ets2Page.ID,
    'American Truck Simulator', 'Select your ATS installation folder.',
    'Searching Steam libraries for ATS...',
    True, 'New Folder');
  AtsPage.Add('ATS folder:');
  if DetectedAts <> '' then
    AtsPage.Values[0] := DetectedAts;
end;

procedure CurPageChanged(CurPageID: Integer);
var
  Found: String;
begin
  if CurPageID = Ets2Page.ID then
  begin
    AutofillGamePage(Ets2Page, 'Euro Truck Simulator 2', DetectedEts2);
    Found := Trim(Ets2Page.Values[0]);
    if Found <> '' then
      DetectedEts2 := Found;
    UpdateGamePageHint(Ets2Page, 'ETS2', DetectedEts2);
  end
  else if CurPageID = AtsPage.ID then
  begin
    AutofillGamePage(AtsPage, 'American Truck Simulator', DetectedAts);
    Found := Trim(AtsPage.Values[0]);
    if Found <> '' then
      DetectedAts := Found;
    UpdateGamePageHint(AtsPage, 'ATS', DetectedAts);
    AtsPage.SubCaptionLabel.Caption := AtsPage.SubCaptionLabel.Caption + #13#10 +
      'At least one game (ETS2 or ATS) is required.';
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
var
  Ets2, Ats: String;
begin
  Result := False;
  Ets2 := Trim(Ets2Page.Values[0]);
  if Ets2 = '' then
    Ets2 := DetectedEts2;
  Ats := Trim(AtsPage.Values[0]);
  if Ats = '' then
    Ats := DetectedAts;

  if PageID = AtsPage.ID then
  begin
    if IsValidGamePath(Ets2) then
    begin
      ChosenEts2 := Ets2;
      ChosenAts := '';
      Result := True;
    end;
  end
  else if PageID = Ets2Page.ID then
  begin
    if (not IsValidGamePath(Ets2)) and IsValidGamePath(Ats) then
    begin
      ChosenAts := Ats;
      ChosenEts2 := '';
      Result := True;
    end;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Path: String;
begin
  Result := True;
  if CurPageID = Ets2Page.ID then
  begin
    Path := Trim(Ets2Page.Values[0]);
    ChosenEts2 := Path;
    if (Path <> '') and (not IsValidGamePath(Path)) then
    begin
      MsgBox('The ETS2 folder is not valid (need base.scs and bin\).' + #13#10 +
        'Leave the field empty if you do not own ETS2.', mbError, MB_OK);
      Result := False;
    end;
    Exit;
  end;
  if CurPageID = AtsPage.ID then
  begin
    ChosenEts2 := Trim(Ets2Page.Values[0]);
    ChosenAts := Trim(AtsPage.Values[0]);
    if (ChosenEts2 <> '') and (not IsValidGamePath(ChosenEts2)) then
    begin
      MsgBox('The ETS2 folder is not valid (need base.scs and bin\).', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    if (ChosenAts <> '') and (not IsValidGamePath(ChosenAts)) then
    begin
      MsgBox('The ATS folder is not valid (need base.scs and bin\).' + #13#10 +
        'Leave the field empty if you do not own ATS.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    if (ChosenEts2 = '') and (ChosenAts = '') then
    begin
      MsgBox('Select at least one game folder (ETS2 or ATS).', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

function GetEts2Arg(Param: String): String;
begin
  if ChosenEts2 = '' then
    ChosenEts2 := Trim(Ets2Page.Values[0]);
  if ChosenEts2 = '' then
    Result := 'N/A'
  else
    Result := ChosenEts2;
end;

function GetAtsArg(Param: String): String;
begin
  if ChosenAts = '' then
    ChosenAts := Trim(AtsPage.Values[0]);
  if ChosenAts = '' then
    Result := 'N/A'
  else
    Result := ChosenAts;
end;

procedure SaveInstallArgs;
var
  Ets2, Ats: String;
begin
  Ets2 := GetEts2Arg('');
  Ats := GetAtsArg('');
  ChosenEts2 := Ets2;
  ChosenAts := Ats;
  SaveStringToFile(ExpandConstant('{app}\.truckdeck-install.ini'),
    'ets2=' + Ets2 + #13#10 + 'ats=' + Ats, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ModSrc, ModDst: String;
begin
  if CurStep = ssInstall then
    SaveInstallArgs;

  if CurStep = ssPostInstall then
  begin
    if WizardIsTaskSelected('installmod') then
    begin
      ModSrc := ExpandConstant('{app}\Extras\TruckDeck_NAV.scs');
      if FileExists(ModSrc) then
      begin
        ModDst := ExpandConstant('{userdocs}\Euro Truck Simulator 2\mod');
        ForceDirectories(ModDst);
        FileCopy(ModSrc, ModDst + '\TruckDeck_NAV.scs', False);
      end;
    end;
  end;
end;
