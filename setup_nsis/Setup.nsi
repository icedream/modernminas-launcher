!include MUI2.nsh
!include "FileFunc.nsh"

Name "Modern Minas Setup"
OutFile "..\Application Files\mmlaunch_${VERSION}\setup.exe"
RequestExecutionLevel user
InstallDir $APPDATA\.modernminas\launcher
BrandingText "http://www.modernminas.de/"
SetCompressor /SOLID lzma
SetCompress force
XPStyle on

;;;;;;;;;;;;;;;
; Definitions
;;;;;;;;;;;;;;;

!ifndef CONFIGURATION
!define CONFIGURATION "Release"
;!define CONFIGURATION "Debug"
!endif
!ifndef SOURCE_BINPATH
!define SOURCE_BINPATH "..\launcher\bin\${CONFIGURATION}\"
;!define SOURCE_BINPATH "..\Application Files\mmlaunch_${VERSION}\"
!endif
!ifndef SOURCE_ROOT
!define SOURCE_ROOT ".."
!endif
!define MUI_CUSTOMFUNCTION_GUIINIT onGUIInit
!define MUI_ICON "..\launcher\Images\MMsymbol.ico"
!define MUI_UNICON "..\launcher\Images\MMsymbol.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP images\Header\nsis.bmp
!define MUI_HEADERIMAGE_UNBITMAP images\Header\nsis.bmp
!define MUI_HEADER_TRANSPARENT_TEXT
!define MUI_WELCOMEFINISHPAGE_BITMAP images\Wizard\win.bmp
!define MUI_UNWELCOMEFINISHPAGE_BITMAP images\Wizard\win.bmp
!define MUI_INSTFILESPAGE_PROGRESSBAR colored
;!define MUI_FINISHPAGE_NOAUTOCLOSE
;!define MUI_FINISHPAGE_RUN
;!define MUI_FINISHPAGE_RUN_TEXT "Run launcher"
;!define MUI_FINISHPAGE_RUN_FUNCTION "Launch"

;;;;;;;;;;;;;
; Sections
;;;;;;;;;;;;;

Section "Install"
	SetOutPath $INSTDIR
	
	nsExec::ExecToLog "taskkill /F /IM mmlaunch.exe"
	
	File /r "${SOURCE_BINPATH}\*.exe"
	File /r "${SOURCE_BINPATH}\*.dll"
	File /r "${SOURCE_BINPATH}\*.pdb"
	File /r ".\additional_files\*"

	FileOpen $4 "$OUTDIR\version.txt" w
	FileWrite $4 ${VERSION}
	FileClose $4
	
	WriteUninstaller "$OUTDIR\uninstall.exe"
	
	CreateDirectory "$SMPROGRAMS\Modern Minas"
	CreateShortCut "$SMPROGRAMS\Modern Minas\Launch Modern Minas.lnk" "$INSTDIR\mmlaunch.exe"
	CreateShortCut "$SMPROGRAMS\Modern Minas\Uninstall Modern Minas.lnk" "$INSTDIR\uninstall.exe"
	
	Exec "$INSTDIR\mmlaunch.exe"
SectionEnd

Section "Uninstall"
	RMDir /r "$SMPROGRAMS\Modern Minas"
	RMDir /r "$INSTDIR"
	RMDir /r "$APPDATA\.modernminas"
SectionEnd

;;;;;;;;;;;;;
; Hooks
;;;;;;;;;;;;;

Function onGUIInit
	; Apply Aero if available
	Aero::Apply
	
FunctionEnd

Function .onInit
	;; Multi-language selection support
	;!insertmacro MUI_LANGDLL_DISPLAY
	
	; Apply setup skin via SkinCrafter
	SetOutPath $TEMP
	File /oname=setup.skf "skins\setup1.skf"
	NSIS_SkinCrafter_Plugin::skin /NOUNLOAD $TEMP\setup.skf
	Delete $TEMP\setup.skf
FunctionEnd

Function Launch
	Exec "$INSTDIR\mmlaunch.exe"
FunctionEnd

;;;;;;;;;;;;;
; Pages
;;;;;;;;;;;;;

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "${SOURCE_ROOT}\LICENSE.rtf"
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
;!insertmacro MUI_UNPAGE_FINISH

;;;;;;;;;;;;;
; Languages
;;;;;;;;;;;;;

!insertmacro MUI_LANGUAGE "English"