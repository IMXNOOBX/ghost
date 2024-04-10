using HandyControl.Tools;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Ghost.globals;
using System.Windows.Interop;
using Ghost.classes;
using HandyControl.Controls;
using System.Runtime.InteropServices;
using HandyControl.Tools.Extension;
using System.Diagnostics;
using System.ComponentModel;
using System.Security.Policy;
using System;
using System.Reflection;
using System.Drawing;


namespace Ghost
{
    public partial class MainWindow : System.Windows.Window
    {
        private static List<ProcessHandler> processes = new List<ProcessHandler>();
        private static ProcessHandler? cache_selected_handler;
        private static TrayUtils trayIcon;
        public MainWindow()
        {
            /**
             * @brief Check command line parameters for silent mode flag
             */
            string[] arguments = Environment.GetCommandLineArgs();

            if (arguments.Length > 1 && arguments[1] == "--silent") {
                Globals.silent = true;
                logger.warn("Running in silent mode...");
            }

            /**
             * @brief Hide window if it was started in silent mode. (Auto start)
             */
            if (Globals.silent)
                this.Visibility = Visibility.Hidden;

            /**
             * @brief Register the tray icon
             */
            trayIcon = new TrayUtils(this);

#if DEBUG
            logger.allocConsole();
            logger.log("Allocated console for debugging.");
#elif RELEASE
            logger.log("Running in release mode.");
#endif

            Globals.isLight = WindowHelper.DetermineIfInLightThemeMode();
            var chrome = new WindowChrome
            {
                CaptionHeight = 0,
                UseAeroCaptionButtons = false,
                CornerRadius = new CornerRadius(15),
                //GlassFrameThickness = new Thickness(0),
                //ResizeBorderThickness = new Thickness(3),
                NonClientFrameEdges = NonClientFrameEdges.None,
            };

            WindowChrome.SetWindowChrome(this, chrome);

            InitializeComponent();

            // Set window properties
            this.Closing += on_closing;
            this.Title = Globals.fullName;
            this.WindowStyle = WindowStyle.None; // WindowStyle.SingleBorderWindow;
            this.Width = Globals.windowSize.X;
            this.MinWidth = Globals.windowSize.X;
            this.Height = Globals.windowSize.Y;
            this.MinHeight = Globals.windowSize.Y;
            //this.Background = isLight ? Brushes.White : Brushes.Black;
            //this.Opacity = 0.95;

            SolidColorBrush brush = new SolidColorBrush(Colors.Black);
            brush.Opacity = 0.8;
            this.Background = brush;

            //this.AllowsTransparency = true;
            //this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Activate();

            // Apply acrylic effect
            if (Globals.applyMica) {
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;

                MicaHelper.Apply(this, BackdropType.Mica);
                if (!MicaHelper.RemoveTitleBar(windowHandle))
                    MicaHelper.Remove(this);

                if (!Globals.isLight)
                    MicaHelper.ApplyDarkMode(this);
            }

            // Update Ui elemets
            {
                AppTitle.Text = Globals.fullName;

                // Read settings from previous sessions
                Config.read();

                // Hide self. startup setter & event
                bHideSelf.IsChecked = Config.settings.self_hide; 
                bHideSelf.Click += (s, e) => {
                    Config.settings.self_hide = (bool)bHideSelf.IsChecked;
                    Utilities.setVisibility(0, (uint)(Config.settings.self_hide ? 1 : 0)); // Hell this types
                };
                // Overlay type. startup setter & event
                i2OverlayType.SelectedIndex = Config.settings.overlay_type;
                i2OverlayType.SelectionChanged += (s, e) => { Config.settings.overlay_type = i2OverlayType.SelectedIndex; };
                // Auto run on startup. startup setter & event
                bAutoRunOnStartup.IsChecked = Startup.is_registered();  
                bAutoRunOnStartup.Click += (s, e) => {
                    if ((bool)bAutoRunOnStartup.IsChecked)
                        Startup.register();
                    else
                        Startup.unregister();
                };
                // Save on exit. startup setter & event
                bSaveOnExit.IsChecked = Config.settings.save_on_exit;
                bSaveOnExit.Click += (s, e) => { Config.settings.save_on_exit = (bool)bSaveOnExit.IsChecked; };
                // Show hidden indicator. startup setter & event
                bHiddenIndicator.IsChecked = Config.settings.show_hidden_indicator;
                bHiddenIndicator.Click += (s, e) => { Config.settings.show_hidden_indicator = (bool)bHiddenIndicator.IsChecked; };
                // Only hide top window. startup setter & event
                bOnlyTopWindow.IsChecked = Config.settings.only_hide_top_window;
                bOnlyTopWindow.Click += (s, e) => { Config.settings.only_hide_top_window = (bool)bOnlyTopWindow.IsChecked; };
                // Update rate. startup setter
                switch (Config.settings.ui_update_interval) {
                    case 1000:
                        rbFast.IsChecked = true;
                        break;
                    case 5000:
                        rbNormal.IsChecked = true;
                        break;
                    case 10000:
                        rbSlow.IsChecked = true;
                        break;
                    default:
                        rbNormal.IsChecked = true;
                        Config.settings.ui_update_interval = 5000;
                        Config.settings.scanner_update_interval = 300;
                        break;
                }
            }

            check_loading();
            // Make it asyncronous
            Task.Run(() => update_ui_processes());
            Task.Run(update_processes);
            Task.Run(resources_thread);
        }

