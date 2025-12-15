document.addEventListener('DOMContentLoaded', function () {
    // QR Code Enlargement
    document.querySelectorAll('.qr-enlarge').forEach(button => {
        button.addEventListener('click', function (e) {
            e.stopPropagation();
            const qrCard = this.closest('.qr-code-item');
            const qrImg = qrCard.querySelector('.qr-code-img');
            const ticketNumber = qrCard.querySelector('.ticket-number').textContent;

            document.getElementById('modalQrImage').src = qrImg.src;
            document.getElementById('modalTicketNumber').textContent = ticketNumber;

            const modal = new bootstrap.Modal(document.getElementById('qrModal'));
            modal.show();
        });
    });

    // Profile Image Upload Preview
    const profileImageUpload = document.getElementById('profileImageUpload');
    const profileImg = document.querySelector('.profile-img');

    if (profileImageUpload && profileImg) {
        profileImageUpload.addEventListener('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    profileImg.src = e.target.result;
                };
                reader.readAsDataURL(file);
            }
        });
    }

    // Smooth tab switching
    const tabLinks = document.querySelectorAll('[data-bs-toggle="tab"]');
    tabLinks.forEach(link => {
        link.addEventListener('shown.bs.tab', function () {
            const activePane = document.querySelector('.tab-pane.active');
            activePane.style.animation = 'fadeIn 0.5s ease';
        });
    });

    // Auto-dismiss alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });

    // Initialize animations
    const style = document.createElement('style');
    style.textContent = `
    @keyframes fadeIn {
                from { opacity: 0; transform: translateY(10px); }
                to { opacity: 1; transform: translateY(0); }
            }

            .tab-pane {
                animation: fadeIn 0.5s ease;
            }
        `;
    document.head.appendChild(style);
});

// Add row indicators for QR codes
document.addEventListener('DOMContentLoaded', function () {
    // Add row numbers to QR codes
    document.querySelectorAll('.qr-codes-container').forEach(container => {
        const items = container.querySelectorAll('.qr-code-item');
        const itemsPerRow = window.innerWidth >= 992 ? 3 : window.innerWidth >= 576 ? 2 : 1;

        items.forEach((item, index) => {
            const row = Math.floor(index / itemsPerRow) + 1;
            const col = (index % itemsPerRow) + 1;

            // Add row/col info as data attributes (optional)
            item.dataset.row = row;
            item.dataset.col = col;

            // Add visual row separator (optional)
            if (col === 1 && row > 1) {
                item.style.borderTop = '2px solid #e9ecef';
                item.style.paddingTop = '1.5rem';
                item.style.marginTop = '0.5rem';
            }
        });

        // Add row headers if you want to label rows
        if (items.length > itemsPerRow) {
            const rows = Math.ceil(items.length / itemsPerRow);
            if (rows > 1) {
                const rowIndicator = document.createElement('div');
                rowIndicator.className = 'row-indicator';
                rowIndicator.innerHTML = `
                    <div class="row-indicator-content">
                        <span>Showing ${items.length} tickets in ${rows} rows</span>
                    </div>
                `;
                container.parentNode.insertBefore(rowIndicator, container.nextSibling);
            }
        }
    });

    // Update on window resize
    let resizeTimer;
    window.addEventListener('resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () {
            document.querySelectorAll('.row-indicator').forEach(el => el.remove());
            // Re-run row calculation
            document.dispatchEvent(new Event('DOMContentLoaded'));
        }, 250);
    });


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

});