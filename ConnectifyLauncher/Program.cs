using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System.ServiceProcess;
using System.Management;

namespace ConnectifyLauncher {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main() {
            string dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string svc = Path.Combine(dir, "ConnectifyService.exe");
            string cli = Path.Combine(dir, "Connectify.exe");
            bool isService = false;
            if (File.Exists(svc) && File.Exists(cli)) {
                RegistryKey runKey = null;
                try {
                    // Remove the client autorun
                    runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    runKey.DeleteValue("Connectify", false);

                } catch (Exception) {
                } finally {
                    if (runKey != null)
                        runKey.Close();
                }

                TaskService ts = null;
                try {
                    // Remove service scheduled task if it exists
                    ts = new TaskService();
                    foreach (Task t in ts.RootFolder.Tasks) {
                        if (t.Name.StartsWith("Connectify")) {
                            ts.RootFolder.DeleteTask(t.Name);
                            break;
                        }
                    }
                } catch (Exception) {
                } finally {
                    if (ts != null)
                        ts.Dispose();
                }

                ServiceController service = null;
                ManagementObject classInstance = null;
                ManagementBaseObject inParams = null;
                try {
                    // Set the service to run manually if it exists
                    service = new ServiceController("Connectify");
                    if (service != null) {
                        isService = true;
                        classInstance = new ManagementObject("root\\CIMV2", "Win32_Service.Name='Connectify'", null);
                        inParams = classInstance.GetMethodParameters("ChangeStartMode");
                        inParams["StartMode"] = "Manual";
                        classInstance.InvokeMethod("ChangeStartMode", inParams, null);
                    }
                } catch (Exception) {
                } finally {
                    if (inParams != null)
                        inParams.Dispose();
                    if (classInstance != null)
                        classInstance.Dispose();
                }
                
                try {
                    if (isService) {
                        // Stop the service if it happens to be running
                        service.Stop();
                    } else {
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
                } catch (Exception) {
                }

                Process daemon = null;
                try {
                    if (isService) {
                        // Start the service
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running);
                    } else {
                        // Launch the service & client
                        daemon = Process.Start(svc);
                        daemon.WaitForInputIdle();
                    }
                } catch (Exception) {
                }

                // Launch the client
                Process client = Process.Start(cli);

                // Wait for client to quit
                client.WaitForExit();

                try {
                    if (isService) {
                        // Stop the service
                        service.Stop();
                    } else {
                        // Kill services
                        daemon.Kill();
                        daemon.WaitForExit();
                        foreach (Process p in Process.GetProcessesByName("Connectifyd")) {
                            p.Kill();
                        }
                    }
                } catch (Exception) {
                } finally {
                    if (service != null)
                        service.Dispose();
                }
            }
        }
    }
}
