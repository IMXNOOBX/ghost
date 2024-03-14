using HandyControl.Tools;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
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

        public Overlay(IntPtr hwnd) {
            Console.WriteLine($"Creating overlay for ({hwnd})");
            this.hwnd = hwnd;
            CreateOverlay();
            InitBackgroundWorker();

            WVS.WindowVisibilityChecker.init();
        }

        public void destroy() {
            Console.WriteLine($"Destroying overlay for ({hwnd})");
            SetWindowProtected(false);
            bgWorker.CancelAsync();
            bgWorker.Dispose();
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
                Topmost = true,
                Left = rect.Left,
                Top = rect.Top,
                Width = rect.Right - rect.Left,
                Height = rect.Bottom - rect.Top,
                ShowInTaskbar = false,
                //Owner = hwnd // Ensures that overlay is always on top of main window
            };

            this.overlayHwnd = overlayWindow.GetHandle();

            overlayWindow.Content = new TextBlock { Text = "This window is hidden", Padding = new Thickness(12, 4, 4, 4), Foreground = System.Windows.Media.Brushes.White };

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
                    bool isVisible = WVS.WindowVisibilityChecker.IsWindowVisibleOnScreen(hwnd);

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

                Thread.Sleep(25);
            }
        }

        public void SetWindowProtected(bool status) {
            Console.WriteLine($"Changed ({hwnd}) display affinity to: {(status ? "Enabled" : "Disabled")}");

            SetWindowDisplayAffinity(
                overlayHwnd,
                (status) ? WDA_MONITOR : WDA_NONE
            );
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
