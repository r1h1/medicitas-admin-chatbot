using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Aviso de atrasos en la atención médica.</summary>
[Authorize]
public class RetrasosController : BaseApiController
{
    private readonly IRetrasoService _servicio;

    public RetrasosController(IRetrasoService servicio) => _servicio = servicio;

    /// <summary>Lista los retrasos reportados.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<List<RetrasoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? fecha, [FromQuery] int? medicoId)
        => Exito(await _servicio.ListarAsync(fecha, medicoId));

    /// <summary>
    /// Reporta un retraso y notifica automáticamente a los pacientes que siguen en cola,
    /// ofreciéndoles esperar o reprogramar.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<RetrasoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reportar([FromBody] ReportarRetrasoRequest request)
        => Creado(await _servicio.ReportarAsync(request, UsuarioActual.Id), "Retraso reportado y pacientes notificados");

    /// <summary>Registra la decisión del paciente ante un retraso: 1 esperar, 2 reprogramar.</summary>
    [HttpPost("respuesta")]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Responder([FromBody] RespuestaRetrasoRequest request)
        => Exito(await _servicio.ResponderAsync(request, UsuarioActual.Id), "Respuesta registrada");

    /// <summary>Marca un retraso como resuelto.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Desactivar(int id)
    {
        await _servicio.DesactivarAsync(id);
        return Exito("ok", "Retraso marcado como resuelto");
    }
}