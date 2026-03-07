<#
.SYNOPSIS
    Records long term ETW traces using WPR with one or more configurable profiles to an output folder.
    When -RestartEveryMinutes duration (default is 10 minutes) is reached profiling is stopped and the files are renamed with a timestamp prefix.
    Profiling continues until -TotalRecordingHours is reached, Ctrl+C is pressed, or if the disk has less than 6 GB of free space. 
    After profiling was stopped you must merge (-Merge) the files before you can transfer them to another machine. 
    Additionally you can compress the files (-Zip or -SevenZip). When -SevenZip is used 7z exe must be in path or reside in current directory.
    7z provides better compression ratios (up to a factor two smaller than zip files).

.PARAMETER Start
    One or more WPR profile names to use (e.g. "Network.Light","CSwitch").
    Which profiles are supported depends on the .wprp file. To show the values use wpr -profiles xxx.wprp 
.PARAMETER SampleRate
    Select CPU profiling sample rate. Default is 10 000 which is a 1ms interval. The sample rate is up to 8 kHz (1250) or also a much larger value e.g. 10ms is 100000. 
.PARAMETER OutputFolder
    The folder where WPR writes the unmerged ETL files. Folder is created if it does not exist.
    Default is C:\ETW. If you use a different one you need to pass it to all commands (start, merge, zip) to ensure they operate on the same folder.

.PARAMETER WprpPath
    Path to the .wprp recording profile with SkipMerge enabled. This script needs a recording profile which creates unmerged etl files which are written to disk. It is needed to restart 
    ETW tracing as fast as possible to record into new files without loosing data. There are usually < 20s gaps in the recording. 
    Default is MultiProfile.wprp which must reside in same location a script or in ..\Profiles folder.

.PARAMETER RestartEveryMinutes
    Minutes to record before a new set of ETL files is created. Default is 10.

.PARAMETER TotalRecordingHours
    Total duration to run the recording in hours. You can specify fractions e.g. 0.5. If not specified, recording will continue until manually stopped with Ctrl+C or when disk space runs low.
    By default this is not set and you record as long as you have enough free disk space.

.PARAMETER SleepExit
    Seconds to sleep before exiting after Ctrl+C or when total recording time is reached. This gives you a chance to read the final log messages before the console window closes. Default is 0 seconds.

.PARAMETER Merge
    If specified, the script will merge files with the same timestamp prefix into a single ETL file using wpr -merge. This should be used after recording is stopped.
.PARAMETER Zip
    If specified, the script will compress merged ETL files using Compress-Archive (ZIP format). This should be used after recording is stopped and files are merged.
.PARAMETER SevenZip
    If specified, the script will compress merged ETL files using 7z (7z exe needs to be in path).
.PARAMETER Stop
    If specified, the script will signal any ongoing recording to stop by creating a marker file. This allows you to stop the recording gracefully from another process without needing to press Ctrl+C in the console.

.EXAMPLE
    .\LongtermRecording.ps1 -Merge -OutputFolder "c:\temp\multi" -SevenZip
    Merge and compress with 7z.
.EXAMPLE
    .\LongtermRecording.ps1 -Merge -OutputFolder "c:\temp\multi"
    Merge files but do not compress.
.EXAMPLE
    .\LongtermRecording.ps1 -OutputFolder "c:\temp\multi" -SevenZip
    Compress an already merged folder.
.EXAMPLE
    .\LongtermRecording.ps1 -Stop -Merge -SevenZip -OutputFolder "c:\temp\multi"
    Stop already running recording via a second script invocation. Pending data is saved to disk.
    Then merge the data and compress it.
.EXAMPLE
    .\LongtermRecording.ps1 -Start "Network.Light" -OutputFolder "c:\temp\network" -TotalRecordingHours 0.5
    Record Network data to folder c:\temp\Network for 30 minutes. Folder is created if it does not yet exist.
.EXAMPLE
    .\LongtermRecording.ps1 -Start "CSwitch","Network.Light" -OutputFolder "c:\temp\multi" -RestartEveryMinutes 3
    Record with multiple profiles and create every 3 minutes a new file because Context Switch tracing will create a lot of data.

#>
[CmdletBinding()]
param(
    [string[]]$Start,
    [string]$OutputFolder="C:\ETW",
    [string]$WprpPath,
    [int]$RestartEveryMinutes = 10,
    [int]$SampleRate = 10000,
    [float]$TotalRecordingHours,
    [switch]$Merge,
    [switch]$Zip,
    [switch]$SevenZip,
    [switch]$Stop,
    [int]$SleepExit
)

