namespace MediCitas.Api.Domain.Enums;

/// <summary>Estados posibles de una cita. Coinciden con la tabla EstadosCita.</summary>
public enum EstadoCitaEnum
{
    Pendiente = 1,
    Confirmada = 2,
    EnAtencion = 3,
    Completada = 4,
    Cancelada = 5,
    NoAsistio = 6,
    Reprogramada = 7
}

public enum TipoConsulta
{
    Nueva = 1,
    Seguimiento = 2
}

public enum CanalOrigen
{
    Panel = 1,
    WhatsApp = 2,
    Portal = 3
}

public enum TipoBloqueo
{
    Vacaciones = 1,
    Feriado = 2,
    Personal = 3,
    Mantenimiento = 4
}

public enum TipoNotificacion
{
    Recordatorio = 1,
    Confirmacion = 2,
    Retraso = 3,
    Cancelacion = 4,
    Reprogramacion = 5,
    Agendamiento = 6
}

public enum EstadoNotificacion
{
    Pendiente = 1,
    Enviada = 2,
    Fallida = 3,
    Respondida = 4
}

public enum DireccionMensaje
{
    Entrante = 1,
    Saliente = 2
}

/// <summary>Nombres de rol usados en los atributos [Authorize(Roles = ...)].</summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Recepcionista = "Recepcionista";
    public const string Medico = "Medico";
    // Paciente se mantiene en la BD por compatibilidad pero no accede al panel.

    public const string Paciente = "Paciente"; // Solo para chatbot; no accede al panel.

    /// Panel completo (admin + recepcionista)
    public const string Personal = Administrador + "," + Recepcionista;
    /// Panel + médico (sus propias citas/pacientes)
    public const string PersonalYMedico = Administrador + "," + Recepcionista + "," + Medico;
    public const string Todos = PersonalYMedico;
}