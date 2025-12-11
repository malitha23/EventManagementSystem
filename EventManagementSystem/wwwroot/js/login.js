/**
 * Event Management System - Login Page JavaScript
 * File: /wwwroot/js/login.js
 */

// Wait for DOM to be fully loaded
document.addEventListener('DOMContentLoaded', function () {
    initializeLoginForm();
    setupEventListeners();
    checkServerMessages();
});

/**
 * Initialize login form functionality
 */
function initializeLoginForm() {
    const loginForm = document.getElementById('loginForm');
    const loginButton = document.getElementById('loginButton');
    const emailInput = document.getElementById('Email');
    const passwordInput = document.getElementById('Password');
    const alertDiv = document.getElementById('loginAlert');

    if (!loginForm || !loginButton) return;

    // Add input event listeners for real-time validation
    if (emailInput) {
        emailInput.addEventListener('input', function () {
            validateEmailField(this);
        });

        emailInput.addEventListener('blur', function () {
            validateEmailField(this);
        });
    }

    if (passwordInput) {
        passwordInput.addEventListener('input', function () {
            validatePasswordField(this);
        });

        passwordInput.addEventListener('blur', function () {
            validatePasswordField(this);
        });
    }
}

/**
 * Setup all event listeners
 */
function setupEventListeners() {
    // Login form submission
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handleLoginSubmit);
    }

    // Forgot password link
    const forgotPasswordLink = document.getElementById('forgotPasswordLink');
    if (forgotPasswordLink) {
        forgotPasswordLink.addEventListener('click', handleForgotPassword);
    }

    // Social login buttons
    const socialButtons = document.querySelectorAll('.social-button');
    socialButtons.forEach(button => {
        button.addEventListener('click', handleSocialLogin);
    });

    // "Remember me" checkbox tooltip
    const rememberMeCheckbox = document.getElementById('rememberMe');
    if (rememberMeCheckbox) {
        rememberMeCheckbox.addEventListener('mouseenter', function () {
            showTooltip(this, 'Keep me signed in for 30 days');
        });

        rememberMeCheckbox.addEventListener('mouseleave', function () {
            hideTooltip();
        });
    }

    // Auto-focus email field if empty
    const emailInput = document.getElementById('Email');
    if (emailInput && !emailInput.value.trim()) {
        setTimeout(() => {
            emailInput.focus();
        }, 300);
    }

    // Enter key to submit form
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.target.matches('.social-button')) {
            const activeElement = document.activeElement;
            if (activeElement &&
                (activeElement.matches('input') || activeElement.matches('button[type="submit"]'))) {
                const loginForm = document.getElementById('loginForm');
                if (loginForm && loginForm.checkValidity()) {
                    e.preventDefault();
                    loginForm.dispatchEvent(new Event('submit'));
                }
            }
        }
    });
}

/**
 * Handle login form submission
 */
async function handleLoginSubmit(e) {
    e.preventDefault();

    const form = e.target;
    const loginButton = document.getElementById('loginButton');
    const emailInput = document.getElementById('Email');
    const passwordInput = document.getElementById('Password');
    const alertDiv = document.getElementById('loginAlert');

    // Clear previous alerts
    clearAlert();

    // Validate form
    if (!validateForm()) {
        return false;
    }

    // Show loading state
    setLoadingState(true);

    try {
        // Prepare form data
        const formData = new FormData(form);

        // Add anti-forgery token if not already present
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            formData.append('__RequestVerificationToken', token.value);
        }

        // Submit form programmatically
        const response = await fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        // Check response
        if (response.redirected) {
            // Server redirected, follow the redirect
            window.location.href = response.url;
            return;
        }

        const result = await response.json();

        if (result.success) {
            // Login successful
            showAlert('Login successful! Redirecting...', 'success');

            // Redirect after delay
            setTimeout(() => {
                window.location.href = result.redirectUrl || '/';
            }, 1500);
        } else {
            // Login failed
            showAlert(result.message || 'Invalid email or password', 'error');

            // Shake animation for wrong credentials
            form.classList.add('shake');
            setTimeout(() => {
                form.classList.remove('shake');
            }, 500);

            // Clear password field
            if (passwordInput) {
                passwordInput.value = '';
                passwordInput.focus();
            }

            setLoadingState(false);
        }

    } catch (error) {
        console.error('Login error:', error);
        showAlert('Network error. Please try again.', 'error');
        setLoadingState(false);
    }

    return false;
}

/**
 * Handle forgot password click
 */
