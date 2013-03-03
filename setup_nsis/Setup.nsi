!include MUI2.nsh
!include "FileFunc.nsh"

Name "Modern Minas Setup"
OutFile modernminas_setup.exe
RequestExecutionLevel user
InstallDir $APPDATA\.modernminas\launcher
BrandingText "http://www.modernminas.de/"
SetCompressor /SOLID lzma
SetCompress force
XPStyle on

;;;;;;;;;;;;;;;
; Definitions
;;;;;;;;;;;;;;;

!define MUI_CUSTOMFUNCTION_GUIINIT onGUIInit
!define MUI_ICON ..\launcher\Images\MMsymbol.ico
!define MUI_UNICON ..\launcher\Images\MMsymbol.ico
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP images\Header\nsis.bmp
!define MUI_HEADERIMAGE_UNBITMAP images\Header\nsis.bmp
!define MUI_HEADER_TRANSPARENT_TEXT
!define MUI_WELCOMEFINISHPAGE_BITMAP images\Wizard\win.bmp
!define MUI_UNWELCOMEFINISHPAGE_BITMAP images\Wizard\win.bmp
!define MUI_INSTFILESPAGE_PROGRESSBAR colored
!define MUI_FINISHPAGE_NOAUTOCLOSE

;;;;;;;;;;;;;
; Macros
;;;;;;;;;;;;;
!define StrRep "!insertmacro StrRep"
!macro StrRep output string old new
    Push "${string}"
    Push "${old}"
    Push "${new}"
    !ifdef __UNINSTALL__
        Call un.StrRep
    !else
        Call StrRep
    !endif
    Pop ${output}
!macroend
 
!macro Func_StrRep un
    Function ${un}StrRep
        Exch $R2 ;new
        Exch 1
        Exch $R1 ;old
        Exch 2
        Exch $R0 ;string
        Push $R3
        Push $R4
        Push $R5
        Push $R6
        Push $R7
        Push $R8
        Push $R9
 
        StrCpy $R3 0
        StrLen $R4 $R1
        StrLen $R6 $R0
        StrLen $R9 $R2
        loop:
            StrCpy $R5 $R0 $R4 $R3
            StrCmp $R5 $R1 found
            StrCmp $R3 $R6 done
            IntOp $R3 $R3 + 1 ;move offset by 1 to check the next character
            Goto loop
        found:
            StrCpy $R5 $R0 $R3
            IntOp $R8 $R3 + $R4
            StrCpy $R7 $R0 "" $R8
            StrCpy $R0 $R5$R2$R7
            StrLen $R6 $R0
            IntOp $R3 $R3 + $R9 ;move offset by length of the replacement string
            Goto loop
        done:
 
        Pop $R9
        Pop $R8
        Pop $R7
        Pop $R6
        Pop $R5
        Pop $R4
        Pop $R3
        Push $R0
        Push $R1
        Pop $R0
        Pop $R1
        Pop $R0
        Pop $R2
        Exch $R1
    FunctionEnd
!macroend
!insertmacro Func_StrRep ""
!insertmacro Func_StrRep "un."

;;;;;;;;;;;;;
; Sections
;;;;;;;;;;;;;

Section "Install"
	SetOutPath $INSTDIR
	
	;File /r /x *vshost* /x app.publish ..\launcher\bin\Release\*.exe
	;File /r /x *vshost* /x app.publish ..\launcher\bin\Release\*.dll
	
	NSISdl::download_quiet /TIMEOUT=10000 http://update.modernminas.de/bootstrap/version "$OUTDIR\version.txt"
	Pop $R0 ;Get the return value
	StrCmp $R0 "success" +3
		MessageBox MB_OK "Download failed: $R0"
		Quit
	
	FileOpen $4 "$OUTDIR\version.txt" r
	FileRead $4 $3
	FileClose $4
	
	NSISdl::download /TIMEOUT=10000 http://update.modernminas.de/bootstrap/package "$OUTDIR\package.lst"
	Pop $R0 ;Get the return value
	StrCmp $R0 "success" +3
		MessageBox MB_OK "Download failed: $R0"
		Quit
	
	FileOpen $4 "$OUTDIR\package.lst" r
	
	package_file:
		FileRead $4 $2
		IfErrors package_finished
		${GetParent} $2 $6
		CreateDirectory "$OUTDIR\$6"
		${StrRep} '$5' $2 '/' '\'
		DetailPrint "Downloading $5"
		DetailPrint " from http://update.modernminas.de/Application%20Files/mmlaunch_$3/$2.deploy"
		DetailPrint " to $OUTDIR\$5"
		NSISdl::download /TIMEOUT=10000 "http://update.modernminas.de/Application%20Files/mmlaunch_$3/$2.deploy" "$OUTDIR\$5"
		Goto package_file
	package_finished:
	
	FileClose $4
	
	WriteUninstaller "$OUTDIR\uninstall.exe"
	
	CreateDirectory "$SMPROGRAMS\Modern Minas"
	CreateShortCut "$SMPROGRAMS\Modern Minas\Launch Modern Minas.lnk" "$INSTDIR\mmlaunch.exe"
	CreateShortCut "$SMPROGRAMS\Modern Minas\Uninstall Modern Minas.lnk" "$INSTDIR\uninstall.exe"
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
	;SetOutPath $TEMP
	;File /oname=setup.skf "skins\setup1.skf"
	;NSIS_SkinCrafter_Plugin::skin /NOUNLOAD $TEMP\setup.skf
	;Delete $TEMP\setup.skf
FunctionEnd

;;;;;;;;;;;;;
; Pages
;;;;;;;;;;;;;

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE.rtf"
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

;;;;;;;;;;;;;
; Languages
;;;;;;;;;;;;;

!insertmacro MUI_LANGUAGE "English"