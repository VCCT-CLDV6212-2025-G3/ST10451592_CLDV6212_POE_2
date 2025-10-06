using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABCRetailFunctions
{
    public class QueueMessageFunction
    {
        private readonly ILogger<QueueMessageFunction> _logger;

        public QueueMessageFunction(ILogger<QueueMessageFunction> logger)
        {
            _logger = logger;
        }

        [Function("QueueMessage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function QueueMessage processed a request.");
            try
            {
                string message = null;

                // Read the request body
                using (var reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    var content = await reader.ReadToEndAsync();

                    // Try to parse as JSON first
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(content);
                        if (jsonDoc.RootElement.TryGetProperty("message", out var messageElement))
                        {
                            message = messageElement.GetString();
                        }
                    }
                    catch
                    {
                        // If not JSON, check if it's form data
                        if (content.Contains("="))
                        {
                            var formData = System.Web.HttpUtility.ParseQueryString(content);
                            message = formData["message"];
                        }
                        else
                        {
                            // Treat as plain text
                            message = content;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "Message is required." });
                    return badResponse;
                }

                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                if (string.IsNullOrEmpty(connectionString))
                {
                    var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await errorResponse.WriteAsJsonAsync(new { error = "AzureWebJobsStorage connection string is missing." });
                    return errorResponse;
                }

                var queueClient = new QueueClient(connectionString, "orderprocessing");
                await queueClient.CreateIfNotExistsAsync();

                // Send the message
                await queueClient.SendMessageAsync(message);

                // Peek at messages in the queue
                var peekedMessages = await queueClient.PeekMessagesAsync(maxMessages: 5);
                var responseList = new List<string>();

                if (peekedMessages.Value != null)
                {
                    foreach (var peekedMessage in peekedMessages.Value)
                    {
                        responseList.Add(peekedMessage.MessageText);
                    }
                }

                _logger.LogInformation($"Message '{message}' added. Peeked {responseList.Count} messages.");

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    AddedMessage = message,
                    PeekedMessages = responseList,
                    TotalPeeked = responseList.Count
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QueueMessage");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
                return errorResponse;
            }
        }
    }
}