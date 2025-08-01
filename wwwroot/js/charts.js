// Global chart instances for management
let biomarkerChart = null;
let correlationChart = null;
let treatmentChart = null;

// Initialize Chart.js with date adapter
Chart.defaults.font.family = '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif';
Chart.defaults.color = '#6c757d';

// Main biomarker trending chart
window.renderBiomarkerChart = function(chartData) {
    const ctx = document.getElementById('biomarkerChart');
    if (!ctx) return;

    // Destroy existing chart if it exists
    if (biomarkerChart) {
        biomarkerChart.destroy();
    }

    // Parse dates and create datasets
    const datasets = chartData.data.datasets.map(dataset => ({
        ...dataset,
        data: dataset.data.map(point => ({
            x: new Date(point.x),
            y: point.y
        })),
        borderWidth: 2,
        pointRadius: 4,
        pointHoverRadius: 6,
        fill: false
    }));

    biomarkerChart = new Chart(ctx, {
        type: 'line',
        data: {
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                intersect: false,
                mode: 'index'
            },
            plugins: {
                title: {
                    display: true,
                    text: 'Biomarker Trends Over Time',
                    font: {
                        size: 16,
                        weight: 'bold'
                    }
                },
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 20
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#fff',
                    bodyColor: '#fff',
                    borderColor: '#fff',
                    borderWidth: 1,
                    cornerRadius: 8,
                    displayColors: true,
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': ' + context.parsed.y + ' ' + getBiomarkerUnits(context.dataset.label);
                        }
                    }
                }
            },
            scales: {
                x: {
                    type: 'time',
                    time: {
                        unit: 'month',
                        displayFormats: {
                            month: 'MMM yyyy'
                        }
                    },
                    title: {
                        display: true,
                        text: 'Date'
                    },
                    grid: {
                        display: false
                    }
                },
                y: {
                    beginAtZero: false,
                    title: {
                        display: true,
                        text: 'Value'
                    },
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)'
                    }
                }
            }
        }
    });
};

// Correlation chart for biomarker relationships
window.renderCorrelationChart = function(correlationData) {
    const ctx = document.getElementById('correlationChart');
    if (!ctx) return;

    if (correlationChart) {
        correlationChart.destroy();
    }

    correlationChart = new Chart(ctx, {
        type: 'scatter',
        data: {
            datasets: correlationData.datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                title: {
                    display: true,
                    text: 'Biomarker Correlations',
                    font: {
                        size: 16,
                        weight: 'bold'
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return context.dataset.label + ': (' + context.parsed.x + ', ' + context.parsed.y + ')';
                        }
                    }
                }
            },
            scales: {
                x: {
                    title: {
                        display: true,
                        text: correlationData.xAxis
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: correlationData.yAxis
                    }
                }
            }
        }
    });
};

// Treatment effectiveness chart
window.renderTreatmentChart = function(treatmentData) {
    const ctx = document.getElementById('treatmentChart');
    if (!ctx) return;

    if (treatmentChart) {
        treatmentChart.destroy();
    }

    treatmentChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: treatmentData.labels,
            datasets: [{
                label: 'Effectiveness Rating',
                data: treatmentData.effectiveness,
                backgroundColor: 'rgba(54, 162, 235, 0.8)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1
            }, {
                label: 'Safety Rating',
                data: treatmentData.safety,
                backgroundColor: 'rgba(255, 99, 132, 0.8)',
                borderColor: 'rgba(255, 99, 132, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                title: {
                    display: true,
                    text: 'Treatment Effectiveness & Safety',
                    font: {
                        size: 16,
                        weight: 'bold'
                    }
                },
                legend: {
                    display: true,
                    position: 'top'
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    max: 5,
                    title: {
                        display: true,
                        text: 'Rating (1-5)'
                    }
                }
            }
        }
    });
};

// Helper function to get biomarker units
function getBiomarkerUnits(biomarkerName) {
    const units = {
        'Vitamin D': 'ng/mL',
        'C-Reactive Protein': 'mg/L',
        'Total Cholesterol': 'mg/dL',
        'HbA1c': '%',
        'LDL Cholesterol': 'mg/dL',
        'HDL Cholesterol': 'mg/dL',
        'Triglycerides': 'mg/dL',
        'TSH': 'mIU/L',
        'Fasting Glucose': 'mg/dL'
    };
    return units[biomarkerName] || '';
}

// Enhanced chart with reference ranges
window.renderBiomarkerChartWithRanges = function(chartData) {
    const ctx = document.getElementById('biomarkerChart');
    if (!ctx) return;

    if (biomarkerChart) {
        biomarkerChart.destroy();
    }

    // Create datasets with reference ranges
    const datasets = [];
    
    chartData.data.datasets.forEach(dataset => {
        // Main data line
        datasets.push({
            label: dataset.label,
            data: dataset.data.map(point => ({
                x: new Date(point.x),
                y: point.y
            })),
            borderColor: dataset.borderColor,
            backgroundColor: dataset.backgroundColor,
            borderWidth: 2,
            pointRadius: 4,
            pointHoverRadius: 6,
            fill: false,
            tension: 0.4
        });

        // Add reference range if available
        if (dataset.referenceRange) {
            datasets.push({
                label: dataset.label + ' (Reference Range)',
                data: dataset.data.map(point => ({
                    x: new Date(point.x),
                    y: dataset.referenceRange.min
                })),
                borderColor: 'rgba(200, 200, 200, 0.5)',
                backgroundColor: 'rgba(200, 200, 200, 0.1)',
                borderWidth: 1,
                borderDash: [5, 5],
                pointRadius: 0,
                fill: false
            });

            datasets.push({
                label: dataset.label + ' (Upper Limit)',
                data: dataset.data.map(point => ({
                    x: new Date(point.x),
                    y: dataset.referenceRange.max
                })),
                borderColor: 'rgba(200, 200, 200, 0.5)',
                backgroundColor: 'rgba(200, 200, 200, 0.1)',
                borderWidth: 1,
                borderDash: [5, 5],
                pointRadius: 0,
                fill: false
            });
        }
    });

    biomarkerChart = new Chart(ctx, {
        type: 'line',
        data: {
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                intersect: false,
                mode: 'index'
            },
            plugins: {
                title: {
                    display: true,
                    text: 'Biomarker Trends with Reference Ranges',
                    font: {
                        size: 16,
                        weight: 'bold'
                    }
                },
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 20
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#fff',
                    bodyColor: '#fff',
                    borderColor: '#fff',
                    borderWidth: 1,
                    cornerRadius: 8,
                    displayColors: true
                }
            },
            scales: {
                x: {
                    type: 'time',
                    time: {
                        unit: 'month',
                        displayFormats: {
                            month: 'MMM yyyy'
                        }
                    },
                    title: {
                        display: true,
                        text: 'Date'
                    }
                },
                y: {
                    beginAtZero: false,
                    title: {
                        display: true,
                        text: 'Value'
                    }
                }
            }
        }
    });
};

// Cleanup function for chart management
window.destroyCharts = function() {
    if (biomarkerChart) {
        biomarkerChart.destroy();
        biomarkerChart = null;
    }
    if (correlationChart) {
        correlationChart.destroy();
        correlationChart = null;
    }
    if (treatmentChart) {
        treatmentChart.destroy();
        treatmentChart = null;
    }
}; 