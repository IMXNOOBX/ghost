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
        public static List<ProcessHandler> processes;
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
            new Thread(() => {
                Console.WriteLine("Getting processes list...");
                var startTime = DateTime.Now;

                processes = ProcessHandler.get_processes();
                foreach (var proc in processes) {
                    if (proc.path.IsNullOrEmpty())
                        continue;

                    Application.Current.Dispatcher.Invoke(() => {
                        ProcessDataGrid.Items.Add(proc);
                    });
                }

                Application.Current.Dispatcher.Invoke(() => {
                    check_loading();
                });

                Console.WriteLine("Processes list updated. " + (DateTime.Now - startTime) + "ms");
            }).Start();
        }

        public void check_loading() {
            bool is_loading = processes == null;

            LoadingLine.Visibility = is_loading ? Visibility.Visible : Visibility.Hidden;
            SearchContainer.Visibility = !is_loading ? Visibility.Visible : Visibility.Hidden;
        }

        private void targetExcludeModified(object sender, RoutedEventArgs e)
        {
            var item = (ProcessHandler)ProcessDataGrid.SelectedItem;
            Console.WriteLine("Target exclude modified: " + item.ToString());
            if (item == null)
                return;

            var process = processes.Find(x => x.name == item.name);
            if (process == null)
                return;

            process.exclude(!process.excluded);
            Console.Beep();

            ProcessDataGrid.Items.Refresh();
        }

        private void filterProcesses(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            ProcessDataGrid.Items.Clear();

            if (processes == null)
                return;

            // Add processes to the list
            foreach (var proc in processes)
            {
                if (proc.path.IsNullOrEmpty())
                    continue;

                if (!proc.name.ToLower().Contains(SearchTextBox.Text.ToLower()))
                    continue;

                ProcessDataGrid.Items.Add(proc);
            }

            check_loading();
        }
    }
}