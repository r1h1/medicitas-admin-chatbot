using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.DTOs.Auth;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Services.Implementations;

public class UsuarioService : IUsuarioService
{
    private readonly IRepositorio<Usuario> _usuarios;
    private readonly IBitacoraService _bitacora;
    private readonly IUsuarioActual _actual;

    public UsuarioService(IRepositorio<Usuario> usuarios, IBitacoraService bitacora, IUsuarioActual actual)
    {
        _usuarios = usuarios;
        _bitacora = bitacora;
        _actual = actual;
    }

    public async Task<List<UsuarioDto>> ListarAsync()
        => await _usuarios.Query().Include(u => u.Rol).AsNoTracking()
            .OrderBy(u => u.Apellidos).Select(u => Mapear(u)).ToListAsync();

    public async Task<UsuarioDto> ObtenerAsync(int id)
        => Mapear(await Buscar(id));

    public async Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request)
    {
        var email = request.Email.Trim().ToLower();
        if (await _usuarios.ExisteAsync(u => u.Email == email))
            throw new ReglaNegocioException("Ya existe un usuario con ese correo");

        var usuario = new Usuario
        {
            Nombres = request.Nombres.Trim(),
            Apellidos = request.Apellidos.Trim(),
            Email = email,
            Telefono = request.Telefono,
            PasswordHash = PasswordHasher.Hash(request.Password),
            RolId = request.RolId,
            MedicoId = request.MedicoId,
            PacienteId = request.PacienteId
        };

        await _usuarios.AgregarAsync(usuario);
        await _usuarios.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Crear", "Usuario", usuario.Id.ToString(), usuario.Email, _actual.Id);

        return await ObtenerAsync(usuario.Id);
    }

    public async Task<UsuarioDto> ActualizarAsync(int id, ActualizarUsuarioRequest request)
    {
        var usuario = await Buscar(id);

        usuario.Nombres = request.Nombres.Trim();
        usuario.Apellidos = request.Apellidos.Trim();
        usuario.Telefono = request.Telefono;
        usuario.RolId = request.RolId;
        usuario.MedicoId = request.MedicoId;
        usuario.Activo = request.Activo;

        await _usuarios.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Actualizar", "Usuario", id.ToString(), null, _actual.Id);

        return await ObtenerAsync(id);
    }

    public async Task DesactivarAsync(int id)
    {
        var usuario = await Buscar(id);

        if (usuario.Email == "admin@medicitas.com")
            throw new ReglaNegocioException("No se puede desactivar la cuenta administradora principal");

        usuario.Activo = false;
        await _usuarios.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Desactivar", "Usuario", id.ToString(), null, _actual.Id);
    }

    private async Task<Usuario> Buscar(int id)
        => await _usuarios.Query().Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id)
           ?? throw new NoEncontradoException("El usuario no existe");

    private static UsuarioDto Mapear(Usuario u) => new()
    {
        Id = u.Id,
        Nombres = u.Nombres,
        Apellidos = u.Apellidos,
        Email = u.Email,
        Telefono = u.Telefono,
        RolId = u.RolId,
        Rol = u.Rol != null ? u.Rol.Nombre : string.Empty,
        PacienteId = u.PacienteId,
        MedicoId = u.MedicoId,
        Activo = u.Activo,
        FechaRegistro = u.FechaRegistro,
        UltimoAcceso = u.UltimoAcceso
    };
}
