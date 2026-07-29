/* Expediente administrativo de pacientes. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'pacientes',
        titulo: 'Pacientes',
        subtitulo: 'Registro y datos de contacto de los pacientes de la clínica',
        roles: ['Administrador', 'Recepcionista', 'Medico']
    });

    if (!usuario) return;

    let modalPaciente, modalHistorial;

    iniciar();

    async function iniciar() {
        modalPaciente = new bootstrap.Modal(document.getElementById('modal-paciente'));
        modalHistorial = new bootstrap.Modal(document.getElementById('modal-historial'));

        document.getElementById('btn-buscar').onclick = cargar;
        document.getElementById('f-busqueda').onkeyup = (e) => { if (e.key === 'Enter') cargar(); };
        document.getElementById('btn-nuevo').onclick = () => abrir();
        document.getElementById('btn-guardar-paciente').onclick = guardar;

        await cargar();

        if (new URLSearchParams(location.search).get('nuevo')) abrir();
    }

    async function cargar() {
        const cuerpo = document.getElementById('tabla-pacientes');
        UI.cargando(cuerpo, 7);

        try {
            const pacientes = await Api.get('/pacientes', { busqueda: document.getElementById('f-busqueda').value });

            if (!pacientes.length) {
                cuerpo.innerHTML = UI.filaVacia(7, 'No hay pacientes registrados todavía.', 'bi-people');
                return;
            }

            cuerpo.innerHTML = pacientes.map(p => `
                <tr>
                    <td class="mc-principal">${UI.escapar(p.nombreCompleto)}</td>
                    <td><i class="bi bi-whatsapp" style="color:var(--whatsapp)"></i> ${UI.escapar(p.telefono)}</td>
                    <td>${UI.escapar(p.email || '—')}</td>
                    <td>${UI.escapar(p.documentoIdentidad || '—')}</td>
                    <td>${p.totalCitas}</td>
                    <td>${p.activo ? UI.etiquetaEstado('Confirmada').replace('Confirmada', 'Activo') : UI.etiquetaEstado('Cancelada').replace('Cancelada', 'Inactivo')}</td>
                    <td class="text-nowrap">
                        <button class="btn-mc-icono" title="Historial" data-historial="${p.id}"><i class="bi bi-clock-history"></i></button>
                        <button class="btn-mc-icono" title="Editar" data-editar="${p.id}"><i class="bi bi-pencil"></i></button>
                        <button class="btn-mc-icono peligro" title="Eliminar" data-eliminar="${p.id}"><i class="bi bi-trash"></i></button>
                    </td>
                </tr>`).join('');

            cuerpo.querySelectorAll('[data-editar]').forEach(b =>
                b.onclick = () => abrir(pacientes.find(p => p.id === Number(b.dataset.editar))));

            cuerpo.querySelectorAll('[data-historial]').forEach(b =>
                b.onclick = () => verHistorial(Number(b.dataset.historial)));

            cuerpo.querySelectorAll('[data-eliminar]').forEach(b =>
                b.onclick = () => eliminar(Number(b.dataset.eliminar)));
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(7, error.message, 'bi-exclamation-triangle');
        }
    }

    function abrir(paciente) {
        const modal = document.getElementById('modal-paciente');
        modal.dataset.id = paciente?.id ?? '';
        document.getElementById('modal-paciente-titulo').textContent = paciente ? 'Editar paciente' : 'Registrar paciente';

        document.getElementById('p-nombres').value = paciente?.nombres ?? '';
        document.getElementById('p-apellidos').value = paciente?.apellidos ?? '';
        document.getElementById('p-telefono').value = paciente?.telefono ?? '';
        document.getElementById('p-email').value = paciente?.email ?? '';
        document.getElementById('p-documento').value = paciente?.documentoIdentidad ?? '';
        document.getElementById('p-nacimiento').value = paciente?.fechaNacimiento ?? '';
        document.getElementById('p-genero').value = paciente?.genero ?? '';
        document.getElementById('p-direccion').value = paciente?.direccion ?? '';
        document.getElementById('p-activo').checked = paciente?.activo ?? true;

        modalPaciente.show();
    }

    async function guardar() {
        const id = document.getElementById('modal-paciente').dataset.id;

        const datos = {
            nombres: document.getElementById('p-nombres').value.trim(),
            apellidos: document.getElementById('p-apellidos').value.trim(),
            telefono: document.getElementById('p-telefono').value.trim(),
            email: document.getElementById('p-email').value.trim() || null,
            documentoIdentidad: document.getElementById('p-documento').value.trim() || null,
            fechaNacimiento: document.getElementById('p-nacimiento').value || null,
            genero: document.getElementById('p-genero').value || null,
            direccion: document.getElementById('p-direccion').value.trim() || null,
            activo: document.getElementById('p-activo').checked
        };

        if (!datos.nombres || !datos.apellidos || !datos.telefono)
            return UI.aviso('Nombres, apellidos y teléfono son obligatorios.');

        await UI.conEspera(document.getElementById('btn-guardar-paciente'), async () => {
            try {
                if (id) await Api.put(`/pacientes/${id}`, datos);
                else await Api.post('/pacientes', datos);

                UI.exito(id ? 'Datos actualizados.' : 'Paciente registrado.');
                modalPaciente.hide();
                await cargar();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function eliminar(id) {
        if (!await UI.confirmar('¿Eliminar este paciente?', 'Eliminar')) return;

        try {
            await Api.del(`/pacientes/${id}`);
            UI.exito('Paciente eliminado.');
            await cargar();
        } catch (error) {
            UI.error(error.message);
        }
    }

    async function verHistorial(id) {
        const cuerpo = document.getElementById('tabla-historial');
        UI.cargando(cuerpo, 5);
        modalHistorial.show();

        try {
            const citas = await Api.get(`/pacientes/${id}/citas`);

            cuerpo.innerHTML = citas.length
                ? citas.map(c => `
                    <tr>
                        <td class="mc-principal">${UI.escapar(c.codigo)}</td>
                        <td>${UI.fechaHora(c.fechaHoraInicio)}</td>
                        <td>${UI.escapar(c.medico)}</td>
                        <td>${UI.escapar(c.especialidad)}</td>
                        <td>${UI.etiquetaEstado(c.estado)}</td>
                    </tr>`).join('')
                : UI.filaVacia(5, 'Este paciente aún no tiene citas.', 'bi-calendar2-x');
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(5, error.message, 'bi-exclamation-triangle');
        }
    }
})();