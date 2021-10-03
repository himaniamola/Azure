using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobStorageLogging
{
    class Program
    {
        static void Main(string[] args)
        {
            var programLog = LogMgr.Instance.GetLogger(typeof(Program));
            programLog.Info("Line 14 executed");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
