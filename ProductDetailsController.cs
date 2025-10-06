// File: Controllers/ProductDetailsController.cs
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

// Namespace for controllers in the ABCRetailWebApp application
namespace ABCRetailWebApp.Controllers
{
    // ProductDetailsController handles product details page, including image uploads
    public class ProductDetailsController : Controller
    {
        // Private field for the AzureStorageService dependency, injected via constructor
        private readonly AzureStorageService _storageService;

        // Constructor for dependency injection of AzureStorageService
        public ProductDetailsController(AzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: /ProductDetails/Index - Displays the product details page with image list and upload form
        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                const int pageSize = 5; // Page size for pagination (shows 5 records per page to align with requirements)

                // Fetch full list of blob names for count and pagination
                var fullBlobNames = await _storageService.GetBlobNamesAsync();

                // Apply pagination to the list
                var blobNames = fullBlobNames.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Set ViewBag properties for the view to display data and handle pagination
                ViewBag.BlobNames = blobNames;
                ViewBag.CurrentPage = page;
                ViewBag.TotalBlobs = fullBlobNames.Count;
            }
            catch (Exception ex)
            {
                // Handle exceptions by storing an error message in TempData for display
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }
            return View();
        }

        // POST: /ProductDetails/UploadImage - Uploads an image to Blob Storage
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file.";
                return RedirectToAction("Index");
            }
            if (!file.ContentType.StartsWith("image/"))
            {
                TempData["ErrorMessage"] = "Only image files are allowed.";
                return RedirectToAction("Index");
            }

            try
            {
                using var stream = file.OpenReadStream();
                await _storageService.UploadImageAsync(file.FileName, stream);
                TempData["SuccessMessage"] = "Image uploaded successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to upload image: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
    }
}