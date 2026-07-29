namespace MediCitas.Api.Domain.Entities;

/// <summary>Horario laboral recurrente de un médico para un día de la semana.</summary>
public class HorarioMedico
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    /// <summary>0 = domingo ... 6 = sábado (equivale a System.DayOfWeek).</summary>
    public byte DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool Activo { get; set; } = true;

    public Medico? Medico { get; set; }
}
