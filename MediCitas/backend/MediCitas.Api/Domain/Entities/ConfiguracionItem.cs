namespace MediCitas.Api.Domain.Entities;

public class ConfiguracionItem
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaModificacion { get; set; } = DateTime.Now;
}
