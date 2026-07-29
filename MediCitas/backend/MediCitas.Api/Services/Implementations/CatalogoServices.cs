using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.DTOs.Catalogos;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Services.Implementations;

// ============================ ESPECIALIDADES ============================
public class EspecialidadService : IEspecialidadService
{
    private readonly IRepositorio<Especialidad> _repo;
    private readonly IRepositorio<Medico> _medicos;
    private readonly IBitacoraService _bitacora;
    private readonly IUsuarioActual _actual;

    public EspecialidadService(IRepositorio<Especialidad> repo, IRepositorio<Medico> medicos,
        IBitacoraService bitacora, IUsuarioActual actual)
    {
        _repo = repo; _medicos = medicos; _bitacora = bitacora; _actual = actual;
    }

    public async Task<List<EspecialidadDto>> ListarAsync(bool soloActivas = false)
    {
        var consulta = _repo.Query().AsNoTracking();
        if (soloActivas) consulta = consulta.Where(e => e.Activo);

        return await consulta.OrderBy(e => e.Nombre).Select(e => new EspecialidadDto
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Descripcion = e.Descripcion,
            Activo = e.Activo,
            TotalMedicos = e.Medicos.Count(m => m.Activo)
        }).ToListAsync();
    }

    public async Task<EspecialidadDto> ObtenerAsync(int id)
    {
        var e = await Buscar(id);
        return new EspecialidadDto
        {
            Id = e.Id, Nombre = e.Nombre, Descripcion = e.Descripcion, Activo = e.Activo,
            TotalMedicos = await _medicos.ContarAsync(m => m.EspecialidadId == id && m.Activo)
        };
    }

    public async Task<EspecialidadDto> CrearAsync(EspecialidadRequest request)
    {
        var nombre = request.Nombre.Trim();
        if (await _repo.ExisteAsync(e => e.Nombre == nombre))
            throw new ReglaNegocioException("Ya existe una especialidad con ese nombre");

        var entidad = new Especialidad { Nombre = nombre, Descripcion = request.Descripcion, Activo = request.Activo };
        await _repo.AgregarAsync(entidad);
        await _repo.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Crear", "Especialidad", entidad.Id.ToString(), nombre, _actual.Id);

        return await ObtenerAsync(entidad.Id);
    }

    public async Task<EspecialidadDto> ActualizarAsync(int id, EspecialidadRequest request)
    {
        var entidad = await Buscar(id);
        var nombre = request.Nombre.Trim();

        if (await _repo.ExisteAsync(e => e.Nombre == nombre && e.Id != id))
            throw new ReglaNegocioException("Ya existe otra especialidad con ese nombre");

        entidad.Nombre = nombre;
        entidad.Descripcion = request.Descripcion;
        entidad.Activo = request.Activo;
        await _repo.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Actualizar", "Especialidad", id.ToString(), nombre, _actual.Id);

        return await ObtenerAsync(id);
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await Buscar(id);

        if (await _medicos.ExisteAsync(m => m.EspecialidadId == id))
            throw new ReglaNegocioException("No se puede eliminar: hay médicos asignados a esta especialidad. Desactívela en su lugar.");

        _repo.Eliminar(entidad);
        await _repo.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Eliminar", "Especialidad", id.ToString(), entidad.Nombre, _actual.Id);
    }

    private async Task<Especialidad> Buscar(int id)
        => await _repo.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("La especialidad no existe");
}

// ============================ MÉDICOS ============================
public class MedicoService : IMedicoService
{
    private readonly IRepositorio<Medico> _repo;
    private readonly ICitaRepositorio _citas;
    private readonly IBitacoraService _bitacora;
    private readonly IUsuarioActual _actual;

    public MedicoService(IRepositorio<Medico> repo, ICitaRepositorio citas,
        IBitacoraService bitacora, IUsuarioActual actual)
    {
        _repo = repo; _citas = citas; _bitacora = bitacora; _actual = actual;
    }

    public async Task<List<MedicoDto>> ListarAsync(int? especialidadId = null, bool soloActivos = false)
    {
        var consulta = _repo.Query().Include(m => m.Especialidad).AsNoTracking();
        if (especialidadId.HasValue) consulta = consulta.Where(m => m.EspecialidadId == especialidadId.Value);
        if (soloActivos) consulta = consulta.Where(m => m.Activo);

        return await consulta.OrderBy(m => m.Apellidos).Select(m => Mapear(m)).ToListAsync();
    }

