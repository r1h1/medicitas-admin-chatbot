/* ============================================================================
   MediCitas - Sistema de Gestión de Citas Médicas
   Script 01: Creación de esquema (SQL Server 2016+ / Somee / SQL Express)
   Autor: Stefany Sofia Monterroso Reyes - 5190-19-765
   Universidad Mariano Gálvez de Guatemala
   ----------------------------------------------------------------------------
   Ejecutar sobre una base de datos ya creada. En Somee la base ya existe,
   por lo que este script NO ejecuta CREATE DATABASE.
   Para entorno local:  CREATE DATABASE MediCitas;  GO  USE MediCitas;  GO
   ============================================================================ */

SET NOCOUNT ON;
GO

/* ----------------------------------------------------------------------------
   1. CATÁLOGO DE ROLES
   Solo tres roles según el alcance del proyecto.
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Roles') IS NULL
CREATE TABLE dbo.Roles (
    Id          INT             NOT NULL IDENTITY(1,1),
    Nombre      NVARCHAR(50)    NOT NULL,
    Descripcion NVARCHAR(200)   NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (Id),
    CONSTRAINT UQ_Roles_Nombre UNIQUE (Nombre)
);
GO

/* ----------------------------------------------------------------------------
   2. ESPECIALIDADES MÉDICAS
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Especialidades') IS NULL
CREATE TABLE dbo.Especialidades (
    Id            INT            NOT NULL IDENTITY(1,1),
    Nombre        NVARCHAR(100)  NOT NULL,
    Descripcion   NVARCHAR(300)  NULL,
    Activo        BIT            NOT NULL CONSTRAINT DF_Especialidades_Activo DEFAULT (1),
    FechaRegistro DATETIME2(0)   NOT NULL CONSTRAINT DF_Especialidades_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Especialidades PRIMARY KEY (Id),
    CONSTRAINT UQ_Especialidades_Nombre UNIQUE (Nombre)
);
GO

/* ----------------------------------------------------------------------------
   3. MÉDICOS
   DuracionCitaMinutos define el tamaño del bloque de agenda del médico.
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Medicos') IS NULL
CREATE TABLE dbo.Medicos (
    Id                  INT            NOT NULL IDENTITY(1,1),
    Nombres             NVARCHAR(100)  NOT NULL,
    Apellidos           NVARCHAR(100)  NOT NULL,
    Colegiado           NVARCHAR(20)   NOT NULL,
    EspecialidadId      INT            NOT NULL,
    Telefono            NVARCHAR(20)   NULL,
    Email               NVARCHAR(150)  NULL,
    DuracionCitaMinutos INT            NOT NULL CONSTRAINT DF_Medicos_Duracion DEFAULT (30),
    Activo              BIT            NOT NULL CONSTRAINT DF_Medicos_Activo DEFAULT (1),
    FechaRegistro       DATETIME2(0)   NOT NULL CONSTRAINT DF_Medicos_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Medicos PRIMARY KEY (Id),
    CONSTRAINT UQ_Medicos_Colegiado UNIQUE (Colegiado),
    CONSTRAINT FK_Medicos_Especialidades FOREIGN KEY (EspecialidadId)
        REFERENCES dbo.Especialidades (Id),
    CONSTRAINT CK_Medicos_Duracion CHECK (DuracionCitaMinutos BETWEEN 10 AND 120)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Medicos_EspecialidadId')
    CREATE INDEX IX_Medicos_EspecialidadId ON dbo.Medicos (EspecialidadId) INCLUDE (Activo);
GO

/* ----------------------------------------------------------------------------
   4. PACIENTES
   El teléfono es la llave natural del chatbot: identifica al paciente en WhatsApp.
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Pacientes') IS NULL
CREATE TABLE dbo.Pacientes (
    Id               INT            NOT NULL IDENTITY(1,1),
    Nombres          NVARCHAR(100)  NOT NULL,
    Apellidos        NVARCHAR(100)  NOT NULL,
    Telefono         NVARCHAR(20)   NOT NULL,
    Email            NVARCHAR(150)  NULL,
    DocumentoIdentidad NVARCHAR(20) NULL,
    FechaNacimiento  DATE           NULL,
    Genero           CHAR(1)        NULL,
    Direccion        NVARCHAR(250)  NULL,
    EsNuevo          BIT            NOT NULL CONSTRAINT DF_Pacientes_EsNuevo DEFAULT (1),
    Activo           BIT            NOT NULL CONSTRAINT DF_Pacientes_Activo DEFAULT (1),
    FechaRegistro    DATETIME2(0)   NOT NULL CONSTRAINT DF_Pacientes_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Pacientes PRIMARY KEY (Id),
    CONSTRAINT UQ_Pacientes_Telefono UNIQUE (Telefono),
    CONSTRAINT CK_Pacientes_Genero CHECK (Genero IS NULL OR Genero IN ('M','F','O'))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Pacientes_Apellidos')
    CREATE INDEX IX_Pacientes_Apellidos ON dbo.Pacientes (Apellidos, Nombres);
GO

/* ----------------------------------------------------------------------------
   5. USUARIOS DEL SISTEMA
   Un usuario puede estar vinculado a un paciente (portal) o a un médico
   (para que el personal vea "su" agenda). El rol define los permisos.
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Usuarios') IS NULL
CREATE TABLE dbo.Usuarios (
    Id            INT            NOT NULL IDENTITY(1,1),
    Nombres       NVARCHAR(100)  NOT NULL,
    Apellidos     NVARCHAR(100)  NOT NULL,
    Email         NVARCHAR(150)  NOT NULL,
    Telefono      NVARCHAR(20)   NULL,
    PasswordHash  NVARCHAR(300)  NOT NULL,
    RolId         INT            NOT NULL,
    PacienteId    INT            NULL,
    MedicoId      INT            NULL,
    Activo        BIT            NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
    FechaRegistro DATETIME2(0)   NOT NULL CONSTRAINT DF_Usuarios_Fecha DEFAULT (SYSDATETIME()),
    UltimoAcceso  DATETIME2(0)   NULL,
    CONSTRAINT PK_Usuarios PRIMARY KEY (Id),
    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),
    CONSTRAINT FK_Usuarios_Roles     FOREIGN KEY (RolId)      REFERENCES dbo.Roles (Id),
    CONSTRAINT FK_Usuarios_Pacientes FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes (Id),
    CONSTRAINT FK_Usuarios_Medicos   FOREIGN KEY (MedicoId)   REFERENCES dbo.Medicos (Id)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Usuarios_RolId')
    CREATE INDEX IX_Usuarios_RolId ON dbo.Usuarios (RolId) INCLUDE (Activo);
GO

/* ----------------------------------------------------------------------------
   6. HORARIOS LABORALES DEL MÉDICO
   DiaSemana: 0 = domingo ... 6 = sábado (compatible con DayOfWeek de .NET)
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.HorariosMedico') IS NULL
CREATE TABLE dbo.HorariosMedico (
    Id         INT      NOT NULL IDENTITY(1,1),
    MedicoId   INT      NOT NULL,
    DiaSemana  TINYINT  NOT NULL,
    HoraInicio TIME(0)  NOT NULL,
    HoraFin    TIME(0)  NOT NULL,
    Activo     BIT      NOT NULL CONSTRAINT DF_Horarios_Activo DEFAULT (1),
    CONSTRAINT PK_HorariosMedico PRIMARY KEY (Id),
    CONSTRAINT FK_HorariosMedico_Medicos FOREIGN KEY (MedicoId)
        REFERENCES dbo.Medicos (Id) ON DELETE CASCADE,
    CONSTRAINT CK_HorariosMedico_Dia   CHECK (DiaSemana BETWEEN 0 AND 6),
    CONSTRAINT CK_HorariosMedico_Rango CHECK (HoraFin > HoraInicio),
    CONSTRAINT UQ_HorariosMedico UNIQUE (MedicoId, DiaSemana, HoraInicio)
);
GO

/* ----------------------------------------------------------------------------
   7. BLOQUEOS DE AGENDA
   MedicoId NULL = bloqueo general de la clínica (feriado nacional).
   Tipo: 1 Vacaciones | 2 Feriado | 3 Personal | 4 Mantenimiento
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.BloqueosAgenda') IS NULL
CREATE TABLE dbo.BloqueosAgenda (
    Id            INT           NOT NULL IDENTITY(1,1),
    MedicoId      INT           NULL,
    FechaInicio   DATE          NOT NULL,
    FechaFin      DATE          NOT NULL,
    HoraInicio    TIME(0)       NULL,
    HoraFin       TIME(0)       NULL,
    Tipo          TINYINT       NOT NULL CONSTRAINT DF_Bloqueos_Tipo DEFAULT (3),
    Motivo        NVARCHAR(200) NULL,
    Activo        BIT           NOT NULL CONSTRAINT DF_Bloqueos_Activo DEFAULT (1),
    FechaRegistro DATETIME2(0)  NOT NULL CONSTRAINT DF_Bloqueos_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_BloqueosAgenda PRIMARY KEY (Id),
    CONSTRAINT FK_BloqueosAgenda_Medicos FOREIGN KEY (MedicoId)
        REFERENCES dbo.Medicos (Id) ON DELETE CASCADE,
    CONSTRAINT CK_BloqueosAgenda_Rango CHECK (FechaFin >= FechaInicio),
    CONSTRAINT CK_BloqueosAgenda_Tipo  CHECK (Tipo BETWEEN 1 AND 4)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BloqueosAgenda_Fechas')
    CREATE INDEX IX_BloqueosAgenda_Fechas ON dbo.BloqueosAgenda (FechaInicio, FechaFin) INCLUDE (MedicoId, Activo);
GO

/* ----------------------------------------------------------------------------
   8. CATÁLOGO DE ESTADOS DE CITA
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.EstadosCita') IS NULL
CREATE TABLE dbo.EstadosCita (
    Id     INT          NOT NULL,
    Nombre NVARCHAR(30) NOT NULL,
    Color  NVARCHAR(9)  NOT NULL,
    CONSTRAINT PK_EstadosCita PRIMARY KEY (Id),
    CONSTRAINT UQ_EstadosCita_Nombre UNIQUE (Nombre)
);
GO

/* ----------------------------------------------------------------------------
   9. CITAS
   Regla clave: no se permite doble reserva del mismo médico en el mismo
   horario cuando la cita está vigente (estados 1,2,3).
   Se garantiza con un índice único filtrado.
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Citas') IS NULL
CREATE TABLE dbo.Citas (
    Id                INT            NOT NULL IDENTITY(1,1),
    Codigo            NVARCHAR(12)   NOT NULL,
    PacienteId        INT            NOT NULL,
    MedicoId          INT            NOT NULL,
    EspecialidadId    INT            NOT NULL,
    FechaHoraInicio   DATETIME2(0)   NOT NULL,
    FechaHoraFin      DATETIME2(0)   NOT NULL,
    EstadoId          INT            NOT NULL CONSTRAINT DF_Citas_Estado DEFAULT (1),
    TipoConsulta      TINYINT        NOT NULL CONSTRAINT DF_Citas_Tipo DEFAULT (1), -- 1 Nueva, 2 Seguimiento
    Motivo            NVARCHAR(300)  NULL,
    Notas             NVARCHAR(500)  NULL,
    CanalOrigen       TINYINT        NOT NULL CONSTRAINT DF_Citas_Canal DEFAULT (1), -- 1 Panel, 2 WhatsApp, 3 Portal
    CitaOriginalId    INT            NULL,
    FechaCreacion     DATETIME2(0)   NOT NULL CONSTRAINT DF_Citas_FechaCreacion DEFAULT (SYSDATETIME()),
    FechaConfirmacion DATETIME2(0)   NULL,
    FechaCancelacion  DATETIME2(0)   NULL,
    MotivoCancelacion NVARCHAR(300)  NULL,
    CreadoPorUsuarioId INT           NULL,
    CONSTRAINT PK_Citas PRIMARY KEY (Id),
    CONSTRAINT UQ_Citas_Codigo UNIQUE (Codigo),
    CONSTRAINT FK_Citas_Pacientes      FOREIGN KEY (PacienteId)     REFERENCES dbo.Pacientes (Id),
    CONSTRAINT FK_Citas_Medicos        FOREIGN KEY (MedicoId)       REFERENCES dbo.Medicos (Id),
    CONSTRAINT FK_Citas_Especialidades FOREIGN KEY (EspecialidadId) REFERENCES dbo.Especialidades (Id),
    CONSTRAINT FK_Citas_EstadosCita    FOREIGN KEY (EstadoId)       REFERENCES dbo.EstadosCita (Id),
    CONSTRAINT FK_Citas_CitaOriginal   FOREIGN KEY (CitaOriginalId) REFERENCES dbo.Citas (Id),
    CONSTRAINT FK_Citas_Usuarios       FOREIGN KEY (CreadoPorUsuarioId) REFERENCES dbo.Usuarios (Id),
    CONSTRAINT CK_Citas_Rango  CHECK (FechaHoraFin > FechaHoraInicio),
    CONSTRAINT CK_Citas_Tipo   CHECK (TipoConsulta IN (1,2)),
    CONSTRAINT CK_Citas_Canal  CHECK (CanalOrigen IN (1,2,3))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Citas_NoDobleReserva')
    CREATE UNIQUE INDEX UX_Citas_NoDobleReserva
        ON dbo.Citas (MedicoId, FechaHoraInicio)
        WHERE EstadoId IN (1,2,3);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Citas_Fecha')
    CREATE INDEX IX_Citas_Fecha ON dbo.Citas (FechaHoraInicio) INCLUDE (EstadoId, MedicoId, PacienteId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Citas_PacienteId')
    CREATE INDEX IX_Citas_PacienteId ON dbo.Citas (PacienteId, FechaHoraInicio DESC);
GO

/* ----------------------------------------------------------------------------
   10. RETRASOS REPORTADOS POR EL MÉDICO
   Al registrarse se generan notificaciones a los pacientes en cola.
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.RetrasosMedico') IS NULL
CREATE TABLE dbo.RetrasosMedico (
    Id                 INT           NOT NULL IDENTITY(1,1),
    MedicoId           INT           NOT NULL,
    Fecha              DATE          NOT NULL,
    MinutosRetraso     INT           NOT NULL,
    Motivo             NVARCHAR(250) NULL,
    PacientesNotificados INT         NOT NULL CONSTRAINT DF_Retrasos_Notif DEFAULT (0),
    RegistradoPorUsuarioId INT       NULL,
    FechaRegistro      DATETIME2(0)  NOT NULL CONSTRAINT DF_Retrasos_Fecha DEFAULT (SYSDATETIME()),
    Activo             BIT           NOT NULL CONSTRAINT DF_Retrasos_Activo DEFAULT (1),
    CONSTRAINT PK_RetrasosMedico PRIMARY KEY (Id),
    CONSTRAINT FK_RetrasosMedico_Medicos  FOREIGN KEY (MedicoId) REFERENCES dbo.Medicos (Id),
    CONSTRAINT FK_RetrasosMedico_Usuarios FOREIGN KEY (RegistradoPorUsuarioId) REFERENCES dbo.Usuarios (Id),
    CONSTRAINT CK_RetrasosMedico_Minutos  CHECK (MinutosRetraso BETWEEN 5 AND 480)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RetrasosMedico_Fecha')
    CREATE INDEX IX_RetrasosMedico_Fecha ON dbo.RetrasosMedico (Fecha, MedicoId);
GO

/* ----------------------------------------------------------------------------
   11. NOTIFICACIONES
   Tipo:   1 Recordatorio | 2 Confirmación | 3 Retraso | 4 Cancelación
           5 Reprogramación | 6 Agendamiento
   Estado: 1 Pendiente | 2 Enviada | 3 Fallida | 4 Respondida
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Notificaciones') IS NULL
CREATE TABLE dbo.Notificaciones (
    Id               INT            NOT NULL IDENTITY(1,1),
    PacienteId       INT            NOT NULL,
    CitaId           INT            NULL,
    Tipo             TINYINT        NOT NULL,
    Mensaje          NVARCHAR(1000) NOT NULL,
    Canal            TINYINT        NOT NULL CONSTRAINT DF_Notif_Canal DEFAULT (1), -- 1 WhatsApp
    Estado           TINYINT        NOT NULL CONSTRAINT DF_Notif_Estado DEFAULT (1),
    FechaProgramada  DATETIME2(0)   NOT NULL,
    FechaEnvio       DATETIME2(0)   NULL,
    Intentos         INT            NOT NULL CONSTRAINT DF_Notif_Intentos DEFAULT (0),
    RespuestaPaciente NVARCHAR(200) NULL,
    CONSTRAINT PK_Notificaciones PRIMARY KEY (Id),
    CONSTRAINT FK_Notificaciones_Pacientes FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes (Id),
    CONSTRAINT FK_Notificaciones_Citas     FOREIGN KEY (CitaId)     REFERENCES dbo.Citas (Id),
    CONSTRAINT CK_Notificaciones_Tipo   CHECK (Tipo BETWEEN 1 AND 6),
    CONSTRAINT CK_Notificaciones_Estado CHECK (Estado BETWEEN 1 AND 4)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Notificaciones_Pendientes')
    CREATE INDEX IX_Notificaciones_Pendientes
        ON dbo.Notificaciones (Estado, FechaProgramada) INCLUDE (PacienteId, CitaId);
GO

/* ----------------------------------------------------------------------------
   12. CONVERSACIONES DEL CHATBOT (máquina de estados por número de WhatsApp)
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.ConversacionesChatbot') IS NULL
CREATE TABLE dbo.ConversacionesChatbot (
    Id               INT            NOT NULL IDENTITY(1,1),
    Telefono         NVARCHAR(20)   NOT NULL,
    PacienteId       INT            NULL,
    EstadoFlujo      NVARCHAR(60)   NOT NULL CONSTRAINT DF_Conv_Estado DEFAULT ('MENU_PRINCIPAL'),
    Contexto         NVARCHAR(MAX)  NULL,   -- JSON con datos temporales del flujo
    UltimaInteraccion DATETIME2(0)  NOT NULL CONSTRAINT DF_Conv_Ultima DEFAULT (SYSDATETIME()),
    Activa           BIT            NOT NULL CONSTRAINT DF_Conv_Activa DEFAULT (1),
    CONSTRAINT PK_ConversacionesChatbot PRIMARY KEY (Id),
    CONSTRAINT UQ_ConversacionesChatbot_Telefono UNIQUE (Telefono),
    CONSTRAINT FK_ConversacionesChatbot_Pacientes FOREIGN KEY (PacienteId) REFERENCES dbo.Pacientes (Id)
);
GO

/* ----------------------------------------------------------------------------
   13. MENSAJES DEL CHATBOT (bitácora de la conversación)
   Direccion: 1 Entrante (paciente) | 2 Saliente (sistema)
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.MensajesChatbot') IS NULL
CREATE TABLE dbo.MensajesChatbot (
    Id             BIGINT         NOT NULL IDENTITY(1,1),
    ConversacionId INT            NOT NULL,
    Direccion      TINYINT        NOT NULL,
    Contenido      NVARCHAR(2000) NOT NULL,
    EstadoFlujo    NVARCHAR(60)   NULL,
    FechaHora      DATETIME2(0)   NOT NULL CONSTRAINT DF_Mensajes_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_MensajesChatbot PRIMARY KEY (Id),
    CONSTRAINT FK_MensajesChatbot_Conversaciones FOREIGN KEY (ConversacionId)
        REFERENCES dbo.ConversacionesChatbot (Id) ON DELETE CASCADE,
    CONSTRAINT CK_MensajesChatbot_Direccion CHECK (Direccion IN (1,2))
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MensajesChatbot_Conversacion')
    CREATE INDEX IX_MensajesChatbot_Conversacion ON dbo.MensajesChatbot (ConversacionId, FechaHora DESC);
GO

/* ----------------------------------------------------------------------------
   14. CONFIGURACIÓN DEL SISTEMA (clave/valor)
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Configuracion') IS NULL
CREATE TABLE dbo.Configuracion (
    Id             INT           NOT NULL IDENTITY(1,1),
    Clave          NVARCHAR(60)  NOT NULL,
    Valor          NVARCHAR(300) NOT NULL,
    Descripcion    NVARCHAR(250) NULL,
    FechaModificacion DATETIME2(0) NOT NULL CONSTRAINT DF_Config_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Configuracion PRIMARY KEY (Id),
    CONSTRAINT UQ_Configuracion_Clave UNIQUE (Clave)
);
GO

/* ----------------------------------------------------------------------------
   15. BITÁCORA DE EVENTOS (registro básico de auditoría)
---------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.BitacoraEventos') IS NULL
CREATE TABLE dbo.BitacoraEventos (
    Id         BIGINT         NOT NULL IDENTITY(1,1),
    UsuarioId  INT            NULL,
    Accion     NVARCHAR(60)   NOT NULL,
    Entidad    NVARCHAR(60)   NOT NULL,
    EntidadId  NVARCHAR(30)   NULL,
    Detalle    NVARCHAR(1000) NULL,
    FechaHora  DATETIME2(0)   NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_BitacoraEventos PRIMARY KEY (Id),
    CONSTRAINT FK_BitacoraEventos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios (Id)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BitacoraEventos_Fecha')
    CREATE INDEX IX_BitacoraEventos_Fecha ON dbo.BitacoraEventos (FechaHora DESC);
GO

PRINT 'Esquema MediCitas creado correctamente.';
GO
