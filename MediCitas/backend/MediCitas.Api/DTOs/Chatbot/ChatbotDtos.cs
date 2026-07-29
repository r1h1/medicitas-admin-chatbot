using System.ComponentModel.DataAnnotations;

namespace MediCitas.Api.DTOs.Chatbot;

/// <summary>Mensaje entrante normalizado, independiente del proveedor de WhatsApp.</summary>
public class MensajeEntranteDto
{
    [Required(ErrorMessage = "El teléfono es obligatorio")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El texto del mensaje es obligatorio")]
    [StringLength(2000)]
    public string Texto { get; set; } = string.Empty;
}

public class RespuestaChatbotDto
{
    public string Telefono { get; set; } = string.Empty;
    public string Respuesta { get; set; } = string.Empty;
    public string EstadoFlujo { get; set; } = string.Empty;
    public List<string> Opciones { get; set; } = new();
}

public class ConversacionDto
{
    public int Id { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public int? PacienteId { get; set; }
    public string Paciente { get; set; } = "Sin registrar";
    public string EstadoFlujo { get; set; } = string.Empty;
    public DateTime UltimaInteraccion { get; set; }
    public int TotalMensajes { get; set; }
}

public class MensajeDto
{
    public long Id { get; set; }
    public byte Direccion { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
}
