@echo off
REM xxWPR.cmd wrapper
REM This is only a thin wrapper around wpr to allow zipping of the relevant etl file, ngen pdbs and screenshots. 
REM when stopping the trace and the output file name is a compressed file it is compressed along with anohter optional directory which 
REM usually contains the screenshots of the trace 

setlocal ENABLEDELAYEDEXPANSION
set ScriptDir=%~dp0

set WPR=wpr.exe
rem set WPR="C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpr.exe"

if "%1" EQU "" goto :Help
if "%1" EQU "-help" goto :Help
if "%1" EQU "-stop" goto :Stop

%WPR% %*
goto :EOF

:Stop
set OutFileName=%2
set WprOutFileName=%2
set CompressEtl=0

if "%3" NEQ "" (
	set ETWScreenshotDir=%3
) else (
	set ETWScreenshotDir=C:\temp\ETWControllerScreenshots
)

if "!OutFileName:~-4!" EQU ".zip" (
	set WprOutFileName=!WprOutFileName:.zip=.etl!
	set CompressEtl=1
)


if "!OutFileName:~-3!" EQU ".7z" (
	REM Use multi threaded compression when 7z is used
	set CompressionOption=-m0=lzma2
	set WprOutFileName=!WprOutFileName:.7z=.etl!
	set CompressEtl=1
)




echo Stopping Tracing 
%WPR% -stop !WprOutFileName!

set ScreenshotDir=!WprOutFileName!.Screenshots

set LogFile=!WprOutFileName!.log
del !LogFile! 2> NUL

rem echo  ext: "!OutFileName:~-4!" Wpr Output file name: !WprOutFileName!, OutputFileName: !OutFileName! ScreenshotDir "!ScreenshotDir!"

echo Copying Screenshots from !ETWScreenshotDir! to !ScreenshotDir!
mkdir !ScreenshotDir! 2> NUL
del /Q !ScreenshotDir!\*.*
copy /Y !ETWScreenshotDir!\*.* !ScreenshotDir! > NUL
call :ClearErrorLevel

if "!CompressEtl!" EQU "1" (
	del "!OutFileName!" 2> NUL
	echo Compressing files to !OutFileName!
	echo ETL File !WprOutFileName!
	echo NGen !WprOutFileName!.NGenPDB
	echo Screenshots !ScreenshotDir!
	set CompressCommand="!ScriptDir!\7z" a !CompressionOption! !OutFileName! !WprOutFileName! -r !ScreenshotDir! !WprOutFileName!.NGenPDB  
	echo !CompressCommand!
	echo !CompressCommand! >> !LogFile!
	!CompressCommand! >> !LogFile!
	if ERRORLEVEL 2 (
		echo 7z returned an error. See log file "!LogFile!" for more details. 
		goto :EOF
	) ELSE (
		echo Cleaning temporary files
		!ScriptDir!\7z a !CompressionOption! !OutFileName! !LogFile! > NUL
		del !LogFile!
		del /Q !WprOutFileName!
		if "!ScreenshotDir!" NEQ "" (
			rd /q /s !ScreenshotDir!
		)
		
		if  "!WprOutFileName!" NEQ "" (
			rd /q /s !WprOutFileName!.NGenPDB
		)
		call :ClearErrorLevel
	)
)
goto :EOF

:ClearErrorLevel
REM after failed copy operations we might return from our script an error level > 0 which is interpreted
REM by other scripts as failure
exit /b 0

:Help
echo xxWPR is a wrapper around WPR. You can pass the same command line arguments to it like WPR.
echo The only difference is that for the -stop command you can pass as output file name not only ETL but also .7z or .zip file names.
echo     -stop xxx.7z [ScreenshotDir] will generate a 7z file from the generated etl file and compress the etl, ngen and optional screenshot folder into
echo                                  into the archive file. If all goes well the input files are deleted and only the compressed file is kept.
goto :EOF