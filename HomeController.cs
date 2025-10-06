using ABCRetailWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ABCRetailWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://abcretailfunctions-10451592.azurewebsites.net/api/"); // Replace with your actual Function App URL
        }

        // GET: /Home/Index - Home page
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Home/Products - List products with images
        public IActionResult Products()
        {
            // Simulate product data (replace with real data from a service or DB)
            var products = new[]
            {
                new { Id = "1", Name = "Product 1", ImageUrl = (string?)"https://st10451592.blob.core.windows.net/productimages/1_550e8400-e29b-41d4-a716-446655440000_image.jpg" },
                new { Id = "2", Name = "Product 2", ImageUrl = (string?)null }
            };
            ViewBag.Products = products;
            return View();
        }

        // GET: /Home/ManageProducts - Upload image for a product
        public IActionResult ManageProducts()
        {
            ViewBag.UploadResult = TempData["UploadResult"] as dynamic;
            // Simulate product list (replace with real data)
            ViewBag.Products = new[] { new { Id = "1", Name = "Product 1" }, new { Id = "2", Name = "Product 2" } };
            return View();
        }

        // POST: /Home/UploadProductImage - Upload image for a specific product
        [HttpPost]
        public async Task<IActionResult> UploadProductImage(IFormFile file, string productId)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "No file uploaded.";
                return RedirectToAction("ManageProducts");
            }
            if (string.IsNullOrEmpty(productId))
            {
                ViewBag.Error = "Product ID is required.";
                return RedirectToAction("ManageProducts");
            }

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(file.OpenReadStream());
            content.Add(streamContent, "file", file.FileName);
            _httpClient.DefaultRequestHeaders.Add("ProductId", productId);

            try
            {
                var response = await _httpClient.PostAsync("UploadBlob", content);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<dynamic>();

                TempData["UploadResult"] = result;
            }
            catch (HttpRequestException ex)
            {
                ViewBag.Error = $"Failed to connect to Function App: {ex.Message}";
                return RedirectToAction("ManageProducts");
            }

            return RedirectToAction("ManageProducts");
        }

        // GET: /Home/ProductDetails/{id} - Show all images for a product
        public IActionResult ProductDetails(string id)
        {
            // Simulate multiple images (replace with real data from storage)
            var imageUrls = new[]
            {
                $"https://st10451592.blob.core.windows.net/productimages/{id}_550e8400-e29b-41d4-a716-446655440000_image1.jpg",
                $"https://st10451592.blob.core.windows.net/productimages/{id}_550e8400-e29b-41d4-a716-446655440000_image2.jpg"
            };
            ViewBag.Product = new { Id = id, Name = $"Product {id}", ImageUrls = imageUrls };
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}