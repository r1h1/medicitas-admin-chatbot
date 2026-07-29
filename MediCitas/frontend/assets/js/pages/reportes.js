/* Reportes del periodo: indicadores de gestión y distribución de citas. */
(() => {
    const usuario = Layout.iniciar({
        pagina: 'reportes',
        titulo: 'Reportes',
        subtitulo: 'Indicadores para medir el impacto del sistema en la clínica',
        roles: ['Administrador', 'Recepcionista']
    });

    if (!usuario) return;

    const PALETA = ['#2563EB', '#38BDF8', '#14B8A6', '#22C55E', '#F59E0B', '#EF4444', '#8B5CF6'];
    let graficas = {};
    let ultimoReporte = null;

    iniciar();

    function iniciar() {
        const hoy = new Date();
        const inicioMes = new Date(hoy.getFullYear(), hoy.getMonth(), 1);

        document.getElementById('r-desde').value = UI.isoFecha(inicioMes);
        document.getElementById('r-hasta').value = UI.isoFecha(hoy);

        document.getElementById('btn-generar').onclick = generar;
        document.getElementById('btn-exportar').onclick = exportar;

        generar();
    }

    async function generar() {
        const desde = document.getElementById('r-desde').value;
        const hasta = document.getElementById('r-hasta').value;

        if (!desde || !hasta) return UI.aviso('Indique el rango de fechas.');
        if (desde > hasta) return UI.aviso('La fecha inicial no puede ser mayor que la final.');

        try {
            ultimoReporte = await Api.get('/dashboard/reporte', { desde, hasta });
            pintarIndicadores(ultimoReporte);
            pintarDias(ultimoReporte);
            pintarEspecialidades(ultimoReporte);
            pintarMedicos(ultimoReporte);
        } catch (error) {
            UI.error(error.message);
        }
    }

    function pintarIndicadores(r) {
        const tarjetas = [
            ['Total de citas', r.totalCitas, 'bi-calendar2-check', 'mc-icono-azul', 'En el periodo'],
            ['Citas atendidas', r.atendidas, 'bi-check2-all', 'mc-icono-verde', 'Completadas'],
            ['Tasa de ausentismo', `${r.tasaAusentismo}%`, 'bi-person-x', 'mc-icono-rojo', `${r.noAsistio} inasistencias`],
            ['Tasa de confirmación', `${r.tasaConfirmacion}%`, 'bi-hand-thumbs-up', 'mc-icono-verde', 'Citas confirmadas'],
            ['Agendadas por WhatsApp', r.citasPorWhatsApp, 'bi-whatsapp', 'mc-icono-verde-wa', `${r.citasPorPanel} desde el panel`],
            ['Recordatorios enviados', r.recordatoriosEnviados, 'bi-bell', 'mc-icono-morado', `${r.retrasosReportados} retrasos avisados`]
        ];

        document.getElementById('indicadores').innerHTML = tarjetas.map(([titulo, valor, icono, clase, pie]) => `
            <div class="col-6 col-lg-2">
                <div class="mc-card mc-kpi h-100">
                    <div class="mc-kpi-icono ${clase}"><i class="bi ${icono}"></i></div>
                    <div>
                        <p class="mc-kpi-label">${titulo}</p>
                        <div class="mc-kpi-valor" style="font-size:1.6rem">${valor}</div>
                        <p class="mc-kpi-pie">${pie}</p>
                    </div>
                </div>
            </div>`).join('');
    }

    function pintarDias(r) {
        graficas.dias?.destroy();
        graficas.dias = new Chart(document.getElementById('grafica-dias'), {
            type: 'line',
            data: {
                labels: r.citasPorDia.map(x => x.etiqueta),
                datasets: [{
                    data: r.citasPorDia.map(x => x.cantidad),
                    borderColor: '#2563EB',
                    backgroundColor: 'rgba(37,99,235,.12)',
                    fill: true,
                    tension: .35,
                    pointBackgroundColor: '#2563EB'
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

    function pintarEspecialidades(r) {
        graficas.especialidades?.destroy();
        graficas.especialidades = new Chart(document.getElementById('grafica-especialidades'), {
            type: 'doughnut',
            data: {
                labels: r.citasPorEspecialidad.map(x => x.etiqueta),
                datasets: [{
                    data: r.citasPorEspecialidad.map(x => x.cantidad),
                    backgroundColor: PALETA,
                    borderWidth: 0
                }]
            },
            options: { cutout: '62%', plugins: { legend: { position: 'bottom', labels: { boxWidth: 10, font: { size: 11 } } } } }
        });
    }

    function pintarMedicos(r) {
        const cuerpo = document.getElementById('tabla-medicos');

        cuerpo.innerHTML = r.citasPorMedico.length
            ? r.citasPorMedico.map(m => `
                <tr>
                    <td class="mc-principal">${UI.escapar(m.etiqueta)}</td>
                    <td>${m.cantidad}</td>
                    <td>
                        <div class="d-flex align-items-center gap-2">
                            <div class="mc-progreso flex-grow-1"><span style="width:${m.porcentaje}%"></span></div>
                            <span class="mc-texto-muted">${m.porcentaje}%</span>
                        </div>
                    </td>
                </tr>`).join('')
            : UI.filaVacia(3, 'Sin citas registradas en el periodo.', 'bi-bar-chart');
    }

    function exportar() {
        if (!ultimoReporte) return UI.aviso('Genere primero el reporte.');

        const filas = [
            ['Reporte MediCitas'],
            ['Desde', ultimoReporte.desde, 'Hasta', ultimoReporte.hasta],
            [],
            ['Indicador', 'Valor'],
            ['Total de citas', ultimoReporte.totalCitas],
            ['Atendidas', ultimoReporte.atendidas],
            ['Canceladas', ultimoReporte.canceladas],
            ['No asistió', ultimoReporte.noAsistio],
            ['Tasa de ausentismo (%)', ultimoReporte.tasaAusentismo],
            ['Tasa de confirmación (%)', ultimoReporte.tasaConfirmacion],
            ['Agendadas por WhatsApp', ultimoReporte.citasPorWhatsApp],
            ['Agendadas desde el panel', ultimoReporte.citasPorPanel],
            ['Recordatorios enviados', ultimoReporte.recordatoriosEnviados],
            ['Retrasos reportados', ultimoReporte.retrasosReportados],
            [],
            ['Médico', 'Citas', 'Participación (%)'],
            ...ultimoReporte.citasPorMedico.map(m => [m.etiqueta, m.cantidad, m.porcentaje]),
            [],
            ['Especialidad', 'Citas', 'Participación (%)'],
            ...ultimoReporte.citasPorEspecialidad.map(e => [e.etiqueta, e.cantidad, e.porcentaje])
        ];

        const csv = filas.map(f => f.map(v => `"${String(v ?? '').replaceAll('"', '""')}"`).join(';')).join('\n');
        const enlace = document.createElement('a');

        enlace.href = URL.createObjectURL(new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' }));
        enlace.download = `reporte-medicitas-${ultimoReporte.desde}-a-${ultimoReporte.hasta}.csv`;
        enlace.click();
        URL.revokeObjectURL(enlace.href);
    }
})();
