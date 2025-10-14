using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TheOtherSide.Controllers
{
    public class AccountController : Controller
    {
        private class User
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        private List<User> LoadUsers()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "AppData", "users.json");
            if (!System.IO.File.Exists(filePath))
                return new List<User>();
            var json = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var users = LoadUsers();
            var user = users.Find(u => u.Username == username && u.Password == password);
            if (user != null)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ErrorMessage = "Usuario y/o contrase�a incorrectos";
            return View();
        }
    }
}
