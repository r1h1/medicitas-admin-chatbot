/* ============================================================================
   MediCitas - Script 02: Datos iniciales
   ----------------------------------------------------------------------------
   IMPORTANTE: aquí NO hay datos de demostración inventados.
   Solo se insertan:
     - Catálogo de roles          (necesario para las llaves foráneas)
     - Catálogo de estados de cita(necesario para las llaves foráneas)
     - Parámetros de configuración por defecto
     - El único usuario administrador solicitado
   Especialidades, médicos, pacientes y citas inician vacíos y se cargan
   desde el panel administrativo.
   ============================================================================ */

SET NOCOUNT ON;
GO

/* ---------- Roles (solo tres, según el alcance del proyecto) ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Administrador')
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES ('Administrador', 'Acceso total al sistema y a la configuración');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Recepcionista')
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES ('Recepcionista', 'Personal administrativo y médico: gestiona agenda, citas y pacientes');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Paciente')
    INSERT INTO dbo.Roles (Nombre, Descripcion)
    VALUES ('Paciente', 'Consulta y gestiona únicamente sus propias citas');
GO

/* ---------- Estados de cita ---------- */
MERGE dbo.EstadosCita AS destino
USING (VALUES
    (1, N'Pendiente',   N'#F59E0B'),
    (2, N'Confirmada',  N'#22C55E'),
    (3, N'EnAtencion',  N'#3B82F6'),
    (4, N'Completada',  N'#14B8A6'),
    (5, N'Cancelada',   N'#EF4444'),
    (6, N'NoAsistio',   N'#6B7280'),
    (7, N'Reprogramada',N'#8B5CF6')
) AS origen (Id, Nombre, Color)
ON destino.Id = origen.Id
WHEN NOT MATCHED THEN
    INSERT (Id, Nombre, Color) VALUES (origen.Id, origen.Nombre, origen.Color);
GO

/* ---------- Parámetros de configuración ---------- */
MERGE dbo.Configuracion AS destino
USING (VALUES
    (N'ClinicaNombre',            N'Clínica de Especialidades Médicas Integradas Dr(a). Isabella Torselli', N'Nombre mostrado en mensajes y reportes'),
    (N'ClinicaTelefono',          N'',      N'Teléfono de contacto de la clínica'),
    (N'ClinicaDireccion',         N'',      N'Dirección de la clínica'),
    (N'RecordatorioHorasAntes',   N'24',    N'Horas de anticipación del primer recordatorio'),
    (N'RecordatorioHorasAntes2',  N'2',     N'Horas de anticipación del segundo recordatorio'),
    (N'CancelacionHorasMinimas',  N'4',     N'Horas mínimas antes de la cita para cancelar o reprogramar'),
    (N'DiasMaximosAgendamiento',  N'60',    N'Máximo de días hacia adelante que se pueden agendar'),
    (N'ToleranciaLlegadaMinutos', N'15',    N'Minutos de tolerancia antes de marcar No asistió')
) AS origen (Clave, Valor, Descripcion)
ON destino.Clave = origen.Clave
WHEN NOT MATCHED THEN
    INSERT (Clave, Valor, Descripcion) VALUES (origen.Clave, origen.Valor, origen.Descripcion);
GO

/* ---------- Usuario administrador ----------
   Correo:      admin@medicitas.com
   Contraseña:  Admin!$MediCitas14
   Formato del hash: iteraciones.saltBase64.hashBase64  (PBKDF2-SHA256)
   El backend genera exactamente este mismo formato al registrar usuarios.
------------------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Email = N'admin@medicitas.com')
INSERT INTO dbo.Usuarios (Nombres, Apellidos, Email, Telefono, PasswordHash, RolId, Activo)
SELECT N'Admin', N'Principal', N'admin@medicitas.com', NULL,
       N'100000.nyxBq3PYTmWQvRLHrjQPWA==.Y6jWIPpJ8Owrrw8kCttUDD5+uEfxLoKMuygO5reqReU=',
       r.Id, 1
FROM dbo.Roles r WHERE r.Nombre = 'Administrador';
GO

PRINT 'Datos iniciales cargados. Usuario: admin@medicitas.com';
GO
