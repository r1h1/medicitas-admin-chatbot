using System.Text.Json;

namespace MediCitas.Api.Common;

/// <summary>
/// Middleware único de manejo de errores. Convierte las excepciones del dominio
/// en respuestas HTTP consistentes y evita exponer detalles internos al cliente.
/// </summary>
public class ManejadorErroresMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorErroresMiddleware> _logger;

    public ManejadorErroresMiddleware(RequestDelegate siguiente, ILogger<ManejadorErroresMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (ValidacionException ex)
        {
            await EscribirAsync(contexto, StatusCodes.Status400BadRequest, ex.Message, ex.Errores);
        }
        catch (NoEncontradoException ex)
        {
            await EscribirAsync(contexto, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ReglaNegocioException ex)
        {
            await EscribirAsync(contexto, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await EscribirAsync(contexto, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en {Ruta}", contexto.Request.Path);
            await EscribirAsync(contexto, StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.");
        }
    }

    private static async Task EscribirAsync(HttpContext contexto, int codigo, string mensaje,
        IEnumerable<string>? errores = null)
    {
        if (contexto.Response.HasStarted) return;

        contexto.Response.Clear();
        contexto.Response.StatusCode = codigo;
        contexto.Response.ContentType = "application/json";

        var cuerpo = ApiResponse<object>.Error(mensaje, errores);
        var opciones = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, opciones));
    }
}
