using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.IO; // Added to support Stream in UploadBlobAsync
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABCRetailWebApp.Services
{
    public class AzureFunctionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureFunctionService> _logger;
        // Fields for the full URLs, including /api/ and ?code=...
        private readonly string _addCustomerFunctionUrl;
        private readonly string _queueMessageFunctionUrl;
        private readonly string _uploadFileFunctionUrl;

        // Existing fields
        private readonly string _functionBaseUrl;
        private readonly string _blobFunctionUrl;

        public AzureFunctionService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AzureFunctionService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;

            // Get function URLs from configuration

            // 1. Function Base URL (used for logging/default only)
            _functionBaseUrl = configuration["AzureFunctions:BaseUrl"]
                ?? "https://abcretailfunctionsst10451592-arfwh6d8cpaydwhh.southafricanorth-01.azurewebsites.net";

            // 2. Blob Function URL (already contained the full path and code, so it's correct)
            _blobFunctionUrl = configuration["AzureFunctions:BlobUrl"]
                ?? "https://abcretailfunctionsst10451592-arfwh6d8cpaydwhh.southafricanorth-01.azurewebsites.net/api/UploadBlob?code=n9uWd6IBIvbqI5u77Q_2Bb9XoVtGgaZwHU8ADgg0WL8nAzFuGCHzlA==";

            // 3. NEW: Retrieve the full, correct URLs for the other functions
            _addCustomerFunctionUrl = configuration["AzureFunctions:AddCustomerUrl"]
                ?? $"{_functionBaseUrl}/api/AddCustomer?code=n9uWd6IBIvbqI5u77Q_2Bb9XoVtGgaZwHU8ADgg0WL8nAzFuGCHzlA==";

            _queueMessageFunctionUrl = configuration["AzureFunctions:QueueMessageUrl"]
                ?? $"{_functionBaseUrl}/api/QueueMessage?code=n9uWd6IBIvbqI5u77Q_2Bb9XoVtGgaZwHU8ADgg0WL8nAzFuGCHzlA==";

            _uploadFileFunctionUrl = configuration["AzureFunctions:UploadFileUrl"]
                ?? $"{_functionBaseUrl}/api/UploadFile?code=n9uWd6IBIvbqI5u77Q_2Bb9XoVtGgaZwHU8ADgg0WL8nAzFuGCHzlA==";
        }

        /// <summary>
        /// Calls the AddCustomer Azure Function to store customer in Table Storage
        /// </summary>
        public async Task<string> AddCustomerAsync(string firstName, string lastName, string email, string phoneNumber, string address)
        {
            try
            {
                var customerData = new
                {
                    firstName,
                    lastName,
                    email,
                    phoneNumber,
                    address
                };

                var json = JsonSerializer.Serialize(customerData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Calling AddCustomer function for {firstName} {lastName}");

                // FIX: Use the full URL read from configuration
                var response = await _httpClient.PostAsync(_addCustomerFunctionUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Customer added successfully via Function");
                    return responseBody;
                }
                else
                {
                    _logger.LogError($"Function call failed: {response.StatusCode} - {responseBody}");
                    throw new Exception($"Function returned {response.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AddCustomer function");
                throw;
            }
        }

        /// <summary>
        /// Calls the QueueMessage Azure Function to add message to Queue Storage
        /// </summary>
        public async Task<string> AddQueueMessageAsync(string message)
        {
            try
            {
                var messageData = new { message };
                var json = JsonSerializer.Serialize(messageData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Calling QueueMessage function with: {message}");

                // FIX: Use the full URL read from configuration
                var response = await _httpClient.PostAsync(_queueMessageFunctionUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Queue message added successfully via Function");
                    return responseBody;
                }
                else
                {
                    _logger.LogError($"Function call failed: {response.StatusCode} - {responseBody}");
                    throw new Exception($"Function returned {response.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling QueueMessage function");
                throw;
            }
        }

        /// <summary>
        /// Calls the UploadBlob Azure Function (master URL) to upload file to Blob Storage
        /// </summary>
        public async Task<string> UploadBlobAsync(Stream fileStream, string fileName, string productId)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);

                content.Add(streamContent, "file", fileName);

                if (!string.IsNullOrEmpty(productId))
                {
                    content.Add(new StringContent(productId), "productId");
                }

                _logger.LogInformation($"Calling UploadBlob function for file: {fileName}");

                // This URL was already correct as it was read as a full URL
                var response = await _httpClient.PostAsync(_blobFunctionUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Blob uploaded successfully via Function");
                    return responseBody;
                }
                else
                {
                    _logger.LogError($"Function call failed: {response.StatusCode} - {responseBody}");
                    throw new Exception($"Function returned {response.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling UploadBlob function");
                throw;
            }
        }

        /// <summary>
        /// Calls the UploadFile Azure Function to upload file to Azure File Share
        /// </summary>
        public async Task<string> UploadFileAsync(string fileContent)
        {
            try
            {
                var content = new StringContent(fileContent, Encoding.UTF8, "text/plain");

                _logger.LogInformation($"Calling UploadFile function with content length: {fileContent.Length}");

                // FIX: Use the full URL read from configuration
                var response = await _httpClient.PostAsync(_uploadFileFunctionUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("File uploaded successfully via Function");
                    return responseBody;
                }
                else
                {
                    _logger.LogError($"Function call failed: {response.StatusCode} - {responseBody}");
                    throw new Exception($"Function returned {response.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling UploadFile function");
                throw;
            }
        }
    }
}