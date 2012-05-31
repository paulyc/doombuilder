; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

[Setup]
AppName=Doom Builder 2
AppVerName=Doom Builder 2.1
AppPublisher=CodeImp
AppPublisherURL=http://www.codeimp.com/
AppSupportURL=http://www.doombuilder.com/
AppUpdatesURL=http://www.doombuilder.com/
DefaultDirName={pf}\Doom Builder 2
DefaultGroupName=Doom Builder
AllowNoIcons=true
InfoBeforeFile=..\Setup\disclaimer.txt
OutputDir=..\Release
OutputBaseFilename=builder2_setup
Compression=lzma/ultra64
SolidCompression=true
SourceDir=..\Build
SetupLogging=false
AppMutex=doombuilder2
PrivilegesRequired=admin
ShowLanguageDialog=no
LanguageDetectionMethod=none
MinVersion=0,5.01.2600
UninstallDisplayIcon={app}\Builder.exe
WizardImageFile=..\Setup\WizModernImage-IS.bmp
WizardSmallImageFile=..\Setup\WizModernSmallImage-IS.bmp

[Languages]
Name: english; MessagesFile: compiler:Default.isl

[Tasks]
Name: desktopicon; Description: {cm:CreateDesktopIcon}; GroupDescription: {cm:AdditionalIcons}; Flags: unchecked

[Files]
Source: Setup\dotnetfx35setup.exe; DestDir: {tmp}; Flags: dontcopy
Source: Setup\slimdx.msi; DestDir: {tmp}; Flags: dontcopy
Source: Builder.exe; DestDir: {app}; Flags: ignoreversion
Source: Builder.cfg; DestDir: {app}; Flags: ignoreversion
Source: Refmanual.chm; DestDir: {app}; Flags: ignoreversion
Source: DevIL.dll; DestDir: {app}; Flags: ignoreversion
Source: Sharpzip.dll; DestDir: {app}; Flags: ignoreversion
Source: Scintilla.dll; DestDir: {app}; Flags: ignoreversion
Source: Trackbar.dll; DestDir: {app}; Flags: ignoreversion
Source: SlimDX.dll; DestDir: {app}; Flags: ignoreversion
Source: GPL.txt; DestDir: {app}; Flags: ignoreversion
Source: Compilers\*; DestDir: {app}\Compilers; Flags: ignoreversion recursesubdirs
Source: Configurations\*; DestDir: {app}\Configurations; Flags: ignoreversion recursesubdirs
Source: Scripting\*; DestDir: {app}\Scripting; Flags: ignoreversion recursesubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files
Source: Plugins\BuilderModes.dll; DestDir: {app}\Plugins; Flags: ignoreversion
Source: Plugins\Loadorder.cfg; DestDir: {app}\Plugins; Flags: ignoreversion onlyifdoesntexist
Source: Sprites\*; DestDir: {app}\Sprites; Flags: ignoreversion recursesubdirs

[Icons]
Name: {group}\Doom Builder; Filename: {app}\Builder.exe
Name: {group}\{cm:UninstallProgram,Doom Builder}; Filename: {uninstallexe}
Name: {commondesktop}\Doom Builder; Filename: {app}\Builder.exe; Tasks: desktopicon

[Run]

[UninstallDelete]
Name: {localappdata}\Doom Builder; Type: filesandordirs
Name: {app}; Type: filesandordirs
[InstallDelete]
Name: {app}\Builder.pdb; Type: files
[Registry]
Root: HKLM; Subkey: SOFTWARE\CodeImp\Doom Builder\; ValueType: string; ValueName: Location; ValueData: {app}; Flags: uninsdeletevalue
[Messages]
ReadyLabel2a=Continue to begin with the installation, or click Back if you want to review or change any settings.
[Code]
// Global variables
var
	page_info_net: TOutputMsgWizardPage;
	page_info_netfailed: TOutputMsgWizardPage;
	page_setup_net: TOutputProgressWizardPage;
	page_setup_components: TOutputProgressWizardPage;
	componentsinstalled: Boolean;
	restartneeded: Boolean;
	netinstallfailed: Boolean;
	netisinstalled: Boolean;

procedure CheckNetIsInstalled();
begin
	netisinstalled := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5') or
					  RegKeyExists(HKLM, 'SOFTWARE\Wow6432Node\Microsoft\NET Framework Setup\NDP\v3.5');
end;

