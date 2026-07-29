using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Catalogos;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Parámetros del sistema e historial de actividad.</summary>
[Authorize(Roles = Roles.Personal)]
public class ConfiguracionController : BaseApiController
{
    private readonly IConfiguracionService _config;
    private readonly IBitacoraService _bitacora;

    public ConfiguracionController(IConfiguracionService config, IBitacoraService bitacora)
    {
        _config = config;
        _bitacora = bitacora;
    }

    /// <summary>Lista los parámetros configurables del sistema.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ConfiguracionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar() => Exito(await _config.ListarAsync());

    /// <summary>Actualiza un parámetro por su clave.</summary>
    [HttpPut("{clave}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(string clave, [FromBody] ConfiguracionDto request)
    {
        await _config.ActualizarAsync(clave, request.Valor);
        return Exito("ok", "Configuración actualizada");
    }

    /// <summary>Historial de actividad reciente del sistema.</summary>
    [HttpGet("bitacora")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Bitacora([FromQuery] int cantidad = 100)
    {
        var eventos = await _bitacora.ListarAsync(cantidad);

        return Exito(eventos.Select(e => new
        {
            e.Id,
            e.Accion,
            e.Entidad,
            e.EntidadId,
            e.Detalle,
            e.FechaHora,
            Usuario = e.Usuario == null ? "Sistema" : e.Usuario.Nombres + " " + e.Usuario.Apellidos
        }));
    }
}
