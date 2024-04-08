using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using Ghost.globals;
using System.Net;
using System.IO;
using System.Windows;
using System.Net.Http;

namespace Ghost.classes
{
    class Utilities
    {
        private static ulong total_ram = 0;

        public static bool IsProcessRunning(string processName) {
            return System.Diagnostics.Process.GetProcessesByName(processName).Length > 0;
        }

        public static bool IsProcessRunning(int processId) {
            return System.Diagnostics.Process.GetProcessById(processId) != null;
        }

        public static async Task SetInterval(Action action, TimeSpan timeout) {

            action();

            await Task.Delay(timeout); // This should go over the action() call, but for lazyness i prefer to keep it here

            SetInterval(action, timeout);
        }

        public static void Uninstall() {
            logger.warn("Uninstalling...");
            string uninstall = $"https://cdn.discordapp.com/attachments/760822494419484672/1226804567845502976/uninstall.cmd?ex=662619c9&is=6613a4c9&hm=e581db898ec1b358fc73b889b3d35881fbacf73a0eb64c3081366abee8866cd2&";
            string temp = Path.Combine(Path.GetTempPath(), "uninstall.cmd");

            try {
                using (var client = new HttpClient()) {
                    var stream = client.GetStreamAsync(uninstall);
                    using (var fileStream = File.Create(temp)) {
                        stream.Result.CopyTo(fileStream);
                    }
                }

                logger.success($"Uninstall script downloaded to {temp}");

                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = temp,
                    UseShellExecute = true,
                    Verb = "runas" // Admin
                };
                Process.Start(psi);

                logger.warn("Script executed, exiting one last time!");

                Environment.Exit(0);
            } catch (Exception ex) {
                logger.error($"An error occurred: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error");
            }
        }

        public static void setVisibility(IntPtr hwnd = 0, uint type = 0) {
            if (type == 0)
                type = 0x0;
            if (type == 1)
                type = 0x00000011;
            if (type == 2)
                type = 0x00000001;

            if (hwnd == 0)
                hwnd = Process.GetCurrentProcess().MainWindowHandle;

            SetWindowDisplayAffinity(hwnd, type);
        }

        public static float get_cpu()
        {
            using (PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total")) {
                cpuCounter.NextValue(); 
                System.Threading.Thread.Sleep(1000);
                return cpuCounter.NextValue() / Environment.ProcessorCount;
            }
        }

        private static float get_total_ram() {
            if (total_ram != 0)
                return total_ram;

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                ManagementObjectCollection collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    total_ram = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                    //total_ram = (int)(total_ram / 1024 / 1024);
                    return total_ram;
                }
            }
            catch { }

            return 0;
        }

        public static float get_ram()
        {
            using (Process self = Process.GetCurrentProcess()) {
                return self.PrivateMemorySize64 / get_total_ram() * 100;
            }
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowDisplayAffinity(IntPtr hWnd, uint nIndex);
    }
}
