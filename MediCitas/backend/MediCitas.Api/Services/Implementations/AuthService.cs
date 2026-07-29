using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.DTOs.Auth;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediCitas.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IRepositorio<Usuario> _usuarios;
    private readonly IRepositorio<Paciente> _pacientes;
    private readonly IRepositorio<Rol> _roles;
    private readonly IBitacoraService _bitacora;
    private readonly JwtSettings _jwt;

    public AuthService(IRepositorio<Usuario> usuarios, IRepositorio<Paciente> pacientes,
        IRepositorio<Rol> roles, IBitacoraService bitacora, IOptions<JwtSettings> jwt)
    {
        _usuarios = usuarios;
        _pacientes = pacientes;
        _roles = roles;
        _bitacora = bitacora;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var usuario = await _usuarios.Query()
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email);

        // Mismo mensaje para usuario inexistente y contraseña incorrecta.
        if (usuario is null || !PasswordHasher.Verificar(request.Password, usuario.PasswordHash))
            throw new ValidacionException("Correo o contraseña incorrectos");

        if (!usuario.Activo)
            throw new ValidacionException("La cuenta está desactivada. Contacte al administrador.");

        usuario.UltimoAcceso = DateTime.Now;
        await _usuarios.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Login", "Usuario", usuario.Id.ToString(), null, usuario.Id);

        var expira = DateTime.Now.AddMinutes(_jwt.MinutosExpiracion);
        return new LoginResponse
        {
            Token = GenerarToken(usuario, expira),
            Expira = expira,
            Usuario = Mapear(usuario)
        };
    }

    public async Task<UsuarioDto> RegistrarPacienteAsync(RegistroPacienteRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var telefono = NormalizarTelefono(request.Telefono);

        if (await _usuarios.ExisteAsync(u => u.Email == email))
            throw new ReglaNegocioException("Ya existe una cuenta registrada con ese correo");

        var paciente = await _pacientes.PrimeroAsync(p => p.Telefono == telefono);
        if (paciente is null)
        {
            paciente = new Paciente
            {
                Nombres = request.Nombres.Trim(),
                Apellidos = request.Apellidos.Trim(),
                Telefono = telefono,
                Email = email
            };
            await _pacientes.AgregarAsync(paciente);
            await _pacientes.GuardarCambiosAsync();
        }

        var rolPaciente = await _roles.PrimeroAsync(r => r.Nombre == Domain.Enums.Roles.Paciente)
                          ?? throw new ReglaNegocioException("El catálogo de roles no está configurado");

        var usuario = new Usuario
        {
            Nombres = request.Nombres.Trim(),
            Apellidos = request.Apellidos.Trim(),
            Email = email,
            Telefono = telefono,
            PasswordHash = PasswordHasher.Hash(request.Password),
            RolId = rolPaciente.Id,
            PacienteId = paciente.Id
        };

        await _usuarios.AgregarAsync(usuario);
        await _usuarios.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Registro", "Usuario", usuario.Id.ToString(), "Autorregistro de paciente", usuario.Id);

        usuario.Rol = rolPaciente;
        return Mapear(usuario);
    }

    public async Task<UsuarioDto> ObtenerPerfilAsync(int usuarioId)
    {
        var usuario = await _usuarios.Query().Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == usuarioId)
                      ?? throw new NoEncontradoException("El usuario no existe");
        return Mapear(usuario);
    }

    public async Task CambiarPasswordAsync(int usuarioId, CambiarPasswordRequest request)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId)
                      ?? throw new NoEncontradoException("El usuario no existe");

        if (!PasswordHasher.Verificar(request.PasswordActual, usuario.PasswordHash))
            throw new ValidacionException("La contraseña actual no es correcta");

        usuario.PasswordHash = PasswordHasher.Hash(request.PasswordNueva);
        await _usuarios.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("CambioPassword", "Usuario", usuario.Id.ToString(), null, usuario.Id);
    }

    private string GenerarToken(Usuario usuario, DateTime expira)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol?.Nombre ?? Domain.Enums.Roles.Paciente)
        };

        if (usuario.PacienteId.HasValue) claims.Add(new Claim("pacienteId", usuario.PacienteId.Value.ToString()));
        if (usuario.MedicoId.HasValue) claims.Add(new Claim("medicoId", usuario.MedicoId.Value.ToString()));

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims,
            expires: expira, signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string NormalizarTelefono(string telefono)
        => new string(telefono.Where(char.IsDigit).ToArray());

    private static UsuarioDto Mapear(Usuario u) => new()
    {
        Id = u.Id,
        Nombres = u.Nombres,
        Apellidos = u.Apellidos,
        Email = u.Email,
        Telefono = u.Telefono,
        RolId = u.RolId,
        Rol = u.Rol?.Nombre ?? string.Empty,
        PacienteId = u.PacienteId,
        MedicoId = u.MedicoId,
        Activo = u.Activo,
        FechaRegistro = u.FechaRegistro,
        UltimoAcceso = u.UltimoAcceso
    };
}
