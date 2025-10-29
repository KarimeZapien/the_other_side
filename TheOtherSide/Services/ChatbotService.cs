using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TheOtherSide.Models;

namespace TheOtherSide.Services
{
    public interface IChatbotService
    {
        Task<ChatResponse> AskAsync(ChatMessage input);
    }

    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;
        private readonly IHostEnvironment _env;

        private class KbItem
        {
            public string id { get; set; } = "";
            public string? category { get; set; }
            public string q { get; set; } = "";
            public string a { get; set; } = "";
            public List<string>? keywords { get; set; }
            public List<string>? followups { get; set; }
            public string? source { get; set; }
        }

        public ChatbotService(HttpClient http, IConfiguration cfg, IHostEnvironment env)
        {
            _http = http;
            _cfg = cfg;
            _env = env;

            var key = _cfg["Chatbot:ApiKey"];
            if (!string.IsNullOrWhiteSpace(key) && key.StartsWith("ENV:", StringComparison.OrdinalIgnoreCase))
            {
                var varName = key.Substring(4);
                var envVal = Environment.GetEnvironmentVariable(varName);
                if (!string.IsNullOrWhiteSpace(envVal)) _apiKey = envVal;
            }
            else
            {
                _apiKey = key ?? "";
            }
        }

        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private readonly string[] _domainHints = new[]
        {
            "envio","envíos","producto","productos","cambio","devolucion","devolución",
            "pago","pagos","pedido","pedidos","rastreo","tracking","mis pedidos","guia","guía"
        };

        private string _apiKey = "";

