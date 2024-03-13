using HandyControl.Tools;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

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
        }

        public void destroy() {
            SetWindowProtected(false);
            bgWorker.CancelAsync();
            bgWorker.Dispose();
            overlayWindow.Close();
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

            Console.WriteLine($"Overlay created successfully for ({hwnd})!");

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
                RECT rect;
                GetWindowRect(hwnd, out rect);

                Dispatcher.Invoke(() => {
                    // Check if any corners of the window are visible
                    bool isVisible = IsWindowVisibleAndOnTop(hwnd);

                    try {
                        if (overlayWindow != null)
                            overlayWindow.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
                    }  catch { }

                    SetWindowPos(overlayHwnd, IntPtr.Zero, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, 0);
                });

                System.Threading.Thread.Sleep(25);
            }
        }

        public void SetWindowProtected(bool status) {
            Console.WriteLine($"Changed ({hwnd}) display affinity to: {(status ? "Enabled" : "Disabled")}");

            SetWindowDisplayAffinity(
                overlayHwnd,
                (status) ? WDA_MONITOR : WDA_NONE
            );
        }

        public static bool IsWindowVisibleAndOnTop(IntPtr hWnd)
        {
            if (!IsWindowVisible(hWnd))
                return false;

            RECT rect;
            GetWindowRect(hWnd, out rect);

            Console.WriteLine($"Checking TopLeft ({rect.Left}/{rect.Top}) {WindowFromPoint(rect.Left, rect.Top)} == {hWnd}");
            Console.WriteLine($"Checking TopRight ({rect.Right}/{rect.Top}) {WindowFromPoint(rect.Right - 1, rect.Top)} == {hWnd}");
            Console.WriteLine($"Checking BottomLeft ({rect.Left}/{rect.Bottom}) {WindowFromPoint(rect.Left, rect.Bottom - 1)} == {hWnd}");
            Console.WriteLine($"Checking BottomRight ({rect.Right}/{rect.Bottom}) {WindowFromPoint(rect.Right - 1, rect.Bottom - 1)} == {hWnd}");


            // Check if all corners of the window are visible and are the same window
            return (WindowFromPoint(rect.Left, rect.Top) == hWnd && // TopLeft Check
                        WindowFromPoint(rect.Right - 1, rect.Top) == hWnd && // TopRight Check
                        WindowFromPoint(rect.Left, rect.Bottom - 1) == hWnd && // BottomLeft Check
                        WindowFromPoint(rect.Right - 1, rect.Bottom - 1) == hWnd); // BottomRight Check
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
        private static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(int x, int y);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int SetWindowDisplayAffinity(IntPtr hWnd, uint nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
