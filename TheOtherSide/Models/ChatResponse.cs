using System.Collections.Generic;

namespace TheOtherSide.Models
{
    public class ChatResponse
    {
        public string Message { get; set; } = "";
        public List<string> Suggestions { get; set; } = new();
        // Fuente de conocimiento, útil para depurar (ej. "kb/faq")
        public string? Source { get; set; }
    }
}
