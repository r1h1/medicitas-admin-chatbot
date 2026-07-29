using MediCitas.Api.Domain.Entities;
using MediCitas.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Data.Repositories;

public class CitaRepositorio : Repositorio<Cita>, ICitaRepositorio
{
    private static readonly int[] EstadosVigentes =
    {
        (int)EstadoCitaEnum.Pendiente,
        (int)EstadoCitaEnum.Confirmada,
        (int)EstadoCitaEnum.EnAtencion
    };

    public CitaRepositorio(MediCitasDbContext contexto) : base(contexto) { }

    private IQueryable<Cita> ConRelaciones() => Conjunto
        .Include(c => c.Paciente)
        .Include(c => c.Medico)
        .Include(c => c.Especialidad)
        .Include(c => c.Estado);

    public async Task<Cita?> ObtenerCompletaAsync(int id)
        => await ConRelaciones().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Cita?> ObtenerPorCodigoAsync(string codigo)
        => await ConRelaciones().FirstOrDefaultAsync(c => c.Codigo == codigo);

    public async Task<List<Cita>> ListarVigentesDelDiaAsync(int medicoId, DateOnly fecha)
    {
        var inicio = fecha.ToDateTime(TimeOnly.MinValue);
        var fin = inicio.AddDays(1);

        return await ConRelaciones()
            .Where(c => c.MedicoId == medicoId
                     && c.FechaHoraInicio >= inicio && c.FechaHoraInicio < fin
                     && EstadosVigentes.Contains(c.EstadoId))
            .OrderBy(c => c.FechaHoraInicio)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Cita>> ListarPorRangoAsync(DateTime desde, DateTime hasta,
        int? medicoId = null, int? pacienteId = null, int? estadoId = null)
    {
        var consulta = ConRelaciones()
            .Where(c => c.FechaHoraInicio >= desde && c.FechaHoraInicio <= hasta);

        if (medicoId.HasValue) consulta = consulta.Where(c => c.MedicoId == medicoId.Value);
        if (pacienteId.HasValue) consulta = consulta.Where(c => c.PacienteId == pacienteId.Value);
        if (estadoId.HasValue) consulta = consulta.Where(c => c.EstadoId == estadoId.Value);

        return await consulta.OrderBy(c => c.FechaHoraInicio).AsNoTracking().ToListAsync();
    }

    public async Task<List<Cita>> ListarProximasDelPacienteAsync(int pacienteId, int cantidad = 5)
        => await ConRelaciones()
            .Where(c => c.PacienteId == pacienteId
                     && c.FechaHoraInicio >= DateTime.Now
                     && EstadosVigentes.Contains(c.EstadoId))
            .OrderBy(c => c.FechaHoraInicio)
            .Take(cantidad)
            .AsNoTracking()
            .ToListAsync();

    public async Task<bool> HorarioOcupadoAsync(int medicoId, DateTime inicio, DateTime fin, int? excluirCitaId = null)
        => await Conjunto.AnyAsync(c =>
                c.MedicoId == medicoId
             && EstadosVigentes.Contains(c.EstadoId)
             && (excluirCitaId == null || c.Id != excluirCitaId)
             && c.FechaHoraInicio < fin
             && inicio < c.FechaHoraFin);

    public async Task<string> GenerarCodigoAsync()
    {
        var ultimo = await Conjunto.OrderByDescending(c => c.Id).Select(c => c.Id).FirstOrDefaultAsync();
        return $"MC-{(ultimo + 1):D6}";
    }
}