        public void update_processes() {
            foreach (var proc in System.Diagnostics.Process.GetProcesses()) {
                if (proc.MainWindowHandle == IntPtr.Zero)
                    continue;

                // Check if the process was excluded before in the config
                var excluded = Config.settings.protected_processes.Find(x => x.name == proc.ProcessName) != null;
                
                if (!excluded)
                    continue;

                // If the process is already in the list and its already excluded, we skip it
                var lExclude = ProcessHandler.cache_processes.Find(x => x.name == proc.ProcessName);

                //Console.WriteLine($"Found cached process {lExclude}({lExclude?.name}) and its {(lExclude?.excluded == true ? "Excluded" : "Not Excludedd")}");

                if (lExclude != null && lExclude.excluded)
                    continue;

                // If the process was in the configuration and its not excluded, we need to exclude it
                Application.Current.Dispatcher.Invoke(() => {
                    var overlay = new Overlay(proc);
                    if (lExclude != null) {
                        lExclude.overlay = overlay;
                        lExclude.excluded = true;
                    }
                    else
                        ProcessHandler.update_process(proc, true, overlay);
                });

            }

            Thread.Sleep(Config.settings.scanner_update_interval);
            update_processes();
        }


        public void update_ui_processes(bool recursive = true) {
            processes = ProcessHandler.get_processes();

            Application.Current.Dispatcher.Invoke(() => {
                // If there is something in the search box, we need to filter the processes
                if (!SearchTextBox.Text.IsNullOrEmpty()) {
                    filter_processes(null, null);
                    return;
                }

                // Else we fill up the list with the processes
                if (ProcessDataGrid.Items.Count > 0)
                    ProcessDataGrid.Items.Clear();

                foreach (var proc in processes)
                {
                    if (proc.path.IsNullOrEmpty())
                        continue;

                        ProcessDataGrid.Items.Add(proc);
                }

                //ProcessDataGrid.Items.DeferRefresh();
            });

            Application.Current.Dispatcher.Invoke(() => {
                check_loading();
            });

            Thread.Sleep(Config.settings.ui_update_interval);

            if (recursive)
                update_ui_processes();
        }

        public void check_loading() {
            bool is_loading = processes == null;

            LoadingLine.Visibility = is_loading ? Visibility.Visible : Visibility.Hidden;
            NavigationBar.Visibility = !is_loading ? Visibility.Visible : Visibility.Hidden;
            SearchContainer.Visibility = !is_loading ? Visibility.Visible : Visibility.Hidden;
            TopInnerSettingsContainer.Visibility = !is_loading ? Visibility.Visible : Visibility.Hidden;
            BottomInnerSettingsContainer.Visibility = !is_loading ? Visibility.Visible : Visibility.Hidden;
        }

