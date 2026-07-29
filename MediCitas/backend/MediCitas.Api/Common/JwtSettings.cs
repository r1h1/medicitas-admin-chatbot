namespace MediCitas.Api.Common;

public class JwtSettings
{
    public const string Seccion = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int MinutosExpiracion { get; set; } = 480;
}

public class ChatbotSettings
{
    public const string Seccion = "Chatbot";

    /// <summary>Si es true los mensajes no salen a WhatsApp: se registran en la bitácora del chatbot.</summary>
    public bool ModoSimulacion { get; set; } = true;
    public string VerifyToken { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
}

public class NotificacionesSettings
{
    public const string Seccion = "Notificaciones";

    public int IntervaloSegundos { get; set; } = 60;
    public int MaximoIntentos { get; set; } = 3;
}
