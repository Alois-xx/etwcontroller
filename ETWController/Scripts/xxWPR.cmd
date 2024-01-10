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
set CmdLine=

:Parse

REM Escape %1 argument to prevent for already quoted arguments double quotes which would result in evaluation errors in if clause
REM e.g. when %1 is "xxx sss" -> if "%1" EQU -> if ""xxx sss"" will lead to parsing error sss"" was unexpected at this time.
set Arg1=
for %%x in (%1) do (
		set Arg1=%%~x
	)

if "!Arg1!" EQU "-start" (
  REM we cannot use !Arg1! because it will swallow ! characters. E.g. Arg1=Profile.wprp!Network will become Profile.wprpNetwork when assigned to Arg1
  set CmdLine=!CmdLine! -start %2
  shift
) else if "!Arg1!" EQU "-setprofint" (
	set SetProfintCmdLine=-setprofint %2
	shift
) else (
	REM pass through other command line options
	set CmdLine=!CmdLine! %1
)

if "!Arg1!" EQU "" goto :Exec
shift
goto :Parse

:Exec
echo Wpr Call: %WPR% !CmdLine!
%WPR% !CmdLine!

if "!SetProfintCmdLine!" NEQ "" (
	echo Setprofint Cmd: %WPR% !SetProfintCmdLine!
	%WPR% !SetProfintCmdLine!
)

goto :EOF

:Stop
set OutFileName=%2
set WprOutFileName=%2
set CompressEtl=0
set StopOptions=

if "%3" NEQ "" (
	if /I "%3" EQU "-skipPdbGen" (
		set StopOptions=%3
	) else  (
		set ETWScreenshotDir=%3
	)
) else (
	set ETWScreenshotDir=C:\temp\ETWControllerScreenshots
)


if "%4" NEQ "" (
	if /I "%4" EQU "-skipPdbGen" (
		set StopOptions=%4
	) else  (
		set ETWScreenshotDir=%4
	)
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
%WPR% -stop !WprOutFileName! !StopOptions!

set NgenPDBFolder=
if EXIST "!WprOutFileName!.NGenPDB" (
	set NgenPDBFolder=!WprOutFileName!.NGenPDB
)

set EmbeddedPDBFolder=
if EXIST "!WprOutFileName!.EmbeddedPdbs" (
	set EmbeddedPDBFolder=!WprOutFileName!.EmbeddedPdbs
)



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
	set CompressCommand="!ScriptDir!\7z" a !CompressionOption! !OutFileName! !WprOutFileName! -r !ScreenshotDir! !NgenPDBFolder! !EmbeddedPDBFolder!
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
		
		if  EXIST "!NgenPDBFolder!" (
			rd /q /s !NgenPDBFolder!
		)

		if EXIST "!EmbeddedPDBFolder!" (
			rd /q /s !EmbeddedPDBFolder!
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