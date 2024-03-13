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
    public class Settings
    {
        public static bool applyMica { get; set; }
        public static int darkTheme { get; set; }
        public static int autoStart { get; set; }
        public static int selfHide { get; set; }

        public Settings() { }
    }

    public class Config {
        private const string name = "config.json";
        private static string path;
        private static Settings settings;


        public static string getAppdata() {
            if (Directory.Exists(path))
                return path;

            string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataFolderPath, config.name);

            if (!Directory.Exists(appFolder)) {
                Directory.CreateDirectory(appFolder);
            }

            path = appFolder;

            return appFolder;
        }


        public static bool save() {
            if (path.IsNullOrEmpty())
                getAppdata();

            try {
                string config_path = Path.Combine(path, name);

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(config_path, json);
            } catch (Exception e) {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        public static Settings read() {
            if (path.IsNullOrEmpty())
                getAppdata();

            string config_path = Path.Combine(path, name);

            if (!File.Exists(config_path)) {
                save();
                return settings;
            }

            try {
                string json = File.ReadAllText(config_path);
                settings = JsonConvert.DeserializeObject<Settings>(json);
            } catch (Exception e) {
                Console.WriteLine(e);
                save();
            }

            return settings;
        }
    }
}