Set-StrictMode -Version Latest
# WPR instance name to not run into conflicts with default recording sessions.
$InstanceName = "-InstanceName LongTerm"
$DefaultProfileName = "MultiProfile.wprp"
# Set current working directory to script location 
Set-Location -LiteralPath (Split-Path -Parent $MyInvocation.MyCommand.Definition)
$RecordingStartTime=Get-Date
$FirstLog=$true
$Win10WarningShown=$false
# File used as marker to stop an already running recording from another script 
$StopMarkerFile = Join-Path $env:TEMP "StopRecording.Marker.txt"
$LastCommandOutput = "" 


function RunWprInLoop()
{
    # --- Validate prerequisites ---
    if (-not (Test-Path $OutputFolder -PathType Container)) 
    {
        mkdir $OutputFolder | Out-Null
    }

    $WprFile = GetDefaultWprpProfileIfNotSet

    if( [string]::IsNullOrWhiteSpace($WprFile) )
    {
        LogErrorAndPrint "WPRP file not found: '$WprpPath'. Please add an existing recording profile file with -WprpPath ... By default .\$DefaultProfileName and ..\Profiles\$DefaultProfileName are tried."
        SleepAndExit 1
    }

    if( $TotalRecordingHours -and $TotalRecordingHours -gt 0) 
    {
        $totalDuration = New-TimeSpan -Minutes ($TotalRecordingHours*60)
        LogAndPrint "Total recording time: $($totalDuration)" Cyan
    }
    
    CancelWpr

    while ($true)
    {
        if( -not (IsEnoughFreeDiskSpaceLeftGB 6))
        {
            SleepAndExit 1
        }

        StartWpr $WprFile
        LogAndPrint "Recording for $RestartEveryMinutes minutes. Press Space to save current data now, or press Ctrl+C to cancel and quit without saving." Green

        $shouldContinue = SleepOrCancelOnSpacePress

        StopWpr

        RenameRawWprFiles

        if( $shouldContinue -eq $false )
        {
            break
        }
    }
}

function StopRecording()
{
    "Stop requested at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Out-File -LiteralPath $StopMarkerFile -Encoding utf8
    LogAndPrint "Requested Stop of any ongoing recording. Pending ETW session data will be saved." Yellow
    # This should be fast enough to save data so one can start zipping data right away
    Sleep 5
}


function Merge()
{
   $kernelEtlFiles = Get-ChildItem -Path $OutputFolder -Filter "*_WPR_initiated_LongTerm_WPR System Collector.etl"
   foreach ($file in $kernelEtlFiles) 
   {
       $prefix = ($file.Name -split "_WPR_initiated")[0]
       # Find all ETL files sharing the same timestamp prefix
       $fileSet = Get-ChildItem -Path $OutputFolder -Filter "${prefix}_WPR_initiated_*.etl"
       MergeFiles($fileSet)
   }
}

