using System.Linq.Expressions;

namespace MediCitas.Api.Data.Repositories;

/// <summary>
/// Repositorio genérico. Encapsula el acceso a datos para que la capa de
/// servicios trabaje contra una abstracción y no directamente contra EF Core.
/// </summary>
public interface IRepositorio<T> where T : class
{
    Task<List<T>> ListarAsync(Expression<Func<T, bool>>? filtro = null);
    Task<T?> ObtenerPorIdAsync(int id);
    Task<T?> PrimeroAsync(Expression<Func<T, bool>> filtro);
    Task<bool> ExisteAsync(Expression<Func<T, bool>> filtro);
    Task<int> ContarAsync(Expression<Func<T, bool>>? filtro = null);
    Task AgregarAsync(T entidad);
    void Actualizar(T entidad);
    void Eliminar(T entidad);
    IQueryable<T> Query();
    Task<int> GuardarCambiosAsync();
}
