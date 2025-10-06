// File: Controllers/CustomersController.cs
using ABCRetailWebApp.Models;
using ABCRetailWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Threading.Tasks;

// Namespace for controllers in the ABCRetailWebApp application
namespace ABCRetailWebApp.Controllers
{
    // CustomersController handles customer-related operations and page, including CRUD
    public class CustomersController : Controller
    {
        // Private field for the AzureStorageService dependency, injected via constructor
        private readonly AzureStorageService _storageService;

        // Constructor for dependency injection of AzureStorageService
        public CustomersController(AzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: /Customers/Index - Displays the customers page with list
        public async Task<IActionResult> Index(int page = 1)
        {
            try
            {
                const int pageSize = 5; // Page size for pagination (shows 5 records per page to align with requirements)

                // Fetch full list of customers for count and pagination
                var fullCustomers = await _storageService.GetCustomersAsync();

                // Apply pagination to the list
                var customers = fullCustomers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Set ViewBag properties for the view to display data and handle pagination
                ViewBag.Customers = customers;
                ViewBag.CurrentPage = page;
                ViewBag.TotalCustomers = fullCustomers.Count;
            }
            catch (Exception ex)
            {
                // Handle exceptions by storing an error message in TempData for display
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }
            return View();
        }

        // GET: /Customers/Create - Displays the form to create a new customer
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Customers/Create - Adds a new customer to Table Storage
        [HttpPost]
        public async Task<IActionResult> Create(string firstName, string lastName, string email, string phoneNumber, string address)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || !IsValidEmail(email) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(address))
            {
                TempData["ErrorMessage"] = "All fields are required and email must be valid.";
                return View();
            }

            try
            {
                var customer = new Customer
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Address = address,
                    CreatedDate = DateTimeOffset.UtcNow // Automatically generate creation date
                };
                await _storageService.AddCustomerAsync(customer);
                TempData["SuccessMessage"] = "Customer added successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to add customer: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // GET: /Customers/Details - Displays details of a specific customer
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var customer = await _storageService.GetCustomerByIdAsync(id); // Get customer by RowKey
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction("Index");
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: /Customers/Edit - Displays the form to edit a customer
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var customer = await _storageService.GetCustomerByIdAsync(id); // Get customer by RowKey
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction("Index");
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: /Customers/Edit - Updates an existing customer in Table Storage
        [HttpPost]
        public async Task<IActionResult> Edit(string id, string firstName, string lastName, string email, string phoneNumber, string address)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || !IsValidEmail(email) || string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(address))
            {
                TempData["ErrorMessage"] = "All fields are required and email must be valid.";
                return View();
            }

            try
            {
                var customer = await _storageService.GetCustomerByIdAsync(id); // Get customer by RowKey
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction("Index");
                }

                customer.FirstName = firstName;
                customer.LastName = lastName;
                customer.Email = email;
                customer.PhoneNumber = phoneNumber;
                customer.Address = address;
                await _storageService.UpdateCustomerAsync(customer); // Update the customer
                TempData["SuccessMessage"] = "Customer updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to update customer: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // GET: /Customers/Delete - Displays the confirmation page to delete a customer
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var customer = await _storageService.GetCustomerByIdAsync(id); // Get customer by RowKey
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found.";
                    return RedirectToAction("Index");
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: /Customers/DeleteConfirmed - Deletes the customer from Table Storage
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _storageService.DeleteCustomerAsync(id); // Delete the customer
                TempData["SuccessMessage"] = "Customer deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to delete customer: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        // Helper method to validate email format
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}