using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

using System.Runtime.InteropServices;
using static WVS.WindowVisibilityChecker;

namespace WVS {
    static class WindowVisibilityChecker {

        #region Setup

        #region Imports
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);
        
        
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();



        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
        #endregion


        // Used to reduce the rect size if the window is partially out of bounds, small tolerance needed
        // https://stackoverflow.com/questions/34139450/getwindowrect-returns-a-size-including-invisible-borders: appears to be 7px invisible border or something
        private const int tolerance = 8;

        #region Structs & Enums
        public struct Rect {
            public int Left, Top, Right, Bottom;

            public static Rect Init(int Left, int Top, int Right, int Bottom) {
                Rect ret = new Rect();
                ret.Left = Left;
                ret.Right = Right;
                ret.Top = Top;
                ret.Bottom = Bottom;
                return ret;
            }

            // removes the invisible border from the calculations
            public void SubtractBordersForWindow() {
                Left += tolerance;
                Bottom -= tolerance;
                Right -= tolerance;
            }


            /// <summary>
            /// Check for collisions between two rects
            /// </summary>
            /// <param name="rect"></param>
            /// <returns></returns>
            public bool Overlap(Rect rect) {
                return rect.Left < Right && rect.Right > Left && rect.Bottom > Top && rect.Top < Bottom;
            }

            /// <summary>
            /// Limits the rects size according to the bounds
            /// </summary>
            /// <param name="rect"></param>
            /// <param name="bounds"></param>
            /// <returns></returns>
            public static Rect ClampRect(Rect rect, Rect bounds) {
                return Rect.Init(Math.Min(Math.Max(bounds.Left, rect.Left), bounds.Right), Math.Min(Math.Max(bounds.Top, rect.Top), bounds.Bottom),
                    Math.Max(Math.Min(bounds.Right, rect.Right), bounds.Left), Math.Max(Math.Min(bounds.Bottom, rect.Bottom), bounds.Top));
            }


            public static implicit operator string(Rect rect)
            {
                return rect.Left + "|" + rect.Top + " " + rect.Right + "|" + rect.Bottom;
            }
        }

        public struct Point
        {
            public int X, Y;

            public static implicit operator string(Point p)
            {
                return p.X + "|" + p.Y;
            }
        }

        /// <summary>
        /// Contains information about the placement of a window on the screen.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int Length;
            public int Flags;
            public ShowWindowCommands ShowCmd;
            public Point MinPosition;
            public Point MaxPosition;
            public Rect NormalPosition;

