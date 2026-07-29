using MediCitas.Api.Domain.Enums;

namespace MediCitas.Api.Domain.Entities;

public class Notificacion
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public int? CitaId { get; set; }
    public TipoNotificacion Tipo { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public byte Canal { get; set; } = 1;
    public EstadoNotificacion Estado { get; set; } = EstadoNotificacion.Pendiente;
    public DateTime FechaProgramada { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public int Intentos { get; set; }
    public string? RespuestaPaciente { get; set; }

    public Paciente? Paciente { get; set; }
    public Cita? Cita { get; set; }
}
