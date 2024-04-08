using Ghost.globals;
using HandyControl.Tools.Extension;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ghost.classes
{
    public class ProtectedProcess {
        public string name { get; set; }
        public string path { get; set; }
        public bool is_window { get; set; } = false;

        public ProtectedProcess() { }
    }

    /**
    * @brief Settings class for the application
    */
    public class Settings {
        public bool apply_mica { get; set; } = false;
        public bool dark_theme { get; set; } = true;
        public bool auto_start { get; set; } = false;
        public bool self_hide { get; set; } = false;
        public bool save_on_exit { get; set; } = false;
        public bool show_hidden_indicator { get; set; } = true;
        public bool only_hide_top_window { get; set; } = true;
        public int ui_update_interval { get; set; } = 5000;
        public int scanner_update_interval { get; set; } = 100;

        public int overlay_type { get; set; } = 0; // 0 = over, 1 = topmost
        public List<ProtectedProcess> protected_processes { get; set; } = new List<ProtectedProcess>();

        public Settings() { }
    }

    public class Config {
        private const string name = "config.json";
        private static string path;
        public static Settings settings = new Settings();

        /**
         * @brief Returns the path to the appdata folder with the ghost folder
         */
        private static string appdata() {
            if (Directory.Exists(path))
                return path;

            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataFolderPath, Globals.name);

            if (!Directory.Exists(appFolder)) 
                Directory.CreateDirectory(appFolder);

            path = appFolder;

            return appFolder;
        }

        /**
         * @brief Saves the current settings to the config file
         */
        public static bool save() {
            if (path.IsNullOrEmpty())
                appdata();

            try {
                string config_path = Path.Combine(path, name);
                logger.success($"Saving config file to {config_path}");

                string json = JsonConvert.SerializeObject(settings);
                
                File.WriteAllText(config_path, json);
            } catch (Exception e) {
                logger.error($"Failed to save config file, Error: {e.Message}");
                return false;
            }

            return true;
        }
        /**
         * @brief Reads the settings from the config file
         */
        public static Settings read() {
            if (path.IsNullOrEmpty())
                appdata();

            string config_path = Path.Combine(path, name);

            if (!File.Exists(config_path)) {
                save();
                return settings;
            }

            try {
                string json = File.ReadAllText(config_path);
                
                logger.log($"Reading config file from {config_path}");
                var read_result = JsonConvert.DeserializeObject<Settings>(json);

                if (read_result != null)
                    settings = read_result;
                else
                    save();

                logger.success($"Read of the config file was {(read_result == null ? "unsuccessfull :c" : "successfull!")}");
            } catch (Exception e) {
                logger.error($"Failed to read config file, Overwritting... Error: {e.Message}");
                save();
            }

            return settings;
        }
    }
}
