using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Diagnostics;
using NDesk.Options;

namespace ProcDumpEx
{
    class Program
    {
        static string targetWaitingProcessNames;
        static string targetRunningProcessNames;
        static ManagementEventWatcher startWatch;        
        static List<string> extraArgs;
        static List<Process> monitoredProcesses;

        static void Main(string[] args)
        {
            var bShowHelp = false;

            var p = new OptionSet()
            {
                {
                    "w=", " Wait for the specified process to launch if it's not running. If there are more than one, separate them with comma.", w => targetWaitingProcessNames = w
                },
                {
                    "d=", " Monitor the specified process already running. If there are more than one, separate them with comma.", d => targetRunningProcessNames = d
                },
                {
                    "h|help", "print the usage", o => bShowHelp = true
                }
            };
            
            try
            {
                extraArgs = new List<string>();
                var items = p.Parse(args);
                items.ForEach(arg => {
                    if (arg.Contains(" ")) {
                        arg = String.Format("\"{0}\"", arg);
                    }
                    extraArgs.Add(arg);
                });
            }
            catch (OptionException e)
            {
                Console.Write("greet: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `greet --help' for more information.");
                return;
            }

            if (bShowHelp)
            {
                showHelp();
                return;
            }

            if (extraArgs == null) extraArgs = new List<string>();
            if (targetWaitingProcessNames == null) targetWaitingProcessNames = "";
            if (targetRunningProcessNames == null) targetRunningProcessNames = "";

            monitoredProcesses = new List<Process>();

            //handle awaiting processes
            if (targetWaitingProcessNames.Length > 0)
            {
                Console.WriteLine("Waiting new process named {0} to start", targetWaitingProcessNames);
            }
            WaitForProcess();

            //handle current running processes
            LoopCurrentProcesses();

            // Establish an event handler to process key press events.
            Console.CancelKeyPress += Console_CancelKeyPress;
            while (true)
            {
                Console.WriteLine("At anytime you can press CTRL+C to quit:");

                // Start a console read operation. Do not display the input.
                var cki = Console.ReadKey(true);

                // Announce the name of the key that was pressed .
                //Console.WriteLine("  Key pressed: {0}\n", cki.Key);

                // Exit if the user pressed the 'X' key.
                if (cki.Key == ConsoleKey.X) break;
            }
        }

        private static void showHelp()
        {
            Console.WriteLine("This program enhances procdump.exe to monitor multiple processes with the speicified name (-w -d arguments)\n");
            Console.WriteLine("Example: procdumpex -ma -c 80 -s 2 -n 10 -w example.exe C:\\temp");
            Console.WriteLine("Example: procdumpex -ma -c 80 -s 2 -n 10 -w \"example1.exe,example2.exe\" C:\\temp");
            Console.WriteLine("Example: procdumpex -ma -s 2 -n 10 -w example.exe -p \"\\Processor(_Total)\\% Processor Time\" 80 C:\\temp");
            Console.WriteLine("\nBelow is the usage for procdump:");
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("procdump.exe", "/?")
            {
                UseShellExecute = false
            };

            process.Start();
            process.WaitForExit();            
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("About to exit.");
            foreach (var p in monitoredProcesses)
            {
                try
                {
                    Console.WriteLine("Exiting process: {0}, PID={1}", p.ProcessName, p.Id);
                    p.Kill();
                }
                catch
                {

                }
            }            
        }

        static void WaitForProcess()
        {
            startWatch = new ManagementEventWatcher(
              new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            startWatch.EventArrived
                                += new EventArrivedEventHandler(startWatch_EventArrived);
            startWatch.Start();
        }

        static void startWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            targetWaitingProcessNames = targetWaitingProcessNames.Trim(new char[] { '"' }); //remove double quotes if any
            foreach (var name in targetWaitingProcessNames.Split(new char[] { ',' })) {
                try
                {
                    if (0 == String.Compare(e.NewEvent.Properties["ProcessName"].Value.ToString(), name.Trim(), true))
                    {
                        Console.WriteLine("Process started: {0}, PID={1}"
                                          , e.NewEvent.Properties["ProcessName"].Value, e.NewEvent.Properties["ProcessID"].Value);

                        //let procdump hook this process                
                        var args = new List<string>(extraArgs);
                        //Assume the last arg is the dump folder path
                        if (extraArgs.Count > 1)
                        {
                            args.Insert(args.Count - 1, e.NewEvent.Properties["ProcessID"].Value.ToString()); //PID
                        }
                        else
                        {
                            args.Insert(0, String.Format("{0}", e.NewEvent.Properties["ProcessID"].Value.ToString())); //PID
                        }
                        Console.WriteLine("Launch procdump.exe {0}", string.Join(" ", args));
                        var p = Process.Start("procdump.exe", string.Join(" ", args));
                        monitoredProcesses.Add(p);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Exception with starting procdump process for {0}, details: {1}", name, ex.Message);
                }
            }
        }

        static void LoopCurrentProcesses()
        {
            targetRunningProcessNames = targetRunningProcessNames.Trim(new char[] { '"' }); //remove double quotes if any
            var names = targetRunningProcessNames.ToLower().Split(new char[] { ',' });

            var list = Process.GetProcesses();
            foreach(var p in list)
            {
                if(names.Contains(p.ProcessName.ToLower() + ".exe"))
                {
                    Console.WriteLine("Found one process {0} already running, let's monitor it first.", p.ProcessName.ToLower() + ".exe");
                    //let procdump hook this process                
                    var args = new List<string>(extraArgs);
                    //Assume the last arg is the dump folder path
                    if (extraArgs.Count > 1)
                    {
                        args.Insert(args.Count - 1, String.Format("{0}", p.Id)); //PID
                    }
                    else
                    {
                        args.Insert(0, String.Format("{0}", p.Id)); //PID
                    }
                    Console.WriteLine("Launch procdump.exe {0}", string.Join(" ", args));
                    var newProcess = Process.Start("procdump.exe", string.Join(" ", args));
                    monitoredProcesses.Add(newProcess);
                }

            }
        }
    }
}
