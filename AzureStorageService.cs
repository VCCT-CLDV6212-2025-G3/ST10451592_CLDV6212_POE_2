// File: Services/AzureStorageService.cs
using ABCRetailWebApp.Models;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ABCRetailWebApp.Services
{
    public class AzureStorageService
    {
        private readonly string _connectionString;
        private readonly TableClient _customersTable;
        private readonly TableClient _productsTable;
        private readonly BlobContainerClient _blobContainer;
        private readonly QueueClient _queueClient;
        private readonly ShareClient _fileShareClient;

        public AzureStorageService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureStorage:ConnectionString"];
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string is missing or empty.");
            }
            _customersTable = new TableClient(_connectionString, "Customers");
            _productsTable = new TableClient(_connectionString, "Products");
            _blobContainer = new BlobContainerClient(_connectionString, "productimages");
            _queueClient = new QueueClient(_connectionString, "orderprocessing");
            _fileShareClient = new ShareClient(_connectionString, "contracts");

            _blobContainer.CreateIfNotExists();
            _queueClient.CreateIfNotExists();
            // Note: _fileShareClient is created, but existence check/creation is best done right before use
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            customer.RowKey = Guid.NewGuid().ToString();
            await _customersTable.UpsertEntityAsync(customer);
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _customersTable.UpsertEntityAsync(customer);
        }

        public async Task DeleteCustomerAsync(string rowKey)
        {
            await _customersTable.DeleteEntityAsync("Customers", rowKey);
        }

        public async Task<Customer> GetCustomerByIdAsync(string rowKey)
        {
            try
            {
                var response = await _customersTable.GetEntityAsync<Customer>("Customers", rowKey);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task AddProductAsync(Product product)
        {
            product.RowKey = Guid.NewGuid().ToString();
            product.ProductID = product.RowKey;
            await _productsTable.UpsertEntityAsync(product);
        }

        public async Task UpdateProductAsync(Product product)
        {
            await _productsTable.UpsertEntityAsync(product);
        }

        public async Task DeleteProductAsync(string rowKey)
        {
            await _productsTable.DeleteEntityAsync("Products", rowKey);
        }

        public async Task<Product> GetProductByIdAsync(string rowKey)
        {
            try
            {
                var response = await _productsTable.GetEntityAsync<Product>("Products", rowKey);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            var customers = new List<Customer>();
            await foreach (var entity in _customersTable.QueryAsync<Customer>())
            {
                customers.Add(entity);
            }
            return customers;
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var entity in _productsTable.QueryAsync<Product>())
            {
                products.Add(entity);
            }
            return products;
        }

        public async Task UploadImageAsync(string fileName, Stream content)
        {
            var blobClient = _blobContainer.GetBlobClient(fileName);
            await blobClient.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
            });
        }

        public async Task<List<string>> GetBlobNamesAsync()
        {
            var blobNames = new List<string>();
            await foreach (var blobItem in _blobContainer.GetBlobsAsync())
            {
                blobNames.Add(blobItem.Name);
            }
            return blobNames;
        }

        public async Task AddQueueMessageAsync(string message)
        {
            await _queueClient.SendMessageAsync(message);
        }

        public async Task<List<PeekedMessage>> PeekQueueMessagesAsync(int maxMessages = 5)
        {
            var response = await _queueClient.PeekMessagesAsync(maxMessages);
            return response.Value.ToList();
        }

        public async Task<List<string>> GetFileNamesAsync()
        {
            try
            {
                await _fileShareClient.CreateIfNotExistsAsync();
                var directoryClient = _fileShareClient.GetRootDirectoryClient();
                var fileNames = new List<string>();

                await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        fileNames.Add(item.Name);
                    }
                }
                return fileNames;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving file names: {ex.Message}");
            }
        }

        // FIX: Replaced the old (string, string) overload with the new (Stream, string, string) overload
        // This method now correctly takes a Stream from the IFormFile and uploads it to Azure File Share.
        public async Task UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                await _fileShareClient.CreateIfNotExistsAsync();
                var directoryClient = _fileShareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                // 1. Create the file on the share, specifying its total length
                // fileStream.Length is available because IFormFile.OpenReadStream() returns a seekable stream.
                await fileClient.CreateAsync(fileStream.Length);

                // 2. Ensure the stream is at the beginning before reading
                fileStream.Position = 0;

                // 3. Upload the entire stream to the Azure File Share
                // ContentType is accepted but ignored as File Share doesn't use it the same way as Blob Storage
                await fileClient.UploadAsync(fileStream);
            }
            catch (Exception ex)
            {
                // Rethrow a specific exception type to aid debugging
                throw new Exception($"Error uploading file: {ex.Message}");
            }
        }
    }
}