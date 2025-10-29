using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheOtherSide.Services; //NO MOVER NO QUITAR
using QuestPDF.Fluent; //NO MOVER NO QUITAR
using QuestPDF.Infrastructure; //NO MOVER NO QUITAR
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// PDF----- MO MOVER
QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddSingleton<OrdersPdfService>();
// PDF----- MO MOVER


// ---------- SESIÓN ----------
builder.Services.AddDistributedMemoryCache(); // almacén en memoria para sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // ajusta si quieres
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // necesario si usas consentimiento de cookies
});

// HttpContextAccessor (lo usas en _Layout.cshtml para leer Session)
builder.Services.AddHttpContextAccessor();

// Chatbot (Ruta B) + HttpClient
builder.Services.AddHttpClient<IChatbotService, ChatbotService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// ---------- PIPELINE ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// (si usas autenticación/autoriza)
app.UseAuthentication();

// **IMPORTANTE: usar sesión ANTES de los endpoints**
app.UseSession();

app.UseAuthorization();

// Endpoints MVC + API
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
