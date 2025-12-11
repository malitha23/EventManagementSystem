document.addEventListener('DOMContentLoaded', function () {
    triggerConfetti();
    initAnimations();
    playSuccessSound();
    setShareUrl();
});

// ----------------------------------------
// Confetti Animation
// ----------------------------------------
function triggerConfetti() {
    const duration = 3 * 1000;
    const animationEnd = Date.now() + duration;
    const defaults = { startVelocity: 30, spread: 360, ticks: 60, zIndex: 9999 };

    function randomInRange(min, max) {
        return Math.random() * (max - min) + min;
    }

    const interval = setInterval(function () {
        const timeLeft = animationEnd - Date.now();
        if (timeLeft <= 0) return clearInterval(interval);

        const particleCount = 50 * (timeLeft / duration);

        confetti({
            ...defaults,
            particleCount,
            origin: { x: randomInRange(0.1, 0.3), y: Math.random() - 0.2 }
        });

        confetti({
            ...defaults,
            particleCount,
            origin: { x: randomInRange(0.7, 0.9), y: Math.random() - 0.2 }
        });
    }, 250);
}

// ----------------------------------------
// Animations
// ----------------------------------------
function initAnimations() {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.animationPlayState = 'running';
                observer.unobserve(entry.target);
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll('.animated-element').forEach(el => observer.observe(el));
}

// ----------------------------------------
// Success Sound
// ----------------------------------------
function playSuccessSound() {
    try {
        const audio = new Audio('https://assets.mixkit.co/sfx/preview/mixkit-winning-chimes-2015.mp3');
        audio.volume = 0.2;
        audio.play().catch(() => { });
    } catch { }
}

// ----------------------------------------
// Share URL
// ----------------------------------------
function setShareUrl() {
    const shareUrlInput = document.getElementById('shareUrl');
    if (shareUrlInput) {
        shareUrlInput.value = window.location.href;
    }
}

// ----------------------------------------
// 🔥 Helper: Find Ticket by Ticket Number
// ----------------------------------------
function findTicketCard(ticketNumber) {
    return Array.from(document.querySelectorAll('.ticket-card2'))
        .find(card => {
            const num = card.querySelector('.ticket-number');
            return num && num.textContent.trim() === ticketNumber;
        });
}

// ----------------------------------------
// Download Ticket
// ----------------------------------------
async function downloadTicket(ticketNumber) {
    const ticketCard = findTicketCard(ticketNumber);

    if (!ticketCard) {
        alert("Ticket not found!");
        return;
    }

    try {
        // Capture HTML card as image
        const canvas = await html2canvas(ticketCard, {
            scale: 2,
            useCORS: true
        });

        // Convert to image URL
        const dataUrl = canvas.toDataURL("image/png");

        // Trigger download
        const link = document.createElement("a");
        link.href = dataUrl;
        link.download = `${ticketNumber}.png`;
        document.body.appendChild(link);
        link.click();
        link.remove();

        showToast(`Ticket ${ticketNumber} downloaded successfully!`);

    } catch (error) {
        console.error("Failed to download ticket:", error);
        alert("Could not generate the ticket image.");
    }
}


// ----------------------------------------
// Share Ticket
// ----------------------------------------
async function shareTicket(ticketNumber) {
    const ticketCard = findTicketCard(ticketNumber);

    if (!ticketCard) {
        alert("Ticket not found!");
        return;
    }

    try {
        // Convert ticket card to PNG image
        const canvas = await html2canvas(ticketCard, {
            scale: 2,
            useCORS: true
        });

        const dataUrl = canvas.toDataURL("image/png");

        // Convert base64 → Blob
        const response = await fetch(dataUrl);
        const blob = await response.blob();

        const file = new File([blob], `${ticketNumber}.png`, { type: "image/png" });

        const shareData = {
            files: [file],
            title: "Event Ticket",
            text: `Here is my ticket (${ticketNumber}).`
        };

        if (navigator.canShare && navigator.canShare(shareData)) {
            await navigator.share(shareData);
        } else {
            // Fallback
            new bootstrap.Modal(document.getElementById("shareModal")).show();

            // Optionally auto-set the image inside modal
            const imgPreview = document.getElementById("sharePreviewImage");
            if (imgPreview) imgPreview.src = dataUrl;
        }

    } catch (error) {
        console.error("Sharing failed:", error);
        alert("Sharing not supported on this device.");
    }
}


