using System.Security.Cryptography;

namespace MediCitas.Api.Common;

/// <summary>
/// Hash de contraseñas con PBKDF2-SHA256 (incluido en .NET, sin dependencias externas).
/// Formato almacenado: iteraciones.saltBase64.hashBase64
/// </summary>
public static class PasswordHasher
{
    private const int Iteraciones = 100_000;
    private const int TamanioSalt = 16;
    private const int TamanioHash = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanioSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, TamanioHash);
        return $"{Iteraciones}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string password, string hashAlmacenado)
    {
        var partes = hashAlmacenado.Split('.', 3);
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones)) return false;

        var salt = Convert.FromBase64String(partes[1]);
        var esperado = Convert.FromBase64String(partes[2]);
        var calculado = Rfc2898DeriveBytes.Pbkdf2(password, salt, iteraciones, HashAlgorithmName.SHA256, esperado.Length);

        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
