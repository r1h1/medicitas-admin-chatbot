using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Catalogos;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Configuración de la agenda: horarios laborales, bloqueos y cupos disponibles.</summary>
[Authorize]
public class AgendaController : BaseApiController
{
    private readonly IAgendaService _agenda;
    private readonly IDisponibilidadService _disponibilidad;

    public AgendaController(IAgendaService agenda, IDisponibilidadService disponibilidad)
    {
        _agenda = agenda;
        _disponibilidad = disponibilidad;
    }

    // -------------------- Horarios laborales --------------------

    /// <summary>Lista los horarios laborales configurados.</summary>
    [HttpGet("horarios")]
    [ProducesResponseType(typeof(ApiResponse<List<HorarioDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarHorarios([FromQuery] int? medicoId)
        => Exito(await _agenda.ListarHorariosAsync(medicoId));

    /// <summary>Agrega una franja horaria a un médico.</summary>
    [HttpPost("horarios")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<HorarioDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CrearHorario([FromBody] HorarioRequest request)
        => Creado(await _agenda.CrearHorarioAsync(request), "Horario agregado");

    /// <summary>Modifica una franja horaria.</summary>
    [HttpPut("horarios/{id:int}")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<HorarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarHorario(int id, [FromBody] HorarioRequest request)
        => Exito(await _agenda.ActualizarHorarioAsync(id, request), "Horario actualizado");

    /// <summary>Elimina una franja horaria.</summary>
    [HttpDelete("horarios/{id:int}")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EliminarHorario(int id)
    {
        await _agenda.EliminarHorarioAsync(id);
        return Exito("ok", "Horario eliminado");
    }

    // -------------------- Bloqueos --------------------

    /// <summary>Lista los bloqueos vigentes: vacaciones, feriados y bloqueos puntuales.</summary>
    [HttpGet("bloqueos")]
    [ProducesResponseType(typeof(ApiResponse<List<BloqueoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarBloqueos([FromQuery] int? medicoId)
        => Exito(await _agenda.ListarBloqueosAsync(medicoId));

    /// <summary>Bloquea un rango de fechas. Sin medicoId el bloqueo aplica a toda la clínica.</summary>
    [HttpPost("bloqueos")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<BloqueoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearBloqueo([FromBody] BloqueoRequest request)
        => Creado(await _agenda.CrearBloqueoAsync(request), "Bloqueo registrado");

    /// <summary>Libera un bloqueo.</summary>
    [HttpDelete("bloqueos/{id:int}")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EliminarBloqueo(int id)
    {
        await _agenda.EliminarBloqueoAsync(id);
        return Exito("ok", "Bloqueo liberado");
    }

    // -------------------- Disponibilidad --------------------

    /// <summary>Cupos libres de un médico en una fecha.</summary>
    [HttpGet("disponibilidad")]
    [ProducesResponseType(typeof(ApiResponse<DisponibilidadDiaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disponibilidad([FromQuery] int medicoId, [FromQuery] DateOnly fecha,
        [FromQuery] int? excluirCitaId)
        => Exito(await _disponibilidad.ObtenerDiaAsync(medicoId, fecha, excluirCitaId));

    /// <summary>Cupos libres de un médico en un rango de hasta 60 días.</summary>
    [HttpGet("disponibilidad/rango")]
    [ProducesResponseType(typeof(ApiResponse<List<DisponibilidadDiaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DisponibilidadRango([FromQuery] int medicoId,
        [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta)
        => Exito(await _disponibilidad.ObtenerRangoAsync(medicoId, desde, hasta));

    /// <summary>Próximas fechas con cupo de un médico.</summary>
    [HttpGet("disponibilidad/proximas")]
    [ProducesResponseType(typeof(ApiResponse<List<DateOnly>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProximasFechas([FromQuery] int medicoId, [FromQuery] int cantidad = 5)
        => Exito(await _disponibilidad.ProximasFechasConCupoAsync(medicoId, cantidad));
}