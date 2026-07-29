using System.Security.Claims;

namespace MediCitas.Api.Common;

/// <summary>Acceso tipado a los claims del usuario autenticado.</summary>
public interface IUsuarioActual
{
    int? Id { get; }
    string? Email { get; }
    string? Rol { get; }
    int? PacienteId { get; }
    int? MedicoId { get; }
    bool EsPersonal { get; }
    bool EsMedico { get; }
    bool EsPersonalOmedico { get; }
}

public class UsuarioActual : IUsuarioActual
{
    private readonly ClaimsPrincipal? _usuario;

    public UsuarioActual(IHttpContextAccessor accessor) => _usuario = accessor.HttpContext?.User;

    public int? Id => Entero(ClaimTypes.NameIdentifier);
    public string? Email => _usuario?.FindFirst(ClaimTypes.Email)?.Value;
    public string? Rol => _usuario?.FindFirst(ClaimTypes.Role)?.Value;
    public int? PacienteId => Entero("pacienteId");
    public int? MedicoId => Entero("medicoId");
    public bool EsPersonal => Rol is Domain.Enums.Roles.Administrador or Domain.Enums.Roles.Recepcionista;
    public bool EsMedico => Rol == Domain.Enums.Roles.Medico;
    public bool EsPersonalOmedico => EsPersonal || EsMedico;

    private int? Entero(string tipo)
        => int.TryParse(_usuario?.FindFirst(tipo)?.Value, out var valor) ? valor : null;
}