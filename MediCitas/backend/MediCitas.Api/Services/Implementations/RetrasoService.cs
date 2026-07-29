using System.Text.Json;
using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Services.Implementations;

/// <summary>
/// Aviso de atrasos. Al reportar un retraso se notifica a los pacientes que
/// todavía están en cola ese día y se les ofrece esperar o reprogramar,
/// que es el flujo descrito en el documento funcional del proyecto.
/// </summary>
public class RetrasoService : IRetrasoService
{
    private readonly IRepositorio<RetrasoMedico> _repo;
    private readonly IRepositorio<Medico> _medicos;
    private readonly IRepositorio<ConversacionChatbot> _conversaciones;
    private readonly ICitaRepositorio _citas;
    private readonly INotificacionService _notificaciones;
    private readonly ICitaService _citaService;
    private readonly IBitacoraService _bitacora;

    public RetrasoService(IRepositorio<RetrasoMedico> repo, IRepositorio<Medico> medicos,
        IRepositorio<ConversacionChatbot> conversaciones, ICitaRepositorio citas,
        INotificacionService notificaciones, ICitaService citaService, IBitacoraService bitacora)
    {
        _repo = repo; _medicos = medicos; _conversaciones = conversaciones; _citas = citas;
        _notificaciones = notificaciones; _citaService = citaService; _bitacora = bitacora;
    }

    public async Task<RetrasoDto> ReportarAsync(ReportarRetrasoRequest request, int? usuarioId)
    {
        var medico = await _medicos.ObtenerPorIdAsync(request.MedicoId)
                     ?? throw new NoEncontradoException("El médico no existe");

        // Solo se avisa a quienes aún no han sido atendidos.
        var enCola = (await _citas.ListarVigentesDelDiaAsync(medico.Id, request.Fecha))
            .Where(c => c.FechaHoraInicio >= DateTime.Now && c.EstadoId != (int)EstadoCitaEnum.EnAtencion)
            .ToList();

        var retraso = new RetrasoMedico
        {
            MedicoId = medico.Id,
            Fecha = request.Fecha,
            MinutosRetraso = request.MinutosRetraso,
            Motivo = request.Motivo,
            PacientesNotificados = enCola.Count,
            RegistradoPorUsuarioId = usuarioId
        };

        await _repo.AgregarAsync(retraso);
        await _repo.GuardarCambiosAsync();

        foreach (var cita in enCola)
        {
            var nuevaHora = cita.FechaHoraInicio.AddMinutes(request.MinutosRetraso);

            var mensaje =
                $"Aviso: {medico.NombreCompleto} lleva un retraso aproximado de {request.MinutosRetraso} minutos. " +
                $"Su cita {cita.Codigo} de las {cita.FechaHoraInicio:hh:mm tt} se atendería alrededor de las {nuevaHora:hh:mm tt}. " +
                "Responda 1 para esperar o 2 para reprogramar su cita.";

            await _notificaciones.RegistrarAsync(cita.PacienteId, cita.Id, TipoNotificacion.Retraso, mensaje);

            // Se deja la conversación esperando la decisión del paciente (1 esperar / 2 reprogramar).
            await PrepararDecisionAsync(cita);
        }

        await _bitacora.RegistrarAsync("ReportarRetraso", "Medico", medico.Id.ToString(),
            $"{request.MinutosRetraso} min, {enCola.Count} pacientes notificados", usuarioId);

        return (await ListarAsync(request.Fecha, medico.Id)).First(r => r.Id == retraso.Id);
    }

    public async Task<List<RetrasoDto>> ListarAsync(DateOnly? fecha, int? medicoId)
    {
        var consulta = _repo.Query().Include(r => r.Medico).AsNoTracking();

        if (fecha.HasValue) consulta = consulta.Where(r => r.Fecha == fecha.Value);
        if (medicoId.HasValue) consulta = consulta.Where(r => r.MedicoId == medicoId.Value);

        var lista = await consulta.OrderByDescending(r => r.FechaRegistro).Take(200).ToListAsync();

        return lista.Select(r => new RetrasoDto
        {
            Id = r.Id,
            MedicoId = r.MedicoId,
            Medico = r.Medico?.NombreCompleto ?? string.Empty,
            Fecha = r.Fecha,
            MinutosRetraso = r.MinutosRetraso,
            Motivo = r.Motivo,
            PacientesNotificados = r.PacientesNotificados,
            FechaRegistro = r.FechaRegistro,
            Activo = r.Activo
        }).ToList();
    }

    /// <summary>Registra la decisión del paciente: 1 espera, 2 reprograma.</summary>
    public async Task<CitaDto> ResponderAsync(RespuestaRetrasoRequest request, int? usuarioId)
    {
        var cita = await _citas.ObtenerCompletaAsync(request.CitaId)
                   ?? throw new NoEncontradoException("La cita no existe");

        if (request.Decision == 1)
        {
            await _notificaciones.RegistrarAsync(cita.PacienteId, cita.Id, TipoNotificacion.Retraso,
                $"Gracias por esperar. Le avisaremos cuando {cita.Medico?.NombreCompleto} pueda atenderle.");

            await _bitacora.RegistrarAsync("RespuestaRetraso", "Cita", cita.Codigo, "Paciente espera", usuarioId);
            return Mapeos.ACitaDto(cita);
        }

        if (request.NuevaFechaHoraInicio is null)
            throw new ValidacionException("Indique la nueva fecha y hora para reprogramar la cita");

        await _bitacora.RegistrarAsync("RespuestaRetraso", "Cita", cita.Codigo, "Paciente reprograma", usuarioId);

        return await _citaService.ReprogramarAsync(cita.Id,
            new ReprogramarCitaRequest
            {
                NuevaFechaHoraInicio = request.NuevaFechaHoraInicio.Value,
                Motivo = "Reprogramación por retraso del médico"
            }, usuarioId, omitirAnticipacion: true);
    }

    /// <summary>Coloca la conversación de WhatsApp en el estado que espera la decisión del paciente.</summary>
    private async Task PrepararDecisionAsync(Cita cita)
    {
        var telefono = cita.Paciente?.Telefono;
        if (string.IsNullOrWhiteSpace(telefono)) return;

        var conversacion = await _conversaciones.PrimeroAsync(c => c.Telefono == telefono);

        if (conversacion is null)
        {
            conversacion = new ConversacionChatbot { Telefono = telefono, PacienteId = cita.PacienteId };
            await _conversaciones.AgregarAsync(conversacion);
        }

        conversacion.EstadoFlujo = "RETRASO_DECISION";
        conversacion.Contexto = JsonSerializer.Serialize(new ContextoChatbot { CitaId = cita.Id });
        conversacion.UltimaInteraccion = DateTime.Now;

        await _conversaciones.GuardarCambiosAsync();
    }

    public async Task DesactivarAsync(int id)
    {
        var retraso = await _repo.ObtenerPorIdAsync(id)
                      ?? throw new NoEncontradoException("El retraso no existe");

        retraso.Activo = false;
        await _repo.GuardarCambiosAsync();
    }
}
