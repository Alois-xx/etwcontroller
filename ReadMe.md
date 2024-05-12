# What is it?

ETWController is the tool to troubleshoot Windows performance issues. It can profile

* Your local machine
* A remote machine
* Both machines simultaneously

What it makes it unique is that it captures all user input (configurable) along with screenshots. That makes it easy to spot the hang or exception from the screenshots. No more guessing what the user is telling you what he did. Now you can see it. And the best thing is that each screenshot/keyboard press gets its own ETW event which makes it trivial to navigate in 
the profiling data to the right time point where much more insights are waiting for you. 

![Main UI](ETWController/Documentation/Images/MainUI.png)

## Predefined Recording Presets
With the latest release is comes with an annotated predefined wpr recording profile called MultiProfile.wprp which shows most features present in ETW.
The recorded data can be analyzed with [ETWAnalyzer](https://github.com/Siemens-Healthineers/ETWAnalyzer) in much greater detail. 

- **Full (Network+CSwitch+File+Frequency)**
  -  The catch all profile if you need to track the complete system which captures most data but can record only about 1-2 minutes on a busy machine.
- **Default (CPU Samples/Disk/.NET Exceptions/Focus)**
  -  This will capture no context switch data which spares a lot of space and is still very detailed.
- **Frequency (CPU Samples/Disk/.NET Exceptions/Focus/Frequency)**
   - Same as Default but captures also CPU frequency data. 
- **File (CPU Samples/Disk/.NET Exceptions/Focus/File IO)**
  -  Same as Default but additionally it captures all File accesses. Low additional overhead.
- **Handle (CPU Samples/Disk/.NET Exceptions/Focus/Handle)**
  - Same as Default but capture all handle create/close/duplicate events. This is the goto profile for tracking handle leaks.
  - WPA can analyze the data under Memory/Handle view but it has no easy view for leaks. 
  - ETWAnalyzer can analyze handle leaks after  ```ETWAnalyzer -extract ObjectRef -fd ..etl``` with ```ETWAnalyzer -dump ObjectRef -Leak```
- **HandleRef (CPU Samples/Disk/.NET Exceptions/Focus/HandleRef)**
  -  Same as Default but capture all kernel object create/destroy/reference events. This is a high volume provider. 
     E.g. when you wait/signal a handle the handle reference count is shortly incremented/decremented during the WaitForSingleObject/SetEvent calls. 
   - WPA has no way to analyze the data. ETWAnalyzer can process the data with ```ETWAnalyzer -extract ObjectRef -fd ..etl```.
- **FileMapping (CPU Samples/Disk/.NET Exceptions/Focus/FileMapping)**
  - Same as Default but capture all MapViewOfFile and UnMapViewOfFile calls. 
  - WPA has no way to analyze the data. ETWAnalyzer can process it with ```ETWAnalyzer -extract ObjectRef -fd ..etl```.    
- **Network (CPU Samples/Disk/.NET Exceptions/Focus/Network)**
  -  Same as Default but also records every network packet source and destination IP. No packet data is captured.
- **VirtualAlloc (Long Term)**
  - If you have big memory leaks this might be a first step to find in longer recording (up to 20 minutes) what went wrong. 
- **UserGDILeaks (Long Term)**
  - Since Windows 10 1909 GDI/User object creation destruction is also instrumented with ETW. Best Viewed with WPA.
- **PMCSample (PMC Sampling for PMC Rollover + Default)**
   - Capture CPU counters in sampling mode. Similar what VTune does.
- **PMCBranch (PMC Cycles per Instruction and Branch data - Counting)**
  - Capture CPU counters in sampling mode. Similar what VTune does.
- **PMCLLC (PMC Cycles per Instruction and LLC data - Counting)**
  - Capture CPU counters in sampling mode. Similar what VTune does.
- **LBR (LBR - Last Branch Record Sampling)**
  - Capture CPU counters in sampling mode. Similar what VTune does.
- **SysCall (CPU Samples/Disk/.NET Exceptions/Focus/SysCall)**
   - Same as Default but records all Kernel calls from user mode processes with stack traces. Useful for security researchers.
    Unlike strace on Linux only the return code of the API call is recorded and no method arguments are collected.
- **WPR Default**
  - WPR Default profile provided by MS. 
- **WPR Default + .NET**
   - WPR Default profile with .NET provider.

All profiles except the WPR Default profiles can be combined with the other profiles defined in MultiProfile.wprp. To change a profile select first a profile and then select ```<Manual Editing>``` to show the ```Start Command``` line.
You can then add additional or other profile settings. The predefined settings are stored in ETWController.exe.config.

You can change the CPU sampling rate by adding to the xxwpr command line ```-setprofint 100000``` to e.g. set a 10ms sample interval. WPR does support this command line switch 
but it does not work when you try to start a profile with a custom sample rate. The xxwpr script works around that limitation and calls wpr -setprofint with your custom CPU sample 
interval after the profile has been started.

## XCopy Deployable on Windows 10/11

### Recording Data
To record the data it needs wpr.exe which is already part of Windows 10/11. If you need the latest version you can download it as part from the Windows SDK https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/ or the Windows ADK. If you need to update the wpr from your machine you can install from the SDK Iso **WPTx64 (OnecoreUAP)-x64_en-us.msi** which is just a 2.5 MB MSI which contains only the command line tool wpr to record things but misses the rest (WPRUI.exe present but does not work, and WPA).
Be sure to extend the PATH environment so you are calling the newer WPR version and not the old one sitting in **C:\Windows\System32\wpr.exe**. ETWController uses the one which is in the path.

### Windows 10 Issues
If you get during stopping the trace an error message from WPR
```
wpr -stop problem.etl

        Cannot change thread mode after it is set.

        Profile Id: RunningProfile

        Error code: 0x80010106
```
you need to install the latest Windows Performance Toolkit for Windows 10 or 11 (https://devblogs.microsoft.com/performance-diagnostics/wpr-start-and-stop-commands/). The problem is that a faulty WPR version was shipped with Windows 10, but things which can be installed
via the Windows ADK or SDK will never be serviced by Windows updates. I have hoped that this issue would fix on itself over time but the guys keep telling me that if you can install a newer
version on your own they will not update it for you. That can cause issues where the OS installation is frozen and you are not allowed to make changes in regulated environments. The Windows 11 
version works out of the box though. 
### Analyzing Data
To view the data it is best to install the latest Windows Performance Toolkit from the Windows 11 SDK (https://developer.microsoft.com/en-us/windows/downloads/sdk-archive).

 
