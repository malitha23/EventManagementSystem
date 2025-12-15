// organizer-dashboard.js
(function () {
    if (typeof $ === 'undefined') {
        console.error('jQuery is not loaded. Dashboard scripts cannot run.');
        return;
    }

    $(document).ready(function () {
        // Get data from canvas data attributes
        var canvas = document.getElementById('revenueChart');
        if (!canvas) return;

        var months = JSON.parse(canvas.dataset.months || '[]');
        var revenues = JSON.parse(canvas.dataset.revenues || '[]');

        window.revenueChartData = {
            months: months,
            revenues: revenues
        };

        if (months.length > 0 && revenues.length > 0) {
            initRevenueChart();
        } else {
            $('#revenueChart').parent().html(
                '<div class="text-center py-5 text-muted">' +
                '<i class="fas fa-chart-line fa-3x mb-3"></i><br>' +
                'No revenue data available for the selected period' +
                '</div>'
            );
        }

        setInterval(refreshDashboardData, 60000);

        $('#chartPeriod').change(function () {
            loadRevenueChart($(this).val());
        });
    });

    // Initialize Revenue Chart
    function initRevenueChart() {
        var canvas = document.getElementById('revenueChart');
        if (!canvas) return;

        var ctx = canvas.getContext('2d');
        if (!ctx) return;

        var months = window.revenueChartData.months || [];
        var revenues = window.revenueChartData.revenues || [];

        var formattedRevenues = revenues.map(function (r) { return parseFloat(r) || 0; });

        if (window.revenueChart instanceof Chart) window.revenueChart.destroy();

        window.revenueChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: months,
                datasets: [{
                    label: 'Revenue (LKR)',
                    data: formattedRevenues,
                    borderColor: '#4e73df',
                    backgroundColor: 'rgba(78, 115, 223, 0.05)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.3,
                    pointBackgroundColor: '#4e73df',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { intersect: false, mode: 'index' },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        backgroundColor: 'rgba(0,0,0,0.8)',
                        titleColor: '#fff',
                        bodyColor: '#fff',
                        borderColor: '#4e73df',
                        borderWidth: 1,
                        padding: 12,
                        callbacks: {
                            label: function (context) {
                                return 'Revenue: LKR ' + context.parsed.y.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { drawBorder: false, color: 'rgba(0,0,0,0.05)' },
                        ticks: {
                            padding: 10,
                            callback: function (value) {
                                if (value >= 1000000) return 'LKR ' + (value / 1000000).toFixed(1) + 'M';
                                if (value >= 1000) return 'LKR ' + (value / 1000).toFixed(0) + 'K';
                                return 'LKR ' + value.toLocaleString();
                            }
                        },
                        title: { display: true, text: 'Revenue (LKR)', color: '#666', font: { size: 12, weight: 'normal' } }
                    },
                    x: { grid: { display: false }, ticks: { maxRotation: 45, minRotation: 45 } }
                }
            }
        });
    }

    function loadRevenueChart(months) {
        var chartContainer = $('#revenueChart').parent();
        var originalHeight = chartContainer.height();
        chartContainer.html(
            '<div class="text-center py-5" style="height:' + originalHeight + 'px; display:flex; align-items:center; justify-content:center;">' +
            '<div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div>' +
            '<span class="ms-3">Loading chart data...</span></div>'
        );

        $.ajax({
            url: '/Organizer/GetMonthlyRevenueChart',
            type: 'GET',
            data: { months: months },
            success: function (data) {
                if (!data || data.error) {
                    chartContainer.html('<div class="text-center py-5 text-danger"><i class="fas fa-exclamation-triangle fa-2x mb-3"></i><br>Unable to load chart data</div>');
                    return;
                }
                window.revenueChartData = { months: data.map(x => x.month), revenues: data.map(x => x.revenue) };
                chartContainer.html('<canvas id="revenueChart"></canvas>');
                if (data.length > 0) setTimeout(initRevenueChart, 100);
                else chartContainer.html('<div class="text-center py-5 text-muted"><i class="fas fa-chart-line fa-3x mb-3"></i><br>No revenue data available</div>');
            },
            error: function () {
                chartContainer.html('<div class="text-center py-5 text-danger"><i class="fas fa-exclamation-triangle fa-2x mb-3"></i><br>Error loading chart data</div>');
            }
        });
    }

    function refreshDashboardData() {
        $.ajax({
            url: '/Organizer/GetDashboardStats', type: 'GET', cache: false, success: function (stats) {
                if (!stats || stats.error) return;
                updateStatCard('.card.border-start-primary .h5', 'LKR ' + formatCurrency(stats.totalRevenue));
                updateStatCard('.card.border-start-success .h5', formatNumber(stats.totalBookings));
                updateStatCard('.card.border-start-info .h5', formatNumber(stats.ticketsSold));
                updateStatCard('.card.border-start-warning .h5', formatNumber(stats.activeEvents));
                updateStatCard('.card.border-start-danger .h5', parseFloat(stats.averageOccupancy).toFixed(1) + '%');
                updateStatCard('.card.border-start-secondary .h5', 'LKR ' + formatCurrency(stats.pendingPayments));
                showRefreshTime();
            }
        });
        $.ajax({ url: '/Organizer/GetRecentBookings', type: 'GET', cache: false, success: function (bookings) { console.log('Bookings refreshed:', bookings.length); } });
    }

    function updateStatCard(selector, value) {
        var element = $(selector);
        if (element.length) {
            element.parent().parent().addClass('highlight-card');
            element.fadeOut(100, function () { $(this).text(value).fadeIn(100, function () { setTimeout(() => element.parent().parent().removeClass('highlight-card'), 500); }); });
        }
    }

    function formatCurrency(value) { var num = parseFloat(value) || 0; return num.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function formatNumber(value) { var num = parseInt(value) || 0; return num.toLocaleString('en-US'); }

    function showRefreshTime() {
        var now = new Date();
        var timeString = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        var refreshIndicator = $('#refresh-indicator');
        if (refreshIndicator.length === 0) {
            refreshIndicator = $('<small id="refresh-indicator" class="text-muted ms-2"></small>');
            $('h1.h3').append(refreshIndicator);
        }
        refreshIndicator.html('<i class="fas fa-sync-alt fa-sm ms-2"></i> Updated ' + timeString);
        refreshIndicator.fadeTo(200, 1).delay(3000).fadeTo(500, 0.5);
    }

    // Add these functions to your existing organizer-dashboard.js

    $(document).ready(function () {
        // ... existing code ...

        // Initialize modal when DOM is ready
        initializeCreateEventModal();

        // ... existing code ...
    });

    function initializeCreateEventModal() {

        $('#createEventModal').on('show.bs.modal', function () {
            loadCategories();
            loadVenues();
        });

        // Toggle new venue input
        $('#addVenueLink').click(function (e) {
            e.preventDefault();
            $('#newVenueFields').toggleClass('d-none');

            if (!$('#newVenueFields').hasClass('d-none')) {
                $('#venueSelect').prop('disabled', true).val('');
            } else {
                $('#venueSelect').prop('disabled', false);
            }
        });

        $('#createEventModal').on('hidden.bs.modal', function () {
            $('#newVenueFields').addClass('d-none');
            $('#venueSelect').prop('disabled', false);
            $('#createEventForm')[0].reset();
        });

        // ✅ SINGLE submit handler
        $('#createEventForm').on('submit', function (e) {
            e.preventDefault();

            const venueId = $('#venueSelect').val();
            const newVenueName = $('#newVenueName').val();
            const newVenueLocation = $('#newVenueLocation').val();
            const newVenueDescription = $('#newVenueDescription').val();
            const newVenueCapacity = $('#newVenueCapacity').val();

            // Validation: venue OR new venue required
            if (!venueId && (!newVenueName || !newVenueLocation || !newVenueCapacity)) {
                alert('Please select a venue or fill all new venue details (name, location, capacity)');
                return;
            }

            const formData = new FormData(this); // automatically includes all form inputs
            const btn = $('#createEventBtn');

            btn.prop('disabled', true);
            btn.find('.spinner-border').removeClass('d-none');

            $.ajax({
                url: '/Event/Create',
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: function (res) {
                    if (res.success) {
                        alert(res.message);
                        $('#createEventModal').modal('hide');
                        location.reload(); // optional
                    } else {
                        alert(res.message);
                    }
                },
                error: function () {
                    alert('Server error occurred');
                },
                complete: function () {
                    btn.prop('disabled', false);
                    btn.find('.spinner-border').addClass('d-none');
                }
            });
        });

    }


    function loadCategories() {
        $.ajax({
            url: '/Event/GetEventCategories',
            type: 'GET',
            success: function (categories) {
                const categorySelect = $('#categorySelect');
                categorySelect.empty().append('<option value="">-- Select Category --</option>');

                categories.forEach(c =>
                    categorySelect.append(`<option value="${c.id}">${c.name}</option>`)
                );
            },
            error: function () {
                alert('Error loading categories');
            }
        });
    }

    function loadVenues() {
        $.ajax({
            url: '/Event/GetVenues',
            type: 'GET',
            success: function (venues) {
                const venueSelect = $('#venueSelect');
                venueSelect.empty().append('<option value="">-- Select Venue --</option>');

                venues.forEach(v =>
                    venueSelect.append(`<option value="${v.id}">${v.name} - ${v.location}</option>`)
                );
            },
            error: function () {
                alert('Error loading venues');
            }
        });
    }



  
})();
