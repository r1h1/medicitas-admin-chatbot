namespace MediCitas.Api.Common;

/// <summary>Envoltura estándar de todas las respuestas de la API.</summary>
public class ApiResponse<T>
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public T? Datos { get; set; }
    public IEnumerable<string>? Errores { get; set; }

    public static ApiResponse<T> Ok(T datos, string mensaje = "Operación realizada correctamente")
        => new() { Exito = true, Mensaje = mensaje, Datos = datos };

    public static ApiResponse<T> Error(string mensaje, IEnumerable<string>? errores = null)
        => new() { Exito = false, Mensaje = mensaje, Errores = errores };
}

/// <summary>Respuesta paginada para listados grandes.</summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
    public int Pagina { get; set; }
    public int TamanioPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas => TamanioPagina == 0 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)TamanioPagina);
}
