using System.ComponentModel.DataAnnotations;
using MediCitas.Api.Domain.Enums;

namespace MediCitas.Api.DTOs.Catalogos;

// -------------------- Especialidades --------------------
public class EspecialidadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public int TotalMedicos { get; set; }
}

public class EspecialidadRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio"), StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
    [StringLength(300)] public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

// -------------------- Médicos --------------------
public class MedicoDto
{
    public int Id { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public string Colegiado { get; set; } = string.Empty;
    public int EspecialidadId { get; set; }
    public string Especialidad { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int DuracionCitaMinutos { get; set; }
    public bool Activo { get; set; }
}

public class MedicoRequest
{
    [Required, StringLength(100)] public string Nombres { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Apellidos { get; set; } = string.Empty;
    [Required(ErrorMessage = "El número de colegiado es obligatorio"), StringLength(20)]
    public string Colegiado { get; set; } = string.Empty;
    [Required] public int EspecialidadId { get; set; }
    [StringLength(20)] public string? Telefono { get; set; }
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [Range(10, 120, ErrorMessage = "La duración debe estar entre 10 y 120 minutos")]
    public int DuracionCitaMinutos { get; set; } = 30;
    public bool Activo { get; set; } = true;
}

// -------------------- Pacientes --------------------
public class PacienteDto
{
    public int Id { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DocumentoIdentidad { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Genero { get; set; }
    public string? Direccion { get; set; }
    public bool EsNuevo { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int TotalCitas { get; set; }
}

public class PacienteRequest
{
    [Required, StringLength(100)] public string Nombres { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Apellidos { get; set; } = string.Empty;
    [Required(ErrorMessage = "El teléfono de WhatsApp es obligatorio"), StringLength(20, MinimumLength = 8)]
    public string Telefono { get; set; } = string.Empty;
    [EmailAddress, StringLength(150)] public string? Email { get; set; }
    [StringLength(20)] public string? DocumentoIdentidad { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    [RegularExpression("^[MFO]$", ErrorMessage = "El género debe ser M, F u O")]
    public string? Genero { get; set; }
    [StringLength(250)] public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
}

// -------------------- Horarios --------------------
public class HorarioDto
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public string Medico { get; set; } = string.Empty;
    public byte DiaSemana { get; set; }
    public string DiaNombre { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public bool Activo { get; set; }
}

public class HorarioRequest
{
    [Required] public int MedicoId { get; set; }
    [Range(0, 6, ErrorMessage = "El día debe estar entre 0 (domingo) y 6 (sábado)")]
    public byte DiaSemana { get; set; }
    [Required] public TimeOnly HoraInicio { get; set; }
    [Required] public TimeOnly HoraFin { get; set; }
    public bool Activo { get; set; } = true;
}

// -------------------- Bloqueos --------------------
public class BloqueoDto
{
    public int Id { get; set; }
    public int? MedicoId { get; set; }
    public string Medico { get; set; } = "Toda la clínica";
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    public TipoBloqueo Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public bool Activo { get; set; }
}

public class BloqueoRequest
{
    /// <summary>Null significa que el bloqueo aplica a toda la clínica.</summary>
    public int? MedicoId { get; set; }
    [Required] public DateOnly FechaInicio { get; set; }
    [Required] public DateOnly FechaFin { get; set; }
    public TimeOnly? HoraInicio { get; set; }
    public TimeOnly? HoraFin { get; set; }
    [Required] public TipoBloqueo Tipo { get; set; } = TipoBloqueo.Personal;
    [StringLength(200)] public string? Motivo { get; set; }
}

// -------------------- Configuración --------------------
public class ConfiguracionDto
{
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
