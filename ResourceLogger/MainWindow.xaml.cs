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
        int _logCounter { get; set; }
        int _checkInt { get; set; }

        double _lastValue { get; set; }

        PerformanceCounter _counter { get; set; }
        CancellationTokenSource _token { get; set; }

        List<string> _entries { get; set; }
        object _entriesLock { get; set; }

        bool _checking { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            ProcessesRefreshButton.Click += ProcessesRefreshButton_Click;
            LogPathBrowseButton.Click += LogPathBrowseButton_Click;

            RefreshProcesses();
        }

        private void RefreshProcesses()
        {
            ProcessesRefreshButton_Click(null, null);

            ProcessesComboBox.SelectedIndex = 0;
        }

        private void ProcessesRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            var procs = Process.GetProcesses().OrderBy(x => x.ProcessName).ToList();

            ProcessesComboBox.Items.Clear();
            procs.ForEach(x =>
            {
                ProcessesComboBox.Items.Add(x.ProcessName);
            });
        }

        private void LogPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    _logPath = Path.Combine(dialog.SelectedPath, $"{GetSelectedProcessName()}_CPU.txt");

                    LogPathTextBox.Text = _logPath;
                }
            }
        }

        private string GetSelectedProcessName()
        {
            return _processName = ProcessesComboBox.Text;
        }

        private void StartChecking(object sender, RoutedEventArgs e)
        {
            _processName = ProcessesComboBox.Text;
            _logInt = Convert.ToInt32(LogEveryTextBox.Text);
            _checkInt = Convert.ToInt32(CheckEveryTextBox.Text);
            _token = new CancellationTokenSource();

            _entriesLock = new object();
            _entries = new List<string>();

            Task.Run(() => CheckTask(_token));

            ProcessNameLabel.Content = $"Process: {_processName}";
            ProcessStartedLabel.Content = $"Started {DateTime.Now.ToString()}";
            LastResultLabel.Content = $"Last Result: {DateTime.Now.ToString()} | Starting...";
            TabController.SelectedItem = ProcessTab;
        }

        private void StopChecking()
        {
            _token.Cancel();
            _checking = false;
        }

        private async Task CheckTask(CancellationTokenSource token)
        {
            try
            {
                _checking = true;

                _counter = new PerformanceCounter("Process", "% Processor Time", _processName);
                _counter.NextValue();

                while (!token.IsCancellationRequested)
                {
                    var value = _counter.NextValue();

                    await AddResult(value);

                    Thread.Sleep(TimeSpan.FromSeconds(_checkInt));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task AddResult(float result)
        {
            var value = Math.Round(result, 2);

            lock(_entriesLock)
            {
                _entries.Add($"{DateTime.Now.ToString()} | {_processName}: {value}%");
            }

            _logCounter++;

            if (_logCounter >= _logInt)
            {
                await WriteResults();
            }

            _lastValue = value;

            Task.Run(UpdateLastResult);
        }

        private async Task AddException(Exception ex)
        {
            lock(_entriesLock)
            {
                _entries.Add($"{DateTime.Now.ToString()} | Exception Occurred: {ex.Message}");
            }

            await WriteResults();
        }

        private async Task WriteResults()
        {
            lock (_entriesLock)
            {
                File.AppendAllLines(_logPath, _entries.ToArray());

                _entries.Clear();
                _logCounter = 0;
            }
        }

        private void ShowException(Exception ex)
        {
            MessageBox.Show($"Exception Occurred: {ex.Message}");
        }

        private async Task InvokeOnUI(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }

        private async Task UpdateLastResult()
        {
            await InvokeOnUI(() =>
            {
                LastResultLabel.Content = $"{DateTime.Now.ToString()} | {_lastValue}%";
            });
        }
    }
}
