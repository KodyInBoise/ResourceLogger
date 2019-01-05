using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ResourceLogger
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window
    {
        public PerformanceHelper Profiler { get; set; }

        CancellationTokenSource _cancelToken { get; set; }


        public ResultsWindow(PerformanceHelper profiler)
        {
            InitializeComponent();

            Profiler = profiler;
            profiler.NewResultsEvent += UpdateLastResult;

            ToggleButton.Click += (s, e) => ToggleButton_Clicked();

            ProcessNameLabel.Content = $"Process Name: {Profiler.ProcessName}";
            ProcessStartedLabel.Content = $"Started: {DateTime.Now.ToString()}";

            Show();

            // Auto start profiler when the window is opened
            ToggleButton_Clicked();
        }

        private void UpdateLastResult(NewResultsArgs args)
        {
            _cancelToken?.Cancel();

            _cancelToken = new CancellationTokenSource();

            Task.Run(() => InvokeOnUI(() =>
            {
                LastResultLabel.Content = $"Last Result: {args.Message}";               
            }), _cancelToken.Token);
        }

        private async Task InvokeOnUI(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }

        private void ToggleButton_Clicked()
        {
            ToggleButton.Visibility = Visibility.Collapsed;

            if (Profiler.IsActive)
            {
                Profiler.Stop();
            }
            else
            {
                Profiler.Start();
            }

            ToggleButton.Content = Profiler.IsActive ? "Stop" : "Start";
            ToggleButton.Visibility = Visibility.Visible;
        }
    }
}
