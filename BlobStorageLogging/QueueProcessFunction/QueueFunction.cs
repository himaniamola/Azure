using System;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace QueueProcessFunction
{
    public static class QueueFunction
    {
        [FunctionName("Function1")]
        public static void Run([QueueTrigger("basicqueue", Connection = "")]string queueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {queueItem}");
            //Process the queue msg
            AddNewUser(queueItem, log);
        }

        private static async void AddNewUser(string msg, ILogger log)
        {
            string connString = Environment.GetEnvironmentVariable("DBConnection");

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                var text = $" INSERT INTO [dbo].[Client](Name, CreateDate) Values('{msg}', GETDATE()); ";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command 
                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        log.LogInformation($"Exception occurred {ex} when trying to add the user");
                    }

                    log.LogInformation($"User added successfully");
                }
            }
        }
    }
}
