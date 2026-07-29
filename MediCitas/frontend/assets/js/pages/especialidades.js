/* Catálogo de especialidades médicas. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'especialidades',
        titulo: 'Especialidades',
        subtitulo: 'Áreas de atención que ofrece la clínica',
        roles: ['Administrador', 'Recepcionista']
    });

    if (!usuario) return;

    let modal;

    iniciar();

    async function iniciar() {
        modal = new bootstrap.Modal(document.getElementById('modal-especialidad'));
        document.getElementById('btn-nueva').onclick = () => abrir();
        document.getElementById('btn-guardar-especialidad').onclick = guardar;
        await cargar();
    }

    async function cargar() {
        const cuerpo = document.getElementById('tabla-especialidades');
        UI.cargando(cuerpo, 5);

        try {
            const especialidades = await Api.get('/especialidades');

            if (!especialidades.length) {
                cuerpo.innerHTML = UI.filaVacia(5, 'Aún no hay especialidades. Cree la primera para poder registrar médicos.', 'bi-clipboard2-pulse');
                return;
            }

            cuerpo.innerHTML = especialidades.map(e => `
                <tr>
                    <td class="mc-principal">${UI.escapar(e.nombre)}</td>
                    <td>${UI.escapar(e.descripcion || '—')}</td>
                    <td>${e.totalMedicos}</td>
                    <td>${e.activo ? '<span class="mc-estado mc-estado-confirmada">Activa</span>' : '<span class="mc-estado mc-estado-noasistio">Inactiva</span>'}</td>
                    <td class="text-nowrap">
                        <button class="btn-mc-icono" title="Editar" data-editar="${e.id}"><i class="bi bi-pencil"></i></button>
                        <button class="btn-mc-icono peligro" title="Eliminar" data-eliminar="${e.id}"><i class="bi bi-trash"></i></button>
                    </td>
                </tr>`).join('');

            cuerpo.querySelectorAll('[data-editar]').forEach(b =>
                b.onclick = () => abrir(especialidades.find(e => e.id === Number(b.dataset.editar))));

            cuerpo.querySelectorAll('[data-eliminar]').forEach(b =>
                b.onclick = () => eliminar(Number(b.dataset.eliminar)));
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(5, error.message, 'bi-exclamation-triangle');
        }
    }

    function abrir(especialidad) {
        document.getElementById('modal-especialidad').dataset.id = especialidad?.id ?? '';
        document.getElementById('modal-especialidad-titulo').textContent = especialidad ? 'Editar especialidad' : 'Nueva especialidad';
        document.getElementById('e-nombre').value = especialidad?.nombre ?? '';
        document.getElementById('e-descripcion').value = especialidad?.descripcion ?? '';
        document.getElementById('e-activo').checked = especialidad?.activo ?? true;
        modal.show();
    }

    async function guardar() {
        const id = document.getElementById('modal-especialidad').dataset.id;

        const datos = {
            nombre: document.getElementById('e-nombre').value.trim(),
            descripcion: document.getElementById('e-descripcion').value.trim() || null,
            activo: document.getElementById('e-activo').checked
        };

        if (!datos.nombre) return UI.aviso('Escriba el nombre de la especialidad.');

        await UI.conEspera(document.getElementById('btn-guardar-especialidad'), async () => {
            try {
                if (id) await Api.put(`/especialidades/${id}`, datos);
                else await Api.post('/especialidades', datos);

                UI.exito(id ? 'Especialidad actualizada.' : 'Especialidad creada.');
                modal.hide();
                await cargar();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function eliminar(id) {
        if (!await UI.confirmar('¿Eliminar esta especialidad?', 'Eliminar')) return;

        try {
            await Api.del(`/especialidades/${id}`);
            UI.exito('Especialidad eliminada.');
            await cargar();
        } catch (error) {
            UI.error(error.message);
        }
    }
})();
