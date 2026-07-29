using System.Globalization;
using System.Text;
using System.Text.Json;
using MediCitas.Api.Common;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Domain.Entities;
using MediCitas.Api.Domain.Enums;
using MediCitas.Api.DTOs.Chatbot;
using MediCitas.Api.DTOs.Citas;
using MediCitas.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MediCitas.Api.Services.Implementations;

/// <summary>Datos temporales que el chatbot guarda entre mensajes.</summary>
public class ContextoChatbot
{
    public int? EspecialidadId { get; set; }
    public int? MedicoId { get; set; }
    public string? Fecha { get; set; }
    public string? Hora { get; set; }
    public int? CitaId { get; set; }
    public bool Reprogramando { get; set; }
    public string? Nombres { get; set; }
    /// <summary>Identificadores que corresponden a las opciones numeradas del último mensaje.</summary>
    public List<int> Opciones { get; set; } = new();
    public List<string> Fechas { get; set; } = new();
}

/// <summary>
/// Chatbot de WhatsApp implementado como máquina de estados con flujos
/// predefinidos. No usa modelos de lenguaje: cada estado interpreta la
/// respuesta del paciente y decide el siguiente paso, tal como delimita
/// el ámbito teórico de la tesis.
/// </summary>
public class ChatbotService : IChatbotService
{
    private const string Menu = "MENU_PRINCIPAL";
    private static readonly CultureInfo Cultura = new("es-GT");

    private readonly IRepositorio<ConversacionChatbot> _conversaciones;
    private readonly IRepositorio<MensajeChatbot> _mensajes;
    private readonly IRepositorio<Paciente> _pacientes;
    private readonly IRepositorio<Especialidad> _especialidades;
    private readonly IRepositorio<Medico> _medicos;
    private readonly ICitaRepositorio _citas;
    private readonly ICitaService _citaService;
    private readonly IDisponibilidadService _disponibilidad;
    private readonly IRepositorio<Notificacion> _notificaciones;

    public ChatbotService(IRepositorio<ConversacionChatbot> conversaciones, IRepositorio<MensajeChatbot> mensajes,
        IRepositorio<Paciente> pacientes, IRepositorio<Especialidad> especialidades, IRepositorio<Medico> medicos,
        ICitaRepositorio citas, ICitaService citaService, IDisponibilidadService disponibilidad,
        IRepositorio<Notificacion> notificaciones)
    {
        _conversaciones = conversaciones; _mensajes = mensajes; _pacientes = pacientes;
        _especialidades = especialidades; _medicos = medicos; _citas = citas;
        _citaService = citaService; _disponibilidad = disponibilidad; _notificaciones = notificaciones;
    }

    public async Task<RespuestaChatbotDto> ProcesarMensajeAsync(MensajeEntranteDto entrada)
    {
        var telefono = AuthService.NormalizarTelefono(entrada.Telefono);
        var texto = entrada.Texto.Trim();

        var conversacion = await ObtenerConversacionAsync(telefono);
        await GuardarMensajeAsync(conversacion, DireccionMensaje.Entrante, texto);

        var contexto = Deserializar(conversacion.Contexto);
        var paciente = conversacion.PacienteId.HasValue
            ? await _pacientes.ObtenerPorIdAsync(conversacion.PacienteId.Value)
            : await _pacientes.PrimeroAsync(p => p.Telefono == telefono);

        if (paciente is not null && conversacion.PacienteId != paciente.Id)
            conversacion.PacienteId = paciente.Id;

        string respuesta;

        // Palabras clave que funcionan desde cualquier punto de la conversación.
        var comando = Normalizar(texto);

        if (comando == "nuevo" || comando == "cambiar")
        {
            // El usuario quiere registrarse con un nuevo nombre (p.ej. cambió de número).
            // Se desvincula la conversación del paciente anterior sin borrarlo.
            conversacion.PacienteId = null;
            conversacion.EstadoFlujo = "REGISTRO_NOMBRE";
            contexto = new ContextoChatbot();
            respuesta = "Entendido. Iniciemos su registro.\n\n¿Cuáles son sus nombres?";
        }
        else if (comando is "menu" or "hola" or "inicio" or "buenas")
        {
            conversacion.EstadoFlujo = Menu;
            contexto = new ContextoChatbot();
            respuesta = MensajeBienvenida(paciente);
        }
        else
        {
            respuesta = await ResolverEstadoAsync(conversacion, contexto, paciente, texto, comando);
        }

        conversacion.Contexto = JsonSerializer.Serialize(contexto);
        conversacion.UltimaInteraccion = DateTime.Now;
        await _conversaciones.GuardarCambiosAsync();

        await GuardarMensajeAsync(conversacion, DireccionMensaje.Saliente, respuesta);

        return new RespuestaChatbotDto
        {
            Telefono = telefono,
            Respuesta = respuesta,
            EstadoFlujo = conversacion.EstadoFlujo
        };
    }

