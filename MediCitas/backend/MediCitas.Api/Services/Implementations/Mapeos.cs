using MediCitas.Api.Domain.Entities;
using MediCitas.Api.DTOs.Citas;

namespace MediCitas.Api.Services.Implementations;

/// <summary>Conversiones entidad → DTO reutilizadas por varios servicios.</summary>
public static class Mapeos
{
    public static CitaDto ACitaDto(Cita c) => new()
    {
        Id = c.Id,
        Codigo = c.Codigo,
        PacienteId = c.PacienteId,
        Paciente = c.Paciente?.NombreCompleto ?? string.Empty,
        PacienteTelefono = c.Paciente?.Telefono ?? string.Empty,
        MedicoId = c.MedicoId,
        Medico = c.Medico?.NombreCompleto ?? string.Empty,
        EspecialidadId = c.EspecialidadId,
        Especialidad = c.Especialidad?.Nombre ?? string.Empty,
        FechaHoraInicio = c.FechaHoraInicio,
        FechaHoraFin = c.FechaHoraFin,
        EstadoId = c.EstadoId,
        Estado = c.Estado?.Nombre ?? string.Empty,
        EstadoColor = c.Estado?.Color ?? "#6B7280",
        TipoConsulta = c.TipoConsulta,
        Motivo = c.Motivo,
        Notas = c.Notas,
        CanalOrigen = c.CanalOrigen,
        FechaCreacion = c.FechaCreacion,
        FechaConfirmacion = c.FechaConfirmacion,
        MotivoCancelacion = c.MotivoCancelacion
    };

    /// <summary>Formato de fecha y hora usado en los mensajes de WhatsApp.</summary>
    public static string FechaLarga(DateTime fecha)
        => fecha.ToString("dddd dd 'de' MMMM 'a las' hh:mm tt",
            new System.Globalization.CultureInfo("es-GT"));
}
