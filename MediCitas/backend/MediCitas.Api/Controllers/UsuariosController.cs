using MediCitas.Api.Common;
using MediCitas.Api.Data;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Auth;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Controllers;

/// <summary>Administración de las cuentas de acceso al sistema.</summary>
[Authorize(Roles = Roles.Administrador)]
public class UsuariosController : BaseApiController
{
    private readonly IUsuarioService _servicio;
    private readonly MediCitasDbContext _contexto;

    public UsuariosController(IUsuarioService servicio, MediCitasDbContext contexto)
    {
        _servicio = servicio;
        _contexto = contexto;
    }

    /// <summary>Lista los roles disponibles para asignar en el panel.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarRoles()
    {
        // Solo se exponen los roles que acceden al panel; Paciente es interno del chatbot.
        var excluir = new[] { "Paciente" };
        var roles = await _contexto.Roles
            .Where(r => !excluir.Contains(r.Nombre))
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.Nombre })
            .ToListAsync();
        return Exito(roles);
    }

    /// <summary>Lista todos los usuarios.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UsuarioDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() => Exito(await _servicio.ListarAsync());

    /// <summary>Obtiene un usuario por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id) => Exito(await _servicio.ObtenerAsync(id));

    /// <summary>Crea un usuario interno (administrador o recepcionista).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioRequest request)
        => Creado(await _servicio.CrearAsync(request), "Usuario creado");

    /// <summary>Actualiza los datos y el rol de un usuario.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarUsuarioRequest request)
        => Exito(await _servicio.ActualizarAsync(id, request), "Usuario actualizado");

    /// <summary>Desactiva un usuario sin borrar su historial.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _servicio.DesactivarAsync(id);
        return Exito("ok", "Usuario desactivado");
    }
}