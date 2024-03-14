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


namespace Ghost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private static List<ProcessHandler> processes;
        private static ProcessHandler? cache_selected_handler;
        public MainWindow()
        {
            var chrome = new WindowChrome
            {
                CaptionHeight = 0,
                UseAeroCaptionButtons = false,
                CornerRadius = new CornerRadius(15),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(3),
                NonClientFrameEdges = NonClientFrameEdges.None,
            };

            //WindowChrome.SetWindowChrome(this, chrome);

            InitializeComponent();

            // Set window properties
            this.Title = config.fullName;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.Width = config.windowSize.X;
            this.Height = config.windowSize.Y;
            this.Background = Brushes.Black;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Activate();

            // Set ToolBar properties
            //ToolBar.Header = config.name;

            bool isLight = WindowHelper.DetermineIfInLightThemeMode();

            // Apply acrylic effect
            if (config.applyMica) {
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;

                MicaHelper.Apply(this, BackdropType.Mica);
                if (!MicaHelper.RemoveTitleBar(windowHandle))
                    MicaHelper.Remove(this);

                if (!isLight)
                    MicaHelper.ApplyDarkMode(this);
            }

            check_loading();
            // Make it asyncronous
            Utilities.SetInterval(update_processes, TimeSpan.FromSeconds(30));
        }

        public async void update_processes() {
            var startTime = DateTime.Now;

            processes = ProcessHandler.get_processes();

            Application.Current.Dispatcher.Invoke(() => {
                // If there is something in the search box, we need to filter the processes
                if (!SearchTextBox.Text.IsNullOrEmpty()) {
                    filterProcesses(null, null);
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

            Console.WriteLine($"Processes list added into ui in {(DateTime.Now - startTime).TotalMilliseconds}ms");
            Application.Current.Dispatcher.Invoke(() => {
                check_loading();
            });
        }

        public void check_loading() {
            bool is_loading = processes == null;

            LoadingLine.Visibility = is_loading ? Visibility.Visible : Visibility.Hidden;
            SearchContainer.Visibility = !is_loading ? Visibility.Visible : Visibility.Hidden;
        }

        private void targetExcludeModified(object sender, RoutedEventArgs e) {
            //var item = processes.ElementAt(cache_index); // (ProcessHandler)ProcessDataGrid.SelectedItem;
            var item = cache_selected_handler;
            Console.WriteLine($"Target exclude modified value {(item != null ? item.name : "is not existant")}");

            if (item == null)
                return;

            var process = processes.Find(x => x.name == item.name);

            if (process == null)
                return;

            // process.excluded is already modified with the status we want to change to
            //                                                              before                                            after
            Console.WriteLine($"Found process {process.name}({process.pid}) [{(!process.excluded ? "hidden" : "visible")}] => [{(process.excluded ? "hidden" : "visible")}]");
            //process.excluded = !process.excluded;

            if (process.overlay != null)
                process.overlay.destroy();

            process.overlay = process.excluded ? new Overlay(process.hwnd) : null;

            ProcessDataGrid.Items.Refresh();
            ProcessDataGrid.SelectedIndex = -1;
        }

        private void filterProcesses(object sender, System.Windows.Controls.TextChangedEventArgs e) {
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
        private void disableSelection(object sender, SelectionChangedEventArgs e) {
            if (ProcessDataGrid.SelectedIndex == -1)
                return;

            cache_selected_handler = ProcessDataGrid.SelectedItem as ProcessHandler;
            ProcessDataGrid.SelectedIndex = -1;
            Console.WriteLine($"Selection changed to {(cache_selected_handler != null ? cache_selected_handler.name : "was not existant")}");
        }
    }
}