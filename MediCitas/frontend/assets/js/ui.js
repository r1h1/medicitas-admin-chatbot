/* Utilidades de interfaz: avisos, formatos y ayudas de tabla. */
const UI = (() => {

    function toast(mensaje, tipo = 'exito', titulo = '') {
        let contenedor = document.getElementById('mc-toasts');
        if (!contenedor) {
            contenedor = document.createElement('div');
            contenedor.id = 'mc-toasts';
            document.body.appendChild(contenedor);
        }

        const aviso = document.createElement('div');
        aviso.className = `mc-toast ${tipo}`;
        aviso.innerHTML = `${titulo ? `<strong>${titulo}</strong>` : ''}${escapar(mensaje)}`;
        contenedor.appendChild(aviso);

        setTimeout(() => aviso.remove(), 4200);
    }

    const exito = (m) => toast(m, 'exito', 'Listo');
    const error = (m) => toast(m, 'error', 'No se pudo completar');
    const aviso = (m) => toast(m, 'aviso', 'Atención');

    function escapar(texto) {
        if (texto === null || texto === undefined) return '';
        return String(texto)
            .replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;').replaceAll("'", '&#039;');
    }

    /**
     * Convierte una cadena ISO a Date siempre en hora local.
     * new Date("2026-09-15") la trata como UTC y en GT (UTC-6) queda el día anterior.
     * Esta función evita ese desfase.
     */
    const parsearLocal = (iso) => {
        if (!iso) return null;
        // Si ya trae hora (contiene T o espacio), el navegador respeta la zona local.
        if (iso.includes('T') || iso.includes(' ')) return new Date(iso);
        // "YYYY-MM-DD" sin hora: construir explícitamente en hora local.
        const [y, m, d] = iso.split('-').map(Number);
        return new Date(y, m - 1, d);
    };

    const fechaCorta = (iso) => {
        const d = parsearLocal(iso);
        return d ? d.toLocaleDateString('es-GT', { day: '2-digit', month: '2-digit', year: 'numeric' }) : '';
    };

    const hora = (iso) => iso ? new Date(iso).toLocaleTimeString('es-GT', { hour: '2-digit', minute: '2-digit', hour12: true }) : '';

    const fechaHora = (iso) => iso ? `${fechaCorta(iso)} · ${hora(iso)}` : '';

    const fechaLarga = (iso) => {
        const d = parsearLocal(iso);
        return d ? d.toLocaleDateString('es-GT', {
            weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
        }) : '';
    };

    const isoFecha = (fecha) => {
        const d = fecha instanceof Date ? fecha : parsearLocal(fecha);
        if (!d) return '';
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    };

    const etiquetaEstado = (estado) =>
        `<span class="mc-estado mc-estado-${(estado || '').toLowerCase()}">${escapar(estado)}</span>`;

    const filaVacia = (columnas, mensaje, icono = 'bi-inbox') =>
        `<tr><td colspan="${columnas}"><div class="mc-vacio"><i class="bi ${icono}"></i>${escapar(mensaje)}</div></td></tr>`;

    function cargando(elemento, columnas = 5) {
        elemento.innerHTML = `<tr><td colspan="${columnas}" class="text-center py-4 mc-texto-muted">
            <span class="spinner-border spinner-border-sm me-2"></span>Cargando…</td></tr>`;
    }

    /** Confirmación simple reutilizable. Devuelve una promesa booleana. */
    function confirmar(mensaje, textoBoton = 'Confirmar') {
        return new Promise(resolver => {
            const id = 'mc-modal-confirmar';
            document.getElementById(id)?.remove();

            const html = `
            <div class="modal fade" id="${id}" tabindex="-1">
              <div class="modal-dialog modal-dialog-centered modal-sm">
                <div class="modal-content">
                  <div class="modal-body text-center p-4">
                    <i class="bi bi-question-circle" style="font-size:2rem;color:var(--warning)"></i>
                    <p class="mt-3 mb-4">${escapar(mensaje)}</p>
                    <div class="d-flex gap-2">
                      <button class="btn btn-mc-secundario w-50" data-bs-dismiss="modal">Cancelar</button>
                      <button class="btn btn-mc-primary w-50" id="${id}-ok">${escapar(textoBoton)}</button>
                    </div>
                  </div>
                </div>
              </div>
            </div>`;

            document.body.insertAdjacentHTML('beforeend', html);
            const elemento = document.getElementById(id);
            const modal = new bootstrap.Modal(elemento);
            let respuesta = false;

            document.getElementById(`${id}-ok`).onclick = () => { respuesta = true; modal.hide(); };
            elemento.addEventListener('hidden.bs.modal', () => { elemento.remove(); resolver(respuesta); });
            modal.show();
        });
    }

    /** Ejecuta una acción mostrando el botón en estado de espera. */
    async function conEspera(boton, accion) {
        const original = boton.innerHTML;
        boton.disabled = true;
        boton.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Procesando…';
        try { await accion(); }
        finally { boton.disabled = false; boton.innerHTML = original; }
    }

    return { toast, exito, error, aviso, escapar, fechaCorta, hora, fechaHora, fechaLarga, isoFecha, etiquetaEstado, filaVacia, cargando, confirmar, conEspera };
})();