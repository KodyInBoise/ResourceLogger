using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace ResourceLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string _processName { get; set; }
        string _logPath { get; set; }

        int _logInt { get; set; }
        int _checkInt { get; set; }

        List<ProcessWrapper> _processes { get; set; }
        List<PerformanceHelper> _profilers { get; set; }
        List<ResultsWindow> _resultWindows { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            DataUtil.Initialize();
            AppSettings.Initialize();

            LogPathBrowseButton.Click += LogPathBrowseButton_Click;
            ProcessesComboBox.DropDownClosed += (s, e) => ProcessesComboBox_SelectionChanged();
            ProcessesRefreshButton.Click += (s, e) => RefreshProcesses();
            StartButton.Click += (s, e) => StartNewProfiler();

            _processes = new List<ProcessWrapper>();
            _profilers = new List<PerformanceHelper>();
            _resultWindows = new List<ResultsWindow>();

            RefreshProcesses();

            ProcessesComboBox_SelectionChanged();
        }

        int _lastSelectedID = -1;
        private void RefreshProcesses()
        {
            _processes = new List<ProcessWrapper>();

            var procs = Process.GetProcesses().OrderBy(x => x.ProcessName).ToList();
            procs.ForEach(x =>
            {
                var wrapper = new ProcessWrapper(x);
                _processes.Add(wrapper);
            });

            ProcessesComboBox.Items.Clear();

            _processes.ForEach(x =>
            {
                ProcessesComboBox.Items.Add(x);
            });

            SetProcessesComboBox();
        }

        private void ProcessesComboBox_SelectionChanged()
        {
            var item = (ProcessWrapper)ProcessesComboBox.SelectedItem;
            _lastSelectedID = item.ID;

            LogPathTextBox.Text = Path.Combine(AppSettings.Instance.OutputDir, $"{item.Name}.txt");

            LogPathTextBox.Focus();
            LogPathTextBox.ScrollToEnd();
        }

        private void SetProcessesComboBox()
        {
            if (_lastSelectedID > 0)
            {
                var item = _processes.Find(x => x.ID == _lastSelectedID);
                if (item != null)
                {
                    ProcessesComboBox.SelectedItem = item;
                }
            }
            else
            {
                ProcessesComboBox.SelectedItem = _processes[0];
            }
        }

        private void LogPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    _logPath = Path.Combine(dialog.SelectedPath, $"{ProcessesComboBox.Text}_CPU.txt");

                    LogPathTextBox.Text = _logPath;
                }
            }
        }

        private ProcessWrapper GetSelectedProcess()
        {
            return (ProcessWrapper)ProcessesComboBox.SelectedItem;
        }

        private void StartNewProfiler()
        {
            try
            {
                var profiler = new PerformanceHelper(ResourceType.CPU, GetSelectedProcess())
                {
                    LogInterval = Convert.ToInt32(LogEveryTextBox.Text), 
                    CheckInterval = Convert.ToInt32(CheckEveryTextBox.Text),
                    LogPath = LogPathTextBox.Text
                };

                var window = new ResultsWindow(profiler);

                _profilers.Add(profiler);
                _resultWindows.Add(window);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start new profiler: {ex.Message}");
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            await Task.Run(() => DataUtil.Instance.SaveSettings());
        }
    }
}
