using Ghost.globals;
using HandyControl.Tools;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WVS;

namespace Ghost.classes
{
    public partial class Overlay : Popup
    {
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = -20;

        private const int ULW_ALPHA = 0x2;
        private const byte ALPHA = 0xFF;

        private IntPtr hwnd;
        private IntPtr overlayHwnd;
        private Window overlayWindow;
        private BackgroundWorker bgWorker;
        private List<Overlay> childOverlays = new List<Overlay>();

        public Overlay(IntPtr hwnd) {
            Console.WriteLine($"Creating overlay for hwnd({hwnd})");
            this.hwnd = hwnd;
            CreateOverlay();
            InitBackgroundWorker();

            WVS.WindowVisibilityChecker.init();
        }

        public Overlay(Process process) {
            Console.WriteLine($"Creating overlay for process({process.ProcessName})");
            this.hwnd = process.MainWindowHandle;
            CreateOverlay();
            InitBackgroundWorker();

            WVS.WindowVisibilityChecker.init();

            if (!Config.settings.only_hide_top_window)
                foreach (var winHandle in GetProcessWindows(process))
                    childOverlays.Add(new Overlay(winHandle));
        }

        public void destroy() {
            Console.WriteLine($"Destroying overlay for ({hwnd})");
            SetWindowProtected(false);
            bgWorker.CancelAsync();
            bgWorker.Dispose();

            ProcessHandler.update_process(hwnd, false);

            foreach (var overlay in childOverlays)
                overlay.destroy();

            Dispatcher.Invoke(() => {
                overlayWindow.Close();
            });
        }

        private Window CreateOverlay() {
            RECT rect;
            GetWindowRect(hwnd, out rect);

            overlayWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = null,
                Topmost = Config.settings.overlay_type == 1,
                Left = rect.Left,
                Top = rect.Top,
                Width = rect.Right - rect.Left,
                Height = rect.Bottom - rect.Top,
                ShowInTaskbar = false,
            };

            // Ensures that overlay is always on top of main window
            if (Config.settings.overlay_type == 0) {
                WindowInteropHelper helper = new WindowInteropHelper(overlayWindow);
                helper.Owner = hwnd;
            }

            this.overlayHwnd = overlayWindow.GetHandle();

            if (Config.settings.show_hidden_indicator)
                overlayWindow.Content = new Image { 
                    Source = new BitmapImage(new Uri("pack://application:,,,/assets/invisible-white.png")), 
                    Stretch = System.Windows.Media.Stretch.Fill,
                    Width = 16, Height = 16,
                    Margin = new Thickness(0, 12, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top
                };

            overlayWindow.Show();

            // This will attach the overlay window to the main window
            var extendedStyle = GetWindowLong(this.overlayHwnd, GWL_EXSTYLE);
            extendedStyle |= WS_EX_TOOLWINDOW;
            SetWindowLong(this.overlayHwnd, GWL_EXSTYLE, extendedStyle);

            SetWindowProtected(true);

            Console.WriteLine($"Overlay created successfully for ({hwnd}) with handle ({this.overlayHwnd})!");

            return overlayWindow;
        }

        private void InitBackgroundWorker() {
            bgWorker = new BackgroundWorker();
            bgWorker.WorkerReportsProgress = false;
            bgWorker.WorkerSupportsCancellation = true;
            bgWorker.DoWork += new DoWorkEventHandler(UpdateOverlay);
            bgWorker.RunWorkerAsync();
        }

        private void UpdateOverlay(object sender, DoWorkEventArgs e) {
            while (!bgWorker.CancellationPending) {
                if (overlayWindow == null)
                    continue;

                if (hwnd == IntPtr.Zero)
                    continue;

                // Check if the hwnd is still valid
                uint pid = 0;
                if (GetWindowThreadProcessId(hwnd, out pid) == 0) {
                    this.destroy();
                    break;
                }

                RECT rect;
                GetWindowRect(hwnd, out rect);

                Dispatcher.Invoke(() => {
                    // Check with diferent methods if the target window is visible on screen
                    // Thanks to: https://github.com/Real-Gollum/Window-Visibility-Detector
                    bool isVisible = Config.settings.overlay_type == 0 || WVS.WindowVisibilityChecker.IsWindowVisibleOnScreen(hwnd); // dont check visibility if overlay is not topmost

                    try {
                        overlayWindow.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;

                        overlayWindow.Left = rect.Left;
                        overlayWindow.Top = rect.Top;
                        overlayWindow.Width = rect.Right - rect.Left;
                        overlayWindow.Height = rect.Bottom - rect.Top;
                    }  catch { }

                    // Causes to lose focus on the main application rendering it unusable
                    //SetWindowPos(overlayHwnd, IntPtr.Zero, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, 0);
                });

                Thread.Sleep(10); // 25
            }
        }

        public void SetWindowProtected(bool status, IntPtr hwnd = 0) {
            if (hwnd == 0)
                hwnd = this.overlayHwnd;

            //Console.WriteLine($"Changed ({hwnd}) display affinity to: {(status ? "Enabled" : "Disabled")}");

            SetWindowDisplayAffinity(
                hwnd,
                (status) ? WDA_MONITOR : WDA_NONE
            );
        }

        public static List<IntPtr> GetProcessWindows(int processId, bool visibleOnly = true)
        {
            List<IntPtr> windows = new List<IntPtr>();
            EnumWindows((hWnd, lParam) =>
            {
                uint windowProcessId;
                GetWindowThreadProcessId(hWnd, out windowProcessId);
                if (windowProcessId == processId && (visibleOnly ? IsWindowVisible(hWnd) : true)) {
                    windows.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        public static List<IntPtr> GetProcessWindows(Process process)
        {
            return GetProcessWindows(process.Id);
        }

        // Windows API methods
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        private const uint WDA_MONITOR = 0x00000001;
        private const uint WDA_NONE = 0x00000000;
        private const int DWMWA_CLOAKED = 14;


        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern int SetWindowDisplayAffinity(IntPtr hWnd, uint nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
    }
}
