# Guía de despliegue

Todo lo descrito aquí se sostiene con planes gratuitos o de bajo costo, suficiente para los meses que dura la evaluación del proyecto.

| Componente | Servicio | Costo |
|---|---|---|
| Base de datos | SQL Server en Somee | Incluido en el plan que ya tienen |
| API | Hosting ASP.NET en Somee | Plan gratuito |
| Frontend | GitHub Pages | Gratuito |

---

## 1. Base de datos en Somee

1. Ingrese al panel de Somee y abra la sección de bases de datos SQL.
2. Anote el nombre del servidor, el de la base, el usuario y la contraseña.
3. Abra el administrador web de Somee o conéctese con SQL Server Management Studio usando esos datos.
4. Ejecute `database/01_MediCitas_Schema.sql`. Si Somee ya le entregó la base creada, elimine o comente la instrucción `CREATE DATABASE` del encabezado del script y ejecute el resto.
5. Ejecute `database/02_MediCitas_DatosIniciales.sql`.
6. Verifique el resultado:

```sql
SELECT COUNT(*) FROM Roles;          -- 3
SELECT COUNT(*) FROM EstadosCita;    -- 7
SELECT COUNT(*) FROM Configuracion;  -- 8
SELECT Email FROM Usuarios;          -- admin@medicitas.com
```

---

## 2. Preparar la API

### 2.1 Cadena de conexión

Somee entrega la cadena en el panel. Reemplace el valor de `ConnectionStrings:MediCitas` en `appsettings.json`:

```
workstation id=MediCitas.mssql.somee.com;packet size=4096;user id=SU_USUARIO;pwd=SU_PASSWORD;data source=MediCitas.mssql.somee.com;persist security info=False;initial catalog=MediCitas;TrustServerCertificate=True
```

### 2.2 Clave de firma del token

Cambie `Jwt:Key` por una cadena aleatoria de al menos 32 caracteres. La que viene en el repositorio es solo para desarrollo.

```json
"Jwt": {
  "Key": "GENERE_UNA_CLAVE_LARGA_Y_ALEATORIA_AQUI",
  "Issuer": "MediCitas",
  "Audience": "MediCitasClientes",
  "MinutosExpiracion": 480
}
```

### 2.3 CORS

Agregue la dirección pública del frontend:

```json
"Cors": {
  "OrigenesPermitidos": [
    "http://localhost:5500",
    "https://SU_USUARIO.github.io"
  ]
}
```

Si omite este paso, el panel cargará pero ninguna llamada a la API funcionará y el navegador mostrará un error de origen cruzado.

### 2.4 Publicar

Desde `backend/MediCitas.Api`:

```bash
dotnet publish -c Release -o ./publicacion
```

La carpeta `publicacion` contiene lo que debe subirse.

### 2.5 Subir a Somee

1. En el panel de Somee, cree un sitio de tipo **ASP.NET**.
2. En la configuración del sitio, seleccione **.NET Core / .NET 8** como versión de runtime.
3. Abra el gestor de archivos o conéctese por FTP con las credenciales que Somee proporciona.
4. Suba todo el contenido de `publicacion` a la carpeta raíz del sitio (`www` o equivalente).
5. Verifique que exista un `web.config` en la raíz. Si `dotnet publish` no lo generó, cree uno con este contenido, sustituyendo el nombre del ensamblado si difiere:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet"
                arguments=".\MediCitas.Api.dll"
                stdoutLogEnabled="false"
                hostingModel="inprocess" />
  </system.webServer>
</configuration>
```

### 2.6 Comprobar

Abra `https://SU_SITIO.somee.com/`. Debe responder con el estado del servicio en formato JSON. La documentación queda en `https://SU_SITIO.somee.com/swagger`.

Si aparece un error 500.30, revise la cadena de conexión: casi siempre es eso.

---

## 3. Frontend en GitHub Pages

1. Cree un repositorio público en GitHub.
2. Antes de subir, edite `frontend/assets/js/config.js`:

```javascript
const MC_CONFIG = {
    API_BASE: 'https://SU_SITIO.somee.com/api',
    NOMBRE_CLINICA: 'MediCitas'
};
```

3. Suba el contenido de la carpeta `frontend` a la raíz del repositorio.
4. En *Settings → Pages*, seleccione la rama `main` y la carpeta raíz.
5. El sitio queda en `https://SU_USUARIO.github.io/NOMBRE_REPOSITORIO/`.

### Advertencia sobre contenido mixto

GitHub Pages sirve por HTTPS. Si la API de Somee queda en HTTP, el navegador bloqueará las llamadas por contenido mixto. Confirme que el sitio de Somee tenga certificado activo y use `https://` en `API_BASE`.

### Alternativa

Azure Static Web Apps ofrece un plan gratuito equivalente y también funciona.

---

## 4. Conectar WhatsApp Business API (opcional)

El prototipo funciona completo en modo simulación. Si desea conectar un número real:

1. Cree una aplicación en Meta for Developers y agregue el producto WhatsApp.
2. Obtenga el `Phone Number ID` y un token de acceso permanente.
3. Actualice `appsettings.json`:

```json
"Chatbot": {
  "ModoSimulacion": false,
  "ApiUrl": "https://graph.facebook.com/v20.0",
  "PhoneNumberId": "SU_PHONE_NUMBER_ID",
  "AccessToken": "SU_TOKEN",
  "VerifyToken": "UNA_CADENA_QUE_USTED_DEFINA"
}
```

4. En la configuración del webhook de Meta, registre `https://SU_SITIO.somee.com/api/chatbot/webhook` y use el mismo `VerifyToken`. Meta enviará una petición GET de verificación que la API responde automáticamente.
5. Suscríbase al evento `messages`.

Advertencia: en modo de prueba, Meta solo permite enviar mensajes a números previamente verificados. Para producción se requiere verificación del negocio, que implica documentación legal de la clínica.

**Para la defensa de la tesis, el modo simulación es la opción sensata.** Evita depender de conectividad, de la aprobación de Meta y de mensajes que podrían no llegar durante la exposición. El flujo demostrado es exactamente el mismo.

---

## 5. Lista de verificación antes de la entrega

- [ ] Los scripts SQL se ejecutaron completos y sin errores
- [ ] `Jwt:Key` fue reemplazada por una clave propia
- [ ] La contraseña del administrador fue cambiada
- [ ] La URL del frontend está registrada en `Cors:OrigenesPermitidos`
- [ ] `API_BASE` en `config.js` apunta a la API publicada
- [ ] Swagger responde en la URL pública
- [ ] El inicio de sesión funciona desde el sitio publicado
- [ ] Se creó al menos una especialidad, un médico y su horario laboral
- [ ] El simulador del chatbot completa un agendamiento
- [ ] El aviso de retraso genera notificaciones visibles
