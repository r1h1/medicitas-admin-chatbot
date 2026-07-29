# Catálogo de endpoints REST

Base: `/api`. Todas las respuestas comparten el mismo sobre:

```json
{
  "exito": true,
  "mensaje": "Operación realizada correctamente",
  "datos": { },
  "errores": []
}
```

Ante un error, `exito` es `false`, `datos` es `null` y `errores` contiene el detalle de las validaciones fallidas.

## Códigos de estado utilizados

| Código | Cuándo se devuelve |
|---|---|
| 200 | Consulta o actualización correcta |
| 201 | Recurso creado |
| 400 | Datos de entrada inválidos |
| 401 | Falta el token o expiró |
| 403 | El rol no autoriza la operación |
| 404 | El recurso no existe |
| 409 | Conflicto con una regla de negocio (horario ocupado, cancelación fuera de plazo) |
| 500 | Error no controlado |

Todas las rutas exigen `Authorization: Bearer {token}` salvo donde se indique lo contrario.

---

## Autenticación — `/api/auth`

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| POST | `/login` | Anónimo | Valida credenciales y devuelve el token |
| POST | `/registro` | Anónimo | Registro público de un paciente |
| GET | `/perfil` | Autenticado | Datos del usuario en sesión |
| POST | `/cambiar-password` | Autenticado | Cambio de la propia contraseña |

**POST /login**

```json
{ "email": "admin@medicitas.com", "password": "Admin!$MediCitas14" }
```

Respuesta: token, fecha de expiración y los datos del usuario, incluidos `pacienteId` y `medicoId` cuando la cuenta está vinculada.

Un correo inexistente y una contraseña incorrecta devuelven el mismo mensaje, de forma deliberada.

---

## Usuarios — `/api/usuarios`

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/roles` | Administrador | Lista los roles disponibles (excluye Paciente) |
| GET | `/` | Administrador | Lista todas las cuentas |
| GET | `/{id}` | Administrador | Detalle de una cuenta |
| POST | `/` | Administrador | Crea una cuenta con rol |
| PUT | `/{id}` | Administrador | Actualiza datos y rol |
| DELETE | `/{id}` | Administrador | Desactiva la cuenta (no la borra) |

`/roles` devuelve los roles que se pueden asignar desde el panel: Administrador (1), Recepcionista (2) y Medico (4). El rol Paciente existe en la base de datos pero no se expone aquí porque los pacientes solo usan el chatbot.

La cuenta `admin@medicitas.com` no puede desactivarse. El correo no es editable después de la creación.

Al crear un usuario con rol Médico es obligatorio vincularlo a un médico registrado en el sistema mediante `medicoId`. Ese vínculo determina qué citas y pacientes puede ver.

---

## Especialidades — `/api/especialidades`

| Método | Ruta | Rol | Parámetros |
|---|---|---|---|
| GET | `/` | Personal | `soloActivas` (bool) |
| GET | `/{id}` | Personal | — |
| POST | `/` | Administrador | `nombre`, `descripcion`, `activo` |
| PUT | `/{id}` | Administrador | igual que POST |
| DELETE | `/{id}` | Administrador | Rechaza el borrado si tiene médicos asignados |

---

## Médicos — `/api/medicos`

| Método | Ruta | Rol | Parámetros |
|---|---|---|---|
| GET | `/` | Personal | `especialidadId`, `soloActivos` |
| GET | `/{id}` | Personal | — |
| GET | `/{id}/agenda` | Personal | `fecha` — citas del médico ese día |
| POST | `/` | Administrador | Datos del médico |
| PUT | `/{id}` | Administrador | Datos del médico |
| DELETE | `/{id}` | Administrador | Rechaza el borrado si tiene citas vigentes |

`duracionCitaMinutos` determina el tamaño de los bloques de disponibilidad de ese médico.

---

## Pacientes — `/api/pacientes`

| Método | Ruta | Rol | Parámetros |
|---|---|---|---|
| GET | `/` | Personal o Médico | `busqueda` — nombre, apellido o teléfono; `medicoId` — filtra por médico (el rol Médico lo aplica automáticamente) |
| GET | `/{id}` | Personal o el propio paciente | — |
| GET | `/{id}/citas` | Personal o el propio paciente | Historial completo |
| GET | `/por-telefono/{telefono}` | Personal | Usado por el chatbot |
| POST | `/` | Personal | Datos del paciente |
| PUT | `/{id}` | Personal | Datos del paciente |
| DELETE | `/{id}` | Administrador | Rechaza el borrado si tiene citas vigentes |

El teléfono es único en todo el sistema.

---

## Agenda — `/api/agenda`

### Horario laboral

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/horarios` | Personal | Filtrable por `medicoId` |
| POST | `/horarios` | Personal | Crea una franja |
| PUT | `/horarios/{id}` | Personal | Actualiza la franja |
| DELETE | `/horarios/{id}` | Personal | Elimina la franja |

`diaSemana` va de 0 (domingo) a 6 (sábado). Se valida que `horaFin` sea posterior a `horaInicio` y que la franja no se traslape con otra del mismo médico y día.

