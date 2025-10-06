using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetailFunctions
{
    public class UploadBlobFunction
    {
        private readonly ILogger<UploadBlobFunction> _logger;

        public UploadBlobFunction(ILogger<UploadBlobFunction> logger)
        {
            _logger = logger;
        }

        [Function("UploadBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function UploadBlob processed a request.");
            try
            {
                // Get ProductId from header
                string productId = req.Headers.GetValues("ProductId")?.FirstOrDefault();
                if (string.IsNullOrEmpty(productId))
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "Product ID is required in header." });
                    return badResponse;
                }

                // Read the multipart form data
                var boundary = GetBoundary(req.Headers);
                if (string.IsNullOrEmpty(boundary))
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "Missing content-type boundary." });
                    return badResponse;
                }

                var fileData = await ParseMultipartFormData(req.Body, boundary);
                if (fileData == null || fileData.Content == null || fileData.Content.Length == 0)
                {
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "No file uploaded." });
                    return badResponse;
                }

                // Generate unique filename
                string originalFileName = fileData.FileName ?? "image.jpg";
                string fileName = $"{productId}_{Guid.NewGuid()}_{originalFileName}";

                // Get connection string
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                if (string.IsNullOrEmpty(connectionString))
                {
                    var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await errorResponse.WriteAsJsonAsync(new { error = "AzureWebJobsStorage connection string is missing." });
                    return errorResponse;
                }

                // Upload to blob storage
                var containerClient = new BlobContainerClient(connectionString, "productimages");
                await containerClient.CreateIfNotExistsAsync();
                var blobClient = containerClient.GetBlobClient(fileName);

                using (var stream = new MemoryStream(fileData.Content))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                _logger.LogInformation($"Blob {fileName} uploaded for product {productId}.");

                string blobUrl = blobClient.Uri.ToString();
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = $"Image uploaded for product {productId}.",
                    url = blobUrl,
                    productId = productId,
                    fileName = fileName
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadBlob");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
                return errorResponse;
            }
        }

        private string GetBoundary(HttpHeadersCollection headers)
        {
            var contentType = headers.GetValues("Content-Type")?.FirstOrDefault();
            if (string.IsNullOrEmpty(contentType))
                return null;

            var parts = contentType.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring("boundary=".Length).Trim('"');
                }
            }
            return null;
        }

        private async Task<FileData> ParseMultipartFormData(Stream body, string boundary)
        {
            using var reader = new StreamReader(body);
            var content = await reader.ReadToEndAsync();

            var delimiter = $"--{boundary}";
            var parts = content.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (part.Contains("Content-Disposition: form-data"))
                {
                    // Extract filename
                    string fileName = null;
                    var lines = part.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    foreach (var line in lines)
                    {
                        if (line.Contains("filename="))
                        {
                            var start = line.IndexOf("filename=\"") + "filename=\"".Length;
                            var end = line.IndexOf("\"", start);
                            if (end > start)
                            {
                                fileName = line.Substring(start, end - start);
                            }
                            break;
                        }
                    }

                    // Extract file content
                    var contentStart = part.IndexOf("\r\n\r\n");
                    if (contentStart >= 0)
                    {
                        contentStart += 4;
                        var contentEnd = part.LastIndexOf("\r\n");
                        if (contentEnd > contentStart)
                        {
                            var fileContent = part.Substring(contentStart, contentEnd - contentStart);
                            return new FileData
                            {
                                FileName = fileName,
                                Content = System.Text.Encoding.UTF8.GetBytes(fileContent)
                            };
                        }
                    }
                }
            }

            return null;
        }

        private class FileData
        {
            public string FileName { get; set; }
            public byte[] Content { get; set; }
        }
    }
}