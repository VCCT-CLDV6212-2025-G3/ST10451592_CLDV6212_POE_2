// File: Models/Customer.cs
using Azure.Data.Tables;

// Namespace for models in the ABCRetailWebApp application
namespace ABCRetailWebApp.Models
{
    // Customer class represents an entity for Table Storage, implementing ITableEntity
    public class Customer : ITableEntity
    {
        // PartitionKey for scalability, defaulted to "Customers"
        public string PartitionKey { get; set; } = "Customers";

        // RowKey for unique identification, defaulted to empty string
        public string RowKey { get; set; } = string.Empty;

        // FirstName property for customer's first name
        public string FirstName { get; set; } = string.Empty;

        // LastName property for customer's last name
        public string LastName { get; set; } = string.Empty;

        // Email property for customer email
        public string Email { get; set; } = string.Empty;

        // PhoneNumber property for customer phone number
        public string PhoneNumber { get; set; } = string.Empty;

        // Address property for customer location/address
        public string Address { get; set; } = string.Empty;

        // CreatedDate for the date the customer was created (generated automatically)
        public DateTimeOffset? CreatedDate { get; set; }

        // Timestamp for the entity
        public DateTimeOffset? Timestamp { get; set; }

        // ETag for concurrency control
        public Azure.ETag ETag { get; set; }
    }
}