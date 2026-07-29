using MediCitas.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Data;

/// <summary>
/// Contexto de Entity Framework Core. La configuración se declara con Fluent API
/// para que el modelo generado coincida exactamente con los scripts SQL del proyecto.
/// </summary>
public class MediCitasDbContext : DbContext
{
    public MediCitasDbContext(DbContextOptions<MediCitasDbContext> options) : base(options) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<HorarioMedico> HorariosMedico => Set<HorarioMedico>();
    public DbSet<BloqueoAgenda> BloqueosAgenda => Set<BloqueoAgenda>();
    public DbSet<EstadoCita> EstadosCita => Set<EstadoCita>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<RetrasoMedico> RetrasosMedico => Set<RetrasoMedico>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<ConversacionChatbot> ConversacionesChatbot => Set<ConversacionChatbot>();
    public DbSet<MensajeChatbot> MensajesChatbot => Set<MensajeChatbot>();
    public DbSet<ConfiguracionItem> Configuracion => Set<ConfiguracionItem>();
    public DbSet<EventoBitacora> BitacoraEventos => Set<EventoBitacora>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ---------------- Roles ----------------
        b.Entity<Rol>(e =>
        {
            e.ToTable("Roles");
            e.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(200);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        // ---------------- Especialidades ----------------
        b.Entity<Especialidad>(e =>
        {
            e.ToTable("Especialidades");
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(300);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        // ---------------- Medicos ----------------
        b.Entity<Medico>(e =>
        {
            e.ToTable("Medicos");
            e.Ignore(x => x.NombreCompleto);
            e.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
            e.Property(x => x.Colegiado).HasMaxLength(20).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(150);
            e.HasIndex(x => x.Colegiado).IsUnique();
            e.HasOne(x => x.Especialidad).WithMany(x => x.Medicos)
             .HasForeignKey(x => x.EspecialidadId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Pacientes ----------------
        b.Entity<Paciente>(e =>
        {
            e.ToTable("Pacientes");
            e.Ignore(x => x.NombreCompleto);
            e.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.DocumentoIdentidad).HasMaxLength(20);
            e.Property(x => x.Genero).HasMaxLength(1).IsFixedLength();
            e.Property(x => x.Direccion).HasMaxLength(250);
            e.HasIndex(x => x.Telefono).IsUnique();
        });

        // ---------------- Usuarios ----------------
        b.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.Ignore(x => x.NombreCompleto);
            e.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
            e.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Rol).WithMany(x => x.Usuarios)
             .HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Paciente).WithMany()
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Medico).WithMany()
             .HasForeignKey(x => x.MedicoId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Horarios ----------------
        b.Entity<HorarioMedico>(e =>
        {
            e.ToTable("HorariosMedico");
            e.HasOne(x => x.Medico).WithMany(x => x.Horarios)
             .HasForeignKey(x => x.MedicoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MedicoId, x.DiaSemana, x.HoraInicio }).IsUnique();
        });

        // ---------------- Bloqueos ----------------
        b.Entity<BloqueoAgenda>(e =>
        {
            e.ToTable("BloqueosAgenda");
            e.Property(x => x.Tipo).HasConversion<byte>();
            e.Property(x => x.Motivo).HasMaxLength(200);
            e.HasOne(x => x.Medico).WithMany(x => x.Bloqueos)
             .HasForeignKey(x => x.MedicoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.FechaInicio, x.FechaFin });
        });

        // ---------------- Estados de cita ----------------
        b.Entity<EstadoCita>(e =>
        {
            e.ToTable("EstadosCita");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(30).IsRequired();
            e.Property(x => x.Color).HasMaxLength(9).IsRequired();
        });

        // ---------------- Citas ----------------
        b.Entity<Cita>(e =>
        {
            e.ToTable("Citas");
            e.Ignore(x => x.EstaVigente);
            e.Property(x => x.Codigo).HasMaxLength(12).IsRequired();
            e.Property(x => x.Motivo).HasMaxLength(300);
            e.Property(x => x.Notas).HasMaxLength(500);
            e.Property(x => x.MotivoCancelacion).HasMaxLength(300);
            e.Property(x => x.TipoConsulta).HasConversion<byte>();
            e.Property(x => x.CanalOrigen).HasConversion<byte>();
            e.HasIndex(x => x.Codigo).IsUnique();
            e.HasIndex(x => x.FechaHoraInicio);

            // Regla de negocio: un médico no puede tener dos citas vigentes a la misma hora.
            e.HasIndex(x => new { x.MedicoId, x.FechaHoraInicio })
             .IsUnique()
             .HasDatabaseName("UX_Citas_NoDobleReserva")
             .HasFilter("[EstadoId] IN (1,2,3)");

            e.HasOne(x => x.Paciente).WithMany(x => x.Citas)
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Medico).WithMany(x => x.Citas)
             .HasForeignKey(x => x.MedicoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Especialidad).WithMany()
             .HasForeignKey(x => x.EspecialidadId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Estado).WithMany(x => x.Citas)
             .HasForeignKey(x => x.EstadoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CitaOriginal).WithMany()
             .HasForeignKey(x => x.CitaOriginalId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Retrasos ----------------
        b.Entity<RetrasoMedico>(e =>
        {
            e.ToTable("RetrasosMedico");
            e.Property(x => x.Motivo).HasMaxLength(250);
            e.HasOne(x => x.Medico).WithMany()
             .HasForeignKey(x => x.MedicoId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Notificaciones ----------------
        b.Entity<Notificacion>(e =>
        {
            e.ToTable("Notificaciones");
            e.Property(x => x.Tipo).HasConversion<byte>();
            e.Property(x => x.Estado).HasConversion<byte>();
            e.Property(x => x.Mensaje).HasMaxLength(1000).IsRequired();
            e.Property(x => x.RespuestaPaciente).HasMaxLength(200);
            e.HasIndex(x => new { x.Estado, x.FechaProgramada });
            e.HasOne(x => x.Paciente).WithMany(x => x.Notificaciones)
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Cita).WithMany()
             .HasForeignKey(x => x.CitaId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Chatbot ----------------
        b.Entity<ConversacionChatbot>(e =>
        {
            e.ToTable("ConversacionesChatbot");
            e.Property(x => x.Telefono).HasMaxLength(20).IsRequired();
            e.Property(x => x.EstadoFlujo).HasMaxLength(60).IsRequired();
            e.HasIndex(x => x.Telefono).IsUnique();
            e.HasOne(x => x.Paciente).WithMany()
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MensajeChatbot>(e =>
        {
            e.ToTable("MensajesChatbot");
            e.Property(x => x.Direccion).HasConversion<byte>();
            e.Property(x => x.Contenido).HasMaxLength(2000).IsRequired();
            e.Property(x => x.EstadoFlujo).HasMaxLength(60);
            e.HasOne(x => x.Conversacion).WithMany(x => x.Mensajes)
             .HasForeignKey(x => x.ConversacionId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- Configuración y bitácora ----------------
        b.Entity<ConfiguracionItem>(e =>
        {
            e.ToTable("Configuracion");
            e.Property(x => x.Clave).HasMaxLength(60).IsRequired();
            e.Property(x => x.Valor).HasMaxLength(300).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(250);
            e.HasIndex(x => x.Clave).IsUnique();
        });

        b.Entity<EventoBitacora>(e =>
        {
            e.ToTable("BitacoraEventos");
            e.Property(x => x.Accion).HasMaxLength(60).IsRequired();
            e.Property(x => x.Entidad).HasMaxLength(60).IsRequired();
            e.Property(x => x.EntidadId).HasMaxLength(30);
            e.Property(x => x.Detalle).HasMaxLength(1000);
            e.HasIndex(x => x.FechaHora);
            e.HasOne(x => x.Usuario).WithMany()
             .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
