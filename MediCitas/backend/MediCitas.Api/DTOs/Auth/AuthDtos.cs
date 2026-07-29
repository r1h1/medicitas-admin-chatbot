using System.ComponentModel.DataAnnotations;

namespace MediCitas.Api.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public UsuarioDto Usuario { get; set; } = new();
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public int? PacienteId { get; set; }
    public int? MedicoId { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}

/// <summary>Autorregistro del paciente desde la pantalla pública.</summary>
public class RegistroPacienteRequest
{
    [Required, StringLength(100)] public string Nombres { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Apellidos { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono de WhatsApp es obligatorio")]
    [StringLength(20, MinimumLength = 8)]
    public string Telefono { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
    public string ConfirmarPassword { get; set; } = string.Empty;
}

/// <summary>Creación de usuarios internos. Solo disponible para el administrador.</summary>
public class CrearUsuarioRequest
{
    [Required, StringLength(100)] public string Nombres { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Apellidos { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [StringLength(20)] public string? Telefono { get; set; }
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [Required, Range(1, 4, ErrorMessage = "El rol seleccionado no es válido")] public int RolId { get; set; }
    public int? MedicoId { get; set; }
    public int? PacienteId { get; set; }
}

public class ActualizarUsuarioRequest
{
    [Required, StringLength(100)] public string Nombres { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Apellidos { get; set; } = string.Empty;
    [StringLength(20)] public string? Telefono { get; set; }
    [Required, Range(1, 4)] public int RolId { get; set; }
    public int? MedicoId { get; set; }
    public bool Activo { get; set; } = true;
}

public class CambiarPasswordRequest
{
    [Required] public string PasswordActual { get; set; } = string.Empty;
    [Required, MinLength(8)] public string PasswordNueva { get; set; } = string.Empty;
}