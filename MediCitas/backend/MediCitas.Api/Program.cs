using System.Text;
using System.Text.Json.Serialization;
using MediCitas.Api.BackgroundJobs;
using MediCitas.Api.Common;
using MediCitas.Api.Data;
using MediCitas.Api.Data.Repositories;
using MediCitas.Api.Services.Implementations;
using MediCitas.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuración fuertemente tipada
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.Seccion));
builder.Services.Configure<ChatbotSettings>(builder.Configuration.GetSection(ChatbotSettings.Seccion));
builder.Services.Configure<NotificacionesSettings>(builder.Configuration.GetSection(NotificacionesSettings.Seccion));

var jwt = builder.Configuration.GetSection(JwtSettings.Seccion).Get<JwtSettings>() ?? new JwtSettings();

// ---------------------------------------------------------------------------
// Base de datos
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<MediCitasDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("MediCitas"),
        sql => sql.EnableRetryOnFailure(3)));

// ---------------------------------------------------------------------------
// Repositorios y servicios de aplicación
// ---------------------------------------------------------------------------
builder.Services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
builder.Services.AddScoped<ICitaRepositorio, CitaRepositorio>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEspecialidadService, EspecialidadService>();
builder.Services.AddScoped<IMedicoService, MedicoService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IAgendaService, AgendaService>();
builder.Services.AddScoped<IDisponibilidadService, DisponibilidadService>();
builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<IRetrasoService, RetrasoService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();
builder.Services.AddHttpClient("whatsapp");

builder.Services.AddHostedService<RecordatoriosWorker>();

// ---------------------------------------------------------------------------
// Autenticación JWT y autorización por roles
// ---------------------------------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// Controladores y validación de modelos
// ---------------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(opciones =>
    {
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opciones.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Las validaciones de los DTO devuelven el mismo formato que el resto de la API.
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
    {
        var errores = contexto.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
            .ToList();

        return new BadRequestObjectResult(
            ApiResponse<object>.Error("Los datos enviados no son válidos", errores));
    };
});

// ---------------------------------------------------------------------------
// CORS para el frontend estático
// ---------------------------------------------------------------------------
var origenes = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(opciones =>
    opciones.AddPolicy("MediCitas", politica =>
    {
        if (origenes.Length > 0)
            politica.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod();
        else
            politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }));

// ---------------------------------------------------------------------------
// Swagger
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MediCitas API",
        Version = "v1",
        Description = "API del sistema de gestión de citas médicas mediante chatbot en WhatsApp y panel administrativo. " +
                      "Universidad Mariano Gálvez de Guatemala.",
        Contact = new OpenApiContact { Name = "Stefany Sofia Monterroso Reyes" }
    });

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Escriba únicamente el token devuelto por /api/auth/login."
    });

    opciones.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) opciones.IncludeXmlComments(xml);
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
app.UseMiddleware<ManejadorErroresMiddleware>();

// Swagger queda habilitado también en producción: es la documentación entregable del proyecto.
app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "MediCitas API v1");
    opciones.DocumentTitle = "MediCitas API";
});

app.UseCors("MediCitas");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Estado del servicio, útil para verificar el despliegue.
app.MapGet("/", () => Results.Ok(new
{
    servicio = "MediCitas API",
    version = "1.0",
    documentacion = "/swagger",
    fecha = DateTime.Now
}));

await DbInicializador.InicializarAsync(app.Services, app.Logger);

app.Run();
