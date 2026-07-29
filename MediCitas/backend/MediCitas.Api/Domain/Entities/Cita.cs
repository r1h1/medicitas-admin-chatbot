using MediCitas.Api.Domain.Enums;

namespace MediCitas.Api.Domain.Entities;

public class Cita
{
    public int Id { get; set; }
    /// <summary>Código corto que el paciente usa en WhatsApp, por ejemplo MC-000123.</summary>
    public string Codigo { get; set; } = string.Empty;
    public int PacienteId { get; set; }
    public int MedicoId { get; set; }
    public int EspecialidadId { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public int EstadoId { get; set; } = (int)EstadoCitaEnum.Pendiente;
    public TipoConsulta TipoConsulta { get; set; } = TipoConsulta.Nueva;
    public string? Motivo { get; set; }
    public string? Notas { get; set; }
    public CanalOrigen CanalOrigen { get; set; } = CanalOrigen.Panel;
    /// <summary>Cita de la que proviene cuando se trata de una reprogramación.</summary>
    public int? CitaOriginalId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public string? MotivoCancelacion { get; set; }
    public int? CreadoPorUsuarioId { get; set; }

    public Paciente? Paciente { get; set; }
    public Medico? Medico { get; set; }
    public Especialidad? Especialidad { get; set; }
    public EstadoCita? Estado { get; set; }
    public Cita? CitaOriginal { get; set; }

    public bool EstaVigente =>
        EstadoId == (int)EstadoCitaEnum.Pendiente ||
        EstadoId == (int)EstadoCitaEnum.Confirmada ||
        EstadoId == (int)EstadoCitaEnum.EnAtencion;
}