    // ------------------------------------------------------------------
    // Máquina de estados
    // ------------------------------------------------------------------
    private async Task<string> ResolverEstadoAsync(ConversacionChatbot conversacion, ContextoChatbot contexto,
        Paciente? paciente, string texto, string comando)
    {
        switch (conversacion.EstadoFlujo)
        {
            case Menu:
                return await MenuPrincipalAsync(conversacion, contexto, paciente, comando);

            case "REGISTRO_NOMBRE":
                return RegistroNombre(conversacion, contexto, texto);

            case "REGISTRO_APELLIDO":
                return await RegistroApellidoAsync(conversacion, contexto, texto);

            case "AGENDAR_ESPECIALIDAD":
                return await SeleccionEspecialidadAsync(conversacion, contexto, comando);

            case "AGENDAR_MEDICO":
                return await SeleccionMedicoAsync(conversacion, contexto, comando);

            case "AGENDAR_FECHA":
                return await SeleccionFechaAsync(conversacion, contexto, comando);

            case "AGENDAR_HORA":
                return await SeleccionHoraAsync(conversacion, contexto, comando);

            case "AGENDAR_CONFIRMAR":
                return await ConfirmarAgendamientoAsync(conversacion, contexto, paciente, comando);

            case "SELECCION_CITA_CONFIRMAR":
                return await ConfirmarAsistenciaAsync(conversacion, contexto, comando);

            case "SELECCION_CITA_CANCELAR":
                return await CancelarCitaAsync(conversacion, contexto, comando);

            case "SELECCION_CITA_REPROGRAMAR":
                return await IniciarReprogramacionAsync(conversacion, contexto, comando);

            case "RETRASO_DECISION":
                return await DecisionRetrasoAsync(conversacion, contexto, comando);

            default:
                conversacion.EstadoFlujo = Menu;
                return MensajeBienvenida(paciente);
        }
    }

    private async Task<string> MenuPrincipalAsync(ConversacionChatbot conversacion, ContextoChatbot contexto,
        Paciente? paciente, string comando)
    {
        var opcion = comando switch
        {
            "1" or "agendar" => 1,
            "2" or "mis citas" or "citas" => 2,
            "3" or "confirmar" => 3,
            "4" or "reprogramar" => 4,
            "5" or "cancelar" => 5,
            _ => 0
        };

        if (opcion == 0)
            return "No reconocí esa opción.\n\n" + MensajeBienvenida(paciente);

        if (opcion == 1)
        {
            if (paciente is null)
            {
                conversacion.EstadoFlujo = "REGISTRO_NOMBRE";
                return "Para agendar necesito registrarle primero.\n\n¿Cuáles son sus nombres?";
            }

            return await MostrarEspecialidadesAsync(conversacion, contexto);
        }

        if (paciente is null)
            return "Aún no tiene citas registradas con este número. Escriba 1 para agendar la primera.";

        var citas = await _citas.ListarProximasDelPacienteAsync(paciente.Id, 5);

        if (citas.Count == 0)
            return "No tiene citas próximas registradas. Escriba 1 para agendar una nueva.";

        if (opcion == 2)
            return "Sus próximas citas:\n\n" + ListarCitas(citas) + "\n\nEscriba MENU para volver al inicio.";

        contexto.Opciones = citas.Select(c => c.Id).ToList();

        conversacion.EstadoFlujo = opcion switch
        {
            3 => "SELECCION_CITA_CONFIRMAR",
            4 => "SELECCION_CITA_REPROGRAMAR",
            _ => "SELECCION_CITA_CANCELAR"
        };

        var accion = opcion switch { 3 => "confirmar", 4 => "reprogramar", _ => "cancelar" };
        return $"Indique el número de la cita que desea {accion}:\n\n" + ListarCitas(citas);
    }

    private static string RegistroNombre(ConversacionChatbot conversacion, ContextoChatbot contexto, string texto)
    {
        if (texto.Length < 2) return "Escriba sus nombres, por favor.";

        contexto.Nombres = texto;
        conversacion.EstadoFlujo = "REGISTRO_APELLIDO";
        return "Gracias. ¿Cuáles son sus apellidos?";
    }

