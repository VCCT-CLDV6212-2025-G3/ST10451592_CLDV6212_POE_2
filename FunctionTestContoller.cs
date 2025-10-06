using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http; // Required for IFormFile
using System;
using System.IO;

namespace ABCRetailWebApp.Controllers
{
    public class FunctionTestController : Controller
    {
        private readonly HttpClient _httpClient;

        // Fields for the full URLs
        private readonly string _addCustomerUrl;
        private readonly string _queueMessageUrl;
        private readonly string _uploadFileUrl; // Retained but not used in any method
        private readonly string _uploadBlobUrl;

        private readonly string _functionBaseUrl;

        public FunctionTestController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();

            _functionBaseUrl = configuration["AzureFunctions:BaseUrl"] ?? "http://localhost:7071";

            // Reading the full URLs with /api and ?code=...
            _addCustomerUrl = configuration["AzureFunctions:AddCustomerUrl"]
                ?? $"{_functionBaseUrl}/api/AddCustomer?code=YOUR_MASTER_KEY_FALLBACK";

            _queueMessageUrl = configuration["AzureFunctions:QueueMessageUrl"]
                ?? $"{_functionBaseUrl}/api/QueueMessage?code=YOUR_MASTER_KEY_FALLBACK";

            _uploadFileUrl = configuration["AzureFunctions:UploadFileUrl"]
                ?? $"{_functionBaseUrl}/api/UploadFile?code=YOUR_MASTER_KEY_FALLBACK";

            _uploadBlobUrl = configuration["AzureFunctions:BlobUrl"]
                ?? $"{_functionBaseUrl}/api/UploadBlob?code=YOUR_MASTER_KEY_FALLBACK";
        }

        // GET: /FunctionTest/Index - Display the test page
        public IActionResult Index()
        {
            ViewBag.FunctionBaseUrl = _functionBaseUrl;
            return View();
        }

        // POST: /FunctionTest/TestAddCustomer - Test AddCustomer function
        [HttpPost]
        public async Task<IActionResult> TestAddCustomer(string firstName, string lastName, string email, string phoneNumber, string address)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                TempData["ErrorMessage"] = "First Name and Last Name are required.";
                return RedirectToAction("Index");
            }

            try
            {
                var customerData = new
                {
                    firstName = firstName,
                    lastName = lastName,
                    email = email,
                    phoneNumber = phoneNumber,
                    address = address
                };

                var json = JsonSerializer.Serialize(customerData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_addCustomerUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Customer added via Function! Response: {responseBody}";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Function call failed: {response.StatusCode} - {responseBody}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error calling function: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /FunctionTest/TestQueueMessage - Test QueueMessage function
        [HttpPost]
        public async Task<IActionResult> TestQueueMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Message is required.";
                return RedirectToAction("Index");
            }

            try
            {
                var messageData = new { message = message };
                var json = JsonSerializer.Serialize(messageData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_queueMessageUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Queue message added! Response: {responseBody}";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Function call failed: {response.StatusCode} - {responseBody}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error calling function: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // ❌ TestUploadFile function removed as per the simplified requirement.

        // POST: /FunctionTest/TestUploadBlob - Test UploadBlob function (Universal File Uploader)
        [HttpPost]
        public async Task<IActionResult> TestUploadBlob(IFormFile file, string productId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                TempData["ErrorMessage"] = "Product ID is required.";
                return RedirectToAction("Index");
            }

            try
            {
                var finalUrl = _uploadBlobUrl;

                // Use HttpRequestMessage to attach a custom header to the request.
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);

                // Add the file stream part for robust binary upload.
                content.Add(streamContent, "file", file.FileName);

                // 🚀 FIX: Create HttpRequestMessage to send the custom header
                using var request = new HttpRequestMessage(HttpMethod.Post, finalUrl)
                {
                    Content = content
                };

                // Add the custom header. The Azure Function must be updated to read "X-Product-ID".
                request.Headers.Add("X-Product-ID", productId);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Provide a detailed success message indicating the header usage.
                    TempData["SuccessMessage"] = $"Blob uploaded! Response: {responseBody}. NOTE: The Product ID was sent in the 'X-Product-ID' custom HTTP header.";
                }
                else
                {
                    // If the error persists, the issue is certainly the Azure Function code itself.
                    TempData["ErrorMessage"] = $"Function call failed: {response.StatusCode} - {responseBody}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error calling function: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}