# Guía de defensa

Documento de apoyo para sustentar el prototipo ante el jurado.

---

## 1. Trazabilidad: objetivos de la tesis frente a lo implementado

| Objetivo planteado | Dónde se cumple | Cómo se demuestra |
|---|---|---|
| Automatizar el agendamiento mediante chatbot | `ChatbotService`, endpoint `/api/chatbot/mensaje` | Simulador del panel: recorrer el flujo completo hasta confirmar una cita |
| Reducir la inasistencia con recordatorios | `NotificacionService`, `RecordatoriosWorker` | Pantalla de Chatbot, tabla de notificaciones |
| Permitir la confirmación de citas | Opción 3 del menú y `PUT /api/citas/{id}/confirmar` | Confirmar desde el chat y verificar el cambio de estado en el panel |
| Notificar retrasos del médico | `RetrasoService`, `POST /api/retrasos` | Reportar un retraso y mostrar las notificaciones generadas |
| Reducir el tiempo administrativo | Panel completo con CRUD y disponibilidad calculada | Agendar una cita en menos de un minuto frente al proceso manual |
| Medir la mejora del proceso | `DashboardService.ReporteGeneralAsync` | Pantalla de Reportes con tasa de ausentismo y de confirmación |

## 2. Requisitos del documento del cliente

| Requisito textual del cliente | Implementación |
|---|---|
| Menú con opción para paciente nuevo y de seguimiento | Campo `TipoConsulta` en la cita; el chatbot registra al paciente nuevo antes de agendar |
| Reagendamiento desde WhatsApp | Opción 4 del menú, estado `SELECCION_CITA_REPROGRAMAR` |
| Mostrar las fechas disponibles más próximas | `GET /api/agenda/disponibilidad/proximas` |
| Avisar del retraso y ofrecer esperar o reprogramar | Estado `RETRASO_DECISION`; la opción 1 mantiene la cita, la 2 la reprograma |
| Avisar a los pacientes que van en cola | `RetrasoService` recorre todas las citas vigentes posteriores a la hora actual |
| Panel con calendario y reloj que alimenta la información | Encabezado con fecha y hora en vivo; el dashboard consume la agenda real |

---

## 3. Preguntas previsibles del jurado

### ¿Por qué no usó inteligencia artificial en el chatbot?

Porque el marco teórico de la tesis lo delimita de forma explícita: el chatbot se restringe a flujos predefinidos y respuestas estructuradas, sin aprendizaje profundo ni procesamiento de lenguaje natural complejo. La implementación respeta ese alcance mediante una máquina de estados finita.

Además hay una razón de ingeniería: en un contexto médico, un modelo generativo puede producir respuestas impredecibles. Un flujo determinista garantiza que el paciente reciba siempre la misma información correcta. El alcance del sistema es estrictamente administrativo, nunca clínico.

### ¿Cómo evita que dos pacientes reserven el mismo horario?

Con dos capas. La primera es una validación en `CitaService` antes de insertar. La segunda, y la que realmente garantiza la integridad, es un índice único filtrado en la base de datos sobre `(MedicoId, FechaHoraInicio)` que solo aplica a las citas en estado pendiente, confirmada o en atención.

Si dos solicitudes llegan simultáneamente —una desde WhatsApp y otra desde el panel—, la validación de aplicación podría dejar pasar ambas, pero el motor rechaza la segunda inserción. El filtro por estado permite además que un horario cancelado vuelva a quedar libre.

### ¿Dónde se guardan los horarios disponibles?

En ningún lado. Se calculan en el momento a partir del horario laboral del médico, dividido en bloques según la duración de su consulta, descartando los bloques ocupados por citas vigentes y los cubiertos por bloqueos de agenda.

Almacenarlos obligaría a mantener sincronizada esa tabla en cada alta, cancelación y reprogramación, con riesgo de inconsistencia. Calcularlos elimina esa clase entera de errores.

### ¿Cómo se protegen las contraseñas?

Con PBKDF2 sobre SHA-256, 100.000 iteraciones y un salt aleatorio distinto por usuario. Nunca se almacena la contraseña en texto plano ni un hash simple, que sería vulnerable a tablas precalculadas. La comparación usa una función de tiempo constante para no filtrar información por diferencias de temporización.

### ¿Por qué cuatro roles en lugar de tres?

El diseño original contemplaba tres roles: Administrador, Recepcionista y Paciente. Durante la implementación se identificó que el médico necesita acceder al panel para ver sus propias citas y pacientes, reportar retrasos y gestionar su agenda — funciones que la recepcionista no debería poder hacer en nombre del médico sin control.

Se agregó el rol **Médico** con visibilidad restringida: al iniciar sesión solo ve sus propias citas y los pacientes que tienen citas con él. No puede ver la agenda de otros médicos ni acceder a la configuración del sistema.