    private async Task<string> RegistroApellidoAsync(ConversacionChatbot conversacion, ContextoChatbot contexto, string texto)
    {
        if (texto.Length < 2) return "Escriba sus apellidos, por favor.";

        var paciente = new Paciente
        {
            Nombres = contexto.Nombres ?? string.Empty,
            Apellidos = texto,
            Telefono = conversacion.Telefono
        };

        await _pacientes.AgregarAsync(paciente);
        await _pacientes.GuardarCambiosAsync();

        conversacion.PacienteId = paciente.Id;

        return $"Registro completo, {paciente.Nombres}.\n\n" + await MostrarEspecialidadesAsync(conversacion, contexto);
    }

    private async Task<string> MostrarEspecialidadesAsync(ConversacionChatbot conversacion, ContextoChatbot contexto)
    {
        var especialidades = await _especialidades.Query().AsNoTracking()
            .Where(e => e.Activo && e.Medicos.Any(m => m.Activo))
            .OrderBy(e => e.Nombre).ToListAsync();

        if (especialidades.Count == 0)
            return "En este momento no hay especialidades disponibles para agendar. Comuníquese con la clínica.";

        contexto.Opciones = especialidades.Select(e => e.Id).ToList();
        conversacion.EstadoFlujo = "AGENDAR_ESPECIALIDAD";

        var texto = new StringBuilder("¿Qué especialidad necesita?\n\n");
        for (var i = 0; i < especialidades.Count; i++)
            texto.AppendLine($"{i + 1}. {especialidades[i].Nombre}");

        return texto.ToString().TrimEnd();
    }

    private async Task<string> SeleccionEspecialidadAsync(ConversacionChatbot conversacion, ContextoChatbot contexto, string comando)
    {
        if (!TryOpcion(contexto, comando, out var especialidadId))
            return "Responda con el número de la especialidad que aparece en la lista.";

        contexto.EspecialidadId = especialidadId;

        var medicos = await _medicos.Query().AsNoTracking()
            .Where(m => m.Activo && m.EspecialidadId == especialidadId)
            .OrderBy(m => m.Apellidos).ToListAsync();

        if (medicos.Count == 0)
            return "No hay médicos disponibles en esa especialidad. Escriba MENU para volver a empezar.";

        contexto.Opciones = medicos.Select(m => m.Id).ToList();
        conversacion.EstadoFlujo = "AGENDAR_MEDICO";

        var texto = new StringBuilder("Seleccione al médico:\n\n");
        for (var i = 0; i < medicos.Count; i++)
            texto.AppendLine($"{i + 1}. {medicos[i].NombreCompleto}");

        return texto.ToString().TrimEnd();
    }

    private async Task<string> SeleccionMedicoAsync(ConversacionChatbot conversacion, ContextoChatbot contexto, string comando)
    {
        if (!TryOpcion(contexto, comando, out var medicoId))
            return "Responda con el número del médico que aparece en la lista.";

        contexto.MedicoId = medicoId;
        return await MostrarFechasAsync(conversacion, contexto);
    }

    private async Task<string> MostrarFechasAsync(ConversacionChatbot conversacion, ContextoChatbot contexto)
    {
        var fechas = await _disponibilidad.ProximasFechasConCupoAsync(contexto.MedicoId!.Value, 5);

        if (fechas.Count == 0)
            return "Ese médico no tiene cupos disponibles en los próximos días. Escriba MENU para elegir otro.";

        contexto.Fechas = fechas.Select(f => f.ToString("yyyy-MM-dd")).ToList();
        conversacion.EstadoFlujo = "AGENDAR_FECHA";

        var texto = new StringBuilder("Estas son las fechas disponibles más próximas:\n\n");
        for (var i = 0; i < fechas.Count; i++)
            texto.AppendLine($"{i + 1}. {fechas[i].ToDateTime(TimeOnly.MinValue).ToString("dddd dd 'de' MMMM", Cultura)}");

        return texto.ToString().TrimEnd();
    }

