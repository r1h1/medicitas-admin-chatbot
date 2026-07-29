/* Cuentas de acceso y roles. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'usuarios',
        titulo: 'Usuarios',
        subtitulo: 'Control de acceso al panel administrativo',
        roles: ['Administrador']
    });

    if (!usuario) return;

    let ROLES = [];
    let modal;

    iniciar();

    async function iniciar() {
        modal = new bootstrap.Modal(document.getElementById('modal-usuario'));
        document.getElementById('btn-nuevo').onclick = () => abrir();
        document.getElementById('btn-guardar-usuario').onclick = guardar;

        // Cargar roles desde la API (excluye "Paciente" que solo usa WhatsApp)
        try {
            ROLES = await Api.get('/usuarios/roles');
            document.getElementById('u-rol').innerHTML =
                ROLES.map(r => `<option value="${r.id}">${UI.escapar(r.nombre)}</option>`).join('');
        } catch {
            document.getElementById('u-rol').innerHTML = '<option value="1">Administrador</option><option value="2">Recepcionista</option><option value="4">Medico</option>';
        }

        const medicos = await Api.get('/medicos');
        document.getElementById('u-medico').innerHTML = '<option value="">Sin vincular</option>' +
            medicos.map(m => `<option value="${m.id}">${UI.escapar(m.nombreCompleto)}</option>`).join('');

        await cargar();
    }

    async function cargar() {
        const cuerpo = document.getElementById('tabla-usuarios');
        UI.cargando(cuerpo, 6);

        try {
            const usuarios = await Api.get('/usuarios');

            cuerpo.innerHTML = usuarios.map(u => `
                <tr>
                    <td class="mc-principal">${UI.escapar(u.nombreCompleto)}</td>
                    <td>${UI.escapar(u.email)}</td>
                    <td>${UI.escapar(u.rol)}</td>
                    <td>${u.ultimoAcceso ? UI.fechaHora(u.ultimoAcceso) : 'Nunca'}</td>
                    <td>${u.activo ? '<span class="mc-estado mc-estado-confirmada">Activa</span>' : '<span class="mc-estado mc-estado-noasistio">Inactiva</span>'}</td>
                    <td class="text-nowrap">
                        <button class="btn-mc-icono" title="Editar" data-editar="${u.id}"><i class="bi bi-pencil"></i></button>
                        ${u.activo ? `<button class="btn-mc-icono peligro" title="Desactivar" data-desactivar="${u.id}"><i class="bi bi-slash-circle"></i></button>` : ''}
                    </td>
                </tr>`).join('');

            cuerpo.querySelectorAll('[data-editar]').forEach(b =>
                b.onclick = () => abrir(usuarios.find(u => u.id === Number(b.dataset.editar))));

            cuerpo.querySelectorAll('[data-desactivar]').forEach(b =>
                b.onclick = () => desactivar(Number(b.dataset.desactivar)));
        } catch (error) {
            cuerpo.innerHTML = UI.filaVacia(6, error.message, 'bi-exclamation-triangle');
        }
    }

    function abrir(datos) {
        document.getElementById('modal-usuario').dataset.id = datos?.id ?? '';
        document.getElementById('modal-usuario-titulo').textContent = datos ? 'Editar usuario' : 'Nuevo usuario';

        document.getElementById('u-nombres').value = datos?.nombres ?? '';
        document.getElementById('u-apellidos').value = datos?.apellidos ?? '';
        document.getElementById('u-email').value = datos?.email ?? '';
        document.getElementById('u-telefono').value = datos?.telefono ?? '';
        document.getElementById('u-rol').value = datos?.rolId ?? 2;
        document.getElementById('u-medico').value = datos?.medicoId ?? '';
        document.getElementById('u-activo').checked = datos?.activo ?? true;
        document.getElementById('u-password').value = '';

        // El correo y la contraseña solo se definen al crear la cuenta.
        document.getElementById('u-email').disabled = Boolean(datos);
        document.getElementById('campo-password').classList.toggle('mc-oculto', Boolean(datos));

        modal.show();
    }

    async function guardar() {
        const id = document.getElementById('modal-usuario').dataset.id;
        const medicoId = document.getElementById('u-medico').value;

        const base = {
            nombres: document.getElementById('u-nombres').value.trim(),
            apellidos: document.getElementById('u-apellidos').value.trim(),
            telefono: document.getElementById('u-telefono').value.trim() || null,
            rolId: Number(document.getElementById('u-rol').value),
            medicoId: medicoId ? Number(medicoId) : null
        };

        if (!base.nombres || !base.apellidos) return UI.aviso('Escriba nombres y apellidos.');

        // Si el rol es Médico, debe vincularse a un médico registrado.
        const rolSeleccionado = ROLES.find(r => r.id === base.rolId);
        if (rolSeleccionado?.nombre === 'Medico' && !base.medicoId)
            return UI.aviso('El rol Médico requiere vincularse a un médico registrado en el sistema.');

        await UI.conEspera(document.getElementById('btn-guardar-usuario'), async () => {
            try {
                if (id) {
                    await Api.put(`/usuarios/${id}`, { ...base, activo: document.getElementById('u-activo').checked });
                    UI.exito('Usuario actualizado.');
                } else {
                    const password = document.getElementById('u-password').value;
                    if (password.length < 8) return UI.aviso('La contraseña debe tener al menos 8 caracteres.');

                    await Api.post('/usuarios', {
                        ...base,
                        email: document.getElementById('u-email').value.trim(),
                        password
                    });
                    UI.exito('Usuario creado.');
                }

                modal.hide();
                await cargar();
            } catch (error) {
                UI.error(error.message);
            }
        });
    }

    async function desactivar(id) {
        if (!await UI.confirmar('¿Desactivar esta cuenta?', 'Desactivar')) return;

        try {
            await Api.del(`/usuarios/${id}`);
            UI.exito('Cuenta desactivada.');
            await cargar();
        } catch (error) {
            UI.error(error.message);
        }
    }
})();