using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
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
                RegistryKey runKey = null;
                try {
                    // Remove the client autorun
                    runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    runKey.DeleteValue("Connectify", false);

                }
                catch (Exception) {
                }
                finally {
                    if (runKey != null)
                        runKey.Close();
                }

                TaskService ts = null;
                try {
                    // Remove service scheduled task
                    ts = new TaskService();
                    foreach (Task t in ts.RootFolder.Tasks) {
                        if (t.Name.StartsWith("Connectify")) {
                            ts.RootFolder.DeleteTask(t.Name);
                            break;
                        }
                    }

                    // Kill services if they happen to be running
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
                finally {
                    if (ts != null)
                        ts.Dispose();
                }

                // Launch the service & client
                Process daemon = Process.Start(svc);
                daemon.WaitForInputIdle();
                Process client = Process.Start(cli);

                // Wait for client to quit & kill services
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
