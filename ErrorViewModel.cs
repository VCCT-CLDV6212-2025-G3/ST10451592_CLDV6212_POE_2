// File: Models/ErrorViewModel.cs
using System;

// Namespace for models in the ABCRetailWebApp application
namespace ABCRetailWebApp.Models
{
    // ErrorViewModel for displaying error details
    public class ErrorViewModel
    {
        // Request ID for the error
        public string RequestId { get; set; }

        // Property to check if RequestId should be shown
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}