using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Dashboard;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Services.Implementations;

/// <summary>Indicadores del panel administrativo y reportes del periodo.</summary>
public class DashboardService : IDashboardService
{
    private readonly ICitaRepositorio _citas;
    private readonly IRepositorio<Paciente> _pacientes;
    private readonly IRepositorio<Medico> _medicos;
    private readonly IRepositorio<HorarioMedico> _horarios;
    private readonly IRepositorio<EstadoCita> _estados;
    private readonly IRepositorio<Notificacion> _notificaciones;
    private readonly IRepositorio<RetrasoMedico> _retrasos;
    private readonly IDisponibilidadService _disponibilidad;

    public DashboardService(ICitaRepositorio citas, IRepositorio<Paciente> pacientes,
        IRepositorio<Medico> medicos, IRepositorio<HorarioMedico> horarios,
        IRepositorio<EstadoCita> estados, IRepositorio<Notificacion> notificaciones,
        IRepositorio<RetrasoMedico> retrasos, IDisponibilidadService disponibilidad)
    {
        _citas = citas; _pacientes = pacientes; _medicos = medicos; _horarios = horarios;
        _estados = estados; _notificaciones = notificaciones; _retrasos = retrasos;
        _disponibilidad = disponibilidad;
    }

    public async Task<DashboardDto> ObtenerAsync(DateOnly? fecha = null)
    {
        var dia = fecha ?? DateOnly.FromDateTime(DateTime.Today);
        var inicioDia = dia.ToDateTime(TimeOnly.MinValue);
        var finDia = dia.ToDateTime(TimeOnly.MaxValue);

        var citasDelDia = await _citas.ListarPorRangoAsync(inicioDia, finDia);

        var resultado = new DashboardDto
        {
            CitasHoy = citasDelDia.Count,
            Confirmadas = citasDelDia.Count(c => c.EstadoId == (int)EstadoCitaEnum.Confirmada),
            Pendientes = citasDelDia.Count(c => c.EstadoId == (int)EstadoCitaEnum.Pendiente),
            Canceladas = citasDelDia.Count(c => c.EstadoId == (int)EstadoCitaEnum.Cancelada),
            PacientesRegistrados = await _pacientes.ContarAsync(p => p.Activo)
        };

        if (resultado.CitasHoy > 0)
        {
            resultado.PorcentajeConfirmadas = Porcentaje(resultado.Confirmadas, resultado.CitasHoy);
            resultado.PorcentajePendientes = Porcentaje(resultado.Pendientes, resultado.CitasHoy);
            resultado.PorcentajeCanceladas = Porcentaje(resultado.Canceladas, resultado.CitasHoy);
        }

        resultado.PorcentajeOcupacion = await CalcularOcupacionAsync(dia, citasDelDia);

        // Citas del mes agrupadas por estado y por especialidad
        var inicioMes = new DateTime(dia.Year, dia.Month, 1);
        var finMes = inicioMes.AddMonths(1).AddSeconds(-1);
        var citasMes = await _citas.ListarPorRangoAsync(inicioMes, finMes);
        resultado.TotalCitasMes = citasMes.Count;

        var estados = await _estados.ListarAsync();

        resultado.CitasPorEstadoMes = estados
            .Select(e => new ConteoEtiquetaDto
            {
                Etiqueta = e.Nombre,
                Cantidad = citasMes.Count(c => c.EstadoId == e.Id),
                Porcentaje = Porcentaje(citasMes.Count(c => c.EstadoId == e.Id), citasMes.Count),
                Color = e.Color
            })
            .Where(x => x.Cantidad > 0)
            .ToList();

        resultado.CitasPorEspecialidadMes = citasMes
            .GroupBy(c => c.Especialidad?.Nombre ?? "Sin especialidad")
            .Select(g => new ConteoEtiquetaDto
            {
                Etiqueta = g.Key,
                Cantidad = g.Count(),
                Porcentaje = Porcentaje(g.Count(), citasMes.Count)
            })
            .OrderByDescending(x => x.Cantidad)
            .Take(6)
            .ToList();

        resultado.ProximasCitasHoy = citasDelDia
            .Where(c => c.EstaVigente)
            .OrderBy(c => c.FechaHoraInicio)
            .Take(6)
            .Select(c => new CitaResumenDto
            {
                Id = c.Id,
                Codigo = c.Codigo,
                Hora = c.FechaHoraInicio.ToString("hh:mm tt"),
                Paciente = c.Paciente?.NombreCompleto ?? string.Empty,
                Medico = c.Medico?.NombreCompleto ?? string.Empty,
                Especialidad = c.Especialidad?.Nombre ?? string.Empty,
                Estado = c.Estado?.Nombre ?? string.Empty,
                EstadoColor = c.Estado?.Color ?? "#6B7280"
            })
            .ToList();

        resultado.ProximosHorariosDisponibles = await ProximosCuposAsync(dia);

        return resultado;
    }

