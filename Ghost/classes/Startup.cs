﻿using Ghost.globals;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ghost.classes
{
    static class Startup
    {
        private static string path = System.Reflection.Assembly.GetExecutingAssembly().Location;

        public static bool is_registered() {
            try
            {
                RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                
                if (key == null) 
                    return false;

                return key.GetValue(Globals.name) != null; ;
            } catch { return false; }
        }

        public static bool register() {
            if (is_registered()) return true;

            try {
                RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                
                if (key == null)
                    return false;

                key.SetValue(
                    Globals.name, 
                    (path.Contains(".dll")) ? 
                        path.Replace(".dll", ".exe") : path
                );

                return true;
            } catch { return false; }
        }

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