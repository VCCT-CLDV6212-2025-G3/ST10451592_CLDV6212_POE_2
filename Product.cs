// File: Models/Product.cs
using Azure.Data.Tables;
using Azure; // Ensure Azure namespace is included for ETag

// Namespace for models in the ABCRetailWebApp application
namespace ABCRetailWebApp.Models
{
    // Product class represents an entity for Table Storage, implementing ITableEntity
    public class Product : ITableEntity
    {
        // PartitionKey for scalability, defaulted to "Products"
        public string PartitionKey { get; set; } = "Products";

        // RowKey for unique identification, defaulted to empty string
        public string RowKey { get; set; } = string.Empty;

        // ProductID property for unique product ID
        public string ProductID { get; set; } = string.Empty;

        // Name property for product name
        public string Name { get; set; } = string.Empty;

        // Description property for product description
        public string Description { get; set; } = string.Empty;

        // Price property for product price
        public double Price { get; set; }

        // Category property for product category
        public string Category { get; set; } = string.Empty;

        // FIX: The missing ImageUrl property that caused the RuntimeBinderException
        public string ImageUrl { get; set; } = string.Empty;

        // Timestamp for the entity
        public DateTimeOffset? Timestamp { get; set; }

        // ETag for concurrency control
        public ETag ETag { get; set; }
    }
}