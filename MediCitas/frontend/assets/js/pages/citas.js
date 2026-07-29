/* Gestión de citas: filtros, agendamiento con cupos reales y aviso de retrasos. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'citas',
        titulo: 'Citas médicas',
        subtitulo: 'Agende, confirme, reprograme y controle el estado de cada cita',
        roles: ['Administrador', 'Recepcionista', 'Medico']
    });

    if (!usuario) return;

    const hoy = UI.isoFecha(new Date());
    let medicos = [];
    let pacientes = [];
    let especialidades = [];
    let slotSeleccionado = null;
    let modalCita;

    iniciar();

    async function iniciar() {
        document.getElementById('f-desde').value = hoy;
        document.getElementById('f-hasta').value = UI.isoFecha(new Date(Date.now() + 30 * 86400000));
        document.getElementById('retraso-fecha').value = hoy;
        document.getElementById('c-fecha').min = hoy;

        modalCita = new bootstrap.Modal(document.getElementById('modal-cita'));

        document.getElementById('btn-filtrar').onclick = cargarCitas;
        document.getElementById('btn-nueva').onclick = abrirNueva;
        document.getElementById('btn-guardar-cita').onclick = guardarCita;
        document.getElementById('btn-retraso').onclick = reportarRetraso;
        document.getElementById('c-especialidad').onchange = filtrarMedicosPorEspecialidad;
        document.getElementById('c-medico').onchange = cargarSlots;
        document.getElementById('c-fecha').onchange = cargarSlots;

        await cargarCatalogos();
        await cargarCitas();
        await cargarRetrasos();

        if (new URLSearchParams(location.search).get('nueva')) abrirNueva();
    }

    async function cargarCatalogos() {
        try {
            [medicos, pacientes, especialidades] = await Promise.all([
                Api.get('/medicos', { soloActivos: true }),
                Api.get('/pacientes'),
                Api.get('/especialidades', { soloActivas: true })
            ]);

            const opcionesMedicos = medicos.map(m => `<option value="${m.id}">${UI.escapar(m.nombreCompleto)}</option>`).join('');
            document.getElementById('f-medico').innerHTML = '<option value="">Todos</option>' + opcionesMedicos;
            document.getElementById('retraso-medico').innerHTML = opcionesMedicos || '<option value="">Sin médicos registrados</option>';

        // Si el usuario es médico, preseleccionar su propio registro y deshabilitar el selector.
        const usuarioActual = Api.obtenerUsuario();
        if (usuarioActual?.medicoId) {
            document.getElementById('retraso-medico').value = usuarioActual.medicoId;
            document.getElementById('retraso-medico').disabled = true;
            document.getElementById('f-medico').value = usuarioActual.medicoId;
            document.getElementById('f-medico').disabled = true;
        }

        // Asegurar valores por defecto en el panel de retraso
        document.getElementById('retraso-fecha').value = hoy;
        document.getElementById('retraso-minutos').value = '20';
            document.getElementById('c-medico').innerHTML = opcionesMedicos;

            document.getElementById('c-especialidad').innerHTML =
                '<option value="">Todas</option>' +
                especialidades.map(e => `<option value="${e.id}">${UI.escapar(e.nombre)}</option>`).join('');

            document.getElementById('c-paciente').innerHTML = pacientes.length
                ? pacientes.map(p => `<option value="${p.id}">${UI.escapar(p.nombreCompleto)} · ${UI.escapar(p.telefono)}</option>`).join('')
                : '<option value="">Registre primero un paciente</option>';
        } catch (error) {
            UI.error(error.message);
        }
    }

    async function cargarCitas() {
        const cuerpo = document.getElementById('tabla-citas');
        UI.cargando(cuerpo, 8);

        try {
            const citas = await Api.get('/citas', {
                desde: document.getElementById('f-desde').value,
                hasta: document.getElementById('f-hasta').value,
                medicoId: document.getElementById('f-medico').value,
                estadoId: document.getElementById('f-estado').value
            });

            document.getElementById('contador-citas').textContent = `${citas.length} citas`;

            if (!citas.length) {
                cuerpo.innerHTML = UI.filaVacia(8, 'No hay citas en el rango seleccionado.', 'bi-calendar2-x');
                return;
            }

            cuerpo.innerHTML = citas.map(c => `
                <tr>
                    <td class="mc-principal">${UI.escapar(c.codigo)}</td>
                    <td>${UI.fechaCorta(c.fechaHoraInicio)}<br><span class="mc-texto-muted">${UI.hora(c.fechaHoraInicio)}</span></td>
                    <td class="mc-principal">${UI.escapar(c.paciente)}<br><span class="mc-texto-muted fw-normal">${UI.escapar(c.pacienteTelefono)}</span></td>
                    <td>${UI.escapar(c.medico)}</td>
                    <td>${UI.escapar(c.especialidad)}</td>
                    <td><i class="bi ${c.canalOrigen === 'WhatsApp' ? 'bi-whatsapp' : c.canalOrigen === 'Portal' ? 'bi-globe' : 'bi-window'}"></i> ${UI.escapar(c.canalOrigen)}</td>
                    <td>${UI.etiquetaEstado(c.estado)}</td>
                    <td class="text-nowrap">
                        ${c.estadoId === 1 ? `<button class="btn-mc-icono" title="Confirmar" data-accion="confirmar" data-id="${c.id}"><i class="bi bi-check2"></i></button>` : ''}
                        ${[1, 2].includes(c.estadoId) ? `<button class="btn-mc-icono" title="Marcar en atención" data-accion="atender" data-id="${c.id}"><i class="bi bi-play"></i></button>` : ''}
                        ${c.estadoId === 3 ? `<button class="btn-mc-icono" title="Completar" data-accion="completar" data-id="${c.id}"><i class="bi bi-check2-all"></i></button>` : ''}
                        ${[1, 2, 3].includes(c.estadoId) ? `<button class="btn-mc-icono" title="Reprogramar" data-accion="reprogramar" data-id="${c.id}"><i class="bi bi-arrow-repeat"></i></button>` : ''}
                        ${[1, 2].includes(c.estadoId) ? `<button class="btn-mc-icono" title="No asistió" data-accion="noasistio" data-id="${c.id}"><i class="bi bi-person-x"></i></button>` : ''}
                        ${[1, 2, 3].includes(c.estadoId) ? `<button class="btn-mc-icono peligro" title="Cancelar" data-accion="cancelar" data-id="${c.id}"><i class="bi bi-x-lg"></i></button>` : ''}
                    </td>
                </tr>`).join('');

            cuerpo.querySelectorAll('[data-accion]').forEach(boton => {
                boton.onclick = () => ejecutarAccion(boton.dataset.accion, Number(boton.dataset.id), citas);
            });
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(8, error.message, 'bi-exclamation-triangle');
        }
    }

    async function ejecutarAccion(accion, id, citas) {
        try {
            if (accion === 'confirmar') {
                await Api.put(`/citas/${id}/confirmar`);
                UI.exito('Cita confirmada.');
            }

            if (accion === 'atender') {
                await Api.put(`/citas/${id}/estado`, { estado: 'EnAtencion' });
                UI.exito('La cita pasó a estado en atención.');
            }

            if (accion === 'completar') {
                await Api.put(`/citas/${id}/estado`, { estado: 'Completada' });
                UI.exito('Cita completada.');
            }

            if (accion === 'noasistio') {
                if (!await UI.confirmar('¿Marcar la cita como no asistida?', 'Marcar')) return;
                await Api.put(`/citas/${id}/estado`, { estado: 'NoAsistio' });
                UI.exito('Registro actualizado.');
            }

            if (accion === 'cancelar') {
                if (!await UI.confirmar('¿Cancelar esta cita? El horario quedará libre.', 'Cancelar cita')) return;
                await Api.put(`/citas/${id}/cancelar`, { motivo: 'Cancelada desde el panel administrativo' });
                UI.exito('Cita cancelada y horario liberado.');
            }

            if (accion === 'reprogramar') {
                abrirReprogramacion(citas.find(c => c.id === id));
                return;
            }

            await cargarCitas();
        } catch (error) {
            UI.error(error.message);
        }
    }

    // ------------------------------ Alta y reprogramación ------------------------------
    function abrirNueva() {
        document.getElementById('modal-cita-titulo').textContent = 'Nueva cita';
        document.getElementById('modal-cita').dataset.citaId = '';
        document.getElementById('c-paciente').disabled = false;
        document.getElementById('c-fecha').value = hoy;
        document.getElementById('c-motivo').value = '';
        slotSeleccionado = null;

        filtrarMedicosPorEspecialidad();
        modalCita.show();
    }

    function abrirReprogramacion(cita) {
        document.getElementById('modal-cita-titulo').textContent = `Reprogramar cita ${cita.codigo}`;
        document.getElementById('modal-cita').dataset.citaId = cita.id;
        document.getElementById('c-paciente').value = cita.pacienteId;
        document.getElementById('c-paciente').disabled = true;
        document.getElementById('c-especialidad').value = cita.especialidadId;
        filtrarMedicosPorEspecialidad();
        document.getElementById('c-medico').value = cita.medicoId;
        document.getElementById('c-fecha').value = hoy;
        document.getElementById('c-motivo').value = cita.motivo || '';
        slotSeleccionado = null;

        cargarSlots();
        modalCita.show();
    }

    function filtrarMedicosPorEspecialidad() {
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
            const citaId = document.getElementById('modal-cita').dataset.citaId;
            const dia = await Api.get('/agenda/disponibilidad', { medicoId, fecha, excluirCitaId: citaId || '' });

            if (dia.bloqueado) {
                contenedor.innerHTML = `<span class="mc-texto-muted"><i class="bi bi-calendar-x"></i> Agenda bloqueada: ${UI.escapar(dia.motivoBloqueo || '')}</span>`;
                return;
            }

            if (!dia.slots.length) {
                contenedor.innerHTML = '<span class="mc-texto-muted">No hay cupos libres en esa fecha. Elija otro día.</span>';
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

    async function guardarCita() {
        if (!slotSeleccionado) return UI.aviso('Seleccione un horario disponible.');

        const citaId = document.getElementById('modal-cita').dataset.citaId;

        await UI.conEspera(document.getElementById('btn-guardar-cita'), async () => {
            try {
                if (citaId) {
                    await Api.put(`/citas/${citaId}/reprogramar`, {
                        nuevaFechaHoraInicio: slotSeleccionado,
                        nuevoMedicoId: Number(document.getElementById('c-medico').value),
                        motivo: document.getElementById('c-motivo').value
                    });
                    UI.exito('Cita reprogramada. Se notificó al paciente.');
                } else {
                    await Api.post('/citas', {
                        pacienteId: Number(document.getElementById('c-paciente').value),
                        medicoId: Number(document.getElementById('c-medico').value),
                        fechaHoraInicio: slotSeleccionado,
                        tipoConsulta: document.getElementById('c-tipo').value,
                        motivo: document.getElementById('c-motivo').value,
                        canalOrigen: 'Panel'
                    });
                    UI.exito('Cita agendada correctamente.');
                }

                modalCita.hide();
                await cargarCitas();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    // ------------------------------ Retrasos ------------------------------
    async function reportarRetraso() {
        const medicoId = document.getElementById('retraso-medico').value;
        if (!medicoId) return UI.aviso('Seleccione un médico.');

        const minutos = Number(document.getElementById('retraso-minutos').value);
        if (!minutos || minutos < 5) return UI.aviso('Indique al menos 5 minutos de retraso.');

        await UI.conEspera(document.getElementById('btn-retraso'), async () => {
            try {
                const retraso = await Api.post('/retrasos', {
                    medicoId: Number(medicoId),
                    fecha: document.getElementById('retraso-fecha').value,
                    minutosRetraso: minutos,
                    motivo: document.getElementById('retraso-motivo').value
                });

                UI.exito(`Se notificó a ${retraso.pacientesNotificados} paciente(s) en cola.`);
                document.getElementById('retraso-motivo').value = '';
                await cargarRetrasos();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function cargarRetrasos() {
        const contenedor = document.getElementById('lista-retrasos');

        try {
            const retrasos = await Api.get('/retrasos', { fecha: hoy });

            contenedor.innerHTML = retrasos.length
                ? retrasos.map(r => `
                    <div class="d-flex align-items-start gap-2" style="font-size:.84rem">
                        <i class="bi bi-megaphone" style="color:var(--warning)"></i>
                        <div class="flex-grow-1">
                            <strong style="color:var(--text)">${UI.escapar(r.medico)}</strong>
                            <div class="mc-texto-muted">${r.minutosRetraso} min · ${r.pacientesNotificados} avisados</div>
                            ${r.motivo ? `<div class="mc-texto-muted">${UI.escapar(r.motivo)}</div>` : ''}
                        </div>
                    </div>`).join('')
                : '<p class="mc-texto-muted mb-0">Sin retrasos reportados hoy.</p>';
        } catch {
            contenedor.innerHTML = '<p class="mc-texto-muted mb-0">No se pudo cargar la información.</p>';
        }
    }
})();