// ----------------------------------------
// Print Ticket
// ----------------------------------------
function printTicket(ticketId) {
    const ticketElement = document.getElementById(ticketId);

    if (!ticketElement) {
        alert('Ticket not found!');
        return;
    }

    const printWindow = window.open('', '_blank');
    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>Print Ticket - ${ticketId}</title>
            <style>
                body { font-family: Arial; padding: 20px; }
                .print-ticket { max-width: 400px; margin: 0 auto; border: 2px solid #333; padding: 20px; text-align: center; }
                .ticket-number { font-size: 1.5rem; font-weight: bold; }
                .qr-code { margin: 20px auto; max-width: 200px; }
                @media print { body { margin: 0; } .print-ticket { border: none; } }
            </style>
        </head>
        <body>${ticketElement.outerHTML}</body>
        </html>
    `);

    printWindow.document.close();
    printWindow.focus();

    setTimeout(() => {
        printWindow.print();
        printWindow.close();
    }, 250);
}

// ----------------------------------------
// Print All Tickets
// ----------------------------------------
function printAllTickets() {
    const tickets = document.querySelectorAll('.ticket-card');
    if (tickets.length === 0) return alert('No tickets found!');

    const printContent = Array.from(tickets).map(t => t.outerHTML).join('');

    const printWindow = window.open('', '_blank');
    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>Print All Tickets</title>
            <style>
                body { font-family: Arial; padding: 20px; }
                .ticket-container { display: grid; grid-template-columns: repeat(2, 1fr); gap: 20px; }
                .ticket-card { border: 2px solid #333; padding: 15px; }
                @media print { body { margin: 0; } }
            </style>
        </head>
        <body>
            <h2 style="text-align:center">Event Tickets</h2>
            <div class="ticket-container">${printContent}</div>
        </body>
        </html>
    `);

    printWindow.document.close();
    printWindow.focus();

    setTimeout(() => {
        printWindow.print();
        printWindow.close();
    }, 250);
}

// ----------------------------------------
// Share All Tickets
// ----------------------------------------
function shareAllTickets() {
    const eventTitle = document.querySelector('.detail-card:first-child p.fs-5').textContent;
    const bookingId = document.querySelector('.booking-id span').textContent.replace('Booking ID: #', '');

    const shareData = {
        title: `My Event Booking - ${eventTitle}`,
        text: `I've booked tickets for ${eventTitle}! Booking ID: ${bookingId}`,
        url: window.location.href
    };

    if (navigator.share && navigator.canShare(shareData)) {
        navigator.share(shareData).catch(console.error);
    } else {
        const subject = `My Event Booking - ${eventTitle}`;
        const body = `Booking Details:\nEvent: ${eventTitle}\nBooking ID: ${bookingId}\n\nView booking: ${window.location.href}`;
        window.location.href = `mailto:?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
    }
}

// ----------------------------------------
// Share via WhatsApp
// ----------------------------------------
function shareViaWhatsApp() {
    const eventTitle = document.querySelector('.detail-card:first-child p.fs-5').textContent;
    const text = `Check out my booking for ${eventTitle}! ${window.location.href}`;
    window.open(`https://wa.me/?text=${encodeURIComponent(text)}`, '_blank');
}

// ----------------------------------------
// Email / SMS Share
// ----------------------------------------
function shareViaEmail() {
    const eventTitle = document.querySelector('.detail-card:first-child p.fs-5').textContent;
    window.location.href = `mailto:?subject=My Event Booking - ${eventTitle}&body=View details: ${window.location.href}`;
}

function shareViaSMS() {
    const eventTitle = document.querySelector('.detail-card:first-child p.fs-5').textContent;
    window.location.href = `sms:?body=${encodeURIComponent(`Check out my booking for ${eventTitle}! ${window.location.href}`)}`;
}

// ----------------------------------------
// Clipboard Copy
// ----------------------------------------
function copyToClipboard() {
    const shareUrl = document.getElementById('shareUrl');
    shareUrl.select();
    shareUrl.setSelectionRange(0, 99999);

    navigator.clipboard.writeText(shareUrl.value)
        .then(() => showToast('Link copied to clipboard!'));
}

function copyShareUrl() {
    copyToClipboard();
}

// ----------------------------------------
// Toast Notifications
// ----------------------------------------
function showToast(message) {
    const existingToast = document.querySelector('.custom-toast');
    if (existingToast) existingToast.remove();

    const toast = document.createElement('div');
    toast.className = 'custom-toast';
    toast.innerHTML = `
        <div class="toast-content">
            <i class="bi bi-check-circle"></i>
            <span>${message}</span>
        </div>
    `;

    document.body.appendChild(toast);

    setTimeout(() => toast.classList.add('show'), 10);

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Toast Styles
const toastStyles = document.createElement('style');
toastStyles.textContent = `
    .custom-toast {
        position: fixed;
        bottom: 30px;
        right: 30px;
        background: #2ecc71;
        color: white;
        padding: 15px 25px;
        border-radius: 12px;
        box-shadow: 0 10px 25px rgba(46,204,113,0.3);
        transform: translateY(100px);
        opacity: 0;
        transition: all 0.3s ease;
        z-index: 10000;
        display: flex;
        align-items: center;
        gap: 10px;
    }
    .custom-toast.show {
        transform: translateY(0);
        opacity: 1;
    }
`;
document.head.appendChild(toastStyles);
