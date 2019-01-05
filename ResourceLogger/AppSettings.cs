using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ResourceLogger
{
    public class AppSettings
    {
        public static AppSettings Instance { get; set; }

        public string OutputDir { get; set; }
        public bool MinimizeWhenActive { get; set; }


        public static void Initialize()
        {
            Instance = DataUtil.Instance.LoadSettings() ?? Create();
        }

        public static AppSettings Create()
        {
            return new AppSettings()
            {
                OutputDir = DataUtil.OutputDirectory,
                MinimizeWhenActive = true,
            };
        }
    }
}
