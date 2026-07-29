# MediCitas — Sistema de gestión de citas médicas

Prototipo funcional desarrollado para la tesis *Desarrollo de sistema de gestión de citas médicas mediante chatbot en WhatsApp y panel administrativo*, aplicado a la Clínica de Especialidades Médicas Integradas Dr(a). Isabella Torselli.

Universidad Mariano Gálvez de Guatemala — Ingeniería en Sistemas de Información.

---

## 1. Qué resuelve el sistema

La clínica administra sus citas de forma manual y por mensajes sueltos de WhatsApp. Eso genera tres problemas concretos que este prototipo ataca:

| Problema | Cómo lo resuelve el sistema |
|---|---|
| Agendamiento manual y lento | Chatbot de WhatsApp con flujo guiado que agenda sin intervención humana |
| Pacientes que no llegan | Recordatorios automáticos y confirmación desde el mismo chat |
| Retrasos de la doctora sin aviso | Aviso masivo a los pacientes en cola, con opción de esperar o reprogramar |

---

## 2. Estructura del proyecto

```
MediCitas/
├── database/                  Scripts SQL de creación y datos iniciales
│   ├── 01_MediCitas_Schema.sql
│   └── 02_MediCitas_DatosIniciales.sql
├── backend/MediCitas.Api/     API REST en ASP.NET Core 8
│   ├── Domain/                Entidades y enumeraciones
│   ├── Data/                  DbContext y repositorios
│   ├── DTOs/                  Objetos de entrada y salida
│   ├── Services/              Reglas de negocio
│   ├── Controllers/           Endpoints HTTP
│   ├── Common/                Utilidades transversales
│   └── BackgroundJobs/        Envío automático de recordatorios
├── frontend/                  Panel web en HTML, CSS, Bootstrap 5 y JS Vanilla
│   ├── index.html             Inicio de sesión y registro
│   ├── dashboard.html         Panel principal
│   ├── ... (10 páginas más)
│   └── assets/{css,js,img}/
└── docs/                      Documentación técnica
```

---

## 3. Requisitos

| Componente | Versión mínima | Notas |
|---|---|---|
| .NET SDK | 8.0 | Descarga gratuita desde dotnet.microsoft.com |
| SQL Server | 2019 o Express | También sirve LocalDB para desarrollo |
| Navegador | Chrome, Edge o Firefox actual | El frontend no requiere compilación |
| Editor | Visual Studio 2022 o VS Code | Opcional |

No se usa Docker, Node.js ni gestores de paquetes de frontend. Las librerías del navegador (Bootstrap, Bootstrap Icons y Chart.js) se cargan desde CDN pública.

---

## 4. Puesta en marcha local

### 4.1 Base de datos

1. Abra SQL Server Management Studio, Azure Data Studio o DBeaver.
2. Ejecute `database/01_MediCitas_Schema.sql`. Crea la base `MediCitas` con sus 15 tablas, llaves, restricciones e índices.
3. Ejecute `database/02_MediCitas_DatosIniciales.sql`. Inserta únicamente los catálogos estructurales (roles, estados de cita, parámetros) y la cuenta de administrador.

No se cargan pacientes, médicos ni citas de ejemplo: el sistema arranca vacío, como corresponde a un despliegue real.

### 4.2 API

1. Abra `backend/MediCitas.Api/appsettings.json` y ajuste la cadena `ConnectionStrings:MediCitas` a su instancia de SQL Server.
2. Desde la carpeta `backend/MediCitas.Api` ejecute:

```bash
dotnet restore
dotnet run
```

3. La API queda disponible en `http://localhost:5175` y la documentación interactiva en `http://localhost:5175/swagger`.

### 4.3 Panel web

El frontend son archivos estáticos, pero debe servirse por HTTP (no abriendo el archivo directamente) para que el navegador permita las llamadas a la API.

- **VS Code:** instale la extensión *Live Server*, haga clic derecho sobre `frontend/index.html` y elija *Open with Live Server*. Queda en `http://localhost:5500`.
- **Alternativa sin extensiones:** desde la carpeta `frontend` ejecute `python -m http.server 5500`.

Si cambia el puerto, agréguelo en `appsettings.json` dentro de `Cors:OrigenesPermitidos` y actualice `API_BASE` en `frontend/assets/js/config.js`.

### 4.4 Primer ingreso

| Campo | Valor |
|---|---|
| Correo | admin@medicitas.com |
| Contraseña | Admin!$MediCitas14 |

Cambie esta contraseña desde *Configuración* antes de cualquier demostración pública.

---

## 5. Secuencia sugerida para la primera prueba

El sistema arranca sin datos, así que el orden importa:

1. **Especialidades** → cree al menos una (ejemplo: Medicina General).
2. **Médicos** → registre un médico y asígnele esa especialidad. Defina la duración de consulta.
3. **Horarios y bloqueos** → agregue franjas horarias al médico. Sin este paso el sistema no puede ofrecer ningún cupo, porque los horarios disponibles se calculan a partir de aquí.
4. **Pacientes** → registre un paciente con su número de WhatsApp.
5. **Citas** → agende una cita y observe cómo el horario deja de aparecer como disponible.
6. **Chatbot** → en el simulador escriba el teléfono del paciente y la palabra *Hola*. Recorra el menú.
7. **Citas → Aviso de retraso** → reporte un retraso y verifique en *Chatbot* que se generaron las notificaciones para los pacientes en cola.

---

## 6. Modo de operación del chatbot

Por defecto el sistema trabaja en **modo simulación**: los mensajes se registran en la base de datos y en el log, pero no se envían a WhatsApp. Esto permite demostrar el flujo completo sin contratar la API de Meta ni exponer un número real.

Para conectar WhatsApp Business API real, edite `appsettings.json`:

```json
"Chatbot": {
  "ModoSimulacion": false,
  "ApiUrl": "https://graph.facebook.com/v20.0",
  "PhoneNumberId": "SU_PHONE_NUMBER_ID",
  "AccessToken": "SU_TOKEN",
  "VerifyToken": "SU_TOKEN_DE_VERIFICACION"
}
```

El webhook que debe registrarse en Meta es `POST /api/chatbot/mensaje`, con verificación en `GET /api/chatbot/webhook`.

---

## 7. Documentación adicional

| Archivo | Contenido |
|---|---|
| `01-Arquitectura.md` | Capas, patrones, modelo de datos y diagrama entidad-relación |
| `02-API-Endpoints.md` | Catálogo completo de endpoints REST |
| `03-Despliegue-Somee.md` | Publicación de la API y del frontend |
| `04-Defensa-Tesis.md` | Trazabilidad de objetivos y respuestas a preguntas del jurado |

---

## 8. Advertencia sobre alcance

Este es un prototipo académico, no un producto certificado para uso clínico. El sistema administra información de agendamiento (nombre, teléfono, motivo de consulta), no expedientes clínicos ni diagnósticos. Un despliegue real requeriría auditoría de seguridad, cifrado en reposo, respaldo automatizado y validación legal del tratamiento de datos personales.
