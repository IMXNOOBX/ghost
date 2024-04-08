using Ghost.globals;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ghost.classes
{
    static class Startup
    {
        private static string path = System.Reflection.Assembly.GetExecutingAssembly().Location;

        /**
         * @brief Checks if the application is registered in the startup
         */
        public static bool is_registered() {
            try
            {
                RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                
                if (key == null) 
                    return false;

                return key.GetValue(Globals.name) != null; ;
            } catch { return false; }
        }
         /**
         * @brief Registers the application to the startup
         */
        public static bool register() {
            if (is_registered()) return true;

            try {
                RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                
                if (key == null)
                    return false;

                var full_path = (path.Contains(".dll")) ?
                        path.Replace(".dll", ".exe") : path;

                key.SetValue(
                    Globals.name, 
                    $"\"{full_path}\" --silent"
                );

                return true;
            } catch { return false; }
        }

        /**
         * @brief Unregisters the application from the startup
         */
        public static bool unregister()  {
            if (!is_registered()) return true;

            try
            {
                RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (key == null)
                    return false;

                key.DeleteValue(Globals.name);

                return true;
            } catch { return false; }
        }
    }
}
