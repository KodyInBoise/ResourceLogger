using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceLogger
{
    public class ResultModel
    {
        public Guid Session { get; set; }
        public DateTime TimeStamp { get; set; }
        public float CPUValue { get; set; }
        public long WorkingSet { get; set; }

        
        public ResultModel(float cpu = 0, long mem = 0)
        {
            TimeStamp = DateTime.Now;
            CPUValue = cpu;
            WorkingSet = mem;
        }

        public string GetMemoryString()
        {
            return Extensions.ByteConverter.GetString(WorkingSet);
        }
    }
}