    public async Task<MedicoDto> ObtenerAsync(int id) => Mapear(await Buscar(id));

    public async Task<MedicoDto> CrearAsync(MedicoRequest request)
    {
        var colegiado = request.Colegiado.Trim();
        if (await _repo.ExisteAsync(m => m.Colegiado == colegiado))
            throw new ReglaNegocioException("Ya existe un médico con ese número de colegiado");

        var medico = new Medico
        {
            Nombres = request.Nombres.Trim(),
            Apellidos = request.Apellidos.Trim(),
            Colegiado = colegiado,
            EspecialidadId = request.EspecialidadId,
            Telefono = request.Telefono,
            Email = request.Email,
            DuracionCitaMinutos = request.DuracionCitaMinutos,
            Activo = request.Activo
        };

        await _repo.AgregarAsync(medico);
        await _repo.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Crear", "Medico", medico.Id.ToString(), medico.NombreCompleto, _actual.Id);

        return await ObtenerAsync(medico.Id);
    }

    public async Task<MedicoDto> ActualizarAsync(int id, MedicoRequest request)
    {
        var medico = await Buscar(id);
        var colegiado = request.Colegiado.Trim();

        if (await _repo.ExisteAsync(m => m.Colegiado == colegiado && m.Id != id))
            throw new ReglaNegocioException("Ya existe otro médico con ese número de colegiado");

        medico.Nombres = request.Nombres.Trim();
        medico.Apellidos = request.Apellidos.Trim();
        medico.Colegiado = colegiado;
        medico.EspecialidadId = request.EspecialidadId;
        medico.Telefono = request.Telefono;
        medico.Email = request.Email;
        medico.DuracionCitaMinutos = request.DuracionCitaMinutos;
        medico.Activo = request.Activo;

        await _repo.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Actualizar", "Medico", id.ToString(), medico.NombreCompleto, _actual.Id);

        return await ObtenerAsync(id);
    }

    public async Task EliminarAsync(int id)
    {
        var medico = await Buscar(id);

        if (await _citas.ExisteAsync(c => c.MedicoId == id))
            throw new ReglaNegocioException("No se puede eliminar: el médico tiene citas registradas. Desactívelo en su lugar.");

        _repo.Eliminar(medico);
        await _repo.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Eliminar", "Medico", id.ToString(), medico.NombreCompleto, _actual.Id);
    }

    private async Task<Medico> Buscar(int id)
        => await _repo.Query().Include(m => m.Especialidad).FirstOrDefaultAsync(m => m.Id == id)
           ?? throw new NoEncontradoException("El médico no existe");

    private static MedicoDto Mapear(Medico m) => new()
    {
        Id = m.Id,
        Nombres = m.Nombres,
        Apellidos = m.Apellidos,
        Colegiado = m.Colegiado,
        EspecialidadId = m.EspecialidadId,
        Especialidad = m.Especialidad != null ? m.Especialidad.Nombre : string.Empty,
        Telefono = m.Telefono,
        Email = m.Email,
        DuracionCitaMinutos = m.DuracionCitaMinutos,
        Activo = m.Activo
    };
}

// ============================ PACIENTES ============================
public class PacienteService : IPacienteService
{
    private readonly IRepositorio<Paciente> _repo;
    private readonly ICitaRepositorio _citas;
    private readonly IBitacoraService _bitacora;
    private readonly IUsuarioActual _actual;

    public PacienteService(IRepositorio<Paciente> repo, ICitaRepositorio citas,
        IBitacoraService bitacora, IUsuarioActual actual)
    {
        _repo = repo; _citas = citas; _bitacora = bitacora; _actual = actual;
    }

    public async Task<List<PacienteDto>> ListarAsync(string? busqueda = null, int? medicoId = null)
    {
        var consulta = _repo.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(p => p.Nombres.Contains(texto)
                                        || p.Apellidos.Contains(texto)
                                        || p.Telefono.Contains(texto));
        }

        // Médico: solo pacientes que tienen al menos una cita con ese médico.
        if (medicoId.HasValue)
            consulta = consulta.Where(p => p.Citas.Any(c => c.MedicoId == medicoId.Value));