### Bloqueos

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/bloqueos` | Personal | Filtrable por `medicoId` |
| POST | `/bloqueos` | Personal | Crea un bloqueo |
| DELETE | `/bloqueos/{id}` | Personal | Libera el bloqueo |

`medicoId` nulo bloquea a toda la clínica. `horaInicio` nula bloquea el día completo.

### Disponibilidad

| Método | Ruta | Rol | Parámetros |
|---|---|---|---|
| GET | `/disponibilidad` | Autenticado | `medicoId`, `fecha`, `excluirCitaId` |
| GET | `/disponibilidad/rango` | Autenticado | `medicoId`, `desde`, `hasta` (máximo 60 días) |
| GET | `/disponibilidad/proximas` | Autenticado | `medicoId`, `cantidad` |

`excluirCitaId` sirve al reprogramar: permite que el horario actual de la cita aparezca como disponible para ella misma.

`/disponibilidad/proximas` es lo que alimenta la respuesta del chatbot cuando el paciente pide las fechas más cercanas.

Respuesta de `/disponibilidad`:

```json
{
  "fecha": "2026-08-03",
  "medicoId": 1,
  "medico": "Isabella Torselli",
  "bloqueado": false,
  "motivoBloqueo": null,
  "slots": [
    { "inicio": "2026-08-03T08:00:00", "fin": "2026-08-03T08:30:00", "etiqueta": "08:00 AM", "disponible": true }
  ]
}
```

---

## Citas — `/api/citas`

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/` | Personal | Filtros: `desde`, `hasta`, `medicoId`, `pacienteId`, `estadoId` |
| GET | `/{id}` | Personal o el propio paciente | Detalle |
| POST | `/` | Personal o paciente | Crea la cita |
| PUT | `/{id}/reprogramar` | Personal o el propio paciente | Mueve la cita |
| PUT | `/{id}/cancelar` | Personal o el propio paciente | Cancela y libera el horario |
| PUT | `/{id}/confirmar` | Personal o el propio paciente | Marca como confirmada |
| PUT | `/{id}/estado` | Personal | Cambia a EnAtencion, Completada o NoAsistio |

**Reglas validadas al crear o reprogramar**

1. El horario debe corresponder a un bloque real del horario laboral del médico.
2. El horario no puede estar ocupado por otra cita vigente.
3. La fecha no puede estar en el pasado.
4. No puede exceder `DiasMaximosAgendamiento` (60 días por defecto).
5. No puede haber un bloqueo de agenda que cubra esa fecha y hora.
6. El paciente no puede tener otra cita vigente el mismo día con el mismo médico.
7. Cancelar o reprogramar exige `CancelacionHorasMinimas` de anticipación (4 horas por defecto), salvo cuando la reprogramación se origina en un retraso del médico.

Al reprogramar, la cita original queda en estado *Reprogramada* y se genera una nueva con `citaOriginalId` apuntando a ella.

---

## Retrasos — `/api/retrasos`

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/` | Personal | Filtros `fecha` y `medicoId` |
| POST | `/` | Personal | Reporta el retraso y avisa a la cola |
| POST | `/respuesta` | Autenticado | Registra la decisión del paciente |

**POST /retrasos**

```json
{ "medicoId": 1, "fecha": "2026-07-28", "minutosRetraso": 25, "motivo": "Emergencia atendida" }
```

Devuelve el registro creado con `pacientesNotificados`, que es la cantidad de personas en cola a las que se les envió el aviso.

**POST /retrasos/respuesta**

`decision` igual a 1 mantiene la cita; igual a 2 la reprograma al horario indicado en `nuevaFechaHoraInicio`.

---

## Chatbot — `/api/chatbot`

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| POST | `/mensaje` | Anónimo | Procesa un mensaje entrante |
| GET | `/webhook` | Anónimo | Verificación exigida por Meta |
| GET | `/conversaciones` | Personal | Lista las conversaciones |
| GET | `/conversaciones/{id}/mensajes` | Personal | Historial de una conversación |
| POST | `/conversaciones/reiniciar` | Personal | Devuelve la conversación al menú |

`/mensaje` es anónimo porque WhatsApp no puede autenticarse con JWT. El mismo endpoint atiende al webhook real y al simulador del panel, de modo que existe un solo flujo conversacional y no dos implementaciones que puedan divergir.

```json
{ "telefono": "50255550000", "texto": "1" }
```

Respuesta:

```json
{
  "telefono": "50255550000",
  "respuesta": "Seleccione la especialidad:\n1. Medicina General",
  "estadoFlujo": "AGENDAR_ESPECIALIDAD",
  "opciones": ["1"]
}
```

---

## Notificaciones — `/api/notificaciones`

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/` | Autenticado | Un paciente solo ve las suyas |
| POST | `/procesar` | Personal | Fuerza el envío de las pendientes |

El envío automático corre en segundo plano cada 60 segundos. `/procesar` existe para poder demostrarlo en vivo sin esperar el ciclo.

---

## Dashboard y reportes — `/api/dashboard`

| Método | Ruta | Rol | Parámetros |
|---|---|---|---|
| GET | `/` | Personal | `fecha` (opcional, por defecto hoy) |
| GET | `/reporte` | Personal | `desde`, `hasta` |

El reporte devuelve total de citas, atendidas, canceladas, inasistencias, tasa de ausentismo, tasa de confirmación, distribución por canal de origen, recordatorios enviados, retrasos reportados y desgloses por médico, especialidad y día. Son las métricas que la tesis plantea medir.

---

## Configuración — `/api/configuracion`

| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/` | Personal | Lista los parámetros |
| PUT | `/{clave}` | Administrador | Actualiza un parámetro |
| GET | `/bitacora` | Personal | Historial de actividad, parámetro `cantidad` |
