namespace MediCitas.Api.Domain.Entities;

public class EventoBitacora
{
    public long Id { get; set; }
    public int? UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public string? EntidadId { get; set; }
    public string? Detalle { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;

    public Usuario? Usuario { get; set; }
}