El rol **Paciente** existe en la base de datos por compatibilidad con el chatbot (el sistema necesita identificar al usuario del chatbot en el token JWT), pero no se expone en el panel — los pacientes solo interactúan por WhatsApp.

Esta decisión se tomó por petición funcional del cliente: la Dra. Torselli necesita acceso directo a su propia agenda sin depender de la recepcionista para consultar sus citas del día.

### ¿Qué pasa si el servidor se reinicia en medio de una conversación?

Nada se pierde. El estado del flujo y el contexto parcial de cada conversación se persisten en la tabla `ConversacionesChatbot`. Al llegar el siguiente mensaje, el servicio recupera el estado desde la base y continúa donde iba. Si el estado se guardara en memoria, un reinicio dejaría a todos los pacientes con conversaciones rotas.

### ¿Cómo escalaría el sistema?

Para el volumen de una clínica no hace falta escalar. Si fuera necesario, la arquitectura en capas permite tres pasos sin reescribir nada: separar la base de datos en un servidor propio, ejecutar varias instancias de la API detrás de un balanceador (los controladores no guardan estado, así que es viable), y mover el worker de recordatorios a un proceso independiente.

Deliberadamente no se incluyeron microservicios ni contenedores: introducirlos en un sistema de este tamaño agrega complejidad operativa que nadie en la clínica podría mantener.

### ¿Por qué el modo simulación en lugar de WhatsApp real?

Es una decisión de riesgo. La API de WhatsApp Business exige verificación del negocio con documentación legal de la clínica, y en modo de prueba solo permite enviar a números previamente registrados. Depender de eso durante la defensa significaría depender de la conectividad y de la aprobación de un tercero.

El código para el envío real está implementado y se activa cambiando un valor en la configuración. El flujo conversacional que se demuestra es idéntico; lo único que cambia es el transporte del mensaje.

### ¿Qué limitaciones reconoce el prototipo?

Es honesto señalarlas antes de que las pregunten:

- No gestiona expedientes clínicos ni diagnósticos, solo agendamiento.
- No implementa autenticación de dos factores, por decisión de alcance.
- El chatbot no entiende lenguaje libre: exige que el paciente responda con números u opciones.
- No incluye pruebas automatizadas, que serían exigibles en un producto real.
- No hay cifrado en reposo de la base de datos.
- Un despliegue productivo requeriría auditoría de seguridad y validación legal del tratamiento de datos personales.

---

## 4. Guion sugerido de demostración (10 minutos)

**Minuto 0–1. Contexto.** El problema: agendamiento manual, inasistencias y retrasos sin aviso.

**Minuto 1–2. Ingreso.** Mostrar el inicio de sesión y explicar los tres roles.

**Minuto 2–4. Configuración inicial.** Crear una especialidad, un médico con su duración de consulta, y su horario laboral. Enfatizar que este último paso es el que habilita los cupos.

**Minuto 4–6. Agendamiento desde el panel.** Registrar un paciente, agendar una cita y mostrar cómo el horario desaparece de los disponibles.

**Minuto 6–8. Chatbot.** En el simulador, escribir *Hola* con el teléfono del paciente y recorrer el flujo: consultar sus citas, luego agendar una nueva. Volver al panel y mostrar que la cita aparece con canal de origen WhatsApp.

**Minuto 8–9. Aviso de retraso.** Reportar 25 minutos de retraso. Mostrar las notificaciones generadas y explicar que cada paciente en cola recibe la opción de esperar o reprogramar. Este es el diferenciador del proyecto.

**Minuto 9–10. Reportes.** Abrir la pantalla de reportes y mostrar tasa de ausentismo, tasa de confirmación y distribución por canal. Cerrar señalando que son exactamente las métricas que la investigación plantea medir.

---

## 5. Justificación de la selección tecnológica

| Decisión | Razón |
|---|---|
| ASP.NET Core 8 | Versión con soporte extendido, multiplataforma, sin licencia |
| SQL Server | Ya disponible en el hosting contratado; motor maduro con soporte transaccional |
| Entity Framework Core | Reduce el código repetitivo de acceso a datos manteniendo control sobre las consultas |
| JavaScript sin framework | El proyecto no justifica una capa de compilación; el resultado se despliega como archivos estáticos y es más fácil de mantener y de explicar |
| Bootstrap 5 | Diseño adaptable sin escribir CSS desde cero |
| Chart.js | Librería de gráficas ligera, de licencia abierta, sin dependencias |
| JWT | Estándar sin estado; permite escalar horizontalmente sin sesiones compartidas |
| PBKDF2 | Incluido en .NET, sin agregar dependencias externas al proyecto |
