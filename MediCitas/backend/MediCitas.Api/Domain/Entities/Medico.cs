namespace MediCitas.Api.Domain.Entities;

public class Medico
{
    public int Id { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Colegiado { get; set; } = string.Empty;
    public int EspecialidadId { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    /// <summary>Tamaño del bloque de agenda en minutos.</summary>
    public int DuracionCitaMinutos { get; set; } = 30;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public Especialidad? Especialidad { get; set; }
    public ICollection<HorarioMedico> Horarios { get; set; } = new List<HorarioMedico>();
    public ICollection<BloqueoAgenda> Bloqueos { get; set; } = new List<BloqueoAgenda>();
    public ICollection<Cita> Citas { get; set; } = new List<Cita>();

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
