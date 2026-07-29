namespace MediCitas.Api.Common;

/// <summary>El recurso solicitado no existe. Se traduce a HTTP 404.</summary>
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}

/// <summary>Una regla de negocio impide completar la operación. Se traduce a HTTP 409.</summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}

/// <summary>Datos de entrada inválidos. Se traduce a HTTP 400.</summary>
public class ValidacionException : Exception
{
    public IEnumerable<string> Errores { get; }

    public ValidacionException(string mensaje, IEnumerable<string>? errores = null) : base(mensaje)
        => Errores = errores ?? Array.Empty<string>();
}
