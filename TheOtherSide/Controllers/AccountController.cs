using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
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

        private string UsersPath =>
            Path.Combine(Directory.GetCurrentDirectory(), "AppData", "users.json");

        private List<User> LoadUsers()
        {
            if (!System.IO.File.Exists(UsersPath))
                return new List<User>();
            var json = System.IO.File.ReadAllText(UsersPath);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }

        private void SaveUsers(List<User> users)
        {
            var dir = Path.GetDirectoryName(UsersPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(UsersPath, json);
        }

        // ===== Login (del slide) =====
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var users = LoadUsers();
            var user = users.Find(u => u.Username == username && u.Password == password);
            if (user != null)
                return RedirectToAction("Index", "Home");

            ViewBag.ErrorMessage = "Usuario o contraseña inválidos.";
            return View();
        }

        // ===== Register =====
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string username, string password, string confirm)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) //por si los dejan en blanco
            {
                ViewBag.ErrorMessage = "El usuario y/o contraseña son obligatorios.";
                return View();
            }
            if (password != confirm) //por si no coinciden
            {
                ViewBag.ErrorMessage = "Las contraseñas no coinciden.";
                return View();
            }

            var users = LoadUsers();
            if (users.Exists(u => u.Username == username)) //si ya existe el usuario
            {
                ViewBag.ErrorMessage = "Este usuario ya está registrado.";
                return View();
            }

            users.Add(new User { Username = username, Password = password });
            SaveUsers(users);

            ViewBag.SuccessMessage = "Se creó el usuario. Puede iniciar sesión.";
            return View(); // o RedirectToAction("Login") si prefieres
        }
    }
}
