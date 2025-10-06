// File: Controllers/FileManagementController.cs
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Http; // Required for IFormFile
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq; 
using System.Threading.Tasks;

namespace ABCRetailWebApp.Controllers
{
    public class FileManagementController : Controller
    {
        private readonly AzureStorageService _storageService;

        public FileManagementController(AzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: /FileManagement/Index - Displays the file management page
        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                const int pageSize = 10;

                // Fetch full list of file names
                var fullFileNames = await _storageService.GetFileNamesAsync();

                // Apply pagination
                var fileNames = fullFileNames.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                ViewBag.FileNames = fileNames;
                ViewBag.CurrentPage = page;
                ViewBag.TotalFiles = fullFileNames.Count;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }
            return View();
        }

        // POST: /FileManagement/UploadFile - Uploads a file to Azure Blob Storage
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file) // Correctly accepts IFormFile
        {
            // Basic validation
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("Index");
            }

            try
            {
                // Call the service method with file stream, file name, and content type
                await _storageService.UploadFileAsync(file.OpenReadStream(), file.FileName, file.ContentType);

                TempData["SuccessMessage"] = $"File '{file.FileName}' uploaded successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to upload file: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}