// When the wizard initializes
procedure InitializeWizard();
begin
	restartneeded := false;
	componentsinstalled := false;
	netinstallfailed := false;
	CheckNetIsInstalled();

	page_info_net := CreateOutputMsgPage(wpPreparing,
		'Installing Microsoft .NET Framework', '',
		'Setup has detected that your system is missing the required version of the Microsoft .NET Framework. ' +
		'Setup will now download and install or update your Microsoft .NET Framework. This requires an internet connection ' +
		'and may take several minutes to complete.' + #10 + #10 +
		'WARNING: The installer will download the Microsoft .NET Framework from the internet, but the progress bar will not ' +
		'go forward until the download is complete. You may send Microsoft an angry letter about that.' + #10 + #10 +
		'Click Install to begin.');

	page_info_netfailed := CreateOutputMsgPage(page_info_net.ID,
		'Installing Microsoft .NET Framework', '',
		'Setup could not install the Microsoft .NET Framework. Make sure you have an internet connection ' +
		'and click Back to try again.' + #10 + #10 +
		'Click Back to try again, or Cancel to exit Setup.');

	page_setup_net := CreateOutputProgressPage('Installing Microsoft .NET Framework', 'Setup is installing Microsoft .NET Framework, please wait.....');
	page_setup_components := CreateOutputProgressPage('Installing Components', 'Setup is installing required components. This may take a few minutes......');
end;



// This is called to check if a page must be skipped
function ShouldSkipPage(PageID: Integer): Boolean;
begin
	// Skip the .NET page?
	if(PageID = page_info_net.ID) then
		Result := netisinstalled
	else if(PageID = page_info_netfailed.ID) then
		Result := (not netinstallfailed) and netisinstalled
	else
		Result := false;
end;


// This is called to determine if we need to restart
function NeedRestart(): Boolean;
begin
	Result := restartneeded;
end;


// This is called when the current page changes
procedure CurPageChanged(CurPageID: Integer);
var
	errorcode: Integer;
begin
	if(CurPageID = wpReady) then
	begin
		if(netisinstalled = false) then
			WizardForm.NextButton.Caption := 'Next >';
	end
	else if(CurPageID = wpFinished) then
	begin
		if(componentsinstalled = false) then
		begin
			page_setup_components.Show;
			ExtractTemporaryFile('slimdx.msi');
			ShellExec('open', 'msiexec', ExpandConstant('/passive /i "{tmp}\slimdx.msi"'), '', SW_SHOW, ewWaitUntilTerminated, errorcode);
			componentsinstalled := true;
			page_setup_components.Hide;
		end
	end
	else if(CurPageID = page_info_net.ID) then
	begin
		WizardForm.NextButton.Caption := 'Install';
	end
	else if(CurPageID = page_info_netfailed.ID) then
	begin
		WizardForm.NextButton.Visible := true;
		WizardForm.NextButton.Enabled := false;
		WizardForm.BackButton.Visible := true;
		WizardForm.BackButton.Enabled := true;
		WizardForm.CancelButton.Visible := true;
		WizardForm.CancelButton.Enabled := true;
	end;
end;


// This is called when the Next button is clicked
function NextButtonClick(CurPage: Integer): Boolean;
var
	errorcode: Integer;
	tempfile: String;
begin

	// Next pressed on .NET info page?
	if(CurPage = page_info_net.ID) then
	begin
		// Show progress page and run setup
		page_setup_net.Show;
		try
		begin

			netinstallfailed := false;
			ExtractTemporaryFile('dotnetfx35setup.exe');
			// We copy the file to the real temp directory so that it isn't removed when Setup is closed.
			// Judging from the return codes, this installer may want to run again after a reboot.
			// See the return codes here: http://msdn.microsoft.com/en-us/library/cc160716.aspx
			tempfile := RemoveBackslash(GetTempDir()) + '\dotnetfx35setup.exe';
			FileCopy(ExpandConstant('{tmp}\dotnetfx35setup.exe'), tempfile, false);
			Exec(tempfile, '/qb /norestart', '', SW_SHOW, ewWaitUntilTerminated, errorcode);

			if((errorcode = 1641) or (errorcode = 3010)) then
			begin
				// Success, but restart needed!
				restartneeded := true;
			end
			else if(errorcode <> 0) then
			begin
				netinstallfailed := true;
			end;

			CheckNetIsInstalled();
		end
		finally
			page_setup_net.Hide;
		end;
	end

	Result := True;
end;


































