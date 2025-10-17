using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

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
            Path.Combine(Directory.GetCurrentDirectory(), "AppData", "users.json"); // Ruta al archivo JSON donde tengo los usuarios

        private List<User> LoadUsers() //carga los usuarios desde el archivo json
        {
            if (!System.IO.File.Exists(UsersPath)) //verifica si el archivo json existe
                return new List<User>();
            var json = System.IO.File.ReadAllText(UsersPath);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>(); //si no hay usuarios, me da una lista vacía
        }

        private void SaveUsers(List<User> users) //guarda los usuarios en el archivo json
        {
            var dir = Path.GetDirectoryName(UsersPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); 
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }); 
            System.IO.File.WriteAllText(UsersPath, json);
        }

        // ===== Login =====
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var users = LoadUsers();
            var user = users.Find(u => u.Username == username && u.Password == password);
            if (user != null)
            {
                // guardar la sesión del usuario
                HttpContext.Session.SetString("Username", username);
                return RedirectToAction("Index", "Home");
            }

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
            return View(); 
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("Username"); 
            return RedirectToAction("Index", "Home");
        }
    }
}