    public async Task<ReporteGeneralDto> ReporteGeneralAsync(DateOnly desde, DateOnly hasta)
    {
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.ToDateTime(TimeOnly.MaxValue);
        var citas = await _citas.ListarPorRangoAsync(inicio, fin);

        var atendidas = citas.Count(c => c.EstadoId == (int)EstadoCitaEnum.Completada);
        var canceladas = citas.Count(c => c.EstadoId == (int)EstadoCitaEnum.Cancelada);
        var noAsistio = citas.Count(c => c.EstadoId == (int)EstadoCitaEnum.NoAsistio);
        var confirmadas = citas.Count(c => c.FechaConfirmacion != null);

        return new ReporteGeneralDto
        {
            Desde = desde,
            Hasta = hasta,
            TotalCitas = citas.Count,
            Atendidas = atendidas,
            Canceladas = canceladas,
            NoAsistio = noAsistio,
            TasaAusentismo = Porcentaje(noAsistio, citas.Count),
            TasaConfirmacion = Porcentaje(confirmadas, citas.Count),
            CitasPorWhatsApp = citas.Count(c => c.CanalOrigen == CanalOrigen.WhatsApp),
            CitasPorPanel = citas.Count(c => c.CanalOrigen == CanalOrigen.Panel),
            RecordatoriosEnviados = await _notificaciones.ContarAsync(n =>
                n.Tipo == TipoNotificacion.Recordatorio &&
                n.Estado == EstadoNotificacion.Enviada &&
                n.FechaEnvio >= inicio && n.FechaEnvio <= fin),
            RetrasosReportados = await _retrasos.ContarAsync(r => r.Fecha >= desde && r.Fecha <= hasta),

            CitasPorMedico = citas
                .GroupBy(c => c.Medico?.NombreCompleto ?? "Sin médico")
                .Select(g => new ConteoEtiquetaDto
                {
                    Etiqueta = g.Key, Cantidad = g.Count(), Porcentaje = Porcentaje(g.Count(), citas.Count)
                })
                .OrderByDescending(x => x.Cantidad).ToList(),

            CitasPorEspecialidad = citas
                .GroupBy(c => c.Especialidad?.Nombre ?? "Sin especialidad")
                .Select(g => new ConteoEtiquetaDto
                {
                    Etiqueta = g.Key, Cantidad = g.Count(), Porcentaje = Porcentaje(g.Count(), citas.Count)
                })
                .OrderByDescending(x => x.Cantidad).ToList(),

            CitasPorDia = citas
                .GroupBy(c => c.FechaHoraInicio.Date)
                .OrderBy(g => g.Key)
                .Select(g => new ConteoEtiquetaDto
                {
                    Etiqueta = g.Key.ToString("dd/MM"), Cantidad = g.Count()
                }).ToList()
        };
    }

    /// <summary>Ocupación = citas vigentes del día entre la capacidad total de la agenda.</summary>
    private async Task<decimal> CalcularOcupacionAsync(DateOnly dia, List<Cita> citasDelDia)
    {
        var diaSemana = (byte)dia.DayOfWeek;

        var franjas = await _horarios.Query().AsNoTracking()
            .Where(h => h.Activo && h.DiaSemana == diaSemana)
            .Join(_medicos.Query().Where(m => m.Activo), h => h.MedicoId, m => m.Id,
                (h, m) => new { h.HoraInicio, h.HoraFin, m.DuracionCitaMinutos })
            .ToListAsync();

        var capacidad = franjas.Sum(f =>
            (int)((f.HoraFin.ToTimeSpan() - f.HoraInicio.ToTimeSpan()).TotalMinutes / f.DuracionCitaMinutos));

        if (capacidad == 0) return 0;

        var ocupadas = citasDelDia.Count(c => c.EstaVigente);
        return Porcentaje(ocupadas, capacidad);
    }

    private async Task<List<SlotResumenDto>> ProximosCuposAsync(DateOnly dia)
    {
        var medicos = await _medicos.Query().Include(m => m.Especialidad).AsNoTracking()
            .Where(m => m.Activo).Take(5).ToListAsync();

        var cupos = new List<SlotResumenDto>();

        foreach (var medico in medicos)
        {
            var disponibilidad = await _disponibilidad.ObtenerDiaAsync(medico.Id, dia);

            cupos.AddRange(disponibilidad.Slots.Take(2).Select(s => new SlotResumenDto
            {
                Medico = medico.NombreCompleto,
                Especialidad = medico.Especialidad?.Nombre ?? string.Empty,
                Inicio = s.Inicio
            }));
        }

        return cupos.OrderBy(c => c.Inicio).Take(5).ToList();
    }

    private static decimal Porcentaje(int parte, int total)
        => total == 0 ? 0 : Math.Round(parte * 100m / total, 2);
}