    private async Task<string> SeleccionFechaAsync(ConversacionChatbot conversacion, ContextoChatbot contexto, string comando)
    {
        if (!int.TryParse(comando, out var indice) || indice < 1 || indice > contexto.Fechas.Count)
            return "Responda con el número de la fecha que aparece en la lista.";

        contexto.Fecha = contexto.Fechas[indice - 1];
        var fecha = DateOnly.Parse(contexto.Fecha);

        var disponibilidad = await _disponibilidad.ObtenerDiaAsync(contexto.MedicoId!.Value, fecha, contexto.CitaId);

        if (disponibilidad.Slots.Count == 0)
            return "Esa fecha ya no tiene cupos. " + await MostrarFechasAsync(conversacion, contexto);

        var horas = disponibilidad.Slots.Take(18).ToList();
        contexto.Fechas = horas.Select(s => s.Inicio.ToString("O")).ToList();
        conversacion.EstadoFlujo = "AGENDAR_HORA";

        var texto = new StringBuilder($"Horarios disponibles para el {fecha.ToDateTime(TimeOnly.MinValue).ToString("dd 'de' MMMM", Cultura)}:\n\n");
        for (var i = 0; i < horas.Count; i++)
            texto.AppendLine($"{i + 1}. {horas[i].Etiqueta}");

        return texto.ToString().TrimEnd();
    }

    private async Task<string> SeleccionHoraAsync(ConversacionChatbot conversacion, ContextoChatbot contexto, string comando)
    {
        if (!int.TryParse(comando, out var indice) || indice < 1 || indice > contexto.Fechas.Count)
            return "Responda con el número del horario que aparece en la lista.";

        contexto.Hora = contexto.Fechas[indice - 1];
        var inicio = DateTime.Parse(contexto.Hora, null, DateTimeStyles.RoundtripKind);

        var medico = await _medicos.Query().Include(m => m.Especialidad).AsNoTracking()
            .FirstAsync(m => m.Id == contexto.MedicoId!.Value);

        conversacion.EstadoFlujo = "AGENDAR_CONFIRMAR";

        var accion = contexto.Reprogramando ? "Nueva fecha de su cita" : "Resumen de su cita";

        return $"{accion}:\n\n" +
               $"Médico: {medico.NombreCompleto}\n" +
               $"Especialidad: {medico.Especialidad?.Nombre}\n" +
               $"Fecha: {Mapeos.FechaLarga(inicio)}\n\n" +
               "Responda SI para confirmar o NO para cancelar el proceso.";
    }

    private async Task<string> ConfirmarAgendamientoAsync(ConversacionChatbot conversacion, ContextoChatbot contexto,
        Paciente? paciente, string comando)
    {
        if (comando is not ("si" or "s" or "1" or "confirmar"))
        {
            conversacion.EstadoFlujo = Menu;
            return "Proceso cancelado. Escriba MENU cuando desee intentarlo de nuevo.";
        }

        if (paciente is null)
        {
            conversacion.EstadoFlujo = Menu;
            return "No pude identificar su registro. Escriba MENU para volver a comenzar.";
        }

        var inicio = DateTime.Parse(contexto.Hora!, null, DateTimeStyles.RoundtripKind);

        try
        {
            CitaDto cita;

            if (contexto.Reprogramando && contexto.CitaId.HasValue)
            {
                cita = await _citaService.ReprogramarAsync(contexto.CitaId.Value,
                    new ReprogramarCitaRequest
                    {
                        NuevaFechaHoraInicio = inicio,
                        NuevoMedicoId = contexto.MedicoId,
                        Motivo = "Reprogramación solicitada por el paciente vía WhatsApp"
                    }, null);
            }
            else
            {
                cita = await _citaService.CrearAsync(new CrearCitaRequest
                {
                    PacienteId = paciente.Id,
                    MedicoId = contexto.MedicoId!.Value,
                    FechaHoraInicio = inicio,
                    TipoConsulta = paciente.EsNuevo ? TipoConsulta.Nueva : TipoConsulta.Seguimiento,
                    Motivo = "Agendada desde WhatsApp",
                    CanalOrigen = CanalOrigen.WhatsApp
                }, null);
            }

            conversacion.EstadoFlujo = Menu;

            return $"Listo. Su cita quedó registrada con el código {cita.Codigo} para el " +
                   $"{Mapeos.FechaLarga(cita.FechaHoraInicio)} con {cita.Medico}.\n\n" +
                   "Le enviaremos un recordatorio antes de la fecha. Escriba MENU para otra gestión.";
        }
        catch (Exception ex) when (ex is ReglaNegocioException or ValidacionException)
        {
            conversacion.EstadoFlujo = Menu;
            return $"No pude completar la reserva: {ex.Message}\n\nEscriba MENU para intentarlo de nuevo.";
        }
    }

