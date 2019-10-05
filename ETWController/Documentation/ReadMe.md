# What is it?

ETWController is the tool to troubleshoot Windows performance issues. It can profile

* Your local machine
* A remote machine
* Both machines simultaneously

What it makes it unique is that it captures all user input (configurable) along with screenshots. That makes it easy to spot the hang or exception from the screenshots. No more guessing what the user is telling you what he did. Now you can see it. And the best thing is that each screenshot/keyboard press gets its own ETW event which makes it trivial to navigate in 
the profiling data to the right time point where much more insights are waiting for you. 

![Main UI](images/MainUI.png)

## XCopy Deployable on Windows 10

### Recording Data
To record the data it needs wpr.exe which is already part of Windows 10. On Windows 7 you need to install the Windows Performance Toolkit which is part of the Windows 8.1 SDK (https://go.microsoft.com/fwlink/p/?LinkId=323507).

### Analyzing Data
To view the data it is best to install the latest Windows Performance Toolkit from the Windows 10 SDK (https://developer.microsoft.com/en-us/windows/downloads/sdk-archive).

 