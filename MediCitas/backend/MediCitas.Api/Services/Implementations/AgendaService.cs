using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Catalogos;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Services.Implementations;

/// <summary>Configuración de la agenda: horarios laborales y bloqueos de fechas.</summary>
public class AgendaService : IAgendaService
{
    private static readonly string[] Dias =
        { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };

    private readonly IRepositorio<HorarioMedico> _horarios;
    private readonly IRepositorio<BloqueoAgenda> _bloqueos;
    private readonly IRepositorio<Medico> _medicos;
    private readonly IBitacoraService _bitacora;
    private readonly IUsuarioActual _actual;

    public AgendaService(IRepositorio<HorarioMedico> horarios, IRepositorio<BloqueoAgenda> bloqueos,
        IRepositorio<Medico> medicos, IBitacoraService bitacora, IUsuarioActual actual)
    {
        _horarios = horarios; _bloqueos = bloqueos; _medicos = medicos;
        _bitacora = bitacora; _actual = actual;
    }

    // -------------------- Horarios --------------------
    public async Task<List<HorarioDto>> ListarHorariosAsync(int? medicoId = null)
    {
        var consulta = _horarios.Query().Include(h => h.Medico).AsNoTracking();
        if (medicoId.HasValue) consulta = consulta.Where(h => h.MedicoId == medicoId.Value);

        var lista = await consulta.OrderBy(h => h.MedicoId).ThenBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraInicio).ToListAsync();

        return lista.Select(Mapear).ToList();
    }

    public async Task<HorarioDto> CrearHorarioAsync(HorarioRequest request)
    {
        await ValidarHorarioAsync(request, null);

        var horario = new HorarioMedico
        {
            MedicoId = request.MedicoId,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            Activo = request.Activo
        };

        await _horarios.AgregarAsync(horario);
        await _horarios.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Crear", "HorarioMedico", horario.Id.ToString(),
            $"Médico {request.MedicoId} {Dias[request.DiaSemana]}", _actual.Id);

        return await ObtenerHorarioAsync(horario.Id);
    }

    public async Task<HorarioDto> ActualizarHorarioAsync(int id, HorarioRequest request)
    {
        var horario = await _horarios.ObtenerPorIdAsync(id)
                      ?? throw new NoEncontradoException("El horario no existe");

        await ValidarHorarioAsync(request, id);

        horario.MedicoId = request.MedicoId;
        horario.DiaSemana = request.DiaSemana;
        horario.HoraInicio = request.HoraInicio;
        horario.HoraFin = request.HoraFin;
        horario.Activo = request.Activo;

        await _horarios.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Actualizar", "HorarioMedico", id.ToString(), null, _actual.Id);

        return await ObtenerHorarioAsync(id);
    }

    public async Task EliminarHorarioAsync(int id)
    {
        var horario = await _horarios.ObtenerPorIdAsync(id)
                      ?? throw new NoEncontradoException("El horario no existe");

        _horarios.Eliminar(horario);
        await _horarios.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Eliminar", "HorarioMedico", id.ToString(), null, _actual.Id);
    }

    private async Task ValidarHorarioAsync(HorarioRequest request, int? idActual)
    {
        if (!await _medicos.ExisteAsync(m => m.Id == request.MedicoId))
            throw new NoEncontradoException("El médico no existe");

        if (request.HoraFin <= request.HoraInicio)
            throw new ValidacionException("La hora de fin debe ser mayor que la hora de inicio");

        var traslape = await _horarios.Query().AnyAsync(h =>
            h.MedicoId == request.MedicoId &&
            h.DiaSemana == request.DiaSemana &&
            (idActual == null || h.Id != idActual) &&
            h.HoraInicio < request.HoraFin && request.HoraInicio < h.HoraFin);

        if (traslape)
            throw new ReglaNegocioException($"El horario se traslapa con otro ya registrado para el {Dias[request.DiaSemana].ToLower()}");
    }

    private async Task<HorarioDto> ObtenerHorarioAsync(int id)
    {
        var horario = await _horarios.Query().Include(h => h.Medico).AsNoTracking()
            .FirstAsync(h => h.Id == id);
        return Mapear(horario);
    }

    // -------------------- Bloqueos --------------------
    public async Task<List<BloqueoDto>> ListarBloqueosAsync(int? medicoId = null)
    {
        var consulta = _bloqueos.Query().Include(b => b.Medico).AsNoTracking().Where(b => b.Activo);
        if (medicoId.HasValue) consulta = consulta.Where(b => b.MedicoId == medicoId.Value || b.MedicoId == null);

        var lista = await consulta.OrderByDescending(b => b.FechaInicio).ToListAsync();

        return lista.Select(b => new BloqueoDto
        {
            Id = b.Id,
            MedicoId = b.MedicoId,
            Medico = b.Medico is null ? "Toda la clínica" : b.Medico.NombreCompleto,
            FechaInicio = b.FechaInicio,
            FechaFin = b.FechaFin,
            HoraInicio = b.HoraInicio,
            HoraFin = b.HoraFin,
            Tipo = b.Tipo,
            TipoNombre = b.Tipo.ToString(),
            Motivo = b.Motivo,
            Activo = b.Activo
        }).ToList();
    }

    public async Task<BloqueoDto> CrearBloqueoAsync(BloqueoRequest request)
    {
        if (request.FechaFin < request.FechaInicio)
            throw new ValidacionException("La fecha final no puede ser anterior a la fecha inicial");

        if (request.MedicoId.HasValue && !await _medicos.ExisteAsync(m => m.Id == request.MedicoId.Value))
            throw new NoEncontradoException("El médico no existe");

        var bloqueo = new BloqueoAgenda
        {
            MedicoId = request.MedicoId,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            HoraInicio = request.HoraInicio,
            HoraFin = request.HoraFin,
            Tipo = request.Tipo,
            Motivo = request.Motivo
        };

        await _bloqueos.AgregarAsync(bloqueo);
        await _bloqueos.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Crear", "BloqueoAgenda", bloqueo.Id.ToString(), request.Motivo, _actual.Id);

        var lista = await ListarBloqueosAsync();
        return lista.First(b => b.Id == bloqueo.Id);
    }

    public async Task EliminarBloqueoAsync(int id)
    {
        var bloqueo = await _bloqueos.ObtenerPorIdAsync(id)
                      ?? throw new NoEncontradoException("El bloqueo no existe");

        bloqueo.Activo = false;
        await _bloqueos.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Eliminar", "BloqueoAgenda", id.ToString(), null, _actual.Id);
    }

    private static HorarioDto Mapear(HorarioMedico h) => new()
    {
        Id = h.Id,
        MedicoId = h.MedicoId,
        Medico = h.Medico?.NombreCompleto ?? string.Empty,
        DiaSemana = h.DiaSemana,
        DiaNombre = Dias[h.DiaSemana],
        HoraInicio = h.HoraInicio,
        HoraFin = h.HoraFin,
        Activo = h.Activo
    };
}
