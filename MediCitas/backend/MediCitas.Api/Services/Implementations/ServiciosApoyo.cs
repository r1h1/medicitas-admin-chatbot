using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.DTOs.Catalogos;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Services.Implementations;

/// <summary>Parámetros del sistema almacenados como clave/valor.</summary>
public class ConfiguracionService : IConfiguracionService
{
    private readonly IRepositorio<ConfiguracionItem> _repo;

    public ConfiguracionService(IRepositorio<ConfiguracionItem> repo) => _repo = repo;

    public async Task<List<ConfiguracionDto>> ListarAsync()
        => await _repo.Query().AsNoTracking().OrderBy(c => c.Clave)
            .Select(c => new ConfiguracionDto { Clave = c.Clave, Valor = c.Valor, Descripcion = c.Descripcion })
            .ToListAsync();

    public async Task<string> ObtenerValorAsync(string clave, string valorPorDefecto = "")
    {
        var item = await _repo.Query().AsNoTracking().FirstOrDefaultAsync(c => c.Clave == clave);
        return item?.Valor ?? valorPorDefecto;
    }

    public async Task<int> ObtenerEnteroAsync(string clave, int valorPorDefecto)
        => int.TryParse(await ObtenerValorAsync(clave), out var valor) ? valor : valorPorDefecto;

    public async Task ActualizarAsync(string clave, string valor)
    {
        var item = await _repo.PrimeroAsync(c => c.Clave == clave);

        if (item is null)
        {
            await _repo.AgregarAsync(new ConfiguracionItem { Clave = clave, Valor = valor });
        }
        else
        {
            item.Valor = valor;
            item.FechaModificacion = DateTime.Now;
        }

        await _repo.GuardarCambiosAsync();
    }
}

/// <summary>Registro básico de eventos para auditoría y para la vista Historial de actividad.</summary>
public class BitacoraService : IBitacoraService
{
    private readonly IRepositorio<EventoBitacora> _repo;
    private readonly ILogger<BitacoraService> _logger;

    public BitacoraService(IRepositorio<EventoBitacora> repo, ILogger<BitacoraService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task RegistrarAsync(string accion, string entidad, string? entidadId, string? detalle, int? usuarioId)
    {
        try
        {
            await _repo.AgregarAsync(new EventoBitacora
            {
                UsuarioId = usuarioId,
                Accion = accion,
                Entidad = entidad,
                EntidadId = entidadId,
                Detalle = detalle
            });
            await _repo.GuardarCambiosAsync();
        }
        catch (Exception ex)
        {
            // La bitácora nunca debe interrumpir la operación principal.
            _logger.LogWarning(ex, "No se pudo registrar el evento {Accion} sobre {Entidad}", accion, entidad);
        }
    }

    public async Task<List<EventoBitacora>> ListarAsync(int cantidad = 100)
        => await _repo.Query().Include(e => e.Usuario).AsNoTracking()
            .OrderByDescending(e => e.FechaHora).Take(cantidad).ToListAsync();
}
