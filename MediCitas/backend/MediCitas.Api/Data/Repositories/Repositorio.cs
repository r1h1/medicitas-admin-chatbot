using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Data.Repositories;

public class Repositorio<T> : IRepositorio<T> where T : class
{
    protected readonly MediCitasDbContext Contexto;
    protected readonly DbSet<T> Conjunto;

    public Repositorio(MediCitasDbContext contexto)
    {
        Contexto = contexto;
        Conjunto = contexto.Set<T>();
    }

    public async Task<List<T>> ListarAsync(Expression<Func<T, bool>>? filtro = null)
        => filtro is null
            ? await Conjunto.AsNoTracking().ToListAsync()
            : await Conjunto.AsNoTracking().Where(filtro).ToListAsync();

    public async Task<T?> ObtenerPorIdAsync(int id) => await Conjunto.FindAsync(id);

    public async Task<T?> PrimeroAsync(Expression<Func<T, bool>> filtro)
        => await Conjunto.FirstOrDefaultAsync(filtro);

    public async Task<bool> ExisteAsync(Expression<Func<T, bool>> filtro)
        => await Conjunto.AnyAsync(filtro);

    public async Task<int> ContarAsync(Expression<Func<T, bool>>? filtro = null)
        => filtro is null ? await Conjunto.CountAsync() : await Conjunto.CountAsync(filtro);

    public async Task AgregarAsync(T entidad) => await Conjunto.AddAsync(entidad);

    public void Actualizar(T entidad) => Conjunto.Update(entidad);

    public void Eliminar(T entidad) => Conjunto.Remove(entidad);

    public IQueryable<T> Query() => Conjunto.AsQueryable();

    public async Task<int> GuardarCambiosAsync() => await Contexto.SaveChangesAsync();
}
