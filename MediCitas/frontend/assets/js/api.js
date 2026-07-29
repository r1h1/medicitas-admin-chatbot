/* Cliente HTTP único del sistema.
   Centraliza el token, el manejo de errores y el formato de respuesta de la API. */
const Api = (() => {
    const TOKEN_KEY = 'mc_token';
    const USUARIO_KEY = 'mc_usuario';

    const obtenerToken = () => localStorage.getItem(TOKEN_KEY);

    const guardarSesion = (token, usuario) => {
        localStorage.setItem(TOKEN_KEY, token);
        localStorage.setItem(USUARIO_KEY, JSON.stringify(usuario));
    };

    const obtenerUsuario = () => {
        try { return JSON.parse(localStorage.getItem(USUARIO_KEY)); }
        catch { return null; }
    };

    const limpiarSesion = () => {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USUARIO_KEY);
    };

    async function solicitar(metodo, ruta, cuerpo) {
        const cabeceras = { 'Content-Type': 'application/json' };
        const token = obtenerToken();
        if (token) cabeceras['Authorization'] = `Bearer ${token}`;

        let respuesta;
        try {
            respuesta = await fetch(`${MC_CONFIG.API_BASE}${ruta}`, {
                method: metodo,
                headers: cabeceras,
                body: cuerpo ? JSON.stringify(cuerpo) : undefined
            });
        } catch {
            throw new Error('No se pudo conectar con el servidor. Verifique su conexión.');
        }

        if (respuesta.status === 401) {
            limpiarSesion();
            if (!location.pathname.endsWith('index.html') && location.pathname !== '/') {
                location.href = 'index.html';
            }
            throw new Error('Su sesión expiró. Inicie sesión de nuevo.');
        }

        if (respuesta.status === 204) return null;

        const datos = await respuesta.json().catch(() => null);

        if (!respuesta.ok) {
            const detalle = datos?.errores?.length ? datos.errores.join(' ') : '';
            throw new Error(`${datos?.mensaje || 'Ocurrió un error al procesar la solicitud'} ${detalle}`.trim());
        }

        return datos?.datos !== undefined ? datos.datos : datos;
    }

    const construirQuery = (parametros = {}) => {
        const query = Object.entries(parametros)
            .filter(([, valor]) => valor !== undefined && valor !== null && valor !== '')
            .map(([clave, valor]) => `${encodeURIComponent(clave)}=${encodeURIComponent(valor)}`)
            .join('&');
        return query ? `?${query}` : '';
    };

    return {
        get: (ruta, parametros) => solicitar('GET', ruta + construirQuery(parametros)),
        post: (ruta, cuerpo) => solicitar('POST', ruta, cuerpo),
        put: (ruta, cuerpo) => solicitar('PUT', ruta, cuerpo),
        del: (ruta) => solicitar('DELETE', ruta),
        guardarSesion, obtenerUsuario, obtenerToken, limpiarSesion
    };
})();
