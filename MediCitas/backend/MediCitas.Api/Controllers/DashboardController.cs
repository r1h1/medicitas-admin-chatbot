using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Dashboard;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Indicadores del panel administrativo y reportes.</summary>
[Authorize(Roles = Roles.Personal)]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _servicio;

    public DashboardController(IDashboardService servicio) => _servicio = servicio;

    /// <summary>Resumen operativo del día: citas, estados, ocupación y próximos cupos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Resumen([FromQuery] DateOnly? fecha)
        => Exito(await _servicio.ObtenerAsync(fecha));

    /// <summary>Reporte del periodo con ausentismo, canal de origen y distribución de citas.</summary>
    [HttpGet("reporte")]
    [ProducesResponseType(typeof(ApiResponse<ReporteGeneralDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reporte([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta)
        => Exito(await _servicio.ReporteGeneralAsync(desde, hasta));
}
