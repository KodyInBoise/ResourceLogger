using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLogger
{
    public class ProcessWrapper
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive => GetIsProcessActive();


        public ProcessWrapper(Process proc)
        {
            ID = proc.Id;
            Name = proc.ProcessName;
        }

        bool GetIsProcessActive()
        {
            var proc = Process.GetProcessById(ID);
            if (proc != null && proc.Responding)
            {
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
