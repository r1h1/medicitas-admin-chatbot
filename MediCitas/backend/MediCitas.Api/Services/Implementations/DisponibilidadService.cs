using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Services.Implementations;

/// <summary>
/// Calcula los bloques de agenda disponibles de un médico.
/// Los cupos no se almacenan: se generan a partir del horario laboral y se
/// descuentan las citas vigentes y los bloqueos. Así nunca quedan desfasados.
/// </summary>
public class DisponibilidadService : IDisponibilidadService
{
    private readonly IRepositorio<Medico> _medicos;
    private readonly IRepositorio<HorarioMedico> _horarios;
    private readonly IRepositorio<BloqueoAgenda> _bloqueos;
    private readonly ICitaRepositorio _citas;
    private readonly IConfiguracionService _config;

    public DisponibilidadService(IRepositorio<Medico> medicos, IRepositorio<HorarioMedico> horarios,
        IRepositorio<BloqueoAgenda> bloqueos, ICitaRepositorio citas, IConfiguracionService config)
    {
        _medicos = medicos; _horarios = horarios; _bloqueos = bloqueos;
        _citas = citas; _config = config;
    }

    public async Task<DisponibilidadDiaDto> ObtenerDiaAsync(int medicoId, DateOnly fecha, int? excluirCitaId = null)
    {
        var medico = await _medicos.Query().Include(m => m.Especialidad).AsNoTracking()
                         .FirstOrDefaultAsync(m => m.Id == medicoId)
                     ?? throw new NoEncontradoException("El médico no existe");

        var resultado = new DisponibilidadDiaDto
        {
            Fecha = fecha,
            MedicoId = medicoId,
            Medico = medico.NombreCompleto
        };

        // 1. Bloqueos de la clínica o del médico
        var bloqueo = await _bloqueos.Query().AsNoTracking().FirstOrDefaultAsync(b =>
            b.Activo && (b.MedicoId == null || b.MedicoId == medicoId) &&
            b.FechaInicio <= fecha && fecha <= b.FechaFin && b.HoraInicio == null);

        if (bloqueo is not null)
        {
            resultado.Bloqueado = true;
            resultado.MotivoBloqueo = bloqueo.Motivo ?? bloqueo.Tipo.ToString();
            return resultado;
        }

        // 2. Horario laboral del día
        var dia = (byte)fecha.DayOfWeek;
        var franjas = await _horarios.Query().AsNoTracking()
            .Where(h => h.MedicoId == medicoId && h.DiaSemana == dia && h.Activo)
            .OrderBy(h => h.HoraInicio).ToListAsync();

        if (franjas.Count == 0) return resultado;

        // 3. Citas ya reservadas y bloqueos parciales del día
        var ocupadas = await _citas.ListarVigentesDelDiaAsync(medicoId, fecha);
        var bloqueosParciales = await _bloqueos.Query().AsNoTracking()
            .Where(b => b.Activo && (b.MedicoId == null || b.MedicoId == medicoId)
                     && b.FechaInicio <= fecha && fecha <= b.FechaFin && b.HoraInicio != null)
            .ToListAsync();

        var duracion = TimeSpan.FromMinutes(medico.DuracionCitaMinutos);
        var ahora = DateTime.Now;

        foreach (var franja in franjas)
        {
            var cursor = fecha.ToDateTime(franja.HoraInicio);
            var limite = fecha.ToDateTime(franja.HoraFin);

            while (cursor.Add(duracion) <= limite) // <= correcto: slot completo cabe dentro de la franja
            {
                var fin = cursor.Add(duracion);

                var ocupado = ocupadas.Any(c => c.Id != excluirCitaId
                                             && c.FechaHoraInicio < fin && cursor < c.FechaHoraFin);

                var enBloqueo = bloqueosParciales.Any(b =>
                    TimeOnly.FromDateTime(cursor) < b.HoraFin!.Value &&
                    b.HoraInicio!.Value < TimeOnly.FromDateTime(fin));

                // Para días futuros mostrar todos los slots; para hoy solo los que aún no pasaron.
                var esHoy = fecha == DateOnly.FromDateTime(ahora);
                if (!ocupado && !enBloqueo && (!esHoy || cursor > ahora))
                {
                    resultado.Slots.Add(new SlotDto
                    {
                        Inicio = cursor,
                        Fin = fin,
                        Etiqueta = cursor.ToString("hh:mm tt"),
                        Disponible = true
                    });
                }

                cursor = fin;
            }
        }

        return resultado;
    }

    public async Task<List<DisponibilidadDiaDto>> ObtenerRangoAsync(int medicoId, DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde) throw new ValidacionException("El rango de fechas es inválido");
        if (hasta.DayNumber - desde.DayNumber > 60)
            throw new ValidacionException("El rango no puede superar 60 días");

        var dias = new List<DisponibilidadDiaDto>();
        for (var f = desde; f <= hasta; f = f.AddDays(1))
            dias.Add(await ObtenerDiaAsync(medicoId, f));

        return dias;
    }

    public async Task<List<DateOnly>> ProximasFechasConCupoAsync(int medicoId, int cantidadDias = 5)
    {
        var maximo = await _config.ObtenerEnteroAsync("DiasMaximosAgendamiento", 60);
        var fechas = new List<DateOnly>();
        var fecha = DateOnly.FromDateTime(DateTime.Today);

        for (var i = 0; i < maximo && fechas.Count < cantidadDias; i++, fecha = fecha.AddDays(1))
        {
            var dia = await ObtenerDiaAsync(medicoId, fecha);
            if (dia.Slots.Count > 0) fechas.Add(fecha);
        }

        return fechas;
    }

    /// <summary>Valida que el horario solicitado sea un cupo real y esté libre.</summary>
    public async Task ValidarSlotAsync(Medico medico, DateTime inicio, DateTime fin, int? excluirCitaId = null)
    {
        if (inicio <= DateTime.Now)
            throw new ReglaNegocioException("No se pueden agendar citas en fechas u horas pasadas");

        var maximo = await _config.ObtenerEnteroAsync("DiasMaximosAgendamiento", 60);
        if (inicio.Date > DateTime.Today.AddDays(maximo))
            throw new ReglaNegocioException($"Solo se pueden agendar citas con un máximo de {maximo} días de anticipación");

        var fecha = DateOnly.FromDateTime(inicio);
        var disponibilidad = await ObtenerDiaAsync(medico.Id, fecha, excluirCitaId);

        if (disponibilidad.Bloqueado)
            throw new ReglaNegocioException($"La agenda no está disponible ese día: {disponibilidad.MotivoBloqueo}");

        if (disponibilidad.Slots.Count == 0)
            throw new ReglaNegocioException("El médico no atiende ese día o ya no tiene cupos disponibles");

        if (!disponibilidad.Slots.Any(s => s.Inicio == inicio))
            throw new ReglaNegocioException("El horario seleccionado ya no está disponible. Elija otro cupo.");

        if (await _citas.HorarioOcupadoAsync(medico.Id, inicio, fin, excluirCitaId))
            throw new ReglaNegocioException("El horario acaba de ser reservado por otro paciente");
    }
}