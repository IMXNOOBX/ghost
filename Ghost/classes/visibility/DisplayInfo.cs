using System.Collections;
using System.Collections.Generic;

using System;
using System.Runtime.InteropServices;

namespace WVS {
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MonitorInfo {
        public uint size;
        public Rect monitor;
        public Rect work;
        public uint flags;
    }

    // copied this class from http://www.java2s.com/Tutorial/CSharp/0520__Windows/EnumerateDisplayMonitors.htm , works fine, retrieves every monitors data

    public static class DisplayInfo {
        delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);
        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hmon, ref MonitorInfo mi);

        public static int numberOfDisplays;
        public static List<ScreenInfo> Screens = new List<ScreenInfo>();
        public static int[]? bounds;

        static bool MonitorEnum(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) {
            MonitorInfo mi = new MonitorInfo();
            mi.size = (uint)Marshal.SizeOf(mi);
            bool success = GetMonitorInfo(hMonitor, ref mi);
            if (!success) { throw new Exception("Couldnt retrieve Display data"); }
            //Debug.Log(mi.monitor.right);

            numberOfDisplays++;

            Screens.Add(new ScreenInfo(mi.monitor.left, mi.monitor.top, Math.Abs(mi.monitor.right - mi.monitor.left), Math.Abs(mi.monitor.bottom - mi.monitor.top), mi.work));

            // for proper coordinate calculation I need the minimum x and y value
            if (mi.monitor.left < bounds[0]) { bounds[0] = mi.monitor.left; }
            if (mi.monitor.top < bounds[1]) { bounds[1] = mi.monitor.top; }

            return true;
        }


        public static void getDisplaysInfo() {
            bounds = new int[] { 100000, 100000 };
            MonitorEnumDelegate med = new MonitorEnumDelegate(MonitorEnum);
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, med, IntPtr.Zero);
        }
    }

    

    public class ScreenInfo {
        public int x, y, width, height;
        public Rect workingArea;

        public ScreenInfo(int x, int y, int width, int height, Rect workingArea) {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.workingArea = workingArea;
        }
    }
}