    private async Task<string> ConfirmarAsistenciaAsync(ConversacionChatbot conversacion, ContextoChatbot contexto, string comando)
    {
        if (!TryOpcion(contexto, comando, out var citaId))
            return "Responda con el número de la cita que aparece en la lista.";

        conversacion.EstadoFlujo = Menu;

        try
        {
            var cita = await _citaService.ConfirmarAsync(citaId, null);
            return $"Su asistencia a la cita {cita.Codigo} del {Mapeos.FechaLarga(cita.FechaHoraInicio)} quedó confirmada. Gracias.";
        }
        catch (Exception ex) when (ex is ReglaNegocioException or NoEncontradoException)
        {
            return $"{ex.Message}\n\nEscriba MENU para otra gestión.";
        }
    }

    private async Task<string> CancelarCitaAsync(ConversacionChatbot conversacion, ContextoChatbot contexto, string comando)
    {
        if (!TryOpcion(contexto, comando, out var citaId))
            return "Responda con el número de la cita que aparece en la lista.";

        conversacion.EstadoFlujo = Menu;

        try
        {
            var cita = await _citaService.CancelarAsync(citaId,
                new CancelarCitaRequest { Motivo = "Cancelada por el paciente vía WhatsApp" }, null);

            return $"Su cita {cita.Codigo} fue cancelada y el horario quedó liberado. " +
                   "Escriba 1 cuando desee agendar una nueva.";
        }
        catch (Exception ex) when (ex is ReglaNegocioException or NoEncontradoException)
        {
            return $"{ex.Message}\n\nEscriba MENU para otra gestión.";
        }
    }

    private async Task<string> IniciarReprogramacionAsync(ConversacionChatbot conversacion, ContextoChatbot contexto, string comando)
    {
        if (!TryOpcion(contexto, comando, out var citaId))
            return "Responda con el número de la cita que aparece en la lista.";

        var cita = await _citas.ObtenerCompletaAsync(citaId);
        if (cita is null)
        {
            conversacion.EstadoFlujo = Menu;
            return "No encontré esa cita. Escriba MENU para volver a intentarlo.";
        }

        contexto.CitaId = cita.Id;
        contexto.MedicoId = cita.MedicoId;
        contexto.EspecialidadId = cita.EspecialidadId;
        contexto.Reprogramando = true;

        return $"Vamos a reprogramar la cita {cita.Codigo}.\n\n" + await MostrarFechasAsync(conversacion, contexto);
    }

    private async Task<string> DecisionRetrasoAsync(ConversacionChatbot conversacion, ContextoChatbot contexto, string comando)
    {
        if (comando is "1" or "esperar")
        {
            conversacion.EstadoFlujo = Menu;
            await MarcarRespuestaRetrasoAsync(contexto.CitaId, "Espera");
            return "Gracias por esperar. Le avisaremos en cuanto el médico pueda atenderle.";
        }

        if (comando is "2" or "reprogramar")
        {
            await MarcarRespuestaRetrasoAsync(contexto.CitaId, "Reprograma");

            var cita = contexto.CitaId.HasValue ? await _citas.ObtenerCompletaAsync(contexto.CitaId.Value) : null;
            if (cita is null)
            {
                conversacion.EstadoFlujo = Menu;
                return "No encontré la cita afectada. Escriba MENU para gestionar sus citas.";
            }

            contexto.MedicoId = cita.MedicoId;
            contexto.EspecialidadId = cita.EspecialidadId;
            contexto.Reprogramando = true;

            return "Vamos a buscar una nueva fecha.\n\n" + await MostrarFechasAsync(conversacion, contexto);
        }

        return "Responda 1 para esperar o 2 para reprogramar su cita.";
    }

    private async Task MarcarRespuestaRetrasoAsync(int? citaId, string respuesta)
    {
        if (citaId is null) return;

        var notificacion = await _notificaciones.Query()
            .Where(n => n.CitaId == citaId && n.Tipo == TipoNotificacion.Retraso)
            .OrderByDescending(n => n.Id).FirstOrDefaultAsync();

        if (notificacion is null) return;

        notificacion.RespuestaPaciente = respuesta;
        notificacion.Estado = EstadoNotificacion.Respondida;
        await _notificaciones.GuardarCambiosAsync();
    }

