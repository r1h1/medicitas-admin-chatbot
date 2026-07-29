using MediCitas.Api.Common;
using MediCitas.Api.DTOs.Auth;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Autenticación de usuarios y registro público de pacientes.</summary>
[AllowAnonymous]
public class AuthController : BaseApiController
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Inicia sesión y devuelve el token JWT.</summary>
    /// <response code="200">Credenciales válidas</response>
    /// <response code="400">Correo o contraseña incorrectos</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
        => Exito(await _auth.LoginAsync(request), "Bienvenido a MediCitas");

    /// <summary>Registra un paciente nuevo y crea su cuenta de acceso al portal.</summary>
    /// <response code="201">Cuenta creada</response>
    /// <response code="409">El correo ya está registrado</response>
    [HttpPost("registro")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registro([FromBody] RegistroPacienteRequest request)
        => Creado(await _auth.RegistrarPacienteAsync(request), "Cuenta creada correctamente");

    /// <summary>Devuelve el perfil del usuario autenticado.</summary>
    [HttpGet("perfil")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Perfil()
        => Exito(await _auth.ObtenerPerfilAsync(UsuarioActual.Id!.Value));

    /// <summary>Cambia la contraseña del usuario autenticado.</summary>
    [HttpPost("cambiar-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
    {
        await _auth.CambiarPasswordAsync(UsuarioActual.Id!.Value, request);
        return Exito("ok", "Contraseña actualizada");
    }
}
