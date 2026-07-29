/* Dashboard: indicadores del día, gráficas del mes y próximos movimientos. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'dashboard',
        titulo: 'Bienvenido, ' + (Api.obtenerUsuario()?.nombres ?? ''),
        subtitulo: 'Resumen general del sistema de gestión de citas médicas',
        roles: ['Administrador', 'Recepcionista']
    });

    if (!usuario) return;

    const PALETA = ['#2563EB', '#38BDF8', '#14B8A6', '#22C55E', '#F59E0B', '#EF4444', '#8B5CF6'];
    let graficas = {};

    cargar();

    async function cargar() {
        try {
            const datos = await Api.get('/dashboard');
            pintarKpis(datos);
            pintarEstados(datos);
            pintarEspecialidades(datos);
            pintarOcupacion(datos);
            pintarProximas(datos);
            pintarCupos(datos);
            pintarResumen(datos);
        } catch (error) {
            UI.error(error.message);
        }
    }

    function pintarKpis(d) {
        document.getElementById('kpi-hoy').textContent = d.citasHoy;
        document.getElementById('kpi-confirmadas').textContent = d.confirmadas;
        document.getElementById('kpi-pendientes').textContent = d.pendientes;
        document.getElementById('kpi-canceladas').textContent = d.canceladas;
        document.getElementById('kpi-pacientes').textContent = d.pacientesRegistrados.toLocaleString('es-GT');

        document.getElementById('kpi-confirmadas-pct').textContent = `${d.porcentajeConfirmadas}% del día`;
        document.getElementById('kpi-pendientes-pct').textContent = `${d.porcentajePendientes}% del día`;
        document.getElementById('kpi-canceladas-pct').textContent = `${d.porcentajeCanceladas}% del día`;
    }

    function pintarEstados(d) {
        const contenedor = document.getElementById('leyenda-estados');

        if (!d.citasPorEstadoMes.length) {
            contenedor.innerHTML = '<p class="mc-texto-muted mb-0">Aún no hay citas registradas este mes.</p>';
            return;
        }

        graficas.estados?.destroy();
        graficas.estados = new Chart(document.getElementById('grafica-estados'), {
            type: 'doughnut',
            data: {
                labels: d.citasPorEstadoMes.map(x => x.etiqueta),
                datasets: [{
                    data: d.citasPorEstadoMes.map(x => x.cantidad),
                    backgroundColor: d.citasPorEstadoMes.map((x, i) => x.color || PALETA[i % PALETA.length]),
                    borderWidth: 0
                }]
            },
            options: { cutout: '68%', plugins: { legend: { display: false } } }
        });

        contenedor.innerHTML = d.citasPorEstadoMes.map((x, i) => `
            <div class="d-flex align-items-center gap-2" style="font-size:.84rem">
                <span style="width:10px;height:10px;border-radius:50%;background:${x.color || PALETA[i % PALETA.length]}"></span>
                <span class="flex-grow-1">${UI.escapar(x.etiqueta)}</span>
                <strong style="color:var(--text)">${x.porcentaje}%</strong>
                <span class="mc-texto-muted">(${x.cantidad})</span>
            </div>`).join('');
    }

    function pintarEspecialidades(d) {
        if (!d.citasPorEspecialidadMes.length) return;

        graficas.especialidades?.destroy();
        graficas.especialidades = new Chart(document.getElementById('grafica-especialidades'), {
            type: 'bar',
            data: {
                labels: d.citasPorEspecialidadMes.map(x => x.etiqueta),
                datasets: [{
                    data: d.citasPorEspecialidadMes.map(x => x.cantidad),
                    backgroundColor: PALETA,
                    borderRadius: 8,
                    maxBarThickness: 46
                }]
            },
            options: {
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: '#F3F4F6' } },
                    x: { grid: { display: false } }
                }
            }
        });
    }

    function pintarOcupacion(d) {
        document.getElementById('ocupacion-valor').textContent = `${Math.round(d.porcentajeOcupacion)}%`;

        graficas.ocupacion?.destroy();
        graficas.ocupacion = new Chart(document.getElementById('grafica-ocupacion'), {
            type: 'doughnut',
            data: {
                labels: ['Ocupado', 'Disponible'],
                datasets: [{
                    data: [d.porcentajeOcupacion, Math.max(0, 100 - d.porcentajeOcupacion)],
                    backgroundColor: ['#2563EB', '#E5E7EB'],
                    borderWidth: 0
                }]
            },
            options: { cutout: '72%', circumference: 180, rotation: 270, plugins: { legend: { display: false } } }
        });
    }

    function pintarProximas(d) {
        const cuerpo = document.getElementById('tabla-proximas');

        if (!d.proximasCitasHoy.length) {
            cuerpo.innerHTML = UI.filaVacia(5, 'No hay citas programadas para hoy.', 'bi-calendar2-x');
            return;
        }

        cuerpo.innerHTML = d.proximasCitasHoy.map(c => `
            <tr>
                <td class="mc-principal">${UI.escapar(c.hora)}</td>
                <td>${UI.escapar(c.paciente)}</td>
                <td>${UI.escapar(c.medico)}</td>
                <td>${UI.escapar(c.especialidad)}</td>
                <td>${UI.etiquetaEstado(c.estado)}</td>
            </tr>`).join('');
    }

    function pintarCupos(d) {
        const contenedor = document.getElementById('lista-cupos');

        if (!d.proximosHorariosDisponibles.length) {
            contenedor.innerHTML = '<p class="mc-texto-muted mb-0">No hay cupos libres hoy. Revise la agenda de los próximos días.</p>';
            return;
        }

        contenedor.innerHTML = d.proximosHorariosDisponibles.map(s => `
            <div class="d-flex align-items-center gap-2" style="font-size:.85rem">
                <i class="bi bi-clock" style="color:var(--accent)"></i>
                <div class="flex-grow-1">
                    <strong style="color:var(--text)">${UI.hora(s.inicio)}</strong>
                    <span class="mc-texto-muted"> · ${UI.escapar(s.medico)}</span>
                </div>
                <span class="mc-texto-muted">${UI.escapar(s.especialidad)}</span>
            </div>`).join('');
    }

    function pintarResumen(d) {
        const filas = [
            ['bi-calendar3', 'mc-icono-azul', 'Citas programadas', d.citasHoy],
            ['bi-check2', 'mc-icono-verde', 'Confirmadas', d.confirmadas],
            ['bi-hourglass', 'mc-icono-ambar', 'Pendientes', d.pendientes],
            ['bi-x-lg', 'mc-icono-rojo', 'Canceladas', d.canceladas],
            ['bi-graph-up', 'mc-icono-morado', 'Citas del mes', d.totalCitasMes]
        ];

        document.getElementById('resumen-dia').innerHTML = filas.map(([icono, clase, texto, valor]) => `
            <div class="d-flex align-items-center gap-2" style="font-size:.85rem">
                <span class="mc-kpi-icono ${clase}" style="width:28px;height:28px;font-size:.85rem;border-radius:8px">
                    <i class="bi ${icono}"></i>
                </span>
                <span class="flex-grow-1">${texto}</span>
                <strong style="color:var(--text)">${valor}</strong>
            </div>`).join('');
    }
})();
