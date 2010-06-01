using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32.TaskScheduler;

namespace ConnectifyLauncher {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main() {
            string dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string svc = Path.Combine(dir, "ConnectifyService.exe");
            string cli = Path.Combine(dir, "Connectify.exe");
            if (File.Exists(svc) && File.Exists(cli)) {
                try {
                    TaskService ts = new TaskService();
                    foreach (Task t in ts.RootFolder.Tasks) {
                        if (t.Name.StartsWith("Connectify")) {
                            ts.RootFolder.DeleteTask(t.Name);
                            break;
                        }
                    }
                    foreach (Process p in Process.GetProcessesByName("ConnectifyService")) {
                        p.Kill();
                        p.WaitForExit();
                    }
                    foreach (Process p in Process.GetProcessesByName("Connectifyd")) {
                        p.Kill();
                        p.WaitForExit();
                    }
                }
                catch (Exception) {
                    return;
                }

                Process daemon = Process.Start(svc);
                daemon.WaitForInputIdle();
                Process client = Process.Start(cli);
                client.WaitForExit();
                daemon.Kill();
                daemon.WaitForExit();
                foreach (Process p in Process.GetProcessesByName("Connectifyd")) {
                    p.Kill();
                }
            }
        }
    }
}
