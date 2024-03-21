using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Management;

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
