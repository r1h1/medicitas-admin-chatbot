using MediCitas.Api.Domain.Entities;
using MediCitas.Api.DTOs.Auth;
using MediCitas.Api.DTOs.Catalogos;
using MediCitas.Api.DTOs.Chatbot;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.DTOs.Dashboard;

namespace MediCitas.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<UsuarioDto> RegistrarPacienteAsync(RegistroPacienteRequest request);
    Task<UsuarioDto> ObtenerPerfilAsync(int usuarioId);
    Task CambiarPasswordAsync(int usuarioId, CambiarPasswordRequest request);
}

public interface IUsuarioService
{
    Task<List<UsuarioDto>> ListarAsync();
    Task<UsuarioDto> ObtenerAsync(int id);
    Task<UsuarioDto> CrearAsync(CrearUsuarioRequest request);
    Task<UsuarioDto> ActualizarAsync(int id, ActualizarUsuarioRequest request);
    Task DesactivarAsync(int id);
}

public interface IEspecialidadService
{
    Task<List<EspecialidadDto>> ListarAsync(bool soloActivas = false);
    Task<EspecialidadDto> ObtenerAsync(int id);
    Task<EspecialidadDto> CrearAsync(EspecialidadRequest request);
    Task<EspecialidadDto> ActualizarAsync(int id, EspecialidadRequest request);
    Task EliminarAsync(int id);
}

public interface IMedicoService
{
    Task<List<MedicoDto>> ListarAsync(int? especialidadId = null, bool soloActivos = false);
    Task<MedicoDto> ObtenerAsync(int id);
    Task<MedicoDto> CrearAsync(MedicoRequest request);
    Task<MedicoDto> ActualizarAsync(int id, MedicoRequest request);
    Task EliminarAsync(int id);
}

public interface IPacienteService
{
    Task<List<PacienteDto>> ListarAsync(string? busqueda = null, int? medicoId = null);
    Task<PacienteDto> ObtenerAsync(int id);
    Task<PacienteDto?> ObtenerPorTelefonoAsync(string telefono);
    Task<PacienteDto> CrearAsync(PacienteRequest request);
    Task<PacienteDto> ActualizarAsync(int id, PacienteRequest request);
    Task EliminarAsync(int id);
}

public interface IAgendaService
{
    Task<List<HorarioDto>> ListarHorariosAsync(int? medicoId = null);
    Task<HorarioDto> CrearHorarioAsync(HorarioRequest request);
    Task<HorarioDto> ActualizarHorarioAsync(int id, HorarioRequest request);
    Task EliminarHorarioAsync(int id);

    Task<List<BloqueoDto>> ListarBloqueosAsync(int? medicoId = null);
    Task<BloqueoDto> CrearBloqueoAsync(BloqueoRequest request);
    Task EliminarBloqueoAsync(int id);
}

public interface IDisponibilidadService
{
    Task<DisponibilidadDiaDto> ObtenerDiaAsync(int medicoId, DateOnly fecha, int? excluirCitaId = null);
    Task<List<DisponibilidadDiaDto>> ObtenerRangoAsync(int medicoId, DateOnly desde, DateOnly hasta);
    Task<List<DateOnly>> ProximasFechasConCupoAsync(int medicoId, int cantidadDias = 5);
    Task ValidarSlotAsync(Medico medico, DateTime inicio, DateTime fin, int? excluirCitaId = null);
}

public interface ICitaService
{
    Task<List<CitaDto>> ListarAsync(DateOnly? desde, DateOnly? hasta, int? medicoId, int? pacienteId, int? estadoId);
    Task<CitaDto> ObtenerAsync(int id);
    Task<CitaDto> CrearAsync(CrearCitaRequest request, int? usuarioId);
    Task<CitaDto> ReprogramarAsync(int id, ReprogramarCitaRequest request, int? usuarioId, bool omitirAnticipacion = false);
    Task<CitaDto> CancelarAsync(int id, CancelarCitaRequest request, int? usuarioId);
    Task<CitaDto> ConfirmarAsync(int id, int? usuarioId);
    Task<CitaDto> CambiarEstadoAsync(int id, CambiarEstadoRequest request, int? usuarioId);
    Task<List<CitaDto>> HistorialPacienteAsync(int pacienteId);
    Task<List<CitaDto>> AgendaDelDiaAsync(int medicoId, DateOnly fecha);
}

public interface IRetrasoService
{
    Task<RetrasoDto> ReportarAsync(ReportarRetrasoRequest request, int? usuarioId);
    Task<List<RetrasoDto>> ListarAsync(DateOnly? fecha, int? medicoId);
    Task<CitaDto> ResponderAsync(RespuestaRetrasoRequest request, int? usuarioId);
    Task DesactivarAsync(int id);
}

public interface INotificacionService
{
    Task ProgramarRecordatoriosAsync(Cita cita);
    Task CancelarPendientesAsync(int citaId);
    Task<Notificacion> RegistrarAsync(int pacienteId, int? citaId, Domain.Enums.TipoNotificacion tipo,
        string mensaje, DateTime? fechaProgramada = null);
    Task<int> ProcesarPendientesAsync(CancellationToken token = default);
    Task<List<NotificacionDto>> ListarAsync(int? pacienteId, Domain.Enums.EstadoNotificacion? estado);
}

public interface IWhatsAppService
{
    /// <summary>Envía el mensaje al paciente. En modo simulación solo lo registra.</summary>
    Task<bool> EnviarAsync(string telefono, string mensaje);
}

public interface IChatbotService
{
    Task<RespuestaChatbotDto> ProcesarMensajeAsync(MensajeEntranteDto mensaje);
    Task<List<ConversacionDto>> ListarConversacionesAsync();
    Task<List<MensajeDto>> ListarMensajesAsync(int conversacionId);
    Task ReiniciarConversacionAsync(string telefono);
}

public interface IDashboardService
{
    Task<DashboardDto> ObtenerAsync(DateOnly? fecha = null);
    Task<ReporteGeneralDto> ReporteGeneralAsync(DateOnly desde, DateOnly hasta);
}

public interface IConfiguracionService
{
    Task<List<ConfiguracionDto>> ListarAsync();
    Task<string> ObtenerValorAsync(string clave, string valorPorDefecto = "");
    Task<int> ObtenerEnteroAsync(string clave, int valorPorDefecto);
    Task ActualizarAsync(string clave, string valor);
}

public interface IBitacoraService
{
    Task RegistrarAsync(string accion, string entidad, string? entidadId, string? detalle, int? usuarioId);
    Task<List<EventoBitacora>> ListarAsync(int cantidad = 100);
}