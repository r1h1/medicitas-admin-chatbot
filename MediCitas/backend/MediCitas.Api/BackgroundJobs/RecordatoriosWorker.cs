using MediCitas.Api.Common;
using MediCitas.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace MediCitas.Api.BackgroundJobs;

/// <summary>
/// Servicio en segundo plano que despacha los recordatorios y avisos programados.
/// Es la pieza que hace automáticos los recordatorios sin depender de que alguien
/// abra el panel. Se ejecuta dentro de la misma aplicación, sin colas externas.
/// </summary>
public class RecordatoriosWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificacionesSettings _opciones;
    private readonly ILogger<RecordatoriosWorker> _logger;

    public RecordatoriosWorker(IServiceScopeFactory scopeFactory,
        IOptions<NotificacionesSettings> opciones, ILogger<RecordatoriosWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _opciones = opciones.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromSeconds(Math.Max(15, _opciones.IntervaloSegundos));
        _logger.LogInformation("Worker de recordatorios iniciado. Revisión cada {Segundos} segundos", intervalo.TotalSeconds);

        // Pequeña espera inicial para que la aplicación termine de levantar.
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificaciones = scope.ServiceProvider.GetRequiredService<INotificacionService>();

                var enviadas = await notificaciones.ProcesarPendientesAsync(stoppingToken);
                if (enviadas > 0) _logger.LogInformation("Se enviaron {Cantidad} notificaciones", enviadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar las notificaciones pendientes");
            }

            await Task.Delay(intervalo, stoppingToken);
        }
    }
}
