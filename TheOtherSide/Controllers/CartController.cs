using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using TheOtherSide.Models;
using TheOtherSide.Services;
using Microsoft.AspNetCore.Http;

namespace TheOtherSide.Controllers
{
    public class StoreController : Controller
    {
        // aqui agregamos nuestros productos de todas las categorias
        private static readonly List<CartItem> AvailableItems = new()
        {
            new CartItem { Id = 2001, Name = "New Balance 1000",                      Price = 3599.00m },
            new CartItem { Id = 2002, Name = "Cartera Guess",                          Price =  599.00m },
            new CartItem { Id = 2003, Name = "Gorra 31 Hats \"NY Flames\"",            Price = 2000.00m },
            new CartItem { Id = 2004, Name = "Playera Gráfica Anuel AA x Reebok",      Price =  999.00m },
            new CartItem { Id = 2005, Name = "Reloj Lacoste",                          Price = 2749.00m },
            new CartItem { Id = 2006, Name = "Perfume Acqua Di Gio",                   Price = 1999.00m },
            new CartItem { Id = 1001, Name = "Rhode Lip Treatment",                    Price =  499.00m },
            new CartItem { Id = 1002, Name = "Pleasing Boat Tote",                     Price = 1299.00m },
            new CartItem { Id = 1003, Name = "28 Colour Denim Jacket Ecru",            Price = 2499.00m },
            new CartItem { Id = 1004, Name = "Perfume Cloud 100 mL",                   Price = 1938.00m },
            new CartItem { Id = 1005, Name = "Kit Rare Beauty x Tajín",                Price =  699.00m },
            new CartItem { Id = 1006, Name = "Starlet Lustrous Liquid Eyeshadow",      Price =  699.00m },
            new CartItem { Id = 3001, Name = "Chips Ahoy Stranger Things",             Price =  210.00m },
            new CartItem { Id = 3002, Name = "Reeses Halloween Edition",               Price =  219.00m },
            new CartItem { Id = 3003, Name = "Cheetos Flaming Hot",                    Price =  179.00m },
            new CartItem { Id = 3004, Name = "Lotus Biscoff",                          Price =  150.00m },
            new CartItem { Id = 3005, Name = "Oreo Double Stuf",                       Price =  205.00m },
            new CartItem { Id = 3006, Name = "Sour Patch Watermelon",                  Price =  199.00m },
            new CartItem { Id = 4001, Name = "Stanley 30 oz Lila",                     Price =  879.00m },
            new CartItem { Id = 4002, Name = "Owala 32 oz Deep Black",                 Price =  749.00m },
            new CartItem { Id = 4003, Name = "Coleman Hielera 60QT",                   Price = 1299.00m },
            new CartItem { Id = 4004, Name = "Shark MultiStyle",                       Price = 6599.00m },
            new CartItem { Id = 4005, Name = "Crayola SuperTips 50 pz",                Price =  219.00m },
            new CartItem { Id = 4006, Name = "Hair, Skin & Nails Gummies",             Price =  239.00m },
        };

        private readonly OrdersPdfService _pdf;
        public StoreController(OrdersPdfService pdf)
        {
            _pdf = pdf;
        }

        private string CartFilePath => Path.Combine(Directory.GetCurrentDirectory(), "AppData", "cart.json");

        private string GetCurrentUsername()
        {
            return HttpContext.Session.GetString("Username")
                   ?? User.Identity?.Name
                   ?? "guest";
        }

        // ----- CARRITO -----
        private List<CartItem> LoadCart()
        {
            if (!System.IO.File.Exists(CartFilePath)) return new();
            var json = System.IO.File.ReadAllText(CartFilePath);
            var cart = JsonSerializer.Deserialize<List<CartItem>>(json) ?? new();
            SyncCartWithCatalog(cart);

            return cart;
        }

        private void SyncCartWithCatalog(List<CartItem> cart)
        {
            foreach (var it in cart)
            {
                var master = AvailableItems.FirstOrDefault(x => x.Id == it.Id);
                if (master != null)
                {
                    it.Name = master.Name;
                    it.Price = master.Price;
                }
            }
        }

