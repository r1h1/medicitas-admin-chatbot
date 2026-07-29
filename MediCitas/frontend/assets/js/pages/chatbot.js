/* Consola del chatbot: simulador del flujo, conversaciones y avisos enviados. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'chatbot',
        titulo: 'Chatbot de WhatsApp',
        subtitulo: 'Pruebe el flujo de atención y revise las conversaciones de los pacientes',
        roles: ['Administrador', 'Recepcionista']
    });

    if (!usuario) return;

    const ATAJOS = ['Hola', '1', '2', '3', '4', '5', 'menu'];
    let modalConversacion;

    iniciar();

    async function iniciar() {
        modalConversacion = new bootstrap.Modal(document.getElementById('modal-conversacion'));

        document.getElementById('btn-enviar').onclick = enviar;
        document.getElementById('btn-reiniciar').onclick = reiniciar;
        document.getElementById('btn-procesar').onclick = procesarPendientes;
        document.getElementById('sim-texto').onkeyup = (e) => { if (e.key === 'Enter') enviar(); };

        document.getElementById('atajos').innerHTML = ATAJOS
            .map(t => `<button class="btn btn-mc-secundario btn-sm" data-atajo="${t}">${t}</button>`).join('');

        document.querySelectorAll('[data-atajo]').forEach(boton => {
            boton.onclick = () => {
                document.getElementById('sim-texto').value = boton.dataset.atajo;
                enviar();
            };
        });

        pintarAviso('Escriba "Hola" para iniciar el flujo de atención.');

        await cargarConversaciones();
        await cargarNotificaciones();
    }

    // ------------------------------ Simulador ------------------------------
    function pintar(texto, clase) {
        const chat = document.getElementById('chat');
        const burbuja = document.createElement('div');
        burbuja.className = `mc-burbuja ${clase}`;
        burbuja.innerHTML = `${UI.escapar(texto)}<small>${new Date().toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit' })}</small>`;
        chat.appendChild(burbuja);
        chat.scrollTop = chat.scrollHeight;
    }

    function pintarAviso(texto) {
        document.getElementById('chat').innerHTML =
            `<p class="mc-texto-muted text-center my-auto">${UI.escapar(texto)}</p>`;
    }

    async function enviar() {
        const campo = document.getElementById('sim-texto');
        const telefono = document.getElementById('sim-telefono').value.trim();
        const texto = campo.value.trim();

        if (!telefono) return UI.aviso('Escriba un número de teléfono.');
        if (!texto) return;

        if (document.getElementById('chat').querySelector('p')) document.getElementById('chat').innerHTML = '';

        pintar(texto, 'entrante');
        campo.value = '';

        try {
            const respuesta = await Api.post('/chatbot/mensaje', { telefono, texto });
            pintar(respuesta.respuesta, 'saliente');

            await cargarConversaciones();
            await cargarNotificaciones();
        } catch (error) {
            pintar(`No se pudo procesar el mensaje: ${error.message}`, 'saliente');
        }
    }

    async function reiniciar() {
        const telefono = document.getElementById('sim-telefono').value.trim();
        if (!telefono) return UI.aviso('Escriba un número de teléfono.');

        try {
            await Api.post(`/chatbot/conversaciones/reiniciar?telefono=${encodeURIComponent(telefono)}`);
            pintarAviso('Conversación reiniciada. Escriba "Hola" para comenzar de nuevo.');
            UI.exito('Conversación reiniciada.');
            await cargarConversaciones();
        } catch (error) {
            UI.error(error.message);
        }
    }

    // ------------------------------ Conversaciones ------------------------------
    async function cargarConversaciones() {
        const cuerpo = document.getElementById('tabla-conversaciones');

        try {
            const conversaciones = await Api.get('/chatbot/conversaciones');

            if (!conversaciones.length) {
                cuerpo.innerHTML = UI.filaVacia(6, 'Todavía no hay conversaciones registradas.', 'bi-chat-dots');
                return;
            }

            cuerpo.innerHTML = conversaciones.map(c => `
                <tr>
                    <td class="mc-principal"><i class="bi bi-whatsapp" style="color:var(--whatsapp)"></i> ${UI.escapar(c.telefono)}</td>
                    <td>${UI.escapar(c.paciente)}</td>
                    <td><span class="mc-estado mc-estado-enatencion">${UI.escapar(c.estadoFlujo)}</span></td>
                    <td>${UI.fechaHora(c.ultimaInteraccion)}</td>
                    <td>${c.totalMensajes}</td>
                    <td><button class="btn-mc-icono" title="Ver historial" data-ver="${c.id}"><i class="bi bi-eye"></i></button></td>
                </tr>`).join('');

            cuerpo.querySelectorAll('[data-ver]').forEach(b =>
                b.onclick = () => verHistorial(Number(b.dataset.ver)));
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(6, error.message, 'bi-exclamation-triangle');
        }
    }

    async function verHistorial(id) {
        const chat = document.getElementById('chat-historial');
        chat.innerHTML = '<p class="mc-texto-muted text-center my-auto">Cargando…</p>';
        modalConversacion.show();

        try {
            const mensajes = await Api.get(`/chatbot/conversaciones/${id}/mensajes`);

            chat.innerHTML = mensajes.length
                ? mensajes.map(m => `
                    <div class="mc-burbuja ${m.direccion === 1 ? 'entrante' : 'saliente'}">
                        ${UI.escapar(m.contenido)}
                        <small>${UI.fechaHora(m.fechaHora)}</small>
                    </div>`).join('')
                : '<p class="mc-texto-muted text-center my-auto">Sin mensajes.</p>';
        } catch (error) {
            chat.innerHTML = `<p class="mc-texto-muted text-center my-auto">${UI.escapar(error.message)}</p>`;
        }
    }

    // ------------------------------ Notificaciones ------------------------------
    async function cargarNotificaciones() {
        const cuerpo = document.getElementById('tabla-notificaciones');

        try {
            const notificaciones = await Api.get('/notificaciones');

            if (!notificaciones.length) {
                cuerpo.innerHTML = UI.filaVacia(5, 'No hay notificaciones generadas.', 'bi-bell');
                return;
            }

            cuerpo.innerHTML = notificaciones.slice(0, 25).map(n => `
                <tr>
                    <td class="mc-principal">${UI.escapar(n.paciente)}</td>
                    <td>${UI.escapar(n.tipoNombre)}</td>
                    <td style="max-width:280px">${UI.escapar(n.mensaje.substring(0, 90))}${n.mensaje.length > 90 ? '…' : ''}</td>
                    <td>${UI.fechaHora(n.fechaProgramada)}</td>
                    <td>${estadoNotificacion(n.estadoNombre)}</td>
                </tr>`).join('');
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(5, error.message, 'bi-exclamation-triangle');
        }
    }

    function estadoNotificacion(estado) {
        const clases = {
            'Pendiente': 'mc-estado-pendiente',
            'Enviada': 'mc-estado-confirmada',
            'Fallida': 'mc-estado-cancelada',
            'Respondida': 'mc-estado-completada'
        };
        return `<span class="mc-estado ${clases[estado] || 'mc-estado-noasistio'}">${UI.escapar(estado)}</span>`;
    }

    async function procesarPendientes() {
        await UI.conEspera(document.getElementById('btn-procesar'), async () => {
            try {
                const enviadas = await Api.post('/notificaciones/procesar');
                UI.exito(`${enviadas} notificación(es) enviadas.`);
                await cargarNotificaciones();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }
})();
