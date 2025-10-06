using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace ABCRetailFunctions
{
    public class AddCustomerFunction
    {
        private readonly ILogger _logger;

        public AddCustomerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AddCustomerFunction>();
        }

        [Function("AddCustomer")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("AddCustomer function processing request.");

            try
            {
                // Read and parse request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"Request body: {requestBody}");

                string firstName = null, lastName = null, email = null, phoneNumber = null, address = null;

                // Try JSON first
                try
                {
                    var jsonDoc = JsonDocument.Parse(requestBody);
                    var root = jsonDoc.RootElement;

                    firstName = root.TryGetProperty("firstName", out var fn) ? fn.GetString() : null;
                    lastName = root.TryGetProperty("lastName", out var ln) ? ln.GetString() : null;
                    email = root.TryGetProperty("email", out var em) ? em.GetString() : null;
                    phoneNumber = root.TryGetProperty("phoneNumber", out var pn) ? pn.GetString() : null;
                    address = root.TryGetProperty("address", out var ad) ? ad.GetString() : null;
                }
                catch
                {
                    // Fall back to form data
                    var formData = HttpUtility.ParseQueryString(requestBody);
                    firstName = formData["firstName"];
                    lastName = formData["lastName"];
                    email = formData["email"];
                    phoneNumber = formData["phoneNumber"];
                    address = formData["address"];
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                    string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phoneNumber) ||
                    string.IsNullOrWhiteSpace(address))
                {
                    _logger.LogWarning("Missing required fields");
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "All fields are required (firstName, lastName, email, phoneNumber, address)." });
                    return badResponse;
                }

                // Get connection string from environment
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("AzureWebJobsStorage connection string is missing");
                    var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                    await errorResponse.WriteAsJsonAsync(new { error = "Storage configuration error." });
                    return errorResponse;
                }

                // Create table client and ensure table exists
                var tableClient = new TableClient(connectionString, "Customers");
                await tableClient.CreateIfNotExistsAsync();
                _logger.LogInformation("Customers table ensured to exist");

                // Create customer entity
                var customerId = Guid.NewGuid().ToString();
                var customer = new TableEntity("Customers", customerId)
                {
                    { "FirstName", firstName },
                    { "LastName", lastName },
                    { "Email", email },
                    { "PhoneNumber", phoneNumber },
                    { "Address", address },
                    { "CreatedDate", DateTimeOffset.UtcNow }
                };

                // Upsert entity (idempotent operation)
                await tableClient.UpsertEntityAsync(customer);
                _logger.LogInformation($"Customer {customerId} added successfully");

                // Return success response
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    message = "Customer added successfully.",
                    id = customerId,
                    firstName = firstName,
                    lastName = lastName,
                    email = email
                });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddCustomer function");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = $"Internal error: {ex.Message}" });
                return errorResponse;
            }
        }
    }


public class Customer : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTimeOffset? CreatedDate { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public Azure.ETag ETag { get; set; }
    }
}