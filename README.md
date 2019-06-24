# ProcDumpEx

As you may know, sysinternals' [procdump](https://docs.microsoft.com/en-us/sysinternals/downloads/procdump) is a great tool to capture crash dump files when certain condition meets.

However, it can only work for one process at a time, by either pid or image name.

If there are more than one processes with the same name opened on your machine, it will just reports:

```
>procdump chrome.exe

ProcDump v9.0 - Sysinternals process dump utility
Copyright (C) 2009-2017 Mark Russinovich and Andrew Richards
Sysinternals - www.sysinternals.com

[17:17:17] Multiple processes match the specified name.
```

The same logic happens to "-w" option in that it only monitors the next ONE process started afterwards.

That's why I decide to enhance it with a new wrapper named ProcDumpEx. It can dump multiple processes in one single command. Use "-d" option I invented to work with existing processes, and "-w" option to wait for certain processes.

Here are some examples:

1. Dump all the running notepad.exe processes
```
procdumpex -ma -d notepad.exe
```

2. Dump all notepad.exe processes started from now on
```
procdumpex -ma -e notepad.exe
```

3. Combine both cases above
```
procdumpex -ma -e notepad.exe -d notepad.exe
```

You can list multiple process names with comma separated in one command.

4. Dump all notepad.exe and calc.exe started later on when they use more than 30% CPU for 3 seconds
```
procdumpex -ma -c 30 -s 3 -e "notepad.exe,calc.exe"
```

A more realistic example is to dump process when a performance counter hits (-p option provided by procdump), for example:

5. Dump following processes when the system total CPU hits 80%.
```
procdumpex -ma -s 2 -n 3  -w "chrome.exe,wmplayer.exe" -d "chrome.exe,wmplayer.exe"  -p "\Processor(_Total)\% Processor Time" 80 C:\temp\dump\PROCESSNAME_PID_YYMMDD_HHMMSS.dmp
```

Another good feature is you just need to click CTRL+C to clean up all command windows that are opened.
