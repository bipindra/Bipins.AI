// Analysis results rendering
var analysisApp = {
    renderCharts: function(analysis) {
        if (!analysis || !analysis.cloudCosts) {
            return;
        }
        
        analysis.cloudCosts.forEach(function(cloudCost) {
            // Service cost chart
            var serviceCtx = document.getElementById('chart-' + cloudCost.provider + '-service');
            if (serviceCtx) {
                var serviceLabels = Object.keys(cloudCost.costByService || {});
                var serviceData = Object.values(cloudCost.costByService || {});
                
                new Chart(serviceCtx, {
                    type: 'doughnut',
                    data: {
                        labels: serviceLabels,
                        datasets: [{
                            data: serviceData,
                            backgroundColor: [
                                'rgba(255, 99, 132, 0.8)',
                                'rgba(54, 162, 235, 0.8)',
                                'rgba(255, 206, 86, 0.8)',
                                'rgba(75, 192, 192, 0.8)',
                                'rgba(153, 102, 255, 0.8)',
                                'rgba(255, 159, 64, 0.8)'
                            ]
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: true,
                        plugins: {
                            title: {
                                display: true,
                                text: 'Cost by Service'
                            },
                            legend: {
                                position: 'bottom'
                            }
                        }
                    }
                });
            }
            
            // Region cost chart
            var regionCtx = document.getElementById('chart-' + cloudCost.provider + '-region');
            if (regionCtx) {
                var regionLabels = Object.keys(cloudCost.costByRegion || {});
                var regionData = Object.values(cloudCost.costByRegion || {});
                
                new Chart(regionCtx, {
                    type: 'bar',
                    data: {
                        labels: regionLabels,
                        datasets: [{
                            label: 'Monthly Cost ($)',
                            data: regionData,
                            backgroundColor: 'rgba(54, 162, 235, 0.8)'
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: true,
                        plugins: {
                            title: {
                                display: true,
                                text: 'Cost by Region'
                            },
                            legend: {
                                display: false
                            }
                        },
                        scales: {
                            y: {
                                beginAtZero: true,
                                ticks: {
                                    callback: function(value) {
                                        return '$' + value.toFixed(2);
                                    }
                                }
                            }
                        }
                    }
                });
            }
        });
    }
};
