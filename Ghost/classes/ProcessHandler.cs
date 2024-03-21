using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.IO;
using HandyControl.Tools.Extension;

namespace Ghost.classes
{
    public class ProcessHandler : System.Diagnostics.Process
    {
        public static List<ProcessHandler> cache_processes { get; private set; } = new List<ProcessHandler>();
        public int pid { get; private set; }
        public string name { get; private set; }
        public string path { get; private set; } = "Inaccesible";
        public IntPtr hwnd { get; private set; }
        public ImageSource icon { get; private set; }
        public Process proc { get; private set; }
        public bool excluded { get; set; } = false;
        public Overlay? overlay { get;  set; }

        public ProcessHandler(Process proc) : this(proc, false, null) { }

        public ProcessHandler(Process proc, bool excluded, Overlay? overlay)
        {
            this.pid = proc.Id;
            this.proc = proc;
            this.name = proc.ProcessName;
            this.hwnd = proc.MainWindowHandle;

            // This block is really resource intesive, just getting the FileName of a process takes many ms
            this.path = proc.MainModule.FileName;

            Application.Current.Dispatcher.Invoke(() => {
                var app_icon = this.path != "Inaccesible" ? ImageSourceForBitmap(Icon.ExtractAssociatedIcon(this.path).ToBitmap()) : null;

                if (app_icon != null)
                    this.icon = app_icon;
                else
                    this.icon = new BitmapImage(new Uri(@"assets/noimage-white.png", UriKind.RelativeOrAbsolute));
            });

            this.excluded = excluded;
            this.overlay = overlay;
        }

        public ImageSource ImageSourceForBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                ImageSource newSource = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(handle);
                return newSource;
            }
            catch
            {
                DeleteObject(handle);
                return null;
            }
        }

        public static bool update_process(Process proc, bool excluded = false, Overlay? overlay = null) {
            try {
                var cached = cache_processes.Find(p => p.name == proc.ProcessName);
                if (cached != null)
                    cache_processes.Remove(cached);
                
                cache_processes.Add(new ProcessHandler(proc, excluded, overlay));
                return true;
            } catch { return false; }
        }

        public static bool update_process(IntPtr hwnd, bool excluded = false)
        {
            var cached = cache_processes.Find(p => p.hwnd == hwnd);

            if (cached == null)
                return false;

            return update_process(cached, excluded);
        }

        // Optimize this function, Process.GetProcesses(): ~15ms and this.get_processes(): ~350ms
        // So far the first scan lasts ~3000ms and the next ones less than 100ms because of the cache, i can live with that
        public static List<ProcessHandler> get_processes() {
            //Console.WriteLine("Getting processes list...");
            var startTime = DateTime.Now;

            List<ProcessHandler> processes = new List<ProcessHandler>();
            int cached_processes = 0;

            //Console.WriteLine($"Diagnostics Process List recived in {(DateTime.Now - startTime).TotalMilliseconds}ms");

            foreach (var proc in System.Diagnostics.Process.GetProcesses()) {
                if ( // These are system processes and take too long to process
                    proc.ProcessName == "svchost" || 
                    proc.ProcessName == "System" || 
                    proc.ProcessName == "Registry" ||
                    proc.ProcessName == "Idle" ||
                    proc.ProcessName == "LsaIso" ||
                    proc.ProcessName == "vshost" ||
                    proc.ProcessName == "WUDFHost" ||
                    proc.ProcessName == "Secure System" ||
                    proc.ProcessName == "smss" ||
                    proc.ProcessName == "csrss" ||
                    proc.ProcessName == "wininit" ||
                    proc.ProcessName == "services" ||
                    proc.ProcessName == "lsass" ||
                    proc.ProcessName == "lsm" ||
                    proc.ProcessName == "winlogon" ||
                    proc.ProcessName == "fontdrvhost" ||
                    proc.ProcessName == "dwm" ||
                    proc.ProcessName == "WmiPrvSE"
                    )
                    continue;
 
                var procStartTime = DateTime.Now;
                var cachedProcess = cache_processes.Find(p => p.name == proc.ProcessName);
                var existingProcess = processes.Find((p) => p.name == proc.ProcessName) != null;

                if (existingProcess)
                    continue;

                if (cachedProcess != null) {
                    cached_processes++;
                    processes.Add(cachedProcess);
                    //Console.WriteLine($"Processed from cache {proc.ProcessName}({proc.Id}) in {(DateTime.Now - procStartTime).TotalMilliseconds}ms/{(DateTime.Now - startTime).TotalMilliseconds}ms");
                    continue;
                }

                try {
                    // Exception thrown: 'System.ComponentModel.Win32Exception' in System.Diagnostics.Process.dll
                    // When creating ProcessHandler we access the MainModule.FileName property,
                    // which throws an exception if the process is not accessible which is accessible but still throws an exception
                    // Please help me fix this
                    //if (
                    //    // proc.MainModule != null &&
                    //    )
                    processes.Add(new ProcessHandler(proc));
                } catch {  }

                //Console.WriteLine($"Processed {proc.ProcessName}({proc.Id}) in {(DateTime.Now - procStartTime).TotalMilliseconds}ms/{(DateTime.Now - startTime).TotalMilliseconds}ms");
            }

            //Console.WriteLine($"Cached processes: {cached_processes} out of {processes.Count - cached_processes}");
            Console.WriteLine($"Processes list completed in {(DateTime.Now - startTime).TotalMilliseconds:F2}ms cache ({cached_processes}/{processes.Count - cached_processes})");

            cache_processes = processes;
            return processes;
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);
    }
}
