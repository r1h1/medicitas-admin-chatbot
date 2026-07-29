/* Estructura común de todas las pantallas internas: barra lateral, encabezado
   y control de acceso por rol. Evita repetir el mismo HTML en cada página. */
const Layout = (() => {

    const MENU = [
        { id: 'dashboard', texto: 'Dashboard', icono: 'bi-house-door', url: 'dashboard.html', roles: ['Administrador', 'Recepcionista'] },
        { id: 'citas', texto: 'Citas', icono: 'bi-calendar2-check', url: 'citas.html', roles: ['Administrador', 'Recepcionista'] },
        { id: 'pacientes', texto: 'Pacientes', icono: 'bi-people', url: 'pacientes.html', roles: ['Administrador', 'Recepcionista'] },
        { id: 'medicos', texto: 'Médicos', icono: 'bi-person-badge', url: 'medicos.html', roles: ['Administrador', 'Recepcionista'] },
        { id: 'especialidades', texto: 'Especialidades', icono: 'bi-clipboard2-pulse', url: 'especialidades.html', roles: ['Administrador', 'Recepcionista'] },
        { id: 'horarios', texto: 'Horarios y bloqueos', icono: 'bi-clock', url: 'horarios.html', roles: ['Administrador', 'Recepcionista'] },
        { id: 'usuarios', texto: 'Usuarios', icono: 'bi-shield-lock', url: 'usuarios.html', roles: ['Administrador'] },
        { id: 'chatbot', texto: 'Chatbot WhatsApp', icono: 'bi-whatsapp', url: 'chatbot.html', roles: ['Administrador', 'Recepcionista'] },
        { id: 'reportes', texto: 'Reportes', icono: 'bi-bar-chart', url: 'reportes.html', roles: ['Administrador', 'Recepcionista'] },
        { id: 'configuracion', texto: 'Configuración', icono: 'bi-gear', url: 'configuracion.html', roles: ['Administrador'] },
        { id: 'mis-citas', texto: 'Mis citas', icono: 'bi-calendar-heart', url: 'mis-citas.html', roles: ['Paciente'] },
        { id: 'citas', texto: 'Mis consultas', icono: 'bi-calendar2-check', url: 'citas.html', roles: ['Medico'] },
        { id: 'pacientes', texto: 'Mis pacientes', icono: 'bi-people', url: 'pacientes.html', roles: ['Medico'] }
    ];

    /**
     * Dibuja el layout y valida la sesión.
     * @param {{pagina:string, titulo:string, subtitulo:string, roles?:string[]}} opciones
     */
    function iniciar(opciones) {
        const usuario = Api.obtenerUsuario();

        if (!usuario || !Api.obtenerToken()) {
            location.href = 'index.html';
            return null;
        }

        if (opciones.roles && !opciones.roles.includes(usuario.rol)) {
            if (usuario.rol === 'Paciente') location.href = 'mis-citas.html';
            else if (usuario.rol === 'Medico') location.href = 'citas.html';
            else location.href = 'dashboard.html';
            return null;
        }

        document.body.insertAdjacentHTML('afterbegin', sidebar(usuario, opciones.pagina));
        document.querySelector('.mc-contenido')
            ?.insertAdjacentHTML('afterbegin', topbar(usuario, opciones));

        document.getElementById('mc-cerrar-sesion').addEventListener('click', () => {
            Api.limpiarSesion();
            location.href = 'index.html';
        });

        document.getElementById('mc-menu-toggle')?.addEventListener('click', alternarMenu);
        iniciarReloj();

        return usuario;
    }

    function sidebar(usuario, paginaActiva) {
        const iniciales = `${usuario.nombres?.[0] ?? ''}${usuario.apellidos?.[0] ?? ''}`.toUpperCase();

        const enlaces = MENU
            .filter(item => item.roles.includes(usuario.rol))
            .map(item => `
                <li><a href="${item.url}" class="${item.id === paginaActiva ? 'activo' : ''}">
                    <i class="bi ${item.icono}"></i><span>${item.texto}</span>
                </a></li>`)
            .join('');

        return `
        <aside class="mc-sidebar" id="mc-sidebar">
            <div class="mc-marca">
                <img src="assets/img/logo-icon-192.png" alt="MediCitas">
                <div>
                    <strong>MediCitas</strong>
                    <small>${usuario.rol === 'Paciente' ? 'Portal del paciente' : 'Panel administrativo'}</small>
                </div>
            </div>

            <div class="mc-perfil">
                <div class="mc-avatar">${UI.escapar(iniciales)}</div>
                <div>
                    <div class="mc-nombre">${UI.escapar(usuario.nombres)} ${UI.escapar(usuario.apellidos)}</div>
                    <div class="mc-rol">${UI.escapar(usuario.rol)}</div>
                    <div class="mc-estado-online"><i class="bi bi-circle-fill"></i> En línea</div>
                </div>
            </div>

            <ul class="mc-nav">${enlaces}</ul>

            <button class="mc-salir" id="mc-cerrar-sesion">
                <i class="bi bi-box-arrow-left"></i><span>Cerrar sesión</span>
            </button>
        </aside>`;
    }

    function topbar(usuario, opciones) {
        return `
        <header class="mc-topbar">
            <button class="mc-menu-toggle" id="mc-menu-toggle" aria-label="Abrir menú"><i class="bi bi-list"></i></button>
            <div>
                <h1>${UI.escapar(opciones.titulo)}</h1>
                <p>${UI.escapar(opciones.subtitulo || '')}</p>
            </div>
            <div class="mc-topbar-meta">
                <div class="mc-chip"><i class="bi bi-calendar3"></i><span id="mc-fecha-actual"></span></div>
                <div class="mc-chip"><i class="bi bi-clock"></i><span id="mc-hora-actual"></span></div>
            </div>
        </header>`;
    }

    function alternarMenu() {
        const sidebar = document.getElementById('mc-sidebar');
        sidebar.classList.toggle('abierto');

        let fondo = document.getElementById('mc-backdrop');

        if (sidebar.classList.contains('abierto') && !fondo) {
            fondo = document.createElement('div');
            fondo.id = 'mc-backdrop';
            fondo.className = 'mc-backdrop';
            fondo.onclick = alternarMenu;
            document.body.appendChild(fondo);
        } else {
            fondo?.remove();
        }
    }

    function iniciarReloj() {
        const pintar = () => {
            const ahora = new Date();
            const fecha = document.getElementById('mc-fecha-actual');
            const hora = document.getElementById('mc-hora-actual');

            if (fecha) fecha.textContent = ahora.toLocaleDateString('es-GT', { day: 'numeric', month: 'long', year: 'numeric' });
            if (hora) hora.textContent = ahora.toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit', hour12: true });
        };

        pintar();
        setInterval(pintar, 30000);
    }

    return { iniciar };
})();