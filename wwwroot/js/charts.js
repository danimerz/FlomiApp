// FlomiApp Chart.js helpers
window.flomiCharts = (function () {
    const instances = {};

    function destroy(id) {
        if (instances[id]) { instances[id].destroy(); delete instances[id]; }
    }

    function create(id, config) {
        destroy(id);
        const ctx = document.getElementById(id);
        if (!ctx) return;
        instances[id] = new Chart(ctx, config);
    }

    return {
        // Line chart: Anmeldungen über Zeit
        renderTimeline: function (canvasId, labels, registered, cancelled) {
            create(canvasId, {
                type: 'line',
                data: {
                    labels,
                    datasets: [
                        {
                            label: 'Anmeldungen',
                            data: registered,
                            borderColor: '#2563eb',
                            backgroundColor: 'rgba(37,99,235,0.10)',
                            tension: 0.4,
                            fill: true,
                            pointRadius: 4,
                            pointBackgroundColor: '#2563eb'
                        },
                        {
                            label: 'Stornierungen',
                            data: cancelled,
                            borderColor: '#ef4444',
                            backgroundColor: 'rgba(239,68,68,0.08)',
                            tension: 0.4,
                            fill: true,
                            pointRadius: 4,
                            pointBackgroundColor: '#ef4444'
                        }
                    ]
                },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    plugins: { legend: { position: 'top' } },
                    scales: {
                        y: { beginAtZero: true, ticks: { stepSize: 1 } },
                        x: { grid: { display: false } }
                    }
                }
            });
        },

        // Donut chart: Anmeldungen nach Kategorie
        renderCategoryDonut: function (canvasId, labels, data) {
            const colors = ['#2563eb','#16a34a','#d97706','#7c3aed','#db2777','#0891b2','#65a30d'];
            create(canvasId, {
                type: 'doughnut',
                data: {
                    labels,
                    datasets: [{
                        data,
                        backgroundColor: colors.slice(0, labels.length),
                        borderWidth: 2,
                        borderColor: '#ffffff'
                    }]
                },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    plugins: {
                        legend: { position: 'right' },
                        tooltip: { callbacks: { label: ctx => ` ${ctx.label}: ${ctx.parsed} Anmeldungen` } }
                    },
                    cutout: '62%'
                }
            });
        },

        // Horizontal bar: Belegungsrate pro Bereich
        renderAreaBars: function (canvasId, labels, registered, capacity) {
            const pct = registered.map((r, i) => capacity[i] >= 999 ? 0 : Math.round(r / capacity[i] * 100));
            create(canvasId, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [{
                        label: 'Belegt (%)',
                        data: pct,
                        backgroundColor: pct.map(p => p >= 100 ? '#16a34a' : p >= 75 ? '#d97706' : '#2563eb'),
                        borderRadius: 6,
                        borderSkipped: false
                    }]
                },
                options: {
                    indexAxis: 'y',
                    responsive: true, maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: (ctx) => {
                                    const i = ctx.dataIndex;
                                    return ` ${registered[i]} / ${capacity[i] >= 999 ? '∞' : capacity[i]} (${ctx.parsed.x}%)`;
                                }
                            }
                        }
                    },
                    scales: {
                        x: { min: 0, max: 100, ticks: { callback: v => v + '%' } },
                        y: { grid: { display: false } }
                    }
                }
            });
        },

        // Bar chart: Anmeldungen nach Stufe
        renderStufeBars: function (canvasId, labels, data) {
            const colors = ['#6d28d9','#2563eb','#0891b2','#16a34a','#d97706','#db2777'];
            create(canvasId, {
                type: 'bar',
                data: {
                    labels,
                    datasets: [{
                        label: 'Anmeldungen',
                        data,
                        backgroundColor: colors.slice(0, labels.length),
                        borderRadius: 8,
                        borderSkipped: false
                    }]
                },
                options: {
                    responsive: true, maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        y: { beginAtZero: true, ticks: { stepSize: 1 } },
                        x: { grid: { display: false } }
                    }
                }
            });
        }
    };
})();
