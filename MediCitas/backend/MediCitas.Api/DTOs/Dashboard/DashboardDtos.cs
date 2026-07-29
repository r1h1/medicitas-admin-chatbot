namespace MediCitas.Api.DTOs.Dashboard;

public class DashboardDto
{
    public int CitasHoy { get; set; }
    public int Confirmadas { get; set; }
    public int Pendientes { get; set; }
    public int Canceladas { get; set; }
    public int PacientesRegistrados { get; set; }
    public decimal PorcentajeOcupacion { get; set; }
    public decimal PorcentajeConfirmadas { get; set; }
    public decimal PorcentajePendientes { get; set; }
    public decimal PorcentajeCanceladas { get; set; }

    public List<ConteoEtiquetaDto> CitasPorEstadoMes { get; set; } = new();
    public List<ConteoEtiquetaDto> CitasPorEspecialidadMes { get; set; } = new();
    public List<CitaResumenDto> ProximasCitasHoy { get; set; } = new();
    public List<SlotResumenDto> ProximosHorariosDisponibles { get; set; } = new();
    public int TotalCitasMes { get; set; }
}

public class ConteoEtiquetaDto
{
    public string Etiqueta { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Porcentaje { get; set; }
    public string? Color { get; set; }
}

public class CitaResumenDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Hora { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public string Medico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string EstadoColor { get; set; } = string.Empty;
}

public class SlotResumenDto
{
    public string Medico { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public DateTime Inicio { get; set; }
}

// -------------------- Reportes --------------------
public class ReporteGeneralDto
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int TotalCitas { get; set; }
    public int Atendidas { get; set; }
    public int Canceladas { get; set; }
    public int NoAsistio { get; set; }
    public decimal TasaAusentismo { get; set; }
    public decimal TasaConfirmacion { get; set; }
    public int CitasPorWhatsApp { get; set; }
    public int CitasPorPanel { get; set; }
    public int RecordatoriosEnviados { get; set; }
    public int RetrasosReportados { get; set; }
    public List<ConteoEtiquetaDto> CitasPorMedico { get; set; } = new();
    public List<ConteoEtiquetaDto> CitasPorEspecialidad { get; set; } = new();
    public List<ConteoEtiquetaDto> CitasPorDia { get; set; } = new();
}
