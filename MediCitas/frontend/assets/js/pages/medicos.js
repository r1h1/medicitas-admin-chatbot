/* Catálogo de médicos. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'medicos',
        titulo: 'Médicos',
        subtitulo: 'Profesionales de la clínica y duración de sus consultas',
        roles: ['Administrador', 'Recepcionista']
    });

    if (!usuario) return;

    let especialidades = [];
    let modalMedico;

    iniciar();

    async function iniciar() {
        modalMedico = new bootstrap.Modal(document.getElementById('modal-medico'));

        document.getElementById('btn-filtrar').onclick = cargar;
        document.getElementById('btn-nuevo').onclick = () => abrir();
        document.getElementById('btn-guardar-medico').onclick = guardar;

        especialidades = await Api.get('/especialidades');

        const opciones = especialidades.map(e => `<option value="${e.id}">${UI.escapar(e.nombre)}</option>`).join('');
        document.getElementById('f-especialidad').innerHTML = '<option value="">Todas</option>' + opciones;
        document.getElementById('m-especialidad').innerHTML = opciones || '<option value="">Cree primero una especialidad</option>';

        await cargar();
    }

    async function cargar() {
        const cuerpo = document.getElementById('tabla-medicos');
        UI.cargando(cuerpo, 7);

        try {
            const medicos = await Api.get('/medicos', { especialidadId: document.getElementById('f-especialidad').value });

            if (!medicos.length) {
                cuerpo.innerHTML = UI.filaVacia(7, 'No hay médicos registrados.', 'bi-person-badge');
                return;
            }

            cuerpo.innerHTML = medicos.map(m => `
                <tr>
                    <td class="mc-principal">${UI.escapar(m.nombreCompleto)}</td>
                    <td>${UI.escapar(m.colegiado)}</td>
                    <td>${UI.escapar(m.especialidad)}</td>
                    <td>${m.duracionCitaMinutos} min</td>
                    <td>${UI.escapar(m.telefono || '—')}<br><span class="mc-texto-muted">${UI.escapar(m.email || '')}</span></td>
                    <td>${m.activo ? '<span class="mc-estado mc-estado-confirmada">Activo</span>' : '<span class="mc-estado mc-estado-noasistio">Inactivo</span>'}</td>
                    <td class="text-nowrap">
                        <a class="btn-mc-icono" title="Ver horarios" href="horarios.html?medico=${m.id}"><i class="bi bi-clock"></i></a>
                        <button class="btn-mc-icono" title="Editar" data-editar="${m.id}"><i class="bi bi-pencil"></i></button>
                        <button class="btn-mc-icono peligro" title="Eliminar" data-eliminar="${m.id}"><i class="bi bi-trash"></i></button>
                    </td>
                </tr>`).join('');

            cuerpo.querySelectorAll('[data-editar]').forEach(b =>
                b.onclick = () => abrir(medicos.find(m => m.id === Number(b.dataset.editar))));

            cuerpo.querySelectorAll('[data-eliminar]').forEach(b =>
                b.onclick = () => eliminar(Number(b.dataset.eliminar)));
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(7, error.message, 'bi-exclamation-triangle');
        }
    }

    function abrir(medico) {
        document.getElementById('modal-medico').dataset.id = medico?.id ?? '';
        document.getElementById('modal-medico-titulo').textContent = medico ? 'Editar médico' : 'Registrar médico';

        document.getElementById('m-nombres').value = medico?.nombres ?? '';
        document.getElementById('m-apellidos').value = medico?.apellidos ?? '';
        document.getElementById('m-colegiado').value = medico?.colegiado ?? '';
        document.getElementById('m-especialidad').value = medico?.especialidadId ?? '';
        document.getElementById('m-duracion').value = medico?.duracionCitaMinutos ?? 30;
        document.getElementById('m-telefono').value = medico?.telefono ?? '';
        document.getElementById('m-email').value = medico?.email ?? '';
        document.getElementById('m-activo').checked = medico?.activo ?? true;

        modalMedico.show();
    }

    async function guardar() {
        const id = document.getElementById('modal-medico').dataset.id;

        const datos = {
            nombres: document.getElementById('m-nombres').value.trim(),
            apellidos: document.getElementById('m-apellidos').value.trim(),
            colegiado: document.getElementById('m-colegiado').value.trim(),
            especialidadId: Number(document.getElementById('m-especialidad').value),
            duracionCitaMinutos: Number(document.getElementById('m-duracion').value),
            telefono: document.getElementById('m-telefono').value.trim() || null,
            email: document.getElementById('m-email').value.trim() || null,
            activo: document.getElementById('m-activo').checked
        };

        if (!datos.nombres || !datos.apellidos || !datos.colegiado || !datos.especialidadId)
            return UI.aviso('Complete nombres, apellidos, colegiado y especialidad.');

        await UI.conEspera(document.getElementById('btn-guardar-medico'), async () => {
            try {
                if (id) await Api.put(`/medicos/${id}`, datos);
                else await Api.post('/medicos', datos);

                UI.exito(id ? 'Médico actualizado.' : 'Médico registrado.');
                modalMedico.hide();
                await cargar();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function eliminar(id) {
        if (!await UI.confirmar('¿Eliminar este médico?', 'Eliminar')) return;

        try {
            await Api.del(`/medicos/${id}`);
            UI.exito('Médico eliminado.');
            await cargar();
        } catch (error) {
            UI.error(error.message);
        }
    }
})();
