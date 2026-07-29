/* Portal del paciente: consulta de citas, agendamiento y avisos recibidos. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'mis-citas',
        titulo: 'Mis citas',
        subtitulo: 'Consulte, agende y gestione sus citas médicas',
        roles: ['Paciente']
    });

    if (!usuario) return;

    let medicos = [];
    let slotSeleccionado = null;
    let modalCita;

    iniciar();

    async function iniciar() {
        if (!usuario.pacienteId) {
            UI.error('Su cuenta no está vinculada a un expediente de paciente. Comuníquese con la clínica.');
            return;
        }

        modalCita = new bootstrap.Modal(document.getElementById('modal-cita'));
        document.getElementById('c-fecha').min = UI.isoFecha(new Date());
        document.getElementById('c-fecha').value = UI.isoFecha(new Date());

        document.getElementById('btn-nueva').onclick = () => modalCita.show();
        document.getElementById('btn-guardar-cita').onclick = guardar;
        document.getElementById('c-especialidad').onchange = filtrarMedicos;
        document.getElementById('c-medico').onchange = cargarSlots;
        document.getElementById('c-fecha').onchange = cargarSlots;

        await cargarCatalogos();
        await cargarDatos();
        await cargarCitas();
        await cargarNotificaciones();
    }

    async function cargarCatalogos() {
        try {
            const [especialidades, lista] = await Promise.all([
                Api.get('/especialidades', { soloActivas: true }),
                Api.get('/medicos', { soloActivos: true })
            ]);

            medicos = lista;

            document.getElementById('c-especialidad').innerHTML = '<option value="">Todas</option>' +
                especialidades.map(e => `<option value="${e.id}">${UI.escapar(e.nombre)}</option>`).join('');

            filtrarMedicos();
        } catch (error) {
            UI.error(error.message);
        }
    }

    async function cargarDatos() {
        try {
            const paciente = await Api.get(`/pacientes/${usuario.pacienteId}`);

            const filas = [
                ['bi-person', 'Nombre', paciente.nombreCompleto],
                ['bi-whatsapp', 'WhatsApp', paciente.telefono],
                ['bi-envelope', 'Correo', paciente.email || '—'],
                ['bi-card-text', 'Documento', paciente.documentoIdentidad || '—'],
                ['bi-calendar2-check', 'Citas registradas', paciente.totalCitas]
            ];

            document.getElementById('datos-paciente').innerHTML = filas.map(([icono, etiqueta, valor]) => `
                <div class="d-flex align-items-center gap-2" style="font-size:.85rem">
                    <i class="bi ${icono}" style="color:var(--primary)"></i>
                    <span class="flex-grow-1">${etiqueta}</span>
                    <strong style="color:var(--text)">${UI.escapar(valor)}</strong>
                </div>`).join('');
        } catch (error) {
            UI.error(error.message);
        }
    }

    async function cargarCitas() {
        const proximas = document.getElementById('lista-proximas');
        const historial = document.getElementById('tabla-historial');

        try {
            const citas = await Api.get(`/pacientes/${usuario.pacienteId}/citas`);
            const ahora = new Date();

            const vigentes = citas.filter(c =>
                [1, 2, 3].includes(c.estadoId) && new Date(c.fechaHoraInicio) >= ahora);

            proximas.innerHTML = vigentes.length
                ? vigentes.map(c => `
                    <div class="mc-card" style="box-shadow:none;border-left:4px solid var(--primary)">
                        <div class="d-flex justify-content-between align-items-start flex-wrap gap-2">
                            <div>
                                <div class="mc-card-title mb-1">${UI.escapar(c.especialidad)} · ${UI.escapar(c.medico)}</div>
                                <p class="mb-1"><i class="bi bi-calendar3"></i> ${UI.fechaLarga(c.fechaHoraInicio)}</p>
                                <p class="mb-2"><i class="bi bi-clock"></i> ${UI.hora(c.fechaHoraInicio)} · Código ${UI.escapar(c.codigo)}</p>
                                ${UI.etiquetaEstado(c.estado)}
                            </div>
                            <div class="d-flex gap-2">
                                ${c.estadoId === 1 ? `<button class="btn btn-mc-primary btn-sm" data-confirmar="${c.id}">Confirmar</button>` : ''}
                                <button class="btn btn-mc-secundario btn-sm" data-cancelar="${c.id}">Cancelar</button>
                            </div>
                        </div>
                    </div>`).join('')
                : '<p class="mc-texto-muted mb-0">No tiene citas próximas. Use el botón de agendar para solicitar una.</p>';

            proximas.querySelectorAll('[data-confirmar]').forEach(b =>
                b.onclick = () => confirmar(Number(b.dataset.confirmar)));

            proximas.querySelectorAll('[data-cancelar]').forEach(b =>
                b.onclick = () => cancelar(Number(b.dataset.cancelar)));

            historial.innerHTML = citas.length
                ? citas.map(c => `
                    <tr>
                        <td class="mc-principal">${UI.escapar(c.codigo)}</td>
                        <td>${UI.fechaHora(c.fechaHoraInicio)}</td>
                        <td>${UI.escapar(c.medico)}</td>
                        <td>${UI.escapar(c.especialidad)}</td>
                        <td>${UI.etiquetaEstado(c.estado)}</td>
                    </tr>`).join('')
                : UI.filaVacia(5, 'Aún no tiene citas registradas.', 'bi-calendar2-x');
        } catch (error) {
            proximas.innerHTML = `<p class="mc-texto-muted mb-0">${UI.escapar(error.message)}</p>`;
        }
    }

    async function cargarNotificaciones() {
        const contenedor = document.getElementById('lista-notificaciones');

        try {
            const notificaciones = await Api.get('/notificaciones');

            contenedor.innerHTML = notificaciones.length
                ? notificaciones.slice(0, 12).map(n => `
                    <div style="font-size:.82rem;border-left:3px solid var(--accent);padding-left:10px">
                        <strong style="color:var(--text)">${UI.escapar(n.tipoNombre)}</strong>
                        <div class="mc-texto-muted">${UI.escapar(n.mensaje)}</div>
                        <div class="mc-texto-muted">${UI.fechaHora(n.fechaProgramada)}</div>
                    </div>`).join('')
                : '<p class="mc-texto-muted mb-0">Sin avisos recibidos.</p>';
        } catch {
            contenedor.innerHTML = '<p class="mc-texto-muted mb-0">No se pudo cargar la información.</p>';
        }
    }

    function filtrarMedicos() {
        const especialidadId = document.getElementById('c-especialidad').value;
        const lista = especialidadId ? medicos.filter(m => m.especialidadId === Number(especialidadId)) : medicos;

        document.getElementById('c-medico').innerHTML = lista.length
            ? lista.map(m => `<option value="${m.id}">${UI.escapar(m.nombreCompleto)}</option>`).join('')
            : '<option value="">Sin médicos disponibles</option>';

        cargarSlots();
    }

    async function cargarSlots() {
        const contenedor = document.getElementById('c-slots');
        const medicoId = document.getElementById('c-medico').value;
        const fecha = document.getElementById('c-fecha').value;
        slotSeleccionado = null;

        if (!medicoId || !fecha) {
            contenedor.innerHTML = '<span class="mc-texto-muted">Seleccione médico y fecha.</span>';
            return;
        }

        contenedor.innerHTML = '<span class="mc-texto-muted"><span class="spinner-border spinner-border-sm me-2"></span>Buscando cupos…</span>';

        try {
            const dia = await Api.get('/agenda/disponibilidad', { medicoId, fecha });

            if (dia.bloqueado) {
                contenedor.innerHTML = `<span class="mc-texto-muted">Ese día la agenda no está disponible: ${UI.escapar(dia.motivoBloqueo || '')}</span>`;
                return;
            }

            if (!dia.slots.length) {
                contenedor.innerHTML = '<span class="mc-texto-muted">No hay horarios libres. Pruebe con otra fecha.</span>';
                return;
            }

            contenedor.innerHTML = dia.slots.map(s =>
                `<button type="button" class="btn btn-mc-secundario btn-sm" data-inicio="${s.inicio}">${UI.escapar(s.etiqueta)}</button>`).join('');

            contenedor.querySelectorAll('[data-inicio]').forEach(boton => {
                boton.onclick = () => {
                    contenedor.querySelectorAll('button').forEach(b => b.classList.remove('btn-mc-primary'));
                    boton.classList.add('btn-mc-primary');
                    slotSeleccionado = boton.dataset.inicio;
                };
            });
        } catch (error) {
            contenedor.innerHTML = `<span class="mc-texto-muted">${UI.escapar(error.message)}</span>`;
        }
    }

    async function guardar() {
        if (!slotSeleccionado) return UI.aviso('Seleccione un horario disponible.');

        await UI.conEspera(document.getElementById('btn-guardar-cita'), async () => {
            try {
                await Api.post('/citas', {
                    pacienteId: usuario.pacienteId,
                    medicoId: Number(document.getElementById('c-medico').value),
                    fechaHoraInicio: slotSeleccionado,
                    tipoConsulta: 'Nueva',
                    motivo: document.getElementById('c-motivo').value,
                    canalOrigen: 'Portal'
                });

                UI.exito('Cita agendada. Recibirá la confirmación por WhatsApp.');
                modalCita.hide();
                await cargarCitas();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function confirmar(id) {
        try {
            await Api.put(`/citas/${id}/confirmar`);
            UI.exito('Cita confirmada.');
            await cargarCitas();
        } catch (error) {
            UI.error(error.message);
        }
    }

    async function cancelar(id) {
        if (!await UI.confirmar('¿Desea cancelar esta cita?', 'Cancelar cita')) return;

        try {
            await Api.put(`/citas/${id}/cancelar`, { motivo: 'Cancelada por el paciente desde el portal' });
            UI.exito('Cita cancelada.');
            await cargarCitas();
        } catch (error) {
            UI.error(error.message);
        }
    }
})();