function MergeFiles([pscustomobject]$filesToMerge)
{
    $mergeCommand = "-merge "
    foreach ($file in $filesToMerge) 
    {
        $mergeCommand += "`"$($file.FullName)`" "
    }
    $mergedEtl = ($filesToMerge[0].DirectoryName) + "\" + ($filesToMerge[0].BaseName -split "_WPR_initiated")[0] + "_LongTerm.etl"
    $mergeCommand += "`"$mergedEtl`""

    $wprExe = LocateWPRExe

    LogAndPrint "Merging file: $mergedEtl" Cyan
    Log "Merge Command: $wprExe $mergeCommand"
   
    $lret = RunProcessLowerPriority $wprExe $mergeCommand
    if ($lret.ExitCode -ne 0) 
    {
        LogErrorAndPrint "wpr -merge failed with exit code $($lret.ExitCode)"
        SleepAndExit $lret.ExitCode
    }
    else
    {
        LogAndPrint "Merge successful." Cyan
        foreach ($file in $filesToMerge) 
        {
            Remove-Item -LiteralPath $file.FullName -Force
        }

        $ngenPdbCache = "C:\ProgramData\WindowsPerformanceRecorder\NGenPdbs_Cache"
        if (Test-Path $ngenPdbCache -PathType Container)
        {
            $ngenPdbFolder = $mergedEtl +  ".NGENPDB"
            if (-not (Test-Path $ngenPdbFolder))
            {
                $linkOutput = cmd /c mklink /D "`"$ngenPdbFolder`"" "`"$ngenPdbCache`""
                LogInfo "$linkOutput"
            }
            else
            {
                LogInfo "Directory link already exists: $ngenPdbFolder"
            }
        }
        else
        {
            LogAndPrint "  NGen PDB cache folder not found, skipping link creation: $ngenPdbCache" Gray
        }
    }
}

function SleepOrCancelOnSpacePress()
{
    # --- Wait (interruptible) ---
    $interrupted = $false
    try
    {
        [Console]::TreatControlCAsInput = $true
        $deadline = (Get-Date).AddMinutes($RestartEveryMinutes)
        while ((Get-Date) -lt $deadline)
        {
            if ([Console]::KeyAvailable)
            {
                $key = [Console]::ReadKey($true)
                if ($key.Key -eq [ConsoleKey]::Spacebar)
                {
                    LogAndPrint "Space pressed — stopping recording early." Yellow
                    break
                }
                if ($key.Modifiers -band [ConsoleModifiers]::Control -and $key.Key -eq [ConsoleKey]::C) {
                    LogAndPrint "Ctrl+C pressed — cancelling recording and exiting." Yellow
                    $interrupted = $true
                    break
                }
            }

            if (HasTotalRecordingTimeElapsed) 
            {
                LogAndPrint "Stopping Recording. Reason: Total recording time reached." Yellow
                return $false
            }

            if (Test-Path $StopMarkerFile -PathType Leaf)
            {
                $lastWrite = (Get-Item -LiteralPath $StopMarkerFile).LastWriteTime
                if (((Get-Date) - $lastWrite).TotalSeconds -lt 10)
                {
                    LogAndPrint "Stopping recording. Reason: -Stop called by other script." Yellow
                    Remove-Item -LiteralPath $StopMarkerFile -Force
                    return $false
                }
            }

            Start-Sleep -Milliseconds 200
        }
    }
    finally {
        [Console]::TreatControlCAsInput = $false
    }

    if ($interrupted) {
        # --- Cancel WPR (discard trace data) ---
        LogInfo "Running wpr -cancel ..."
        CancelWpr
        SleepAndExit 0
    }

    return $true
}

function SetProfilingSampleRate([int]$sampleRate)
{
    $SetProfintCommand = "-setprofint $sampleRate"
    $wprExe = LocateWPRExe
    $result = RunProcessLowerPriority $wprExe $SetProfintCommand

    if ($result.ExitCode -ne 0)
    {
        LogErrorAndPrint "Failed to set profiling sample rate with exit code $($result.ExitCode). Command: SetProfilingSampleRate $SampleRate, Command Output: $($result.Output)"
        SleepAndExit $result.ExitCode
    }
    else
    {
        $ms = [math]::Round($sampleRate/10000,3)
        LogAndPrint "Profiling sample rate is $SampleRate = $ms ms." Yellow
    }
}

function CancelWpr()
{
    $CancelCommand = "-cancel $InstanceName"
    $wprExe = LocateWPRExe
    $result = RunProcessLowerPriority $wprExe $CancelCommand
}

function StartWpr([string] $wprFile)
{
    $wprCommand  = ""
    $wprProfile = ""
    $wprpPathPrefix = $wprFile + "!";

    if (-not $Start -or $Start.Count -eq 0)
    {
        LogErrorAndPrint "At least one profile must be specified with -Start parameter. Multiple profiles can be passed as comma separated values. E.g. -start Default,Network.Light"
        SleepAndExit 1
    }

    for ($i = 0; $i -lt $Start.Count; $i++) 
    {
        $wprProfile = $wprProfile + " -start `"$wprpPathPrefix$($Start[$i])`" "
    }
    
    $wprCommand = "$wprProfile $InstanceName -filemode -recordtempto ""$OutputFolder"""
    $wprExe = LocateWPRExe
    $result = RunProcessLowerPriority $wprExe $wprCommand

    if ($result.ExitCode -ne 0)
    {
        LogErrorAndPrint "wpr -start failed with exit code $($result.ExitCode). Command: $wprCommand, Command Output: $($result.Output)"

        if( $result.ExitCode -eq 0xc5585011 )
        {
            LogErrorAndPrint "WPR is not running with enough privileges to start the recording. Please run this script elevated (as administrator) to be able to record with WPR."
        }
        SleepAndExit $result.ExitCode
    }

    SetProfilingSampleRate $SampleRate

}

function StopWpr()
{
    $dummyEtl = Join-Path $OutputFolder "dummy.etl"
    $StopCommand = "-stop $dummyEtl $InstanceName"
    $wprExe = LocateWPRExe

    LogInfo "Stop Command: $wprExe $StopCommand"
    $result = RunProcessLowerPriority $wprExe $StopCommand

    if ($result.ExitCode -ne 0) 
    {
        LogErrorAndPrint "wpr -stop failed with exit code $($result.ExitCode). Command: $StopCommand, Output: $($result.Output)"
        SleepAndExit $result.ExitCode
    }
}

function GetFilesToCompressFromEtlFile([System.IO.FileInfo]$etlFile)
{
    if( $SevenZip -eq $true )
    {
        # For 7z, we use .7z extension to get better compression ratios
        $zipPath = [System.IO.Path]::ChangeExtension($etlFile.FullName, ".7z")
    }
    else
    {
        $zipPath = [System.IO.Path]::ChangeExtension($etlFile.FullName, ".zip")
    }

    $ngenPdbFolder = $etlFile.FullName + ".NGENPDB"

    $pathsToCompress = New-Object System.Collections.Generic.List[System.Object]
    $pathsToCompress.Add($etlFile.FullName)
    $totalBytes = $etlFile.Length

    if (Test-Path $ngenPdbFolder -PathType Container)
    {
        $pathsToCompress.Add($ngenPdbFolder)
        $totalBytes = $totalBytes + (Get-ChildItem -LiteralPath $ngenPdbFolder -Recurse -File | Measure-Object Length -Sum).Sum
    }

    $logFile = ""
    if(Test-Path (GetLogFileName) )
    {
        $logFile = [System.IO.Path]::ChangeExtension($etlFile.FullName, ".log")
        Copy-Item -LiteralPath (GetLogFileName) -Destination $logFile -Force
        $pathsToCompress.Add($logFile)
        LogInfo "Adding Log file $logFile to compress"
    }

    LogInfo "List of files to compress: $($pathsToCompress.ToArray() -join ' ')"

    [pscustomobject]@{
        File             = $etlFile
        ZipPath          = $zipPath
        NgenPdbFolder    = $ngenPdbFolder
        LogFile          = $logFile
        TotalBytes       = $totalBytes
        ItemsToCompress  = $pathsToCompress
    }
}

function CompressWithPowerShell($item)
{
    try
    {
        Compress-Archive -LiteralPath $item.ItemsToCompress -DestinationPath $item.ZipPath -Force -ErrorAction Stop
        return $true
    }
    catch
    {
        LogErrorAndPrint "Compression failed for $($item.File.Name): $($_.Exception.Message)"
        return $false
    }
}

function RenameRawWprFiles()
{
    # --- Rename ETL files with timestamp prefix ---
    $timestamp = Get-Date -Format "H_mm_ss"
    $etlFiles = Get-ChildItem -Path $OutputFolder -Filter "WPR_initiated_*.etl"

    if ($etlFiles.Count -eq 0) 
    {
        LogWarningAndPrint "No WPR_initiated_*.etl files found in '$OutputFolder'."
    }
    else 
    {
        LogAndPrint "Renaming $($etlFiles.Count) ETL file(s) with prefix '${timestamp}_' ..." Cyan

        foreach ($file in $etlFiles) 
        {
            $newName = "${timestamp}_$($file.Name)"
            Rename-Item -LiteralPath $file.FullName -NewName $newName
            LogInfo "  $($file.Name)  ->  $newName"
        }
    }
}

# If the WPRP file is not set via parameter, we try to resolve it from default locations. We expect a WPRP file with SkipMerge enabled to ensure unmerged ETL files are generated.
function GetDefaultWprpProfileIfNotSet()
{
    $WPRPFile = $null

    # Resolve WprpPath: use parameter, then current directory, then ..\Profiles fallback
    if ([string]::IsNullOrWhiteSpace($WprpPath))
    {
        $wprpFileName = $DefaultProfileName
        $localPath = Join-Path "." $wprpFileName
        $fallbackPath = Join-Path "..\Profiles" $wprpFileName

        if (Test-Path $localPath -PathType Leaf)
        {
            $WPRPFile = (Resolve-Path $localPath).Path
        }
        elseif (Test-Path $fallbackPath -PathType Leaf)
        {
            $WPRPFile = (Resolve-Path $fallbackPath).Path
        }
        else
        { 
            $WPRPFile = $null
        }
    }
    elseif ( (Test-Path $WprpPath -PathType Leaf) )
    {
        $WPRPFile = (Resolve-Path $WprpPath).Path
    }

    LogInfo "WPRPFile resolved to: '$WPRPFile'"
    $WPRPFile = PatchMultiProfileIfNeeded $WPRPFile

    ExitIfWprpFileHasSkipMergeNotSet $WPRPFile

    return $WPRPFile
}

# If the provided WPRP file is named MultiProfile.wprp, we create a patched copy with SkipMerge enabled to ensure unmerged ETL files are generated.
function PatchMultiProfileIfNeeded([string]$multiProfile)
{
    $lret = $multiProfile
    $fileName = [System.IO.Path]::GetFileName($multiProfile)
    if ($fileName -eq $DefaultProfileName)
    {
        # Create a patched copy with _NoMerge suffix
        $noMergeFile = [System.IO.Path]::Combine($env:ProgramData,"LongTermRecording", [System.IO.Path]::GetFileNameWithoutExtension($multiProfile) + "_NoMerge.wprp")

        LogInfo "Patch default profile file $multiProfile to $noMergeFile"
        try
        {
            [xml]$wprpXml = Get-Content -LiteralPath $multiProfile -ErrorAction Stop
        }
        catch
        {
            LogErrorAndPrint "Failed to read WPRP file: '$multiProfile'. $($_.Exception.Message)"
            SleepAndExit 1
        }

        # Find all TraceMergeProperty nodes and add <SkipMerge Value="true"/>
        $ns = $wprpXml.DocumentElement.NamespaceURI
        $traceMergeProps = $wprpXml.SelectNodes("//TraceMergeProperty")

        if ($traceMergeProps.Count -eq 0)
        {
            LogErrorAndPrint "WPRP file '$multiProfile' does not contain any <TraceMergeProperty> nodes. Cannot add SkipMerge."
            SleepAndExit 1
        }

        foreach ($prop in $traceMergeProps)
        {
            $skipMerge = $wprpXml.CreateElement("SkipMerge", $ns)
            $skipMerge.SetAttribute("Value", "true")
            # Insert as first child so it appears before CustomEvents
            [void]$prop.InsertBefore($skipMerge, $prop.FirstChild)
        }

        $wprpXml.Save($noMergeFile)
        $lret = $noMergeFile
        LogAndPrint "Created patched WPRP file with SkipMerge enabled: '$noMergeFile'" Cyan

    }

    return $lret;
}

function RunProcessLowerPriority([string]$exePath, [string]$arguments, [bool]$bPrint)
{
   $psi = New-Object System.Diagnostics.ProcessStartInfo
   $psi.FileName = $exePath
   $psi.Arguments = $arguments
   $psi.UseShellExecute = $false
   $psi.RedirectStandardOutput = $true
   $psi.RedirectStandardError = $true
   $psi.CreateNoWindow = $true

   LogInfo "Starting BelowNormal Process: $exePath $arguments"
   
   try
   {
       $proc = New-Object System.Diagnostics.Process
       $proc.StartInfo = $psi
       $proc.EnableRaisingEvents = $true

       # Use StringBuilder to collect output from async event handlers
       $stdoutBuilder = New-Object System.Text.StringBuilder

       # Register events before starting the process
       $outEvent = Register-ObjectEvent -InputObject $proc -EventName OutputDataReceived -Action {
           if ($null -ne $EventArgs.Data) { $Event.MessageData.AppendLine($EventArgs.Data) }
       } -MessageData $stdoutBuilder

       $errEvent = Register-ObjectEvent -InputObject $proc -EventName ErrorDataReceived -Action {
           if ($null -ne $EventArgs.Data) { $Event.MessageData.AppendLine($EventArgs.Data) }
       } -MessageData $stdoutBuilder

       [void]$proc.Start()
       $proc.PriorityClass = "BelowNormal"

       $proc.BeginOutputReadLine()
       $proc.BeginErrorReadLine()

       $proc.WaitForExit()
   }
   catch
   {
       LogErrorAndPrint "Failed to start process $exePath with arguments $arguments. Error: $($_.Exception.Message)"
       return @{ ExitCode = -1; Output = "" }
   }
   finally
   {
       # Clean up event subscriptions
       if ($outEvent) { Unregister-Event -SourceIdentifier $outEvent.Name -ErrorAction SilentlyContinue }
       if ($errEvent) { Unregister-Event -SourceIdentifier $errEvent.Name -ErrorAction SilentlyContinue }
   }

   $stdout = $stdoutBuilder.ToString()

   if ($bPrint)
   {
       LogAndPrint "Process Exit Code: $($proc.ExitCode) Output: $stdout" Gray
   }
   else
   {
       LogInfo "Exit Code $($proc.ExitCode)     : $stdout"   
   }

   return @{ ExitCode = $proc.ExitCode; Output = $stdout }
}

function CompressWithSevenZip($item)
{
    # Locate 7z in current directory or path 
    $sevenZip = Get-Command .\7z -ErrorAction SilentlyContinue
    if (-not $sevenZip)
    {
        $sevenZip = Get-Command 7z -ErrorAction SilentlyContinue
        if(-not $sevenZip)
        {
            LogErrorAndPrint "7z not found in PATH or current directory. Install 7-Zip or add it to PATH."
            return $false
        }
    }

    $sevenZipPath = $sevenZip.Source

    $quotedItems = $item.ItemsToCompress | ForEach-Object { "`"$_`"" }
    $sevenZipArgs = "a -y `"$($item.ZipPath)`" " + ($quotedItems -join " ")

    $result = RunProcessLowerPriority $sevenZipPath $sevenZipArgs $false
    
    if ($result.ExitCode -ne 0)
    {
        LogErrorAndPrint "7z failed for $($item.File.Name) with exit code $($result.ExitCode). Command: $sevenZipArgs, Output: $($result.Output)"
        return $false
    }

    return $true
}

function CompressWprFiles([switch]$UseSevenZip)
{
    $etlFiles = @(Get-ChildItem -Path $OutputFolder -Filter "*_Longterm.etl")
    LogAndPrint "Compress Files in folder $OutputFolder. Found $($etlFiles.Count) ETL file(s) to compress." Cyan
    $allSucceeded = $true

    foreach ($file in $etlFiles) 
    {
         $item = GetFilesToCompressFromEtlFile $file
         $zipFileName = [System.IO.Path]::GetFileName($item.ZipPath)
         LogAndPrint "Compressing $($item.File.Name) to $zipFileName" Cyan

         $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
         if ($UseSevenZip) 
         { 
             $succeeded = CompressWithSevenZip $item 
         }
         else 
         { 
             $succeeded = CompressWithPowerShell $item 
         }

         $stopwatch.Stop()

         $elapsed = $stopwatch.Elapsed.ToString("hh\:mm\:ss\.fff")

         if (-not $succeeded)
         {
             $allSucceeded = $false
             continue
         }

         $elapsedSeconds = $stopwatch.Elapsed.TotalSeconds
         $uncompressedSizeMB = [math]::Round(($item.TotalBytes / 1MB), 2)
         $compressedSizeMb = [math]::Round((Get-Item -LiteralPath $item.ZipPath).Length / 1MB, 2)

         $mbPerSecond = if ($elapsedSeconds -gt 0) { [math]::Round($uncompressedSizeMB / $elapsedSeconds,2) } else { 0 }
         $compressionRatio = [math]::Round($uncompressedSizeMB/$compressedSizeMb,1)

         LogAndPrint "    Compression successful. Duration: $elapsed ($mbPerSecond MB/s). Compression Ratio:  $compressionRatio UncompressedSize: $uncompressedSizeMB MB" Cyan
         Remove-Item -LiteralPath $item.File.FullName -Force
         if (Test-Path $item.NgenPdbFolder -PathType Container)
         {
             # Write-Host "Deleting NGen PDB folder: $([System.IO.Path]::GetFileName($item.NgenPdbFolder))" -ForegroundColor Cyan
             Remove-Item -LiteralPath $item.NgenPdbFolder -Recurse -Force
         }
         if( Test-Path $Item.LogFile )
         {
             Remove-Item -LiteralPath $item.LogFile -Force
         }
    }

    if( $allSucceeded -and @($etlFiles).Count -gt 0)
    {
        # Keep only log lines from current process the other content is already stored in zip file. 
        # That way we have the full history before compression in compressed archive and the rest is in log file
        RemoveLogLinesFromOldProcesses
    }
}

function RemoveLogLinesFromOldProcesses()
{
    $logFile = GetLogFileName
    if (-not (Test-Path $logFile -PathType Leaf))
    {
        return
    }

    $currentPidPattern = "[PID:$PID]"
    $currentProcessLines = Get-Content -LiteralPath $logFile -Encoding utf8 | Where-Object { $_ -match [regex]::Escape($currentPidPattern) }
    if ($currentProcessLines)
    {
        $currentProcessLines | Out-File -LiteralPath $logFile -Encoding utf8 -Force
    }
    else
    {
        Remove-Item -LiteralPath $logFile -Force
    }
}

function ExitIfWprpFileHasSkipMergeNotSet([string]$wprFile)
{
    if ([string]::IsNullOrWhiteSpace($wprFile))
    {
        return
    }

    try
    {
        [xml]$wprpXml = Get-Content -LiteralPath $wprFile -ErrorAction Stop
    }
    catch
    {
        LogErrorAndPrint "Failed to read WPRP file: '$wprFile'. $($_.Exception.Message)"
        SleepAndExit 1
    }

    $skipMergeNode = $wprpXml.SelectSingleNode("//SkipMerge[@Value='true']")
    if (-not $skipMergeNode)
    {
        LogErrorAndPrint "WPRP file '$wprFile' does not contain <SkipMerge Value=""true""/> in <Profiles>/<Profile>/<TraceMergeProperties>. Please modify .wprp Profile accordingly to support writing of unmerged etl files."
        SleepAndExit 1
    }
}

function IsEnoughFreeDiskSpaceLeftGB($MinimumFreeSpaceGB)
{
    $drive = (Get-Item -LiteralPath $OutputFolder).PSDrive
    $freeBytes = $drive.Free
    if ($freeBytes -lt $MinimumFreeSpaceGB*1024*1024*1024)
    {
        $freeGb = [math]::Round($freeBytes / 1GB, 2)
        $message = "Drive '$($drive.Root)' has less than ${MinimumFreeSpaceGB} GB free space (${freeGb} GB). Stopping Profiling to prevent filling the disk completely."
        LogWarningAndPrint $message
        return $false
    }

    return $true
}

function HasTotalRecordingTimeElapsed()
{
    if (-not $TotalRecordingHours -or $TotalRecordingHours -le 0)
    {
        return $false
    }

    $elapsed = (Get-Date) - $script:RecordingStartTime
    return $elapsed.TotalHours -ge $TotalRecordingHours
}

function LogAndPrint([string]$Message, [ConsoleColor]$Color)
{
    if ($PSBoundParameters.ContainsKey('Color')) 
    {
        Write-Host $Message -ForegroundColor $Color
    }
    else 
    {
        Write-Host $Message
    }
    Log $Message
}


function LogInfo([string]$Message)
{
    Log ("Info: " + $Message)
}

function LogWarningAndPrint([string]$Message)
{
    LogAndPrint ("Warning: " + $Message) Yellow
}

function LogWarning([string]$Message)
{
    Log ("Warning: " + $Message)
}

function LogErrorAndPrint([string]$Message)
{
    LogAndPrint ("Error: " + $Message) Red
}

function Log([string]$Message)
{
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $logFile = GetLogFileName

    $logPrefix = "[$timestamp] [PID:$PID] "

    if ($script:FirstLog)
    {
        $script:FirstLog = $false
        $CommandName = $PSCmdlet.MyInvocation.InvocationName;
        # Get the list of parameters for the command
        $ParameterList = (Get-Command -Name $CommandName).Parameters;

        $arguments = ""
        $ignoredParameters = @("Verbose", "Debug", "ErrorAction", "WarningAction", "InformationAction", "OutVariable", "OutBuffer", "PipelineVariable", "ErrorVariable" , "WarningVariable", "InformationVariable")
       
        foreach ($Parameter in $ParameterList) 
        {
            $keys = @($Parameter.Keys)
            for ($i = 0; $i -lt $keys.Count; $i++) 
            {
                if( $ignoredParameters -contains $keys[$i] )
                {
                    continue
                }
                $key = $keys[$i]
                # Grab each parameter value, using Get-Variable
                $valueObject = gv $key -ErrorAction SilentlyContinue
                $value = $null
                if( $valueObject -ne $null )
                {
                    $value = $valueObject.Value
                }

                $arguments += "-"+$key + " : " + $value + ", "
            }
        }
        $logPrefix + "============================================================================" | Out-File -LiteralPath $logFile -Append -Encoding utf8
        $logPrefix + "Script arguments: " + $arguments | Out-File -LiteralPath $logFile -Append -Encoding utf8
    }

    $lines = $Message -split "`r?`n"
    foreach ($line in $lines)
    {
        $logPrefix + $line | Out-File -LiteralPath $logFile -Append -Encoding utf8
    }
}

function GetLogFileName()
{
    $dir = Join-Path $env:ProgramData "LongTermRecording"
    if (-not (Test-Path $dir -PathType Container)) # allow all users write acces to our log/scratch folder to prevent file access violations when writing from different users to it
    {
        if (-not (Test-Path $dir -PathType Container))
        {
            $null = New-Item -Path $dir -ItemType Directory -Force
            # Grant Users modify access so any user can update the patched profile
            $acl = Get-Acl -LiteralPath $dir
            $sid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-32-545")
            $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($sid, "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
            $acl.AddAccessRule($rule)
            Set-Acl -LiteralPath $dir -AclObject $acl
        }
    }
    $logFileName = Join-Path $dir "LongTermRecording.log"
    return $logFileName
}

# We have a common exit point to allow the user to read the output of the command is executed in a new shell window. The time to read is controlled by -SleepExit input parameter. Default is 0
function SleepAndExit([int]$exitCode)
{
    if( $exitCode -ne 0)
    {
        LogAndPrint "Exit code was not 0. See more information in log file $(GetLogFileName)" Yellow
    }
    LogInfo "Sleeping before exit for $SleepExit seconds"
    Start-Sleep -Seconds $SleepExit
    exit $exitCode
}


function LocateWPRExe()
{
    $osBuild = [System.Environment]::OSVersion.Version.Build

    # Windows 11 (build >= 22000) and Server 2025 (build >= 26100) ship with a sufficiently recent wpr.exe
    if ($osBuild -ge 22000)
    {
        $searchPaths = @( "$env:windir\system32\wpr.exe" )
    }
    else
    {
        # On older OS versions prefer the SDK WPR which contains bug fixes not present in the inbox version
        $searchPaths = @(
            "$env:ProgramFiles\Windows Kits\10\Windows Performance Toolkit\wpr.exe",
            "${env:ProgramFiles(x86)}\Windows Kits\10\Windows Performance Toolkit\wpr.exe",
            "$env:windir\system32\wpr.exe"
        )
    }

    foreach ($path in $searchPaths) 
    {
        if (Test-Path $path -PathType Leaf) 
        {
            if( $osBuild -lt 22000 -and  $path -like "*system32*" -and $Win10WarningShown -eq $false)
            {
                $Win10WarningShown = $true  # Show warning only once 
                LogAndPrint "Warning: Using wpr from Windows 10. This has known bugs and might be not compatible with the recording profile. If you get errors please install the latest supported (Windows 11 SDK works also) Windows Performance Toolkit version." Yellow
            }
            return $path
        }
    }

    LogErrorAndPrint "wpr.exe not found. This should not happen and is a programming bug because wpr is part of Windows."
}

# --- Main Script Logic ---

if( $Stop -eq $true)
{
    StopRecording
    if( $Merge -eq $false)  # No merge during stop requested exit
    {
        SleepAndExit 0
    }
    else
    {
        LogAndPrint "Proceeding with merge and compression after stop request. Output Folder is $OutputFolder. You need to pass in for the merge operation the same folder which you did use to start the recording." Yellow
    }
}

if( $Merge -eq $true)
{
    CancelWpr
    Merge
}

if( $SevenZip -eq $true )
{
    CancelWpr
    CompressWprFiles -UseSevenZip
}
elseif( $Zip -eq $true )
{
    CancelWpr
    CompressWprFiles
}

if( $Start -ne $null -and $Merge -eq $false )
{
    RunWprInLoop
}
elseif( $Start -eq $null -and $Merge -eq $false -and $SevenZip -eq $false -and $Zip -eq $false)
{
    LogAndPrint "No action specified. Use -Start to begin recording loop, -Merge to merge existing files, or -Zip/-SevenZip to compress existing files." Yellow
}   

SleepAndExit 0 





