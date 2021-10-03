using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.IO;

namespace BasicFunction
{
    public static class BasicFunctions
    {
        [FunctionName("BlobTriggerFunction")]
        public static void Run([BlobTrigger("application/{name}")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
