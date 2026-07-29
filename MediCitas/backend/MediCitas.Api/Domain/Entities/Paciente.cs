namespace MediCitas.Api.Domain.Entities;

public class Paciente
{
    public int Id { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    /// <summary>Número de WhatsApp. Es la llave natural que usa el chatbot.</summary>
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DocumentoIdentidad { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Genero { get; set; }
    public string? Direccion { get; set; }
    public bool EsNuevo { get; set; } = true;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    public ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
