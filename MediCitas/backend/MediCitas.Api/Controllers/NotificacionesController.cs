using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Recordatorios y avisos enviados a los pacientes.</summary>
[Authorize]
public class NotificacionesController : BaseApiController
{
    private readonly INotificacionService _servicio;

    public NotificacionesController(INotificacionService servicio) => _servicio = servicio;

    /// <summary>Lista las notificaciones. Un paciente solo ve las suyas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<NotificacionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int? pacienteId, [FromQuery] EstadoNotificacion? estado)
    {
        if (!UsuarioActual.EsPersonal) pacienteId = UsuarioActual.PacienteId;
        return Exito(await _servicio.ListarAsync(pacienteId, estado));
    }

    /// <summary>Fuerza el envío inmediato de las notificaciones pendientes.</summary>
    [HttpPost("procesar")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Procesar()
    {
        var enviadas = await _servicio.ProcesarPendientesAsync();
        return Exito(enviadas, $"{enviadas} notificaciones enviadas");
    }
}