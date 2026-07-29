namespace MediCitas.Api.Domain.Entities;

/// <summary>Estado actual del flujo conversacional de un número de WhatsApp.</summary>
public class ConversacionChatbot
{
    public int Id { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public int? PacienteId { get; set; }
    public string EstadoFlujo { get; set; } = "MENU_PRINCIPAL";
    /// <summary>Datos temporales del flujo serializados en JSON.</summary>
    public string? Contexto { get; set; }
    public DateTime UltimaInteraccion { get; set; } = DateTime.Now;
    public bool Activa { get; set; } = true;

    public Paciente? Paciente { get; set; }
    public ICollection<MensajeChatbot> Mensajes { get; set; } = new List<MensajeChatbot>();
}
