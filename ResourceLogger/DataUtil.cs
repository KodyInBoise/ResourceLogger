using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLogger
{
    public class DataUtil
    {
        public static DataUtil Instance { get; private set; }

        public static string RootDirectory => Instance._rootDir;
        public static string OutputDirectory => Instance._outputDir;

        string _rootDir { get; set; }
        string _outputDir { get; set; }
        string _settingsPath { get; set; }

        public DataUtil()
        {
            _rootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BasicLogger");
            _outputDir = Path.Combine(_rootDir, "Output");
            _settingsPath = Path.Combine(_rootDir, "settings.json");

            if (!Directory.Exists(_rootDir))
            {
                Directory.CreateDirectory(_rootDir);
            }

            if (!Directory.Exists(_outputDir))
            {
                Directory.CreateDirectory(_outputDir);
            }
        }

        public static void Initialize()
        {
            Instance = new DataUtil();
        }

        public void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(AppSettings.Instance);

            File.WriteAllText(_settingsPath, json);
        }

        public AppSettings LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                var content = File.ReadAllText(_settingsPath);

                if (!string.IsNullOrEmpty(content))
                {
                    return JsonConvert.DeserializeObject<AppSettings>(content);
                }
            }

            return AppSettings.Create(); 
        }

        public static async Task<IEnumerable<LogEntryModel>> ParseLogEntries(IEnumerable<string> entries)
        {
            var parsedEntries = new List<LogEntryModel>();

            foreach (var entry in entries)
            {
                var parts = entry.Split('|');
                var messageParts = parts[1].Split(':');

                parsedEntries.Add(new LogEntryModel()
                {
                    Timestamp = DateTime.Parse(parts[0].TrimEnd()),
                    Message = messageParts[0],
                    Value = messageParts[1],
                });
            }

            return parsedEntries.ToList();
        }
    }
}
