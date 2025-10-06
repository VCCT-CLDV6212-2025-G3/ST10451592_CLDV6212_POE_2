using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ABCRetailFunctions
{
    public class UploadFileFunction
    {
        private readonly ILogger _logger;

        public UploadFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadFileFunction>();
        }

        [Function("UploadFile")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function UploadFile processed a request.");
            try
            {
                // Read body
                using var reader = new StreamReader(req.Body, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(content))
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "No file content uploaded." });
                    return badResponse;
                }

                // Generate filename with timestamp
                string fileName = $"contract_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

                // Get connection string
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                if (string.IsNullOrEmpty(connectionString))
                {
                    var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await errorResponse.WriteAsJsonAsync(new { error = "AzureWebJobsStorage connection string is missing." });
                    return errorResponse;
                }

                // Create share client
                var shareClient = new ShareClient(connectionString, "contracts");
                await shareClient.CreateIfNotExistsAsync();

                // Get directory and file client
                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                // Convert content to stream
                using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

                // Upload file
                await fileClient.CreateAsync(fileStream.Length);
                fileStream.Position = 0;
                await fileClient.UploadAsync(fileStream);

                _logger.LogInformation($"File {fileName} uploaded to Azure File Share.");

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = $"File {fileName} uploaded successfully.",
                    fileName = fileName,
                    size = fileStream.Length
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadFile");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
                return errorResponse;
            }
        }
    }
}