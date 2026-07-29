namespace MediCitas.Api.Domain.Entities;

public class EstadoCita
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Color { get; set; } = "#6B7280";

    public ICollection<Cita> Citas { get; set; } = new List<Cita>();
}
