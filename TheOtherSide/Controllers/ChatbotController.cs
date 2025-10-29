using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TheOtherSide.Models;
using TheOtherSide.Services;

namespace TheOtherSide.Controllers
{
    [ApiController]
    [Route("chatbot")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _svc;

        public ChatbotController(IChatbotService svc)
        {
            _svc = svc;
        }

        [HttpPost("message")]
        public async Task<ActionResult<ChatResponse>> Message([FromBody] ChatMessage input)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ChatResponse
                {
                    Message = "El mensaje es demasiado corto o inválido. Intenta con otra pregunta.",
                    Suggestions = new() { "¿Cuánto tarda el envío?", "Guía de tallas mujer" },
                    Source = "validation"
                });

            try
            {
                var result = await _svc.AskAsync(input);
                return Ok(result);
            }
            catch (InvalidOperationException ioe)
            {
                return StatusCode(500, new ChatResponse
                {
                    Message = "Falta configuración del proveedor de IA. Verifica la API key y el appsettings.",
                    Suggestions = new() { "¿Cuánto tarda el envío?", "Métodos de pago" },
                    Source = $"config/{ioe.GetType().Name}"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ChatResponse
                {
                    Message = "Tuvimos un problema al responder. Inténtalo de nuevo.",
                    Suggestions = new() { "¿Cuánto tarda el envío?", "Guía de tallas mujer" },
                    Source = "exception"
                });
            }
        }
    }
}
