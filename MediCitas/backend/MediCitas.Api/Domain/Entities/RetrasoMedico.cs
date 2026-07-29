namespace MediCitas.Api.Domain.Entities;

/// <summary>Retraso reportado para la agenda de un médico en una fecha específica.</summary>
public class RetrasoMedico
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public DateOnly Fecha { get; set; }
    public int MinutosRetraso { get; set; }
    public string? Motivo { get; set; }
    public int PacientesNotificados { get; set; }
    public int? RegistradoPorUsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public bool Activo { get; set; } = true;

    public Medico? Medico { get; set; }
}
