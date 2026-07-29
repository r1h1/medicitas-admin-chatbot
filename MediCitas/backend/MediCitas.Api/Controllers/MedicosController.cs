using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Catalogos;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Médicos de la clínica y su agenda diaria.</summary>
[Authorize]
public class MedicosController : BaseApiController
{
    private readonly IMedicoService _servicio;
    private readonly ICitaService _citas;

    public MedicosController(IMedicoService servicio, ICitaService citas)
    {
        _servicio = servicio;
        _citas = citas;
    }

    /// <summary>Lista los médicos, con filtro opcional por especialidad.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MedicoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int? especialidadId, [FromQuery] bool soloActivos = false)
        => Exito(await _servicio.ListarAsync(especialidadId, soloActivos));

    /// <summary>Obtiene la información básica de un médico.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MedicoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id) => Exito(await _servicio.ObtenerAsync(id));

    /// <summary>Agenda del día de un médico: pacientes citados en orden de atención.</summary>
    [HttpGet("{id:int}/agenda")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<List<CitaDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Agenda(int id, [FromQuery] DateOnly? fecha)
        => Exito(await _citas.AgendaDelDiaAsync(id, fecha ?? DateOnly.FromDateTime(DateTime.Today)));

    /// <summary>Registra un médico.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<MedicoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] MedicoRequest request)
        => Creado(await _servicio.CrearAsync(request), "Médico registrado");

    /// <summary>Actualiza los datos de un médico.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<MedicoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] MedicoRequest request)
        => Exito(await _servicio.ActualizarAsync(id, request), "Médico actualizado");

    /// <summary>Elimina un médico que no tenga citas registradas.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return Exito("ok", "Médico eliminado");
    }
}