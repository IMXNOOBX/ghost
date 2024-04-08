using Ghost.globals;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Ghost.classes
{
    class TrayUtils
    {
        private static NotifyIcon trayIcon;
        private static MainWindow mainWindow;
        public TrayUtils(MainWindow mw) {
            if (trayIcon != null)
                return;

            mainWindow = mw;
            trayIcon = new NotifyIcon();
            trayIcon.Init();

            trayIcon.Icon = new BitmapImage(new Uri("pack://application:,,,/assets/reload-white.png"));
            trayIcon.Text = Globals.fullName;
            trayIcon.ToolTip = Globals.name;
            //trayIcon.IsBlink = true;

            init_options();
        }

        private void init_options() {
            trayIcon.ContextMenu = new ContextMenu();

            var open = new MenuItem { Header = "Open" };
            var open_github = new MenuItem { Header = "Open Github" };
            
            var settings = new MenuItem { Header = "Settings" };
            var settings_uninstall = new MenuItem { Header = "Uninstall" };

            var exit = new MenuItem { Header = "Exit", Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location) };

            /**
             * Event handlers
             */
            open.Click += (sender, e) => {
                mainWindow.Show();
            };
            open_github.Click += (sender, e) => {
                Process.Start(new ProcessStartInfo { FileName = Globals.website, UseShellExecute = true });
            };
            settings_uninstall.Click += (sender, e) => {
                Console.WriteLine("Open clicked");
                var result = MessageBox.Ask($"Are you sure you want to uninstall {Globals.name}?", "Uninstall");

                if (result == System.Windows.MessageBoxResult.OK) {

                }
            };
            exit.Click += (sender, e) => {
                mainWindow.Close();
            };


            /**
             * Add the menu items to the tray icon
             */
            trayIcon.ContextMenu.Items.Add(open);
            trayIcon.ContextMenu.Items.Add(open_github);

            settings.Items.Add(settings_uninstall);
            trayIcon.ContextMenu.Items.Add(settings);

            trayIcon.ContextMenu.Items.Add(exit);
        }

        public void notify(string title, string message) {
            trayIcon.ShowBalloonTip(title, message, HandyControl.Data.NotifyIconInfoType.None);
        }
    }
}
