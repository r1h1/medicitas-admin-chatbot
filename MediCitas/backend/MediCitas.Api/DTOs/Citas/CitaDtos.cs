using System.ComponentModel.DataAnnotations;
using MediCitas.Api.Domain.Enums;

namespace MediCitas.Api.DTOs.Citas;

public class CitaDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int PacienteId { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public string PacienteTelefono { get; set; } = string.Empty;
    public int MedicoId { get; set; }
    public string Medico { get; set; } = string.Empty;
    public int EspecialidadId { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public DateTime FechaHoraInicio { get; set; }
    public DateTime FechaHoraFin { get; set; }
    public int EstadoId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string EstadoColor { get; set; } = string.Empty;
    public TipoConsulta TipoConsulta { get; set; }
    public string? Motivo { get; set; }
    public string? Notas { get; set; }
    public CanalOrigen CanalOrigen { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string? MotivoCancelacion { get; set; }
}

public class CrearCitaRequest
{
    [Required(ErrorMessage = "Seleccione un paciente")] public int PacienteId { get; set; }
    [Required(ErrorMessage = "Seleccione un médico")] public int MedicoId { get; set; }
    [Required(ErrorMessage = "Seleccione fecha y hora")] public DateTime FechaHoraInicio { get; set; }
    public TipoConsulta TipoConsulta { get; set; } = TipoConsulta.Nueva;
    [StringLength(300)] public string? Motivo { get; set; }
    [StringLength(500)] public string? Notas { get; set; }
    public CanalOrigen CanalOrigen { get; set; } = CanalOrigen.Panel;
}

public class ReprogramarCitaRequest
{
    [Required] public DateTime NuevaFechaHoraInicio { get; set; }
    public int? NuevoMedicoId { get; set; }
    [StringLength(300)] public string? Motivo { get; set; }
}

public class CancelarCitaRequest
{
    [StringLength(300)] public string? Motivo { get; set; }
}

public class CambiarEstadoRequest
{
    [Required] public EstadoCitaEnum Estado { get; set; }
    [StringLength(300)] public string? Observacion { get; set; }
}

/// <summary>Bloque de agenda disponible calculado a partir del horario del médico.</summary>
public class SlotDto
{
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public string Etiqueta { get; set; } = string.Empty;
    public bool Disponible { get; set; } = true;
}

public class DisponibilidadDiaDto
{
    public DateOnly Fecha { get; set; }
    public int MedicoId { get; set; }
    public string Medico { get; set; } = string.Empty;
    public bool Bloqueado { get; set; }
    public string? MotivoBloqueo { get; set; }
    public List<SlotDto> Slots { get; set; } = new();
}

public class ReportarRetrasoRequest
{
    [Required] public int MedicoId { get; set; }
    [Required] public DateOnly Fecha { get; set; }
    [Range(5, 480, ErrorMessage = "El retraso debe estar entre 5 y 480 minutos")]
    public int MinutosRetraso { get; set; }
    [StringLength(250)] public string? Motivo { get; set; }
}

public class RetrasoDto
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public string Medico { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public int MinutosRetraso { get; set; }
    public string? Motivo { get; set; }
    public int PacientesNotificados { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Decisión del paciente ante un retraso: 1 esperar, 2 reprogramar.</summary>
public class RespuestaRetrasoRequest
{
    [Required] public int CitaId { get; set; }
    [Required, Range(1, 2)] public int Decision { get; set; }
    public DateTime? NuevaFechaHoraInicio { get; set; }
}

public class NotificacionDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public string Paciente { get; set; } = string.Empty;
    public int? CitaId { get; set; }
    public string? CitaCodigo { get; set; }
    public TipoNotificacion Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public EstadoNotificacion Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string? RespuestaPaciente { get; set; }
}
