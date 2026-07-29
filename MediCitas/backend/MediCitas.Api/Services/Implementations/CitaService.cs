using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Services.Implementations;

public class CitaService : ICitaService
{
    private readonly ICitaRepositorio _citas;
    private readonly IRepositorio<Medico> _medicos;
    private readonly IRepositorio<Paciente> _pacientes;
    private readonly IDisponibilidadService _disponibilidad;
    private readonly INotificacionService _notificaciones;
    private readonly IConfiguracionService _config;
    private readonly IBitacoraService _bitacora;

    public CitaService(ICitaRepositorio citas, IRepositorio<Medico> medicos, IRepositorio<Paciente> pacientes,
        IDisponibilidadService disponibilidad, INotificacionService notificaciones,
        IConfiguracionService config, IBitacoraService bitacora)
    {
        _citas = citas; _medicos = medicos; _pacientes = pacientes;
        _disponibilidad = disponibilidad; _notificaciones = notificaciones;
        _config = config; _bitacora = bitacora;
    }

    public async Task<List<CitaDto>> ListarAsync(DateOnly? desde, DateOnly? hasta,
        int? medicoId, int? pacienteId, int? estadoId)
    {
        var inicio = (desde ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30))).ToDateTime(TimeOnly.MinValue);
        var fin = (hasta ?? DateOnly.FromDateTime(DateTime.Today.AddDays(60))).ToDateTime(TimeOnly.MaxValue);

        var lista = await _citas.ListarPorRangoAsync(inicio, fin, medicoId, pacienteId, estadoId);
        return lista.Select(Mapeos.ACitaDto).ToList();
    }

    public async Task<CitaDto> ObtenerAsync(int id)
        => Mapeos.ACitaDto(await BuscarAsync(id));

    public async Task<CitaDto> CrearAsync(CrearCitaRequest request, int? usuarioId)
    {
        var paciente = await _pacientes.ObtenerPorIdAsync(request.PacienteId)
                       ?? throw new NoEncontradoException("El paciente no existe");

        if (!paciente.Activo)
            throw new ReglaNegocioException("El paciente está desactivado");

        var medico = await _medicos.ObtenerPorIdAsync(request.MedicoId)
                     ?? throw new NoEncontradoException("El médico no existe");

        if (!medico.Activo)
            throw new ReglaNegocioException("El médico no está disponible para agendar citas");

        var inicio = request.FechaHoraInicio;
        var fin = inicio.AddMinutes(medico.DuracionCitaMinutos);

        await _disponibilidad.ValidarSlotAsync(medico, inicio, fin);

        // Un paciente no puede tener dos citas vigentes el mismo día con el mismo médico.
        var duplicada = await _citas.Query().AnyAsync(c =>
            c.PacienteId == paciente.Id && c.MedicoId == medico.Id &&
            c.FechaHoraInicio.Date == inicio.Date &&
            (c.EstadoId == (int)EstadoCitaEnum.Pendiente || c.EstadoId == (int)EstadoCitaEnum.Confirmada));

        if (duplicada)
            throw new ReglaNegocioException("El paciente ya tiene una cita vigente con ese médico ese mismo día");

        var cita = new Cita
        {
            Codigo = await _citas.GenerarCodigoAsync(),
            PacienteId = paciente.Id,
            MedicoId = medico.Id,
            EspecialidadId = medico.EspecialidadId,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            EstadoId = (int)EstadoCitaEnum.Pendiente,
            TipoConsulta = request.TipoConsulta,
            Motivo = request.Motivo,
            Notas = request.Notas,
            CanalOrigen = request.CanalOrigen,
            CreadoPorUsuarioId = usuarioId
        };

        await _citas.AgregarAsync(cita);
        await _citas.GuardarCambiosAsync();

        if (paciente.EsNuevo)
        {
            paciente.EsNuevo = false;
            await _pacientes.GuardarCambiosAsync();
        }

        var completa = await BuscarAsync(cita.Id);

        await _notificaciones.RegistrarAsync(paciente.Id, cita.Id, TipoNotificacion.Agendamiento,
            $"Su cita {cita.Codigo} quedó agendada para el {Mapeos.FechaLarga(inicio)} con {completa.Medico!.NombreCompleto} " +
            $"({completa.Especialidad!.Nombre}). Responda CONFIRMAR para confirmar su asistencia.");

        await _notificaciones.ProgramarRecordatoriosAsync(completa);
        await _bitacora.RegistrarAsync("Crear", "Cita", cita.Codigo, $"{Mapeos.FechaLarga(inicio)}", usuarioId);

        return Mapeos.ACitaDto(completa);
    }

    public async Task<CitaDto> ReprogramarAsync(int id, ReprogramarCitaRequest request, int? usuarioId,
        bool omitirAnticipacion = false)
    {
        var cita = await BuscarAsync(id);
        ValidarModificable(cita);

        // La reprogramación provocada por un retraso del médico ocurre el mismo día,
        // por eso en ese caso se omite la regla de anticipación mínima.
        if (!omitirAnticipacion) await ValidarAnticipacionAsync(cita);

        var medico = request.NuevoMedicoId.HasValue && request.NuevoMedicoId != cita.MedicoId
            ? await _medicos.ObtenerPorIdAsync(request.NuevoMedicoId.Value)
              ?? throw new NoEncontradoException("El médico seleccionado no existe")
            : cita.Medico!;

        var inicio = request.NuevaFechaHoraInicio;
        var fin = inicio.AddMinutes(medico.DuracionCitaMinutos);

        await _disponibilidad.ValidarSlotAsync(medico, inicio, fin, cita.Id);

        // La cita anterior se marca como Reprogramada y se crea una nueva. Así queda historial.
        await _notificaciones.CancelarPendientesAsync(cita.Id);
        cita.EstadoId = (int)EstadoCitaEnum.Reprogramada;

        var nueva = new Cita
        {
            Codigo = await _citas.GenerarCodigoAsync(),
            PacienteId = cita.PacienteId,
            MedicoId = medico.Id,
            EspecialidadId = medico.EspecialidadId,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            EstadoId = (int)EstadoCitaEnum.Pendiente,
            TipoConsulta = cita.TipoConsulta,
            Motivo = cita.Motivo,
            Notas = request.Motivo,
            CanalOrigen = cita.CanalOrigen,
            CitaOriginalId = cita.Id,
            CreadoPorUsuarioId = usuarioId
        };

        await _citas.AgregarAsync(nueva);
        await _citas.GuardarCambiosAsync();

        var completa = await BuscarAsync(nueva.Id);

        await _notificaciones.RegistrarAsync(cita.PacienteId, nueva.Id, TipoNotificacion.Reprogramacion,
            $"Su cita fue reprogramada. Nueva fecha: {Mapeos.FechaLarga(inicio)} con {completa.Medico!.NombreCompleto}. " +
            $"Código {nueva.Codigo}.");

        await _notificaciones.ProgramarRecordatoriosAsync(completa);
        await _bitacora.RegistrarAsync("Reprogramar", "Cita", cita.Codigo, $"Nueva cita {nueva.Codigo}", usuarioId);

        return Mapeos.ACitaDto(completa);
    }

    public async Task<CitaDto> CancelarAsync(int id, CancelarCitaRequest request, int? usuarioId)
    {
        var cita = await BuscarAsync(id);
        ValidarModificable(cita);
        await ValidarAnticipacionAsync(cita);

        cita.EstadoId = (int)EstadoCitaEnum.Cancelada;
        cita.FechaCancelacion = DateTime.Now;
        cita.MotivoCancelacion = request.Motivo;
        await _citas.GuardarCambiosAsync();

        // Al cancelar, el horario vuelve a quedar disponible automáticamente:
        // el cálculo de cupos solo considera citas en estado vigente.
        await _notificaciones.CancelarPendientesAsync(cita.Id);
        await _notificaciones.RegistrarAsync(cita.PacienteId, cita.Id, TipoNotificacion.Cancelacion,
            $"Su cita {cita.Codigo} del {Mapeos.FechaLarga(cita.FechaHoraInicio)} fue cancelada. " +
            "Escriba AGENDAR cuando desee programar una nueva.");

        await _bitacora.RegistrarAsync("Cancelar", "Cita", cita.Codigo, request.Motivo, usuarioId);

        return Mapeos.ACitaDto(await BuscarAsync(id));
    }

    public async Task<CitaDto> ConfirmarAsync(int id, int? usuarioId)
    {
        var cita = await BuscarAsync(id);

        if (cita.EstadoId != (int)EstadoCitaEnum.Pendiente)
            throw new ReglaNegocioException("Solo se pueden confirmar citas en estado pendiente");

        cita.EstadoId = (int)EstadoCitaEnum.Confirmada;
        cita.FechaConfirmacion = DateTime.Now;
        await _citas.GuardarCambiosAsync();

        await _bitacora.RegistrarAsync("Confirmar", "Cita", cita.Codigo, null, usuarioId);

        return Mapeos.ACitaDto(await BuscarAsync(id));
    }

    public async Task<CitaDto> CambiarEstadoAsync(int id, CambiarEstadoRequest request, int? usuarioId)
    {
        var cita = await BuscarAsync(id);
        var nuevo = (int)request.Estado;

        if (cita.EstadoId == nuevo)
            throw new ReglaNegocioException("La cita ya se encuentra en ese estado");

        if (cita.EstadoId is (int)EstadoCitaEnum.Cancelada or (int)EstadoCitaEnum.Reprogramada)
            throw new ReglaNegocioException("No se puede modificar el estado de una cita cancelada o reprogramada");

        cita.EstadoId = nuevo;
        if (request.Estado == EstadoCitaEnum.Confirmada) cita.FechaConfirmacion = DateTime.Now;
        if (request.Estado == EstadoCitaEnum.Cancelada)
        {
            cita.FechaCancelacion = DateTime.Now;
            cita.MotivoCancelacion = request.Observacion;
            await _notificaciones.CancelarPendientesAsync(cita.Id);
        }
        if (!string.IsNullOrWhiteSpace(request.Observacion)) cita.Notas = request.Observacion;

        await _citas.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("CambiarEstado", "Cita", cita.Codigo, request.Estado.ToString(), usuarioId);

        return Mapeos.ACitaDto(await BuscarAsync(id));
    }

    public async Task<List<CitaDto>> HistorialPacienteAsync(int pacienteId)
    {
        var lista = await _citas.Query()
            .Include(c => c.Paciente).Include(c => c.Medico)
            .Include(c => c.Especialidad).Include(c => c.Estado)
            .Where(c => c.PacienteId == pacienteId)
            .OrderByDescending(c => c.FechaHoraInicio)
            .AsNoTracking().ToListAsync();

        return lista.Select(Mapeos.ACitaDto).ToList();
    }

    public async Task<List<CitaDto>> AgendaDelDiaAsync(int medicoId, DateOnly fecha)
    {
        var lista = await _citas.ListarVigentesDelDiaAsync(medicoId, fecha);
        return lista.Select(Mapeos.ACitaDto).ToList();
    }

    private async Task<Cita> BuscarAsync(int id)
        => await _citas.ObtenerCompletaAsync(id) ?? throw new NoEncontradoException("La cita no existe");

    private static void ValidarModificable(Cita cita)
    {
        if (!cita.EstaVigente)
            throw new ReglaNegocioException($"La cita está en estado {cita.Estado?.Nombre} y ya no puede modificarse");
    }

    private async Task ValidarAnticipacionAsync(Cita cita)
    {
        var horas = await _config.ObtenerEnteroAsync("CancelacionHorasMinimas", 4);

        if (cita.FechaHoraInicio <= DateTime.Now.AddHours(horas))
            throw new ReglaNegocioException(
                $"Los cambios deben solicitarse con al menos {horas} horas de anticipación. Comuníquese con la clínica.");
    }
}