    // ------------------------------------------------------------------
    // Consultas para el panel
    // ------------------------------------------------------------------
    public async Task<List<ConversacionDto>> ListarConversacionesAsync()
        => await _conversaciones.Query().Include(c => c.Paciente).AsNoTracking()
            .OrderByDescending(c => c.UltimaInteraccion)
            .Select(c => new ConversacionDto
            {
                Id = c.Id,
                Telefono = c.Telefono,
                PacienteId = c.PacienteId,
                Paciente = c.Paciente != null ? c.Paciente.Nombres + " " + c.Paciente.Apellidos : "Sin registrar",
                EstadoFlujo = c.EstadoFlujo,
                UltimaInteraccion = c.UltimaInteraccion,
                TotalMensajes = c.Mensajes.Count
            }).Take(100).ToListAsync();

    public async Task<List<MensajeDto>> ListarMensajesAsync(int conversacionId)
        => await _mensajes.Query().AsNoTracking()
            .Where(m => m.ConversacionId == conversacionId)
            .OrderBy(m => m.FechaHora)
            .Select(m => new MensajeDto
            {
                Id = m.Id,
                Direccion = (byte)m.Direccion,
                Contenido = m.Contenido,
                FechaHora = m.FechaHora
            }).ToListAsync();

    public async Task ReiniciarConversacionAsync(string telefono)
    {
        var normalizado = AuthService.NormalizarTelefono(telefono);
        var conversacion = await _conversaciones.PrimeroAsync(c => c.Telefono == normalizado);

        if (conversacion is null) return;

        conversacion.EstadoFlujo = Menu;
        conversacion.Contexto = null;
        await _conversaciones.GuardarCambiosAsync();
    }

    // ------------------------------------------------------------------
    // Utilidades internas
    // ------------------------------------------------------------------
    private async Task<ConversacionChatbot> ObtenerConversacionAsync(string telefono)
    {
        var conversacion = await _conversaciones.PrimeroAsync(c => c.Telefono == telefono);

        if (conversacion is null)
        {
            conversacion = new ConversacionChatbot { Telefono = telefono };
            await _conversaciones.AgregarAsync(conversacion);
            await _conversaciones.GuardarCambiosAsync();
        }

        return conversacion;
    }

    private async Task GuardarMensajeAsync(ConversacionChatbot conversacion, DireccionMensaje direccion, string contenido)
    {
        await _mensajes.AgregarAsync(new MensajeChatbot
        {
            ConversacionId = conversacion.Id,
            Direccion = direccion,
            Contenido = contenido,
            EstadoFlujo = conversacion.EstadoFlujo
        });
        await _mensajes.GuardarCambiosAsync();
    }

    private static string MensajeBienvenida(Paciente? paciente)
    {
        var saludo = paciente is null ? "Bienvenido a MediCitas." : $"Hola {paciente.Nombres}, bienvenido a MediCitas.";

        return $"{saludo}\n\n" +
               "1. Agendar una cita\n" +
               "2. Ver mis próximas citas\n" +
               "3. Confirmar asistencia\n" +
               "4. Reprogramar una cita\n" +
               "5. Cancelar una cita\n\n" +
               "Responda con el número de la opción.";
    }

    private static string ListarCitas(List<Cita> citas)
    {
        var texto = new StringBuilder();

        for (var i = 0; i < citas.Count; i++)
        {
            var c = citas[i];
            texto.AppendLine($"{i + 1}. {c.Codigo} · {Mapeos.FechaLarga(c.FechaHoraInicio)}");
            texto.AppendLine($"   {c.Medico?.NombreCompleto} · {c.Estado?.Nombre}");
        }

        return texto.ToString().TrimEnd();
    }

    private static bool TryOpcion(ContextoChatbot contexto, string comando, out int id)
    {
        id = 0;
        if (!int.TryParse(comando, out var indice)) return false;
        if (indice < 1 || indice > contexto.Opciones.Count) return false;

        id = contexto.Opciones[indice - 1];
        return true;
    }

    private static ContextoChatbot Deserializar(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new ContextoChatbot();

        try { return JsonSerializer.Deserialize<ContextoChatbot>(json) ?? new ContextoChatbot(); }
        catch { return new ContextoChatbot(); }
    }

    private static string Normalizar(string texto)
    {
        var minuscula = texto.Trim().ToLower(Cultura);
        var normalizado = minuscula.Normalize(NormalizationForm.FormD);
        var sinAcentos = new string(normalizado
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        return sinAcentos.Normalize(NormalizationForm.FormC);
    }
}