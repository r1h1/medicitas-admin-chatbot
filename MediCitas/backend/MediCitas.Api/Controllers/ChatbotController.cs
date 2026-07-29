using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Chatbot;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MediCitas.Api.Controllers;

/// <summary>
/// Punto de entrada del chatbot de WhatsApp y consola de conversaciones del panel.
/// El endpoint /mensaje es el mismo que usa el webhook y el simulador del panel,
/// de modo que la lógica del flujo es una sola.
/// </summary>
[Authorize]
public class ChatbotController : BaseApiController
{
    private readonly IChatbotService _chatbot;
    private readonly ChatbotSettings _opciones;

    public ChatbotController(IChatbotService chatbot, IOptions<ChatbotSettings> opciones)
    {
        _chatbot = chatbot;
        _opciones = opciones.Value;
    }

    /// <summary>Procesa un mensaje entrante y devuelve la respuesta del flujo.</summary>
    /// <response code="200">Respuesta generada</response>
    [HttpPost("mensaje")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RespuestaChatbotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Mensaje([FromBody] MensajeEntranteDto mensaje)
        => Exito(await _chatbot.ProcesarMensajeAsync(mensaje));

    /// <summary>Verificación del webhook exigida por WhatsApp Business API.</summary>
    [HttpGet("webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult VerificarWebhook([FromQuery(Name = "hub.mode")] string? modo,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (modo == "subscribe" && token == _opciones.VerifyToken)
            return Content(challenge ?? string.Empty);

        return StatusCode(StatusCodes.Status403Forbidden);
    }

    /// <summary>Lista las conversaciones registradas.</summary>
    [HttpGet("conversaciones")]
    [Authorize(Roles = Roles.Personal)]
    [ProducesResponseType(typeof(ApiResponse<List<ConversacionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Conversaciones() => Exito(await _chatbot.ListarConversacionesAsync());

    /// <summary>Historial de mensajes de una conversación.</summary>
    [HttpGet("conversaciones/{id:int}/mensajes")]
    [Authorize(Roles = Roles.Personal)]
    [ProducesResponseType(typeof(ApiResponse<List<MensajeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mensajes(int id) => Exito(await _chatbot.ListarMensajesAsync(id));

    /// <summary>Devuelve la conversación al menú principal.</summary>
    [HttpPost("conversaciones/reiniciar")]
    [Authorize(Roles = Roles.Personal)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reiniciar([FromQuery] string telefono)
    {
        await _chatbot.ReiniciarConversacionAsync(telefono);
        return Exito("ok", "Conversación reiniciada");
    }
}
