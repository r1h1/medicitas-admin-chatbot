using MediCitas.Api.Domain.Enums;

namespace MediCitas.Api.Domain.Entities;

public class MensajeChatbot
{
    public long Id { get; set; }
    public int ConversacionId { get; set; }
    public DireccionMensaje Direccion { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public string? EstadoFlujo { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;

    public ConversacionChatbot? Conversacion { get; set; }
}
