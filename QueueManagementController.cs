// File: Controllers/QueueManagementController.cs
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

// Namespace for controllers in the ABCRetailWebApp application
namespace ABCRetailWebApp.Controllers
{
    // QueueManagementController handles queue management page, including message list and add form
    public class QueueManagementController : Controller
    {
        // Private field for the AzureStorageService dependency, injected via constructor
        private readonly AzureStorageService _storageService;

        // Constructor for dependency injection of AzureStorageService
        public QueueManagementController(AzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: /QueueManagement/Index - Displays the queue management page with message list and add form
        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                const int pageSize = 5; // Page size for pagination (shows 5 records per page to align with requirements)

                // Fetch full list of queue messages for count and pagination (max peek is 32)
                var fullQueueMessages = await _storageService.PeekQueueMessagesAsync(32);

                // Apply pagination to the list
                var queueMessages = fullQueueMessages.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Set ViewBag properties for the view to display data and handle pagination
                ViewBag.QueueMessages = queueMessages;
                ViewBag.CurrentPage = page;
                ViewBag.TotalMessages = fullQueueMessages.Count;
            }
            catch (Exception ex)
            {
                // Handle exceptions by storing an error message in TempData for display
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }
            return View();
        }

        // POST: /QueueManagement/AddQueueMessage - Adds a message to Queue Storage
        [HttpPost]
        public async Task<IActionResult> AddQueueMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Message is required.";
                return RedirectToAction("Index");
            }

            try
            {
                await _storageService.AddQueueMessageAsync(message);
                TempData["SuccessMessage"] = "Message added to queue successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to add message: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
    }
}