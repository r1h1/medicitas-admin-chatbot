using MediCitas.Api.Common;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Catalogos;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Expediente administrativo de pacientes.</summary>
[Authorize(Roles = Roles.PersonalYMedico)]
public class PacientesController : BaseApiController
{
    private readonly IPacienteService _servicio;
    private readonly ICitaService _citas;

    public PacientesController(IPacienteService servicio, ICitaService citas)
    {
        _servicio = servicio;
        _citas = citas;
    }

    /// <summary>Lista los pacientes. El parámetro busqueda filtra por nombre, apellido o teléfono.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<List<PacienteDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, [FromQuery] int? medicoId)
    {
        var filtroMedicoId = UsuarioActual.EsMedico ? UsuarioActual.MedicoId : medicoId;
        return Exito(await _servicio.ListarAsync(busqueda, filtroMedicoId));
    }

    /// <summary>Obtiene un paciente. El paciente autenticado solo puede consultar su propio expediente.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PacienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Obtener(int id)
    {
        ValidarAcceso(id);
        return Exito(await _servicio.ObtenerAsync(id));
    }

    /// <summary>Historial completo de citas de un paciente.</summary>
    [HttpGet("{id:int}/citas")]
    [ProducesResponseType(typeof(ApiResponse<List<CitaDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Historial(int id)
    {
        ValidarAcceso(id);
        return Exito(await _citas.HistorialPacienteAsync(id));
    }

    /// <summary>Busca un paciente por su número de WhatsApp.</summary>
    [HttpGet("por-telefono/{telefono}")]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<PacienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PorTelefono(string telefono)
    {
        var paciente = await _servicio.ObtenerPorTelefonoAsync(telefono)
                       ?? throw new NoEncontradoException("No hay un paciente registrado con ese teléfono");
        return Exito(paciente);
    }

    /// <summary>Registra un paciente.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.PersonalYMedico)]
    [ProducesResponseType(typeof(ApiResponse<PacienteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear([FromBody] PacienteRequest request)
        => Creado(await _servicio.CrearAsync(request), "Paciente registrado");

    /// <summary>Actualiza los datos de un paciente.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PacienteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(int id, [FromBody] PacienteRequest request)
    {
        ValidarAcceso(id);
        return Exito(await _servicio.ActualizarAsync(id, request), "Datos actualizados");
    }

    /// <summary>Elimina un paciente sin citas registradas.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return Exito("ok", "Paciente eliminado");
    }

    /// <summary>Un paciente solo puede acceder a su propio expediente.</summary>
    private void ValidarAcceso(int pacienteId)
    {
        // Médico puede acceder a cualquier paciente que tenga citas con él (filtro aplicado en servicio).
        if (UsuarioActual.EsMedico || UsuarioActual.EsPersonal) return;
        if (UsuarioActual.PacienteId != pacienteId)
            throw new UnauthorizedAccessException("No tiene permiso para consultar la información de otro paciente");
    }
}