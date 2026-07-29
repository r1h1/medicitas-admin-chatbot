/* Parámetros del sistema, cambio de contraseña e historial de actividad. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'configuracion',
        titulo: 'Configuración',
        subtitulo: 'Parámetros operativos del sistema e historial de actividad',
        roles: ['Administrador']
    });

    if (!usuario) return;

    iniciar();

    async function iniciar() {
        document.getElementById('btn-password').onclick = cambiarPassword;
        await cargarConfiguracion();
        await cargarBitacora();
    }

    async function cargarConfiguracion() {
        const contenedor = document.getElementById('lista-configuracion');

        try {
            const parametros = await Api.get('/configuracion');

            contenedor.innerHTML = parametros.map(p => `
                <div>
                    <label class="form-label" for="cfg-${p.clave}">${UI.escapar(p.descripcion || p.clave)}</label>
                    <div class="input-group">
                        <input class="form-control" id="cfg-${p.clave}" value="${UI.escapar(p.valor)}">
                        <button class="btn btn-mc-primary" data-clave="${UI.escapar(p.clave)}">Guardar</button>
                    </div>
                    <span class="mc-texto-muted">Clave técnica: ${UI.escapar(p.clave)}</span>
                </div>`).join('');

            contenedor.querySelectorAll('[data-clave]').forEach(boton => {
                boton.onclick = () => guardarParametro(boton, boton.dataset.clave);
            });
        } catch (error) {
            contenedor.innerHTML = `<p class="mc-texto-muted mb-0">${UI.escapar(error.message)}</p>`;
        }
    }

    async function guardarParametro(boton, clave) {
        const valor = document.getElementById(`cfg-${clave}`).value.trim();
        if (!valor) return UI.aviso('El valor no puede quedar vacío.');

        await UI.conEspera(boton, async () => {
            try {
                await Api.put(`/configuracion/${encodeURIComponent(clave)}`, { clave, valor });
                UI.exito('Parámetro actualizado.');
                await cargarBitacora();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function cambiarPassword() {
        const actual = document.getElementById('pass-actual').value;
        const nueva = document.getElementById('pass-nueva').value;

        if (!actual || nueva.length < 8) return UI.aviso('Escriba la contraseña actual y una nueva de al menos 8 caracteres.');

        await UI.conEspera(document.getElementById('btn-password'), async () => {
            try {
                await Api.post('/auth/cambiar-password', { passwordActual: actual, passwordNueva: nueva });
                UI.exito('Contraseña actualizada.');
                document.getElementById('pass-actual').value = '';
                document.getElementById('pass-nueva').value = '';
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function cargarBitacora() {
        const cuerpo = document.getElementById('tabla-bitacora');
        UI.cargando(cuerpo, 4);

        try {
            const eventos = await Api.get('/configuracion/bitacora', { cantidad: 80 });

            cuerpo.innerHTML = eventos.length
                ? eventos.map(e => `
                    <tr>
                        <td>${UI.fechaHora(e.fechaHora)}</td>
                        <td class="mc-principal">${UI.escapar(e.usuario)}</td>
                        <td>${UI.escapar(e.accion)}</td>
                        <td>${UI.escapar(e.detalle || e.entidad || '—')}</td>
                    </tr>`).join('')
                : UI.filaVacia(4, 'Sin actividad registrada.', 'bi-journal-text');
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(4, error.message, 'bi-exclamation-triangle');
        }
    }
})();
