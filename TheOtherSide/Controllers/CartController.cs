using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using TheOtherSide.Models;
using Microsoft.AspNetCore.Http;

namespace TheOtherSide.Controllers
{
    public class StoreController : Controller
    {
        // aqui agregamos nuestros productos de todas las categorias
        private static readonly List<CartItem> AvailableItems = new()
        {
            new CartItem { Id = 2001, Name = "New Balance 1000",      Price = 3599.00m },
            new CartItem { Id = 2002, Name = "Cartera Guess",          Price =  599.00m },
            new CartItem { Id = 2003, Name = "Gorra New Era NY",       Price =  999.00m },
            new CartItem { Id = 2004, Name = "Gorra Carhartt de Lona", Price =  750.00m },
            new CartItem { Id = 2005, Name = "Reloj Lacoste",          Price = 2749.00m },
            new CartItem { Id = 2006, Name = "Perfume Acqua Di Gio",   Price = 1999.00m },
        };

        private string CartFilePath => Path.Combine(Directory.GetCurrentDirectory(), "AppData", "cart.json");
        private string SaleSummaryFilePath => Path.Combine(Directory.GetCurrentDirectory(), "AppData", "sale.json"); // <-- resumen


        private string GetCurrentUsername()
        {
            return HttpContext.Session.GetString("Username")
                   ?? User.Identity?.Name
                   ?? "guest";
        }

        // ----- CART (items) -----
        private List<CartItem> LoadCart()
        {
            if (!System.IO.File.Exists(CartFilePath)) return new();
            var json = System.IO.File.ReadAllText(CartFilePath);
            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new();
        }
        private void SaveCart(List<CartItem> cart)
        {
            var dir = Path.GetDirectoryName(CartFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(cart, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(CartFilePath, json);
        }

        // ----- SALE SUMMARY (Username, Total, Confirmed) -----
        private void SaveSaleSummary(SaleSummary summary)
        {
            summary.Username = GetCurrentUsername(); // asegura el usuario actual
            var dir = Path.GetDirectoryName(SaleSummaryFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(SaleSummaryFilePath, json);
        }

        // VM para vistas (tu Sale con items del carrito)
        private Sale BuildCurrentSaleVM()
            => new Sale { Username = GetCurrentUsername(), Cart = LoadCart(), Confirmed = false };



        [HttpPost]
        public IActionResult AddToCart(int id)
        {
            var cart = LoadCart();
            var item = AvailableItems.Find(g => g.Id == id);
            if (item != null) cart.Add(item); // permite varias unidades
            SaveCart(cart);
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = LoadCart();
            cart.RemoveAll(g => g.Id == id); // quita todas las unidades del product id
            SaveCart(cart);
            return Redirect(Request.Headers["Referer"].ToString());
        }


        public IActionResult Confirmation()
        {
            var saleVM = BuildCurrentSaleVM(); // username + items actuales del carrito
            return View(saleVM);
        }

        [HttpPost]
        public IActionResult ConfirmSale()
        {
            var cart = LoadCart();
            var total = cart.Sum(x => x.Price);

            // Guarda SOLO el resumen de venta en sale.json
            var summary = new SaleSummary
            {
                Username = GetCurrentUsername(),
                Total = total,
                Confirmed = true
            };
            SaveSaleSummary(summary);

            // Limpia el carrito
            SaveCart(new List<CartItem>());

            TempData["SuccessMessage"] = $"¡Compra confirmada con éxito, {summary.Username}! Total: ${summary.Total}";
            return RedirectToAction("Confirmation");
        }


        // ===== Offcanvas para poder ver el carrito =====
        [HttpGet]
        public IActionResult CartPanel()
        {
            var saleVM = BuildCurrentSaleVM();
            return PartialView("~/Views/Shared/_CartBody.cshtml", saleVM);
        }

        
    }
}
