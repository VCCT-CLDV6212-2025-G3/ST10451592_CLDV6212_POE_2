// File: Controllers/ProductsController.cs
using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Linq; // Needed for LINQ extensions like .Skip(), .Take(), and .Where()
using System.Collections.Generic; // Needed for IEnumerable<T>

// Namespace for controllers in the ABCRetailWebApp application
namespace ABCRetailWebApp.Controllers
{
    // ProductsController handles product-related operations and page, including CRUD
    public class ProductsController : Controller
    {
        // Private field for the AzureStorageService dependency, injected via constructor
        private readonly AzureStorageService _storageService;

        // Constructor for dependency injection of AzureStorageService
        public ProductsController(AzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: /Products/Index - Displays the products page with list
        // FIX: Added 'searchQuery' parameter to enable searching
        public async Task<IActionResult> Index(string searchQuery, int page = 1)
        {
            try
            {
                const int pageSize = 5; // Page size for pagination

                // 1. Fetch full list of products
                var fullProducts = await _storageService.GetProductsAsync();

                // 2. Start with the full list and apply filtering
                IEnumerable<Product> filteredProducts = fullProducts.AsEnumerable();

                // Store the search query in ViewBag to persist it in the search box
                ViewBag.CurrentSearchQuery = searchQuery;

                // 3. Apply Filtering logic if a search term is present
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    string searchLower = searchQuery.Trim().ToLower();

                    filteredProducts = filteredProducts.Where(p =>
                        // Filter by Name, Description, or Category (case-insensitive)
                        p.Name.ToLower().Contains(searchLower) ||
                        p.Description.ToLower().Contains(searchLower) ||
                        p.Category.ToLower().Contains(searchLower)
                    );
                }

                // 4. Apply Pagination to the filtered list
                ViewBag.TotalProducts = filteredProducts.Count();

                var products = filteredProducts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 5. Set ViewBag properties for the view
                ViewBag.Products = products;
                ViewBag.CurrentPage = page;
                // ViewBag.TotalProducts is already set above
            }
            catch (Exception ex)
            {
                // Handle exceptions by storing an error message in TempData for display
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }
            return View();
        }

        // GET: /Products/Create - Displays the form to create a new product
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Products/Create - Adds a new product to Table Storage
        [HttpPost]
        public async Task<IActionResult> Create(string name, string description, double price, string category)
        {
            if (price < 0 || price > 10000 || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(category))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return View();
            }

            try
            {
                var product = new Product
                {
                    Name = name,
                    Description = description,
                    Price = price,
                    Category = category,
                    // FIX: Initialize the new ImageUrl property
                    ImageUrl = string.Empty
                };
                await _storageService.AddProductAsync(product);
                TempData["SuccessMessage"] = "Product added successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to add product: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // GET: /Products/Details - Displays details of a specific product
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var product = await _storageService.GetProductByIdAsync(id); // Get product by RowKey
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index");
                }
                // The view will now display ImageUrl and the image based on the model
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: /Products/Edit - Displays the form to edit a product
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var product = await _storageService.GetProductByIdAsync(id); // Get product by RowKey
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index");
                }
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: /Products/Edit - Updates an existing product in Table Storage
        [HttpPost]
        // FIX: Added 'imageUrl' parameter to capture the new field from the form
        public async Task<IActionResult> Edit(string id, string name, string description, double price, string category, string imageUrl)
        {
            if (price < 0 || price > 10000 || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(category))
            {
                // Note: ImageUrl is not required, so we only validate other fields
                TempData["ErrorMessage"] = "Name, Description, Price, and Category are required.";
                return View();
            }

            try
            {
                var product = await _storageService.GetProductByIdAsync(id); // Get product by RowKey
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                // Update all properties
                product.Name = name;
                product.Description = description;
                product.Price = price;
                product.Category = category;
                // FIX: Update the new ImageUrl property
                product.ImageUrl = imageUrl;

                await _storageService.UpdateProductAsync(product); // Update the product
                TempData["SuccessMessage"] = "Product updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to update product: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // GET: /Products/Delete - Displays the confirmation page to delete a product
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                // This GET action retrieves the product model to display the confirmation page (Delete.cshtml)
                var product = await _storageService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index");
                }
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: /Products/DeleteConfirmed - Deletes the product from Table Storage
        [HttpPost, ActionName("Delete")] // Use ActionName to map the POST to /Products/Delete
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                // Assuming DeleteProductAsync(id) handles the default PartitionKey internally.
                await _storageService.DeleteProductAsync(id);
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to delete product: {ex.Message}";
            }
            return RedirectToAction("Index");
        }
    }
}