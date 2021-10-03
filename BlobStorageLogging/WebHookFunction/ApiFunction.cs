using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Configuration;
using System.Data.SqlClient;

namespace WebHookFunction
{
    public static class ApiFunction
    {
        /// This can be called using http://localhost:7071/api/WebHookProcess
        [FunctionName("WebHookProcess")]
        public static async Task<IActionResult> WebHookProcess(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("WebHookProcess processed a request.");

            string responseMessage = "This WebHookProcess executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        [FunctionName("LogUserRequest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            UpdateClientLastAccessed(log);

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        private static async void UpdateClientLastAccessed(ILogger log)
        {
            string connString = Environment.GetEnvironmentVariable("DBConnection");

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                var text = " UPDATE [dbo].[Client] SET LastAccessed = GETDATE(); ";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.
                    int rows = -1;
                    try
                    {
                        rows = await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        log.LogInformation($"Exception occurred {ex}");
                    }

                    log.LogInformation($"{rows} rows are updated");
                }
            }
        }

        //http://localhost:7071/api/QueueOutput?msg=MessageForQueue
        [FunctionName("QueueOutput")]
        [return: Queue("basicqueue")]
        public static string QueueOutput([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            string msgReceived = req.Query["msg"];
            log.LogInformation($"C# function processed: {msgReceived}");
            return msgReceived;
        }
    }
}