function handleForgotPassword(e) {
    e.preventDefault();

    Swal.fire({
        title: 'Forgot Password?',
        html: `
            <div class="forgot-password-modal">
                <p>Enter your email address and we'll send you a link to reset your password.</p>
                <div class="mb-3">
                    <input type="email" id="forgotPasswordEmail" 
                           class="form-control" 
                           placeholder="Enter your email address"
                           autocomplete="email">
                    <div class="invalid-feedback mt-1" id="forgotPasswordError"></div>
                </div>
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: 'Send Reset Link',
        cancelButtonText: 'Cancel',
        confirmButtonColor: '#3498db',
        showLoaderOnConfirm: true,
        preConfirm: async () => {
            const emailInput = document.getElementById('forgotPasswordEmail');
            const errorDiv = document.getElementById('forgotPasswordError');

            if (!emailInput || !errorDiv) return;

            // Validate email
            const email = emailInput.value.trim();

            if (!email) {
                errorDiv.textContent = 'Please enter your email address';
                emailInput.classList.add('is-invalid');
                return false;
            }

            if (!isValidEmail(email)) {
                errorDiv.textContent = 'Please enter a valid email address';
                emailInput.classList.add('is-invalid');
                return false;
            }

            // Clear any previous errors
            errorDiv.textContent = '';
            emailInput.classList.remove('is-invalid');

            // Simulate API call (replace with actual API call)
            try {
                // In production, replace this with actual API call
                // const response = await fetch('/api/auth/forgot-password', {
                //     method: 'POST',
                //     headers: { 'Content-Type': 'application/json' },
                //     body: JSON.stringify({ email: email })
                // });
                // const result = await response.json();

                // Simulate API delay
                await new Promise(resolve => setTimeout(resolve, 1500));

                // Simulated success response
                return { success: true, email: email };

            } catch (error) {
                console.error('Forgot password error:', error);
                Swal.showValidationMessage('Network error. Please try again.');
                return false;
            }
        },
        allowOutsideClick: () => !Swal.isLoading()
    }).then((result) => {
        if (result.isConfirmed && result.value && result.value.success) {
            Swal.fire({
                icon: 'success',
                title: 'Check Your Email!',
                html: `
                    <div class="success-message">
                        <p>We've sent a password reset link to:</p>
                        <p class="email-address">${result.value.email}</p>
                        <p class="text-muted small mt-3">
                            <i class="fas fa-info-circle me-1"></i>
                            The link will expire in 1 hour.
                        </p>
                    </div>
                `,
                confirmButtonText: 'OK',
                confirmButtonColor: '#2ecc71'
            });
        }
    }).then(() => {
        // Focus back on email field in login form
        const emailInput = document.getElementById('Email');
        if (emailInput) {
            emailInput.focus();
        }
    });
}

/**
 * Handle social login button click
 */
function handleSocialLogin(e) {
    const platform = this.getAttribute('title') || this.querySelector('i').className.split('fa-')[1] || 'social';

    Swal.fire({
        icon: 'info',
        title: `${platform.charAt(0).toUpperCase() + platform.slice(1)} Login`,
        html: `
            <div class="social-login-info">
                <div class="mb-3">
                    <i class="fab fa-${platform} fa-3x text-primary mb-3"></i>
                </div>
                <p>This feature is coming soon!</p>
                <p class="text-muted small">
                    We're working on integrating ${platform} login. 
                    For now, please use email and password to sign in.
                </p>
            </div>
        `,
        confirmButtonText: 'Continue with Email',
        confirmButtonColor: '#3498db',
        showCancelButton: true,
        cancelButtonText: 'Close'
    }).then((result) => {
        if (result.isConfirmed) {
            const emailInput = document.getElementById('Email');
            if (emailInput) {
                emailInput.focus();
            }
        }
    });
}

/**
 * Validate the entire form
 */
function validateForm() {
    const emailInput = document.getElementById('Email');
    const passwordInput = document.getElementById('Password');
    let isValid = true;

    // Validate email
    if (!validateEmailField(emailInput)) {
        isValid = false;
    }

    // Validate password
    if (!validatePasswordField(passwordInput)) {
        isValid = false;
    }

    return isValid;
}

/**
 * Validate email field
 */
function validateEmailField(input) {
    if (!input) return false;

    const value = input.value.trim();
    const errorMessage = document.getElementById('emailError') ||
        input.nextElementSibling?.classList?.contains('invalid-feedback') ?
        input.nextElementSibling : null;

    let isValid = true;
    let message = '';

    if (!value) {
        message = 'Email address is required';
        isValid = false;
    } else if (!isValidEmail(value)) {
        message = 'Please enter a valid email address';
        isValid = false;
    }

    // Update UI
    if (isValid) {
        input.classList.remove('is-invalid');
        input.classList.add('is-valid');
    } else {
        input.classList.remove('is-valid');
        input.classList.add('is-invalid');
    }

    // Update error message
    if (errorMessage) {
        errorMessage.textContent = message;
        errorMessage.style.display = isValid ? 'none' : 'block';
    }

    return isValid;
}

/**
 * Validate password field
 */
function validatePasswordField(input) {
    if (!input) return false;

    const value = input.value;
    const errorMessage = document.getElementById('passwordError') ||
        input.nextElementSibling?.classList?.contains('invalid-feedback') ?
        input.nextElementSibling : null;

    let isValid = true;
    let message = '';

    if (!value) {
        message = 'Password is required';
        isValid = false;
    } else if (value.length < 6) {
        message = 'Password must be at least 6 characters';
        isValid = false;
    }

    // Update UI
    if (isValid) {
        input.classList.remove('is-invalid');
        input.classList.add('is-valid');
    } else {
        input.classList.remove('is-valid');
        input.classList.add('is-invalid');
    }

    // Update error message
    if (errorMessage) {
        errorMessage.textContent = message;
        errorMessage.style.display = isValid ? 'none' : 'block';
    }

    return isValid;
}

/**
 * Check email validity
 */
function isValidEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

/**
 * Show alert message
 */
function showAlert(message, type = 'error') {
    const alertDiv = document.getElementById('loginAlert');
    if (!alertDiv) return;

    // Clear previous content
    alertDiv.innerHTML = '';

    // Create message content
    let icon = '';
    switch (type) {
        case 'success':
            icon = '<i class="fas fa-check-circle me-2"></i>';
            break;
        case 'warning':
            icon = '<i class="fas fa-exclamation-triangle me-2"></i>';
            break;
        case 'error':
            icon = '<i class="fas fa-exclamation-circle me-2"></i>';
            break;
        default:
            icon = '<i class="fas fa-info-circle me-2"></i>';
    }

    alertDiv.innerHTML = `
        <div class="d-flex align-items-center">
            ${icon}
            <span>${message}</span>
            <button type="button" class="btn-close ms-auto" onclick="clearAlert()"></button>
        </div>
    `;

    // Set alert type
    alertDiv.className = `login-alert ${type} show`;

    // Auto-hide after 5 seconds for success messages
    if (type === 'success') {
        setTimeout(clearAlert, 5000);
    }

    // Scroll to alert
    alertDiv.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

/**
 * Clear alert message
 */
function clearAlert() {
    const alertDiv = document.getElementById('loginAlert');
    if (alertDiv) {
        alertDiv.classList.remove('show', 'success', 'error', 'warning');
        setTimeout(() => {
            alertDiv.innerHTML = '';
        }, 300);
    }
}

/**
 * Set loading state for login button
 */
function setLoadingState(isLoading) {
    const loginButton = document.getElementById('loginButton');
    const buttonText = loginButton?.querySelector('.button-text');
    const spinner = loginButton?.querySelector('.login-button-spinner');

    if (!loginButton || !buttonText || !spinner) return;

    if (isLoading) {
        loginButton.classList.add('loading');
        loginButton.disabled = true;
        buttonText.style.opacity = '0.7';
        spinner.style.display = 'block';
    } else {
        loginButton.classList.remove('loading');
        loginButton.disabled = false;
        buttonText.style.opacity = '1';
        spinner.style.display = 'none';
    }
}

/**
 * Show tooltip
 */
function showTooltip(element, text) {
    // Remove existing tooltip
    hideTooltip();

    // Create tooltip
    const tooltip = document.createElement('div');
    tooltip.className = 'login-tooltip';
    tooltip.textContent = text;

    // Position tooltip
    const rect = element.getBoundingClientRect();
    tooltip.style.position = 'fixed';
    tooltip.style.top = `${rect.top - 40}px`;
    tooltip.style.left = `${rect.left + rect.width / 2}px`;
    tooltip.style.transform = 'translateX(-50%)';

    // Add to DOM
    document.body.appendChild(tooltip);

    // Store reference
    window.currentTooltip = tooltip;
}

/**
 * Hide tooltip
 */
function hideTooltip() {
    if (window.currentTooltip) {
        window.currentTooltip.remove();
        window.currentTooltip = null;
    }
}

/**
 * Check for server messages and display them
 */
function checkServerMessages() {
    // Check for model state errors
    @if (!ViewData.ModelState.IsValid) {
        <text>
            const errors = [];
        @foreach (var error in ViewData.ModelState.Values.SelectMany(v => v.Errors))
            {
                <text>errors.push('@Html.Raw(error.ErrorMessage.Replace("'", "\\'"))');</text>
            }
        
        if (errors.length > 0) {
                showAlert(errors.join('<br>'), 'error');
        }
        </text>
    }

    // Check for success messages
    @if (TempData["SuccessMessage"] != null) {
        <text>
        setTimeout(() => {
                showAlert('@TempData["SuccessMessage"]', 'success');
        }, 500);
        </text>
    }

    // Check for error messages
    @if (TempData["ErrorMessage"] != null) {
        <text>
        setTimeout(() => {
                showAlert('@TempData["ErrorMessage"]', 'error');
        }, 500);
        </text>
    }
}

// Make functions available globally
window.showAlert = showAlert;
window.clearAlert = clearAlert;
window.validateEmailField = validateEmailField;
window.validatePasswordField = validatePasswordField;