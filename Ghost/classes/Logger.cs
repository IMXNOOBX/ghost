using Ghost.globals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ghost.classes
{
    public static class logger
    {
        private static string path;

        static logger()
        {
            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataFolderPath, Globals.name);

            if (!Directory.Exists(appFolder))
                Directory.CreateDirectory(appFolder);

            path = Path.Combine(appFolder, "logs");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            
            string session_file = $"session_{DateTime.Now:yyyyMMdd}.log";
            path = Path.Combine(path, session_file);
        }

        private static void cout(string log, ConsoleColor color) {
            string message = $"[{DateTime.Now}] {log}";
            
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();

            using (StreamWriter sw = File.AppendText(path)) {
                sw.WriteLine(message);
            }
        }

        public static void allocConsole() {
            AllocConsole();
            Console.Title = Globals.name + " | Debug Console";
        }

        public static void log(string message) {
            cout($"[LOG] {message}", ConsoleColor.Blue);
        }

        public static void success(string message) {
            cout($"[LOG] {message}", ConsoleColor.Green);
        }

        public static void warn(string message) {
            cout($"[LOG] {message}", ConsoleColor.Yellow);
        }

        public static void error(string message) {
            cout($"[LOG] {message}", ConsoleColor.Red);
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
