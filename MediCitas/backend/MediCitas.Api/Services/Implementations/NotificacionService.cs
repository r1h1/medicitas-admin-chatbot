using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MediCitas.Api.Services.Implementations;

/// <summary>
/// Programa y despacha los mensajes hacia el paciente. Cada mensaje queda
/// registrado en la tabla Notificaciones antes de intentar enviarse, de manera
/// que el envío puede reintentarse y el panel puede auditarlo.
/// </summary>
public class NotificacionService : INotificacionService
{
    private readonly IRepositorio<Notificacion> _repo;
    private readonly IWhatsAppService _whatsapp;
    private readonly IConfiguracionService _config;
    private readonly NotificacionesSettings _opciones;
    private readonly ILogger<NotificacionService> _logger;

    public NotificacionService(IRepositorio<Notificacion> repo, IWhatsAppService whatsapp,
        IConfiguracionService config, IOptions<NotificacionesSettings> opciones,
        ILogger<NotificacionService> logger)
    {
        _repo = repo; _whatsapp = whatsapp; _config = config;
        _opciones = opciones.Value; _logger = logger;
    }

    /// <summary>Crea los recordatorios automáticos de una cita según la configuración.</summary>
    public async Task ProgramarRecordatoriosAsync(Cita cita)
    {
        var horasPrimero = await _config.ObtenerEnteroAsync("RecordatorioHorasAntes", 24);
        var horasSegundo = await _config.ObtenerEnteroAsync("RecordatorioHorasAntes2", 2);

        foreach (var horas in new[] { horasPrimero, horasSegundo }.Distinct())
        {
            var programada = cita.FechaHoraInicio.AddHours(-horas);
            if (programada <= DateTime.Now) continue;

            var texto = horas >= 12
                ? $"Recordatorio: mañana tiene su cita {cita.Codigo} el {Mapeos.FechaLarga(cita.FechaHoraInicio)} " +
                  $"con {cita.Medico?.NombreCompleto}. Responda CONFIRMAR, REPROGRAMAR o CANCELAR."
                : $"Su cita {cita.Codigo} es hoy a las {cita.FechaHoraInicio:hh:mm tt} con {cita.Medico?.NombreCompleto}. " +
                  "Le esperamos. Responda CANCELAR si no podrá asistir.";

            await RegistrarAsync(cita.PacienteId, cita.Id, TipoNotificacion.Recordatorio, texto, programada);
        }
    }

    /// <summary>Anula los recordatorios que aún no se han enviado de una cita.</summary>
    public async Task CancelarPendientesAsync(int citaId)
    {
        var pendientes = await _repo.Query()
            .Where(n => n.CitaId == citaId
                     && n.Estado == EstadoNotificacion.Pendiente
                     && n.Tipo == TipoNotificacion.Recordatorio)
            .ToListAsync();

        foreach (var n in pendientes) _repo.Eliminar(n);
        if (pendientes.Count > 0) await _repo.GuardarCambiosAsync();
    }

    public async Task<Notificacion> RegistrarAsync(int pacienteId, int? citaId, TipoNotificacion tipo,
        string mensaje, DateTime? fechaProgramada = null)
    {
        var notificacion = new Notificacion
        {
            PacienteId = pacienteId,
            CitaId = citaId,
            Tipo = tipo,
            Mensaje = mensaje,
            FechaProgramada = fechaProgramada ?? DateTime.Now,
            Estado = EstadoNotificacion.Pendiente
        };

        await _repo.AgregarAsync(notificacion);
        await _repo.GuardarCambiosAsync();

        return notificacion;
    }

    /// <summary>Envía las notificaciones cuya hora programada ya llegó. Lo invoca el worker en segundo plano.</summary>
    public async Task<int> ProcesarPendientesAsync(CancellationToken token = default)
    {
        var ahora = DateTime.Now;

        var pendientes = await _repo.Query()
            .Include(n => n.Paciente)
            .Where(n => n.Estado == EstadoNotificacion.Pendiente
                     && n.FechaProgramada <= ahora
                     && n.Intentos < _opciones.MaximoIntentos)
            .OrderBy(n => n.FechaProgramada)
            .Take(50)
            .ToListAsync(token);

        var enviadas = 0;

        foreach (var notificacion in pendientes)
        {
            if (token.IsCancellationRequested) break;

            notificacion.Intentos++;

            try
            {
                var exito = await _whatsapp.EnviarAsync(notificacion.Paciente!.Telefono, notificacion.Mensaje);

                notificacion.Estado = exito
                    ? EstadoNotificacion.Enviada
                    : notificacion.Intentos >= _opciones.MaximoIntentos
                        ? EstadoNotificacion.Fallida
                        : EstadoNotificacion.Pendiente;

                if (exito)
                {
                    notificacion.FechaEnvio = DateTime.Now;
                    enviadas++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo al enviar la notificación {Id}", notificacion.Id);
                if (notificacion.Intentos >= _opciones.MaximoIntentos)
                    notificacion.Estado = EstadoNotificacion.Fallida;
            }
        }

        if (pendientes.Count > 0) await _repo.GuardarCambiosAsync();

        return enviadas;
    }

    public async Task<List<NotificacionDto>> ListarAsync(int? pacienteId, EstadoNotificacion? estado)
    {
        var consulta = _repo.Query().Include(n => n.Paciente).Include(n => n.Cita).AsNoTracking();

        if (pacienteId.HasValue) consulta = consulta.Where(n => n.PacienteId == pacienteId.Value);
        if (estado.HasValue) consulta = consulta.Where(n => n.Estado == estado.Value);

        var lista = await consulta.OrderByDescending(n => n.FechaProgramada).Take(300).ToListAsync();

        return lista.Select(n => new NotificacionDto
        {
            Id = n.Id,
            PacienteId = n.PacienteId,
            Paciente = n.Paciente?.NombreCompleto ?? string.Empty,
            CitaId = n.CitaId,
            CitaCodigo = n.Cita?.Codigo,
            Tipo = n.Tipo,
            TipoNombre = n.Tipo.ToString(),
            Mensaje = n.Mensaje,
            Estado = n.Estado,
            EstadoNombre = n.Estado.ToString(),
            FechaProgramada = n.FechaProgramada,
            FechaEnvio = n.FechaEnvio,
            RespuestaPaciente = n.RespuestaPaciente
        }).ToList();
    }
}