        private void target_exclude_modified(object sender, RoutedEventArgs e) {

            // Filter out invalid selected processes
            var item = cache_selected_handler;

            if (item == null)
                return;

            var process = processes.Find(x => x.name == item.name);

            if (process == null)
                return;

            // process.excluded is already modified with the status we want to change to
            //                                                              before                                            after
            logger.warn($"Found process {process.name}({process.pid}) [{(!process.excluded ? "hidden" : "visible")}] => [{(process.excluded ? "hidden" : "visible")}]");
            if (process.overlay != null)
                process.overlay.destroy();

            process.overlay = process.excluded ? new Overlay(process.proc) : null;

            // Store the process in the protected list for next runs
            if (process.excluded && Config.settings.protected_processes.Find(x => x.name == process.name) == null)
                Config.settings.protected_processes
                    .Add(new ProtectedProcess { name = process.name, path = process.path });
            else if (!process.excluded)
                Config.settings.protected_processes
                    .Remove(Config.settings.protected_processes.Find(x => x.name == process.name)); // Unsafe Code
             
            // Refresh the list
            ProcessDataGrid.Items.Refresh();
            ProcessDataGrid.SelectedIndex = -1;
        }

        private void filter_processes(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            ProcessDataGrid.Items.Clear();

            if (processes == null)
                return;

            string search = SearchTextBox.Text.ToLower();

            // Add processes to the list
            foreach (var proc in processes)
            {
                if (proc.path.IsNullOrEmpty())
                    continue;

                if (
                    !(proc.name.ToLower().Contains(search) ||
                        proc.path.ToLower().Contains(search) ||
                        proc.pid.ToString().Contains(search))
                    )
                    continue;

                ProcessDataGrid.Items.Add(proc);
            }

            check_loading();
        }

        private void handled_event(object sender, MouseButtonEventArgs e) { }
        private void disable_selection(object sender, SelectionChangedEventArgs e) {
            if (ProcessDataGrid.SelectedIndex == -1)
                return;

            cache_selected_handler = ProcessDataGrid.SelectedItem as ProcessHandler;
            ProcessDataGrid.SelectedIndex = -1;
        }

        private void refresh_list(object sender, RoutedEventArgs e) {
            Task.Run(() => update_ui_processes(false));
            logger.log("Manually refreshing list...");
        }

        private void drag_window(object sender, MouseButtonEventArgs e) {
            try {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            } catch { }
        }

        private void minimize_window(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }

        private void misc_button_click(object sender, RoutedEventArgs e){
            if (sender == ManualSaveSettings)
                Config.save();
            else if (sender == OpenGithub)
                Process.Start(new ProcessStartInfo { FileName = Globals.repository, UseShellExecute = true });
            else if (sender == ReportIssue)
                Process.Start(new ProcessStartInfo { FileName = $"{Globals.repository}/issues", UseShellExecute = true });
        }

        private void updaterate_checked(object sender, RoutedEventArgs e) {
            if (rbFast.IsChecked == true) {
                Config.settings.ui_update_interval = 1000;
                Config.settings.scanner_update_interval = 100;
            }
            else if (rbNormal.IsChecked == true){
                Config.settings.ui_update_interval = 5000;
                Config.settings.scanner_update_interval = 300;
            }
            else if (rbSlow.IsChecked == true) { 
                Config.settings.ui_update_interval = 10000;
                Config.settings.scanner_update_interval = 500;
            }
        }

        // Close with a little animation
        private void close_window(object sender, RoutedEventArgs e) {
            int speed_delta = 10;
            //while (this.Width > 0 && this.Height > 0 && this.Opacity > 0) {
            //    this.Width -= (this.Width - 4 * speed_delta >= 0) ? 4 * speed_delta : 0;
            //    this.Height -= (this.Height - 3 * speed_delta >= 0) ? 3 * speed_delta : 0;
            //    this.Opacity -= 0.01 + (speed_delta / 100);
            //}
            while (this.Top > -System.Windows.SystemParameters.WorkArea.Height) {
                this.Top -= 0.01 + (speed_delta);
            }

            this.Close();
            Environment.Exit(0); // Fallback in case there are other windows open
        }

        private void resources_thread() {

            var cpu = Utilities.get_cpu();
            var ram = Utilities.get_ram();

            Application.Current.Dispatcher.Invoke(() => {
                cpuProgressBar.Value = cpu;
                cpuProgressBar.SetValue(HandyControl.Controls.VisualElement.TextProperty, $"{cpu:F2}%");
                ramProgressBar.Value = ram;
                ramProgressBar.SetValue(HandyControl.Controls.VisualElement.TextProperty, $"{ram:F2}%");
            });

            Thread.Sleep(1000);

            resources_thread();
        }

        static void on_closing(object? sender, CancelEventArgs e) {
            if (Config.settings.save_on_exit)
                Config.save();
        }
    }
}