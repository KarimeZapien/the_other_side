using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TheOtherSide.Models
{
    public class ChatTurn
    {
        // "user" o "assistant"
        [Required] public string Role { get; set; } = "";
        [Required] public string Content { get; set; } = "";
    }

    public class ChatMessage
    {
        [Required, MinLength(1), MaxLength(500)]
        public string Text { get; set; } = "";

        // Historial corto (opcional)
        public List<ChatTurn>? History { get; set; }

        // "es-MX" por defecto
        public string Locale { get; set; } = "es-MX";
    }
}
