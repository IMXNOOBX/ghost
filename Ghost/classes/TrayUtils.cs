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

        private static BitmapImage open_image;
        private static BitmapImage github_image;
        private static BitmapImage close_image;

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

            open_image = new BitmapImage(new Uri("pack://application:,,,/assets/application-white.png"));
            github_image = new BitmapImage(new Uri("pack://application:,,,/assets/github-white.png"));
            close_image = new BitmapImage(new Uri("pack://application:,,,/assets/close-white.png"));

            init_options();
        }

        /**
         * @brief Initializes the tray icon options
         */
        private void init_options() {
            trayIcon.ContextMenu = new ContextMenu();

            var open = new MenuItem { Header = "Open", Icon = new System.Windows.Controls.Image { Source = open_image } };
            var open_github = new MenuItem { Header = "Open Github", Icon = new System.Windows.Controls.Image { Source = github_image } };
            
            var settings = new MenuItem { Header = "Settings", Icon = "\\" };
            var settings_uninstall = new MenuItem { Header = "Uninstall", Icon = ":C" };

            var exit = new MenuItem { Header = "Exit", Icon = new System.Windows.Controls.Image { Source = close_image } };

            /**
             * @brief Event handlers
             */
            open.Click += (sender, e) => {
                mainWindow.Show();
            };
            open_github.Click += (sender, e) => {
                Process.Start(new ProcessStartInfo { FileName = Globals.repository, UseShellExecute = true });
            };
            settings_uninstall.Click += (sender, e) => {
                var result = MessageBox.Ask($"Are you sure you want to uninstall {Globals.name}?", "Uninstall");

                if (result == System.Windows.MessageBoxResult.OK) {
                    Utilities.Uninstall();
                }
            };
            exit.Click += (sender, e) => {
                mainWindow.Close();
            };


            /**
             * @brief Add the menu items to the tray icon
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
