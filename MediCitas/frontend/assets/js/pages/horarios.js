/* Configuración de la agenda: horario laboral y bloqueos de fechas. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'horarios',
        titulo: 'Horarios y bloqueos',
        subtitulo: 'De esta configuración depende qué cupos ofrece el sistema a los pacientes',
        roles: ['Administrador', 'Recepcionista']
    });

    if (!usuario) return;

    const DIAS = ['Domingo', 'Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado'];
    let medicos = [];
    let modalHorario, modalBloqueo;

    iniciar();

    async function iniciar() {
        modalHorario = new bootstrap.Modal(document.getElementById('modal-horario'));
        modalBloqueo = new bootstrap.Modal(document.getElementById('modal-bloqueo'));

        document.getElementById('btn-nuevo-horario').onclick = () => abrirHorario();
        document.getElementById('btn-guardar-horario').onclick = guardarHorario;
        document.getElementById('btn-nuevo-bloqueo').onclick = () => modalBloqueo.show();
        document.getElementById('btn-guardar-bloqueo').onclick = guardarBloqueo;
        document.getElementById('f-medico').onchange = cargarHorarios;

        medicos = await Api.get('/medicos');
        const opciones = medicos.map(m => `<option value="${m.id}">${UI.escapar(m.nombreCompleto)}</option>`).join('');

        document.getElementById('f-medico').innerHTML = '<option value="">Todos</option>' + opciones;
        document.getElementById('h-medico').innerHTML = opciones || '<option value="">Registre primero un médico</option>';
        document.getElementById('b-medico').innerHTML = '<option value="">Toda la clínica</option>' + opciones;

        const medicoUrl = new URLSearchParams(location.search).get('medico');
        if (medicoUrl) document.getElementById('f-medico').value = medicoUrl;

        const hoy = UI.isoFecha(new Date());
        document.getElementById('b-inicio').value = hoy;
        document.getElementById('b-fin').value = hoy;

        await cargarHorarios();
        await cargarBloqueos();
    }

    async function cargarHorarios() {
        const cuerpo = document.getElementById('tabla-horarios');
        UI.cargando(cuerpo, 6);

        try {
            const horarios = await Api.get('/agenda/horarios', { medicoId: document.getElementById('f-medico').value });

            if (!horarios.length) {
                cuerpo.innerHTML = UI.filaVacia(6, 'Sin horarios configurados. Sin ellos el sistema no puede ofrecer cupos.', 'bi-clock');
                return;
            }

            cuerpo.innerHTML = horarios.map(h => `
                <tr>
                    <td class="mc-principal">${UI.escapar(h.medico)}</td>
                    <td>${DIAS[h.diaSemana]}</td>
                    <td>${h.horaInicio.substring(0, 5)}</td>
                    <td>${h.horaFin.substring(0, 5)}</td>
                    <td>${h.activo ? '<span class="mc-estado mc-estado-confirmada">Activo</span>' : '<span class="mc-estado mc-estado-noasistio">Inactivo</span>'}</td>
                    <td class="text-nowrap">
                        <button class="btn-mc-icono" title="Editar" data-editar="${h.id}"><i class="bi bi-pencil"></i></button>
                        <button class="btn-mc-icono peligro" title="Eliminar" data-eliminar="${h.id}"><i class="bi bi-trash"></i></button>
                    </td>
                </tr>`).join('');

            cuerpo.querySelectorAll('[data-editar]').forEach(b =>
                b.onclick = () => abrirHorario(horarios.find(h => h.id === Number(b.dataset.editar))));

            cuerpo.querySelectorAll('[data-eliminar]').forEach(b =>
                b.onclick = () => eliminarHorario(Number(b.dataset.eliminar)));
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(6, error.message, 'bi-exclamation-triangle');
        }
    }

    function abrirHorario(horario) {
        document.getElementById('modal-horario').dataset.id = horario?.id ?? '';
        document.getElementById('modal-horario-titulo').textContent = horario ? 'Editar franja horaria' : 'Agregar franja horaria';

        if (horario) {
            document.getElementById('h-medico').value = horario.medicoId;
            document.getElementById('h-dia').value = horario.diaSemana;
            document.getElementById('h-inicio').value = horario.horaInicio.substring(0, 5);
            document.getElementById('h-fin').value = horario.horaFin.substring(0, 5);
        }

        modalHorario.show();
    }

    async function guardarHorario() {
        const id = document.getElementById('modal-horario').dataset.id;

        const datos = {
            medicoId: Number(document.getElementById('h-medico').value),
            diaSemana: Number(document.getElementById('h-dia').value),
            horaInicio: document.getElementById('h-inicio').value + ':00',
            horaFin: document.getElementById('h-fin').value + ':00',
            activo: true
        };

        if (!datos.medicoId) return UI.aviso('Seleccione un médico.');

        await UI.conEspera(document.getElementById('btn-guardar-horario'), async () => {
            try {
                if (id) await Api.put(`/agenda/horarios/${id}`, datos);
                else await Api.post('/agenda/horarios', datos);

                UI.exito('Horario guardado.');
                modalHorario.hide();
                await cargarHorarios();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function eliminarHorario(id) {
        if (!await UI.confirmar('¿Eliminar esta franja horaria?', 'Eliminar')) return;

        try {
            await Api.del(`/agenda/horarios/${id}`);
            UI.exito('Franja eliminada.');
            await cargarHorarios();
        } catch (error) {
            UI.error(error.message);
        }
    }

    async function cargarBloqueos() {
        const cuerpo = document.getElementById('tabla-bloqueos');
        UI.cargando(cuerpo, 5);

        try {
            const bloqueos = await Api.get('/agenda/bloqueos');

            if (!bloqueos.length) {
                cuerpo.innerHTML = UI.filaVacia(5, 'No hay fechas bloqueadas.', 'bi-calendar-check');
                return;
            }

            cuerpo.innerHTML = bloqueos.map(b => `
                <tr>
                    <td class="mc-principal">${UI.escapar(b.medico)}</td>
                    <td>${UI.fechaCorta(b.fechaInicio)}<br><span class="mc-texto-muted">al ${UI.fechaCorta(b.fechaFin)}</span></td>
                    <td>${UI.escapar(b.tipoNombre)}</td>
                    <td>${UI.escapar(b.motivo || '—')}</td>
                    <td><button class="btn-mc-icono peligro" title="Liberar" data-liberar="${b.id}"><i class="bi bi-trash"></i></button></td>
                </tr>`).join('');

            cuerpo.querySelectorAll('[data-liberar]').forEach(b =>
                b.onclick = () => liberarBloqueo(Number(b.dataset.liberar)));
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(5, error.message, 'bi-exclamation-triangle');
        }
    }

    async function guardarBloqueo() {
        const medicoId = document.getElementById('b-medico').value;

        const datos = {
            medicoId: medicoId ? Number(medicoId) : null,
            fechaInicio: document.getElementById('b-inicio').value,
            fechaFin: document.getElementById('b-fin').value,
            tipo: document.getElementById('b-tipo').value,
            motivo: document.getElementById('b-motivo').value.trim() || null
        };

        if (!datos.fechaInicio || !datos.fechaFin) return UI.aviso('Indique el rango de fechas.');

        await UI.conEspera(document.getElementById('btn-guardar-bloqueo'), async () => {
            try {
                await Api.post('/agenda/bloqueos', datos);
                UI.exito('Fechas bloqueadas.');
                modalBloqueo.hide();
                await cargarBloqueos();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function liberarBloqueo(id) {
        if (!await UI.confirmar('¿Liberar este bloqueo?', 'Liberar')) return;

        try {
            await Api.del(`/agenda/bloqueos/${id}`);
            UI.exito('Bloqueo liberado.');
            await cargarBloqueos();
        } catch (error) {
            UI.error(error.message);
        }
    }
})();
