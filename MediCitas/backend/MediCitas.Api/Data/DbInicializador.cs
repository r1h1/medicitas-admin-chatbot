using MediCitas.Api.Common;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Data;

/// <summary>
/// Crea el esquema si no existe e inserta únicamente los datos indispensables:
/// catálogos de roles y estados, parámetros del sistema y el usuario administrador.
/// No genera información de demostración.
/// </summary>
public static class DbInicializador
{
    public const string EmailAdministrador = "admin@medicitas.com";
    private const string PasswordAdministrador = "Admin!$MediCitas14";

    public static async Task InicializarAsync(IServiceProvider proveedor, ILogger logger)
    {
        using var scope = proveedor.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<MediCitasDbContext>();

        try
        {
            // Si el proyecto tiene migraciones se aplican; si no, se crea el esquema
            // cuando la base de datos aún no existe (escenario de desarrollo local).
            // En Somee la base ya existe: allí se ejecutan los scripts de la carpeta database.
            if (contexto.Database.GetMigrations().Any())
                await contexto.Database.MigrateAsync();
            else
                await contexto.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo preparar el esquema automáticamente. Se usará la base de datos existente.");
        }

        // ---------- Roles ----------
        var roles = new (string Nombre, string Descripcion)[]
        {
            (Roles.Administrador, "Acceso total al sistema y a la configuración"),
            (Roles.Recepcionista, "Personal administrativo: gestiona agenda, citas y pacientes"),
            (Roles.Medico, "Médico: ve únicamente sus propias citas y pacientes"),
            ("Paciente", "Reservado para compatibilidad interna con el chatbot")
        };

        foreach (var (nombre, descripcion) in roles)
            if (!await contexto.Roles.AnyAsync(r => r.Nombre == nombre))
                contexto.Roles.Add(new Rol { Nombre = nombre, Descripcion = descripcion });

        // ---------- Estados de cita ----------
        var estados = new (int Id, string Nombre, string Color)[]
        {
            (1, "Pendiente", "#F59E0B"),
            (2, "Confirmada", "#22C55E"),
            (3, "EnAtencion", "#3B82F6"),
            (4, "Completada", "#14B8A6"),
            (5, "Cancelada", "#EF4444"),
            (6, "NoAsistio", "#6B7280"),
            (7, "Reprogramada", "#8B5CF6")
        };

        foreach (var (id, nombre, color) in estados)
            if (!await contexto.EstadosCita.AnyAsync(e => e.Id == id))
                contexto.EstadosCita.Add(new EstadoCita { Id = id, Nombre = nombre, Color = color });

        // ---------- Parámetros ----------
        var parametros = new (string Clave, string Valor, string Descripcion)[]
        {
            ("ClinicaNombre", "Clínica de Especialidades Médicas Integradas Dr(a). Isabella Torselli", "Nombre mostrado en mensajes y reportes"),
            ("ClinicaTelefono", "", "Teléfono de contacto de la clínica"),
            ("ClinicaDireccion", "", "Dirección de la clínica"),
            ("RecordatorioHorasAntes", "24", "Horas de anticipación del primer recordatorio"),
            ("RecordatorioHorasAntes2", "2", "Horas de anticipación del segundo recordatorio"),
            ("CancelacionHorasMinimas", "4", "Horas mínimas antes de la cita para cancelar o reprogramar"),
            ("DiasMaximosAgendamiento", "60", "Máximo de días hacia adelante que se pueden agendar"),
            ("ToleranciaLlegadaMinutos", "15", "Minutos de tolerancia antes de marcar No asistió")
        };

        foreach (var (clave, valor, descripcion) in parametros)
            if (!await contexto.Configuracion.AnyAsync(c => c.Clave == clave))
                contexto.Configuracion.Add(new ConfiguracionItem { Clave = clave, Valor = valor, Descripcion = descripcion });

        await contexto.SaveChangesAsync();

        // ---------- Usuario administrador ----------
        if (!await contexto.Usuarios.AnyAsync(u => u.Email == EmailAdministrador))
        {
            var rolAdmin = await contexto.Roles.FirstAsync(r => r.Nombre == Roles.Administrador);

            contexto.Usuarios.Add(new Usuario
            {
                Nombres = "Admin",
                Apellidos = "Principal",
                Email = EmailAdministrador,
                PasswordHash = PasswordHasher.Hash(PasswordAdministrador),
                RolId = rolAdmin.Id
            });

            await contexto.SaveChangesAsync();
            logger.LogInformation("Usuario administrador creado: {Email}", EmailAdministrador);
        }
    }
}