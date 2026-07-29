using MediCitas.Api.Domain.Enums;

namespace MediCitas.Api.Domain.Entities;

/// <summary>Rango de fechas en el que no se puede agendar. Si MedicoId es null aplica a toda la clínica.</summary>
public class BloqueoAgenda
{
    public int Id { get; set; }
    public int? MedicoId { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    /// <summary>Si es null el bloqueo cubre el día completo.</summary>
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public TipoBloqueo Tipo { get; set; } = TipoBloqueo.Personal;
    public string? Motivo { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public Medico? Medico { get; set; }
}