            public static WINDOWPLACEMENT Default
            {
                get
                {
                    WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                    result.Length = Marshal.SizeOf(result);
                    return result;
                }
            }
        }

        public enum ShowWindowCommands
        {
            Hide = 0,
            Normal = 1,
            ShowMinimized = 2,
            Maximize = 3,
            ShowMaximized = 3,
            ShowNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActive = 7,
            ShowNA = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimize = 11
        }


        public enum WindowVisibilityStatus {
            invalid = -1,
            hidden,
            oob,
            coveredByFullScreen,
            completelyCovered,
            partiallyCovered,
            completelyVisible,
            activeWindow
        }

        #endregion

        #region Constants
        const int SM_CXVIRTUALSCREEN = 78;
        const int SM_CYVIRTUALSCREEN = 79;
        const int SM_CMONITORS = 80;
        #endregion

        #region Variables
        private static int width, height, numberOfDisplays;
        public static int sizeOfTaskbar;
        public static WindowVisibilityStatus extendedInfo = WindowVisibilityStatus.invalid;
        private static Rect window;
        private static IntPtr currentHwnd;
        private static List<Rect> rects;
        #endregion

        /// <summary>
        /// All windows with their title inside this list will be ignored.
        /// </summary>
        public static HashSet<string> ignoreList = new HashSet<string>() { "Program Manager" };

        public static void init(bool DebugInfo = false) {
            getDisplaysData(DebugInfo);

            sizeOfTaskbar = DisplayInfo.Screens[0].height - DisplayInfo.Screens[0].workingArea.bottom;
        }


        private static void getDisplaysData(bool Debuginfo = false) {
            width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            numberOfDisplays = GetSystemMetrics(SM_CMONITORS);

            DisplayInfo.getDisplaysInfo();
        }


        public static string ExtendedInfoToMessage() {
            switch (extendedInfo) {
                case WindowVisibilityStatus.hidden: return "Completely hidden";
                case WindowVisibilityStatus.oob: return "Out of bounds";
                case WindowVisibilityStatus.coveredByFullScreen: return "Covered by a fullscreen window";
                case WindowVisibilityStatus.completelyCovered: return "Covered completely";
                case WindowVisibilityStatus.partiallyCovered: return "Partially covered";
                case WindowVisibilityStatus.completelyVisible: return "Completely Visible";
                case WindowVisibilityStatus.activeWindow: return "Active window";

                default: return "No info available yet. If this doesnt change over time, the window probably doesnt exist";
            }
        }

        #endregion

        /// <summary>
        /// Returns true if the window is visible in any way (Excluding Aero Peek)
        /// </summary>
        /// <param name="handle">Handle of the window to process</param>
        /// <param name="includeTaskbarAsNormalScreen">If this parameter is true, the program assumes that the taskbar won't cover up windows / is hidden</param>
        /// <returns>True or false. Use ExtendedInfo variable to get more specific information</returns>
        // includeTaskbar includes the taskbar as normal screen in every calculation, increasing the size needed for a non fullscreen window (aka something maximized) to count as "completely covering"
        public static bool IsWindowVisibleOnScreen(IntPtr handle, bool includeTaskbarAsNormalScreen = false) {
            currentHwnd = handle;                                      // Store the current window's handle
            extendedInfo = WindowVisibilityStatus.completelyVisible;   // Default: Visible
            

            // Basic sanity checks
            if (!IsWindow(handle)) { extendedInfo = WindowVisibilityStatus.invalid; return false; }
            if (GetForegroundWindow() == handle) { extendedInfo = WindowVisibilityStatus.activeWindow; return true; }

            
            // Get the current placement and additional data for the window
            // WINDOWPLACEMENT placement = WINDOWPLACEMENT.Default;
            //GetWindowPlacement(handle, ref placement);

            // Those two are pretty much equivalent, but the second one doesn't need additional calls to the user32.dll
            if (!IsWindowVisible(handle) || IsIconic(handle)) { extendedInfo = WindowVisibilityStatus.hidden; return false; }
            //if (placement.ShowCmd == ShowWindowCommands.Hide || placement.ShowCmd == ShowWindowCommands.ShowMinimized) { extendedInfo = WindowVisibilityStatus.hidden; return false; }


            // Check for out of bounds:
            GetWindowRect(handle, out window);
            window.SubtractBordersForWindow();
            
            //Console.WriteLine(window);
            
            // Check if the window is inside the client working area
            if (window.Left > DisplayInfo.bounds[0] + width || window.Right < DisplayInfo.bounds[0] || window.Top > DisplayInfo.bounds[1] + height || window.Bottom < DisplayInfo.bounds[1]) { extendedInfo = WindowVisibilityStatus.oob; return false; }
            

            // Check every window for overlap
            rects = new List<Rect>();

            // If the window overlaps with the taskbar, reduce its size so it doesn't
            if (!includeTaskbarAsNormalScreen && window.Bottom > DisplayInfo.bounds[1] + height - sizeOfTaskbar) { window.Bottom = DisplayInfo.bounds[1] + height - sizeOfTaskbar; }

            // Enumerate over all windows and check if they cover our window
            CallBackPtr callBackPtr = new CallBackPtr(EnumReport.Report);
            EnumReport.EnumDesktopWindows(IntPtr.Zero, callBackPtr, IntPtr.Zero);
            
            //Console.WriteLine("Covered by " + rects.Count + " windows");
            if (rects.Count > 0) { IsCoveredByWindows(); }

            // Everything smaller than partiallyCovered (4) is not visible
            if (extendedInfo < WindowVisibilityStatus.partiallyCovered) { return false; }
            return true;
        }


        //https://stackoverflow.com/questions/16180667/given-a-rectangular-area-and-a-set-of-rectangles-check-if-the-entire-area-is-co/16180879#16180879 @Syam answer:
        private static bool IsCoveredByWindows() {
            List<Rect> cover = new List<Rect>();        // The list of rectangles that need to be covered in order for the whole window to be hidden
            cover.Add(Rect.ClampRect(window, Rect.Init(DisplayInfo.bounds[0], DisplayInfo.bounds[1], DisplayInfo.bounds[0] + width, DisplayInfo.bounds[1] + height)));  // Add the initial window

            for (int i = 0; i < rects.Count; i++) {
                int length = cover.Count;
                Stack<Rect> remove = new Stack<Rect>();

                for (int i2 = 0; i2 < length; i2++) {
                    // Check for overlap
                    if (rects[i].Overlap(cover[i2])) {
                        remove.Push(cover[i2]);

                        // Now add between 0 and 4 new rects in a way, that none of them overlap with rects[i]
                        if (rects[i].Left - cover[i2].Left > 0) { cover.Add(Rect.Init(cover[i2].Left, cover[i2].Top, rects[i].Left, cover[i2].Bottom)); }
                        if (cover[i2].Right - rects[i].Right > 0) { cover.Add(Rect.Init(rects[i].Right, cover[i2].Top, cover[i2].Right, cover[i2].Bottom)); }
                        // Now for Vertical (Note that the width is reduced/different so that the new rects dont overlap)
                        if (rects[i].Top - cover[i2].Top > 0) { cover.Add(Rect.Init(Math.Max(rects[i].Left, cover[i2].Left), cover[i2].Top, Math.Min(rects[i].Right, cover[i2].Right), rects[i].Top)); }
                        if (cover[i2].Bottom - rects[i].Bottom > 0) { cover.Add(Rect.Init(Math.Max(rects[i].Left, cover[i2].Left), rects[i].Top, Math.Min(rects[i].Right, cover[i2].Right), cover[i2].Bottom)); }
                    }
                }

                length = remove.Count;
                for (int i2 = 0; i2 < length; i2++) {
                    cover.Remove(remove.Pop());
                }

                // If there are no uncovered areas left, return true
                if (cover.Count == 0) { extendedInfo = WindowVisibilityStatus.completelyCovered; return true; }

                /*for (int i2 = 0; i2 < cover.Count; i2++) {
                    Console.WriteLine("cover["+i2+"]: " + RectToString(cover[i2]));
                }*/
            }

            extendedInfo = WindowVisibilityStatus.partiallyCovered;
            return false;
        }

        
        

        private delegate bool CallBackPtr(IntPtr hwnd, int lParam);



        private class EnumReport {

            #region Imports
            [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool EnumDesktopWindows(IntPtr hDesktop, CallBackPtr lpEnumCallbackFunction, IntPtr lParam);

            //[DllImport("user32.dll")]
            //private static extern bool EnumChildWindows(IntPtr window, CallBackPtr callback, int lParam);

            [DllImport("user32.dll")]
            private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", SetLastError = true)]
            static extern bool GetLayeredWindowAttributes(IntPtr hwnd, out uint crKey, out byte bAlpha, out uint dwFlags);

            [DllImport("user32.dll", EntryPoint = "GetWindowText",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);
            [DllImport("user32.dll", EntryPoint = "GetClassName",
            ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetClassName(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);
            #endregion


            //private static Rect rect = new Rect();
            private static WINDOWPLACEMENT placement = WINDOWPLACEMENT.Default;


            public static bool Report(IntPtr hwnd, int lParam) {
                // No more checking needed after our window is reached, false should end EnumDesktopWindows
                if (hwnd == currentHwnd) { return false; }
                
                //GetWindowPlacement(hwnd, ref placement);
                
                // Check for visibility
                if (IsWindowVisible(hwnd) && !IsIconic(hwnd)) {
                //if (placement.ShowCmd != ShowWindowCommands.Hide && placement.ShowCmd != ShowWindowCommands.ShowMinimized) {
                    StringBuilder strbTitle = new StringBuilder(255);
                    int nLength = GetWindowText(hwnd, strbTitle, strbTitle.Capacity + 1);

                    
                    long windowLong = GetWindowLong(hwnd, -20);

                    // Visibility Check, not properly implemented yet, some windows have the layered attribute, even if they don't use it
                    // Ignore all of them with an opacity below 255
                    if ((windowLong & 0x00080000) == 0x00080000) { byte alpha; if (GetLayeredWindowAttributes(hwnd, out _, out alpha, out _) && alpha < 255) { return true; } }

                    // Ignore windows with the WS_EX_NOACTIVATE flag, it's impossible to interact with them, they are mostly system windows, most of the time they are not visible (Especially you, CicerUIWnd)
                    if ((windowLong & 0x08000000) == 0x08000000) { return true; }


                    // Almost every window has a title, except for system windows. Also compare every window title against the ignore list
                    if (strbTitle.ToString().Length == 0 || ignoreList.Contains(strbTitle.ToString())) { return true; }

                    GetClassName(hwnd, strbTitle, strbTitle.Capacity + 1);
                    // Ignore System Windows (Such as Microsoft Text Input Application)
                    if (strbTitle.ToString() == "Windows.UI.Core.CoreWindow") { return true; }


                    // Check if windows overlap
                    Rect rect = new Rect();
                    GetWindowRect(hwnd, out rect);
                    rect.SubtractBordersForWindow();

                    if (rect.Overlap(WindowVisibilityChecker.window)) {
                        WindowVisibilityChecker.rects.Add(rect);
                        
                        //Console.WriteLine(strbTitle.ToString() + " " + strbTitle.ToString().Length + ": " + rect);

                        // Sadly placement.NormalPosition does not consider the maximized / minimized status of a window, making it more or less useless
                        //Console.WriteLine(strbTitle.ToString() + " " + strbTitle.ToString().Length + ": " + placement.NormalPosition + " " + placement.MaxPosition);
                    }
                    
                }

                // And check all child windows ? - Nope, not implemented yet

                return true;
            }
        }


    }
}