using MediCitas.Api.Domain.Entities;

namespace MediCitas.Api.Data.Repositories;

/// <summary>Consultas propias de la agenda que no cubre el repositorio genérico.</summary>
public interface ICitaRepositorio : IRepositorio<Cita>
{
    Task<Cita?> ObtenerCompletaAsync(int id);
    Task<Cita?> ObtenerPorCodigoAsync(string codigo);
    Task<List<Cita>> ListarVigentesDelDiaAsync(int medicoId, DateOnly fecha);
    Task<List<Cita>> ListarPorRangoAsync(DateTime desde, DateTime hasta, int? medicoId = null, int? pacienteId = null, int? estadoId = null);
    Task<List<Cita>> ListarProximasDelPacienteAsync(int pacienteId, int cantidad = 5);
    Task<bool> HorarioOcupadoAsync(int medicoId, DateTime inicio, DateTime fin, int? excluirCitaId = null);
    Task<string> GenerarCodigoAsync();
}
