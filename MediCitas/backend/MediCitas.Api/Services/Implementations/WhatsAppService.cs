using System.Text;
using System.Text.Json;
using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MediCitas.Api.Services.Implementations;

/// <summary>
/// Salida de mensajes hacia WhatsApp.
/// En modo simulación (valor por defecto del prototipo) el mensaje se guarda en
/// la conversación del paciente y se escribe en el log, sin costo ni credenciales.
/// Al activar la API oficial basta con configurar ApiUrl, PhoneNumberId y AccessToken.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly ChatbotSettings _opciones;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IRepositorio<ConversacionChatbot> _conversaciones;
    private readonly IRepositorio<MensajeChatbot> _mensajes;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(IOptions<ChatbotSettings> opciones, IHttpClientFactory httpFactory,
        IRepositorio<ConversacionChatbot> conversaciones, IRepositorio<MensajeChatbot> mensajes,
        ILogger<WhatsAppService> logger)
    {
        _opciones = opciones.Value; _httpFactory = httpFactory;
        _conversaciones = conversaciones; _mensajes = mensajes; _logger = logger;
    }

    public async Task<bool> EnviarAsync(string telefono, string mensaje)
    {
        await RegistrarSalidaAsync(telefono, mensaje);

        if (_opciones.ModoSimulacion || string.IsNullOrWhiteSpace(_opciones.AccessToken))
        {
            _logger.LogInformation("WhatsApp (simulado) → {Telefono}: {Mensaje}", telefono, mensaje);
            return true;
        }

        try
        {
            var cliente = _httpFactory.CreateClient("whatsapp");
            var cuerpo = new
            {
                messaging_product = "whatsapp",
                to = telefono,
                type = "text",
                text = new { body = mensaje }
            };

            var solicitud = new HttpRequestMessage(HttpMethod.Post,
                $"{_opciones.ApiUrl.TrimEnd('/')}/{_opciones.PhoneNumberId}/messages")
            {
                Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json")
            };
            solicitud.Headers.Add("Authorization", $"Bearer {_opciones.AccessToken}");

            var respuesta = await cliente.SendAsync(solicitud);
            if (!respuesta.IsSuccessStatusCode)
                _logger.LogWarning("WhatsApp respondió {Codigo} al enviar a {Telefono}",
                    (int)respuesta.StatusCode, telefono);

            return respuesta.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el mensaje a {Telefono}", telefono);
            return false;
        }
    }

    /// <summary>Deja el mensaje saliente en la bitácora de la conversación del paciente.</summary>
    private async Task RegistrarSalidaAsync(string telefono, string mensaje)
    {
        try
        {
            var conversacion = await _conversaciones.Query().FirstOrDefaultAsync(c => c.Telefono == telefono);

            if (conversacion is null)
            {
                conversacion = new ConversacionChatbot { Telefono = telefono };
                await _conversaciones.AgregarAsync(conversacion);
                await _conversaciones.GuardarCambiosAsync();
            }

            await _mensajes.AgregarAsync(new MensajeChatbot
            {
                ConversacionId = conversacion.Id,
                Direccion = DireccionMensaje.Saliente,
                Contenido = mensaje,
                EstadoFlujo = conversacion.EstadoFlujo
            });
            await _mensajes.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo registrar el mensaje saliente para {Telefono}", telefono);
        }
    }
}