        public async Task<ChatResponse> AskAsync(ChatMessage input)
        {
            var txt = (input.Text ?? "").Trim();

            // 1) Saludos
            if (IsGreeting(txt))
            {
                return new ChatResponse
                {
                    Message = "¡Hey que tal! 😊 ¿Necesitas una guía en tu proxima compra?",
                    Suggestions = new() { "¿Cuánto tarda el envío?", "Métodos de pago", "Cómo hacer una devolución" },
                    Source = "rules/greeting"
                };
            }

            // 2) Fuera de dominio
            if (!IsDomainRelated(txt))
            {
                return new ChatResponse
                {
                    Message = "Puedo ayudarte con temas de la tienda: productos,  envios, cambios/devoluciones, pagos y mas. ¿Sobre cuál quieres saber?",
                    Suggestions = new() { "Productos de la tienda", "¿Cuánto tarda el envío?", "Devoluciones del pedido", "Métodos de pago"},
                    Source = "rules/oob"
                };
            }

            // 3) KB y contexto relevante
            var kbItems = await LoadKbAsync();
            var contextSnippets = PickRelevant(kbItems, txt, topN: 4);

            // 4) Prompt para IA
            var systemPrompt = await LoadSystemPromptAsync();
            var provider = _cfg["Chatbot:Provider"] ?? "OpenAI";
            var model = _cfg["Chatbot:Model"] ?? "gpt-3.5-turbo";
            var temperature = _cfg.GetValue("Chatbot:Temperature", 0.2);
            var maxTokens = _cfg.GetValue("Chatbot:MaxOutputTokens", 300);
            var baseUrl = _cfg["Chatbot:BaseUrl"] ?? "https://api.openai.com/v1";

            if (string.IsNullOrWhiteSpace(_apiKey))
                return DevOrProdError("Falta Chatbot:ApiKey (o variable de entorno).", "config/missing_api_key");

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "system", content = $"Contexto de conocimiento (KB):\n{BuildKbContext(contextSnippets)}" }
            };

            if (input.History != null)
            {
                foreach (var turn in input.History.TakeLast(6))
                    messages.Add(new { role = turn.Role, content = turn.Content });
            }
            messages.Add(new { role = "user", content = txt });

            // 5) Llamada al proveedor
            var payload = new { model, temperature, max_tokens = maxTokens, messages };
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");

            try
            {
                using var res = await _http.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();

                if (!res.IsSuccessStatusCode)
                {
                    // Fallback a KB + diagnóstico en Development
                    var fallback = KbFallback(contextSnippets, txt);
                    if (_env.IsDevelopment())
                    {
                        fallback.Message += $"\n\n[DEV] Provider={provider} Status={(int)res.StatusCode} {res.StatusCode}\n{Truncate(body, 220)}";
                        fallback.Source = $"provider/{provider}/error/dev";
                    }
                    return fallback;
                }

                using var doc = JsonDocument.Parse(body);
                var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

                var sug = contextSnippets.SelectMany(k => k.followups ?? new()).Distinct().Take(4).ToList();
                return new ChatResponse
                {
                    Message = content.Trim(),
                    Suggestions = sug.Count > 0 ? sug : new() { "¿Cuánto tarda el envío?", "Métodos de pago" },
                    Source = "kb/faq+llm"
                };
            }
            catch (Exception ex)
            {
                // Fallback si hay problemas de red / timeout
                var fallback = KbFallback(contextSnippets, txt);
                if (_env.IsDevelopment())
                {
                    fallback.Message += $"\n\n[DEV] Exception: {ex.GetType().Name} - {Truncate(ex.Message, 180)}";
                    fallback.Source = "provider/exception/dev";
                }
                return fallback;
            }
        }

        private ChatResponse DevOrProdError(string devMsg, string source)
        {
            var baseMsg = "Ahora mismo no puedo responder. Elige una de estas opciones o intenta más tarde.";
            var r = new ChatResponse
            {
                Message = baseMsg,
                Suggestions = new() { "¿Cuánto tarda el envío?", "Métodos de pago", "Cómo hago una devolución" },
                Source = source
            };
            if (_env.IsDevelopment()) r.Message += $"\n\n[DEV] {devMsg}";
            return r;
        }

        private static ChatResponse KbFallback(List<KbItem> snippets, string query)
        {
            // Usa la KB local para no dejar al usuario sin respuesta
            var best = snippets.FirstOrDefault();
            if (best != null)
            {
                var s = snippets.SelectMany(k => k.followups ?? new()).Distinct().Take(4).ToList();
                return new ChatResponse
                {
                    Message = best.a,
                    Suggestions = s.Count > 0 ? s : new() { "¿Cuánto tarda el envío?", "Guía de tallas mujer", "Métodos de pago" },
                    Source = "kb/fallback"
                };
            }
            return new ChatResponse
            {
                Message = "No pude obtener respuesta de la IA. ¿Puedes intentar de nuevo o elegir una opción?",
                Suggestions = new() { "¿Cuánto tarda el envío?", "Guía de tallas mujer", "Métodos de pago" },
                Source = "fallback/no_kb"
            };
        }

        private static string Truncate(string s, int n) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= n ? s : s.Substring(0, n) + "…");

        private bool IsGreeting(string text)
        {
            var t = text.ToLowerInvariant();
            return t is "hola" or "buenas" or "buenas tardes" or "buenos dias" or "buenos días"
                or "qué tal" or "que tal" or "hey" or "holi" or "ola"
                or "como estas" or "cómo estás" or "como estas?" or "cómo estás?";
        }

        private bool IsDomainRelated(string text)
        {
            var t = RemoveDiacritics(text.ToLowerInvariant());
            return _domainHints.Any(h => t.Contains(RemoveDiacritics(h)));
        }

        private async Task<string> LoadSystemPromptAsync()
        {
            var path = _cfg["Chatbot:SystemPromptPath"] ?? "AppData/prompts/system.txt";
            var full = Path.Combine(_env.ContentRootPath, path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) return await File.ReadAllTextAsync(full, Encoding.UTF8);

            return """
            Chubby”, asistente virtual de TheOtherSide (e-commerce de productos americanos). 
            Hablas en español (es-MX) con tono cordial y claro. Tu objetivo: resolver dudas y guiar al cliente para completar su compra con seguridad.
            Si te saludan, responde amable y ofrece opciones. Si te piden algo fuera de dominio, dilo y sugiere un tema válido.
            Nunca pidas ni aceptes datos sensibles (tarjetas, documentos) por chat. Si no sabes, sé honesto y sugiere pasos.
            """;
        }

        private async Task<List<KbItem>> LoadKbAsync()
        {
            var webRoot = (_env as Microsoft.AspNetCore.Hosting.IWebHostEnvironment)?.WebRootPath
                          ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var kbPath = Path.Combine(webRoot, "data", "faq.json");
            if (!File.Exists(kbPath)) return new();

            var txt = await File.ReadAllTextAsync(kbPath, Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<KbItem>>(txt, _json) ?? new();
            return items;
        }

        private static string RemoveDiacritics(string text) =>
            text.Normalize(System.Text.NormalizationForm.FormD)
                .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c)).ToString()
                .Normalize(System.Text.NormalizationForm.FormC);

        private static double TokenMatchScore(string q, string text)
        {
            var sep = new char[] { ' ', ',', '.', ';', ':', '-', '_', '/', '(', ')', '[', ']' };
            var qSet = new HashSet<string>(RemoveSymbols(q).Split(sep, StringSplitOptions.RemoveEmptyEntries).Select(W));
            var tSet = new HashSet<string>(RemoveSymbols(text).Split(sep, StringSplitOptions.RemoveEmptyEntries).Select(W));
            if (qSet.Count == 0) return 0;
            int hit = qSet.Count(tok => tSet.Contains(tok));
            return (double)hit / qSet.Count;

            static string W(string s) => RemoveDiacritics(s.ToLowerInvariant());
            static string RemoveSymbols(string s) => new string(s.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ').ToArray());
        }

        private static string BuildKbContext(IEnumerable<KbItem> items)
        {
            var sb = new StringBuilder();
            foreach (var it in items)
            {
                sb.AppendLine($"- [{it.id}] {it.q}");
                sb.AppendLine($"  {it.a}");
            }
            return sb.ToString();
        }

        private static List<KbItem> PickRelevant(List<KbItem> kb, string query, int topN)
        {
            var scored = kb
                .Select(i =>
                {
                    var fields = $"{i.q} {(i.keywords == null ? "" : string.Join(' ', i.keywords))}";
                    var score = Math.Max(
                        TokenMatchScore(query, fields),
                        TokenMatchScore(query, i.a ?? string.Empty)
                    );
                    return new { item = i, score };
                })
                .OrderByDescending(t => t.score)
                .Take(topN)
                .Where(t => t.score >= 0.15)
                .Select(t => t.item)
                .ToList();

            return scored;
        }
    }
}

