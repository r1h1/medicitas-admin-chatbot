using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Gestión completa de citas médicas.</summary>
[Authorize(Roles = Roles.PersonalYMedico)]
public class CitasController : BaseApiController
{
    private readonly ICitaService _servicio;

    public CitasController(ICitaService servicio) => _servicio = servicio;

    /// <summary>
    /// Lista las citas del rango indicado. Un paciente autenticado solo obtiene las suyas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CitaDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] int? medicoId, [FromQuery] int? pacienteId, [FromQuery] int? estadoId)
    {
        // Médico: ve solo sus propias citas. Personal: ve todas (puede filtrar por médico).
        if (UsuarioActual.EsMedico) medicoId = UsuarioActual.MedicoId;
        return Exito(await _servicio.ListarAsync(desde, hasta, medicoId, pacienteId, estadoId));
    }

    /// <summary>Obtiene el detalle de una cita.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id)
    {
        var cita = await _servicio.ObtenerAsync(id);
        ValidarAcceso(cita);
        return Exito(cita);
    }

    /// <summary>
    /// Agenda una cita. Valida que el cupo exista, esté libre y respete los bloqueos de agenda.
    /// </summary>
    /// <response code="201">Cita agendada</response>
    /// <response code="409">El horario no está disponible</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] CrearCitaRequest request)
    {
        if (!UsuarioActual.EsPersonal)
        {
            request.PacienteId = UsuarioActual.PacienteId ?? 0;
            request.CanalOrigen = CanalOrigen.Portal;
        }

        return Creado(await _servicio.CrearAsync(request, UsuarioActual.Id), "Cita agendada");
    }

    /// <summary>Reprograma una cita: cierra la actual y crea una nueva conservando el historial.</summary>
    [HttpPut("{id:int}/reprogramar")]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reprogramar(int id, [FromBody] ReprogramarCitaRequest request)
    {
        ValidarAcceso(await _servicio.ObtenerAsync(id));
        return Exito(await _servicio.ReprogramarAsync(id, request, UsuarioActual.Id), "Cita reprogramada");
    }

    /// <summary>Cancela una cita y libera automáticamente el horario.</summary>
    [HttpPut("{id:int}/cancelar")]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(int id, [FromBody] CancelarCitaRequest request)
    {
        ValidarAcceso(await _servicio.ObtenerAsync(id));
        return Exito(await _servicio.CancelarAsync(id, request, UsuarioActual.Id), "Cita cancelada");
    }

    /// <summary>Confirma la asistencia del paciente.</summary>
    [HttpPut("{id:int}/confirmar")]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirmar(int id)
    {
        ValidarAcceso(await _servicio.ObtenerAsync(id));
        return Exito(await _servicio.ConfirmarAsync(id, UsuarioActual.Id), "Asistencia confirmada");
    }

    /// <summary>Cambia el estado de la cita: en atención, completada, no asistió, cancelada.</summary>
    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = Roles.Personal)]
    [ProducesResponseType(typeof(ApiResponse<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request)
        => Exito(await _servicio.CambiarEstadoAsync(id, request, UsuarioActual.Id), "Estado actualizado");

    private void ValidarAcceso(CitaDto cita)
    {
        if (!UsuarioActual.EsPersonal && UsuarioActual.PacienteId != cita.PacienteId)
            throw new UnauthorizedAccessException("No tiene permiso para gestionar esta cita");
    }
}