        private void SaveCart(List<CartItem> cart)
        {
            var dir = Path.GetDirectoryName(CartFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(cart, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(CartFilePath, json);
        }

        // ----- Historial de ventas -----
        private string SalesLogFilePath => Path.Combine(Directory.GetCurrentDirectory(), "AppData", "sale.json");

        private List<SaleEntry> LoadSalesLog()
        {
            if (!System.IO.File.Exists(SalesLogFilePath)) return new();
            var json = System.IO.File.ReadAllText(SalesLogFilePath);
            return JsonSerializer.Deserialize<List<SaleEntry>>(json) ?? new();
        }

        private void SaveSalesLog(List<SaleEntry> entries)
        {
            var dir = Path.GetDirectoryName(SalesLogFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(SalesLogFilePath, json);
        }

        private Sale BuildCurrentSaleVM()
            => new Sale { Username = GetCurrentUsername(), Cart = LoadCart(), Confirmed = false };

        // TODAS LAS ACCIONES DEL CARRITO
        [HttpPost]
        public IActionResult AddToCart(int id, string? size)
        {
            var cart = LoadCart();
            var item = AvailableItems.Find(g => g.Id == id);
            if (item != null)
            {
                cart.Add(new CartItem
                {
                    Id= item.Id, Name=item.Name, Price= item.Price,
                    Size=string.IsNullOrWhiteSpace(size) ? "Única":size
                });

            } // permite que agreguemos varios productos
            SaveCart(cart);
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int id, string? size)
        {
            var cart = LoadCart();
            var keySize = string.IsNullOrWhiteSpace(size) ? "Única" : size;
            cart.RemoveAll(g => g.Id == id && (g.Size ?? "Única") == keySize);
            SaveCart(cart);
            return Redirect(Request.Headers["Referer"].ToString());
        }

        public IActionResult Confirmation()
        {
            var saleVM = BuildCurrentSaleVM(); // username + items actuales del carrito o sea te muestra la compra sin confirmar
            return View(saleVM);
        }

        [HttpPost]
        public IActionResult ConfirmSale()
        {
            var cart = LoadCart();
            if (cart.Count == 0)
            {
                TempData["SuccessMessage"] = "Tu carrito está vacío.";
                return RedirectToAction("Confirmation");
            }

            var lines = cart
                .GroupBy(x => new { x.Id, x.Name, x.Price , Size = x.Size ?? "Única"})
                .Select(g => new SaleDetailLine
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Price = g.Key.Price,
                    Size=g.Key.Size,
                    Qty = g.Count(),
                    Subtotal = g.Sum(i => i.Price)
                })
                .ToList();

            var total = lines.Sum(x => x.Subtotal);

            var log = LoadSalesLog();

            log.Add(new SaleEntry
            {
                Username = GetCurrentUsername(),
                Total = total,
                Confirmed = true,
                DateUtc = System.DateTime.UtcNow,
                Items = lines
            });

            SaveSalesLog(log);             // guarda/acumula TODAS las ventas
            SaveCart(new List<CartItem>()); // limpia carrito

            TempData["SuccessMessage"] = $"¡Compra confirmada con éxito, {GetCurrentUsername()}! Total: ${total}";
            return RedirectToAction("Confirmation");
        }

        public IActionResult MyOrders()
        {
            var user = GetCurrentUsername();
            var all = LoadSalesLog();
            var mine = all
                .Where(s => s.Confirmed && string.Equals(s.Username, user, System.StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.DateUtc)
                .ToList();
            return View(mine);
        }

        public IActionResult MyOrdersPdf()
        {
            var user = GetCurrentUsername();
            var all = LoadSalesLog();
            var mine = all
                .Where(s => s.Confirmed && string.Equals(s.Username, user, System.StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.DateUtc)
                .ToList();

            var bytes = _pdf.Build(mine, user);
            var fileName = $"MisPedidos_{user}_{System.DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        [HttpPost]
        public IActionResult ProcessPayment(string cardNumber, int mm, int yy, string cvv)
        {
            bool cardOk = !string.IsNullOrWhiteSpace(cardNumber)
                  && cardNumber.All(char.IsDigit)
                  && cardNumber.Length == 16;

            bool mmOk = mm >= 1 && mm <= 12;  
            bool yyOk = yy >= 26 && yy <= 99; 
            bool cvvOk = !string.IsNullOrWhiteSpace(cvv)
                         && cvv.All(char.IsDigit)
                         && cvv.Length == 3;

            if (!(cardOk && mmOk && yyOk && cvvOk))
            {
                TempData["SuccessMessage"] = "Datos de pago inválidos. Tarjeta (16 dígitos), MM (1–12), AA (>=26) y CVV (3).";
                return RedirectToAction("Pago");
            }

            var cart = LoadCart();
            if (cart.Count == 0)
            {
                TempData["SuccessMessage"] = "Tu carrito está vacío.";
                return RedirectToAction("Confirmation");
            }

            var lines = cart
                .GroupBy(x => new { x.Id, x.Name, x.Price , Size =x.Size ?? "Única"})
                .Select(g => new SaleDetailLine
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Price = g.Key.Price,
                    Size = g.Key.Size,
                    Qty = g.Count(),
                    Subtotal = g.Sum(i => i.Price)
                })
                .ToList();

            var total = lines.Sum(x => x.Subtotal);

            var log = LoadSalesLog();
            log.Add(new SaleEntry
            {
                Username = GetCurrentUsername(),
                Total = total,
                Confirmed = true,
                DateUtc = System.DateTime.UtcNow,
                Items = lines
            });
            SaveSalesLog(log);

            SaveCart(new List<CartItem>());

            TempData["SuccessMessage"] = $"¡Pago realizado y pedido confirmado, {GetCurrentUsername()}! Total: ${total}";
            return RedirectToAction("MyOrders");
        }

        // ===== Offcanvas para poder ver el carrito =====
        [HttpGet]
        public IActionResult CartPanel()
        {
            var saleVM = BuildCurrentSaleVM();
            return PartialView("~/Views/Shared/_CartBody.cshtml", saleVM);
        }

        [HttpGet]
        public IActionResult Pago()
        {
            var cart = LoadCart();
            if (cart.Count == 0)
            {
                TempData["SuccessMessage"] = "Tu carrito está vacío.";
                return RedirectToAction("Confirmation");
            }

            var saleVM = BuildCurrentSaleVM();
            return View(saleVM);
        }
    }
}
