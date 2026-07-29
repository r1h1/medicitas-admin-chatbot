using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Catalogos;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Catálogo de especialidades médicas.</summary>
[Authorize]
public class EspecialidadesController : BaseApiController
{
    private readonly IEspecialidadService _servicio;

    public EspecialidadesController(IEspecialidadService servicio) => _servicio = servicio;

    /// <summary>Lista las especialidades. Con soloActivas=true devuelve únicamente las habilitadas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EspecialidadDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] bool soloActivas = false)
        => Exito(await _servicio.ListarAsync(soloActivas));

    /// <summary>Obtiene una especialidad por su identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EspecialidadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id) => Exito(await _servicio.ObtenerAsync(id));

    /// <summary>Crea una especialidad.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Personal)]
    [ProducesResponseType(typeof(ApiResponse<EspecialidadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] EspecialidadRequest request)
        => Creado(await _servicio.CrearAsync(request), "Especialidad creada");

    /// <summary>Actualiza una especialidad.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Personal)]
    [ProducesResponseType(typeof(ApiResponse<EspecialidadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] EspecialidadRequest request)
        => Exito(await _servicio.ActualizarAsync(id, request), "Especialidad actualizada");

    /// <summary>Elimina una especialidad sin médicos asociados.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return Exito("ok", "Especialidad eliminada");
    }
}
