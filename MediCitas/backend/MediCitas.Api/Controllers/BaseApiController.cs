using MediCitas.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace MediCitas.Api.Controllers;

/// <summary>Controlador base: unifica la ruta, el formato de respuesta y el acceso al usuario autenticado.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected IUsuarioActual UsuarioActual => HttpContext.RequestServices.GetRequiredService<IUsuarioActual>();

    protected IActionResult Exito<T>(T datos, string mensaje = "Operación realizada correctamente")
        => Ok(ApiResponse<T>.Ok(datos, mensaje));

    protected IActionResult Creado<T>(T datos, string mensaje = "Registro creado correctamente")
        => StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Ok(datos, mensaje));
}