        return await consulta.OrderBy(p => p.Apellidos).Select(p => new PacienteDto
        {
            Id = p.Id, Nombres = p.Nombres, Apellidos = p.Apellidos, Telefono = p.Telefono,
            Email = p.Email, DocumentoIdentidad = p.DocumentoIdentidad, FechaNacimiento = p.FechaNacimiento,
            Genero = p.Genero, Direccion = p.Direccion, EsNuevo = p.EsNuevo, Activo = p.Activo,
            FechaRegistro = p.FechaRegistro, TotalCitas = p.Citas.Count
        }).ToListAsync();
    }

    public async Task<PacienteDto> ObtenerAsync(int id)
    {
        var p = await Buscar(id);
        return await MapearAsync(p);
    }

    public async Task<PacienteDto?> ObtenerPorTelefonoAsync(string telefono)
    {
        var normalizado = AuthService.NormalizarTelefono(telefono);
        var p = await _repo.PrimeroAsync(x => x.Telefono == normalizado);
        return p is null ? null : await MapearAsync(p);
    }

    public async Task<PacienteDto> CrearAsync(PacienteRequest request)
    {
        var telefono = AuthService.NormalizarTelefono(request.Telefono);
        if (await _repo.ExisteAsync(p => p.Telefono == telefono))
            throw new ReglaNegocioException("Ya existe un paciente registrado con ese número de teléfono");

        var paciente = new Paciente
        {
            Nombres = request.Nombres.Trim(),
            Apellidos = request.Apellidos.Trim(),
            Telefono = telefono,
            Email = request.Email,
            DocumentoIdentidad = request.DocumentoIdentidad,
            FechaNacimiento = request.FechaNacimiento,
            Genero = request.Genero,
            Direccion = request.Direccion,
            Activo = request.Activo
        };

        await _repo.AgregarAsync(paciente);
        await _repo.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Crear", "Paciente", paciente.Id.ToString(), paciente.NombreCompleto, _actual.Id);

        return await MapearAsync(paciente);
    }

    public async Task<PacienteDto> ActualizarAsync(int id, PacienteRequest request)
    {
        var paciente = await Buscar(id);
        var telefono = AuthService.NormalizarTelefono(request.Telefono);

        if (await _repo.ExisteAsync(p => p.Telefono == telefono && p.Id != id))
            throw new ReglaNegocioException("Ya existe otro paciente con ese número de teléfono");

        paciente.Nombres = request.Nombres.Trim();
        paciente.Apellidos = request.Apellidos.Trim();
        paciente.Telefono = telefono;
        paciente.Email = request.Email;
        paciente.DocumentoIdentidad = request.DocumentoIdentidad;
        paciente.FechaNacimiento = request.FechaNacimiento;
        paciente.Genero = request.Genero;
        paciente.Direccion = request.Direccion;
        paciente.Activo = request.Activo;

        await _repo.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Actualizar", "Paciente", id.ToString(), paciente.NombreCompleto, _actual.Id);

        return await MapearAsync(paciente);
    }

    public async Task EliminarAsync(int id)
    {
        var paciente = await Buscar(id);

        if (await _citas.ExisteAsync(c => c.PacienteId == id))
            throw new ReglaNegocioException("No se puede eliminar: el paciente tiene citas registradas. Desactívelo en su lugar.");

        _repo.Eliminar(paciente);
        await _repo.GuardarCambiosAsync();
        await _bitacora.RegistrarAsync("Eliminar", "Paciente", id.ToString(), paciente.NombreCompleto, _actual.Id);
    }

    private async Task<Paciente> Buscar(int id)
        => await _repo.ObtenerPorIdAsync(id) ?? throw new NoEncontradoException("El paciente no existe");

    private async Task<PacienteDto> MapearAsync(Paciente p) => new()
    {
        Id = p.Id, Nombres = p.Nombres, Apellidos = p.Apellidos, Telefono = p.Telefono, Email = p.Email,
        DocumentoIdentidad = p.DocumentoIdentidad, FechaNacimiento = p.FechaNacimiento, Genero = p.Genero,
        Direccion = p.Direccion, EsNuevo = p.EsNuevo, Activo = p.Activo, FechaRegistro = p.FechaRegistro,
        TotalCitas = await _citas.ContarAsync(c => c.PacienteId == p.Id)
    };
}