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
        public int pid { get; private set; }
        public string name { get; private set; }
        public string path { get; private set; }
        public IntPtr hwnd { get; private set; }
        public Icon Icon { get; private set; }
        public Process proc { get; private set; }
        public bool excluded { get; set; } = false;
        public Overlay overlay { get;  set; }

        public ProcessHandler(Process proc) {
            this.pid = proc.Id;
            this.proc = proc;
            this.name = proc.ProcessName;
            this.hwnd = proc.MainWindowHandle;

            try { 
                this.path = proc.MainModule.FileName;
                this.Icon = Icon.ExtractAssociatedIcon(path);
            } catch { this.path = ""; }
        }

        // Optimize this function, Process.GetProcesses(): ~15ms and this.get_processes(): ~350ms
        public static List<ProcessHandler> get_processes()
        {
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
    }
}
