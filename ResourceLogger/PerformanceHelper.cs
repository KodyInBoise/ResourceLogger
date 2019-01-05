using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResourceLogger
{
    public enum ResourceType
    {
        CPU,
        Memory
    }

    public delegate void NewResultsReceivedEvent(NewResultsArgs args);

    public class NewResultsArgs
    {
        public DateTime Timestamp { get; private set; }
        public string ProcessName { get; set; }
        public float Value { get; set; }
        public Exception Exception { get; set; }

        public string Message => GetMessage();


        public NewResultsArgs(string process, float value, Exception ex = null)
        {
            Timestamp = DateTime.Now;
            ProcessName = process;
            Value = value;
            Exception = ex;
        }

        string GetMessage()
        {
            if (Exception != null)
            {
                return $"{Timestamp.ToString()} | Exception Occurred: {Exception.Message}";
            }

            return $"{Timestamp.ToString()} | {GetRoundedValue()}%";
        }

        double GetRoundedValue(int decimals = 2)
        {
            return Math.Round(Value, decimals);
        }
    }


    public class PerformanceHelper
    {
        public ResourceType Type { get; set; }

        public NewResultsReceivedEvent NewResultsEvent;

        public string LogPath { get; set; }
        public string ProcessName => _process.Name;

        public int LogInterval { get; set; }
        public int CheckInterval { get; set; }

        public double LastValue { get; set; }

        public bool IsActive { get; set; }

        ProcessWrapper _process { get; set; }
        PerformanceCounter _counter { get; set; }
        CancellationTokenSource _token { get; set; }

        List<string> _entries { get; set; }
        object _entriesLock { get; set; }
        int _logCounter { get; set; }


        public PerformanceHelper(ResourceType type, ProcessWrapper proc)
        {
            _process = proc;
            _entriesLock = new object();
            _entries = new List<string>();

            IsActive = false;
            Type = type;

            switch (type)
            {
                case ResourceType.CPU:
                    _counter = new PerformanceCounter("Process", "% Processor Time", _process.Name);
                    break;
                default:
                    break;
            }
        }

        public void Start()
        {
            IsActive = true;

            _token = new CancellationTokenSource();

            Task.Run(() => CheckTask(_token));
        }

        public void Stop()
        {
            _token.Cancel();

            IsActive = false;
        }

        private async Task CheckTask(CancellationTokenSource token)
        {
            try
            {
                _counter.NextValue();

                while (!token.IsCancellationRequested)
                {
                    var value = _counter.NextValue();

                    await AddResult(value);

                    // Invoke delegate for UI updates
                    NewResultsEvent?.Invoke(new NewResultsArgs(_process.Name, value));

                    Thread.Sleep(TimeSpan.FromSeconds(CheckInterval));
                }
            }
            catch (Exception ex)
            {
                NewResultsEvent?.Invoke(new NewResultsArgs(_process.Name, -1, ex));
            }
        }

        private async Task AddResult(float result)
        {
            var value = Math.Round(result, 2);

            lock (_entriesLock)
            {
                _entries.Add($"{DateTime.Now.ToString()} | {_process.Name}: {value}%");
            }

            _logCounter++;

            if (_logCounter >= LogInterval)
            {
                await WriteResults();
            }

            LastValue = value;

            //Task.Run(UpdateLastResult);
        }

        private async Task AddException(Exception ex)
        {
            lock (_entriesLock)
            {
                _entries.Add($"{DateTime.Now.ToString()} | Exception Occurred: {ex.Message}");
            }

            await WriteResults();
        }

        private async Task WriteResults()
        {
            lock (_entriesLock)
            {
                File.AppendAllLines(LogPath, _entries.ToArray());

                _entries.Clear();
                _logCounter = 0;
            }
        }

        public async Task<IEnumerable<string>> ReadAllResults()
        {
            lock (_entriesLock)
            {
                if (File.Exists(LogPath))
                {
                    return File.ReadAllLines(LogPath);
                }

                return _entries.ToList();
            }
        }
    }
}
