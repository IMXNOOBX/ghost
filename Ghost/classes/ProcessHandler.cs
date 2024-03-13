using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ghost.classes
{
    public class ProcessHandler : System.Diagnostics.Process
    {
        public string name { get; private set; }
        public string path { get; private set; }
        public IntPtr hwnd { get; private set; }
        public Icon Icon { get; private set; }
        public bool excluded { get; set; }
        public Process proc { get; private set; }

        public ProcessHandler(Process proc) {
            this.proc = proc;
            this.name = proc.ProcessName;
            this.hwnd = proc.MainWindowHandle;
            this.excluded = is_excluded();
            try { 
                this.path = proc.MainModule.FileName;
                this.Icon = Icon.ExtractAssociatedIcon(path);
            } catch { this.path = ""; }
        }

        public bool exclude(bool exclude, bool visible) {
            this.excluded = exclude;
            int result = SetWindowDisplayAffinity(
                hwnd, 
                (exclude) ? 
                    ((visible) ? WDA_MONITOR : WDA_EXCLUDEFROMCAPTURE) 
                    : WDA_NONE
                );

            if (result != 0) {
                Console.WriteLine("Failed to exclude window: " + name + " (" + result + ")");
                
                SetWindowLongPtr(hwnd, GWL_STYLE, GetWindowLongPtr(hwnd, GWL_STYLE) & ~WS_VISIBLE);
                result = SetWindowDisplayAffinity(hwnd, exclude ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE);
            }

            return result == 0;
        }

        public bool is_excluded() {
            int affinity;
            int result = GetWindowDisplayAffinity(hwnd, out affinity);

            if (result == 0)
                return false; // Default to false if the function fails as most windows are not excluded

            return affinity == WDA_EXCLUDEFROMCAPTURE;
        }

        public static List<ProcessHandler> get_processes() {
            List<ProcessHandler> processes = new List<ProcessHandler>();

            try
            {
                foreach (var proc in System.Diagnostics.Process.GetProcesses())
                    if (processes.Find((p) => p.name == proc.ProcessName) == null)
                        processes.Add(new ProcessHandler(proc));
            }
            catch { }

            return processes;
        }

        /**
         * Export Win32 API functions
         */
        public const int GWL_STYLE = -16;
        public const int WS_VISIBLE = 0x10000000;

        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_NONE = 0x00000000;

        [DllImport("user32.dll")]
        private static extern int SetWindowDisplayAffinity(IntPtr hWnd, uint nIndex);
        [DllImport("user32.dll")]
        private static extern int GetWindowDisplayAffinity(IntPtr hWnd, out int affinity);

        // Import the necessary WinAPI functions
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }
}
