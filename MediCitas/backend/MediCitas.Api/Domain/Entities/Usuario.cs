namespace MediCitas.Api.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    /// <summary>Vincula la cuenta con un paciente para el portal del paciente.</summary>
    public int? PacienteId { get; set; }
    /// <summary>Vincula la cuenta con un médico para mostrar "Mi agenda" en el panel.</summary>
    public int? MedicoId { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public DateTime? UltimoAcceso { get; set; }

    public Rol? Rol { get; set; }
    public Paciente? Paciente { get; set; }
    public Medico? Medico { get; set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
