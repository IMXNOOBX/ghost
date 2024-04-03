using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Ghost.globals
{
    internal class Globals
    {
        public const string version = "1.0.0";
        public const string name = "Ghost";
        public const string fullName = "Ghost Window Hidder";
        public const string developer = "IMXNOOBX";
        public const string website = "https://github.com/IMXNOOBX/ghost";

        public static Vector2 windowSize = new Vector2(800, 500);

        public static bool silent = false; // Silent run, made for the auto start so it doesnt bother the user
        public const bool applyMica = false;
        public static bool isLight = false;

        public const int ui_update_interval = 5000; // This is the interval for the UI update, in ms
        public const int scanner_update_interval = 100; // This is the interval the app will be scanning for new processes, in ms
    }
}
