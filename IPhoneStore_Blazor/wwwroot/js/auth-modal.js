// =========================================================================
// KIỂM TRA TRÙNG LẶP THỜI GIAN THỰC (REAL-TIME AVAILABILITY CHECK)
// ĐỒNG BỘ POP-UP MODAL VỚI TRANG ĐĂNG KÝ CHUẨN (ẢNH 1 & MỤC 2.4.1.3)
// =========================================================================
(function () {
    var checkTimers = {};
    var checkStatus = {
        phone: null,
        username: null,
        email: null
    };

    function setFieldState(input, feedback, state, message) {
        if (!input || !feedback) return;

        input.classList.remove('modal-check-invalid', 'modal-check-valid', 'is-invalid', 'is-valid');
        feedback.className = 'form-text mt-1';
        feedback.style.fontSize = '0.75rem';
        feedback.style.lineHeight = '1.3';

        if (state === 'error') {
            input.classList.add('modal-check-invalid', 'is-invalid');
            feedback.className = 'form-text mt-1 text-danger fw-semibold modal-feedback-error';
            feedback.innerHTML = '✕ ' + message;
        } else if (state === 'valid') {
            input.classList.add('modal-check-valid', 'is-valid');
            feedback.className = 'form-text mt-1 text-success fw-semibold modal-feedback-success';
            feedback.innerHTML = '✓ ' + message;
        } else if (state === 'loading') {
            feedback.className = 'form-text mt-1 text-muted';
            feedback.innerHTML = '<span class="spinner-border spinner-border-sm me-1" style="width:0.75rem;height:0.75rem;"></span>Đang kiểm tra...';
        } else {
            feedback.innerHTML = '';
        }
    }

    function doCheck(type, input, feedback, isBlur) {
        if (!input || !feedback) return;
        var val = (input.value || '').trim();

        if (val.length === 0) {
            setFieldState(input, feedback, 'empty');
            checkStatus[type] = (type === 'username' || type === 'email') ? true : null;
            return;
        }

        // Với email: khi đang gõ, nếu chưa có ký tự @ hoặc dấu chấm thì tạm không gọi API
        if (!isBlur && type === 'email') {
            if (val.indexOf('@') <= 0 || val.indexOf('.') === -1) {
                setFieldState(input, feedback, 'empty');
                return;
            }
        }

        setFieldState(input, feedback, 'loading');

        fetch('/Account/CheckAvailability?type=' + encodeURIComponent(type) + '&value=' + encodeURIComponent(val))
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (data.available) {
                    setFieldState(input, feedback, 'valid', data.message || 'Khả dụng.');
                    checkStatus[type] = true;
                } else {
                    var msg = data.message || 'Không khả dụng!';
                    if (data.exists && type === 'phone') {
                        msg += ' <a href="javascript:void(0);" onclick="document.getElementById(\'login-tab\')?.click();" class="text-primary text-decoration-underline ms-1 fw-bold">Đăng nhập ngay &rarr;</a>';
                    }
                    setFieldState(input, feedback, 'error', msg);
                    checkStatus[type] = false;
                }
            })
            .catch(function () {
                setFieldState(input, feedback, 'empty');
            });
    }

    function scheduleCheck(type, input, feedback, delay) {
        if (checkTimers[type]) {
            clearTimeout(checkTimers[type]);
        }
        checkTimers[type] = setTimeout(function () {
            doCheck(type, input, feedback, false);
        }, delay || 250);
    }

    // 1. EVENT DELEGATION: Bắt sự kiện 'input' trên toàn document (không sợ Blazor re-render)
    document.addEventListener('input', function (e) {
        var target = e.target;
        if (!target) return;

        if (target.id === 'modalBlazorUser') {
            var fb = document.getElementById('modalBlazorUserFeedback');
            scheduleCheck('username', target, fb, 250);
        } else if (target.id === 'modalBlazorPhone') {
            var fb = document.getElementById('modalBlazorPhoneFeedback');
            scheduleCheck('phone', target, fb, 250);
        } else if (target.id === 'modalBlazorEmail') {
            var fb = document.getElementById('modalBlazorEmailFeedback');
            scheduleCheck('email', target, fb, 300);
        }
    });

    // 2. EVENT DELEGATION: Bắt sự kiện 'focusout' (onBlur) để kiểm tra tức thì khi rời khỏi ô nhập
    document.addEventListener('focusout', function (e) {
        var target = e.target;
        if (!target) return;

        if (target.id === 'modalBlazorUser') {
            var fb = document.getElementById('modalBlazorUserFeedback');
            if (checkTimers['username']) clearTimeout(checkTimers['username']);
            doCheck('username', target, fb, true);
        } else if (target.id === 'modalBlazorPhone') {
            var fb = document.getElementById('modalBlazorPhoneFeedback');
            if (checkTimers['phone']) clearTimeout(checkTimers['phone']);
            doCheck('phone', target, fb, true);
        } else if (target.id === 'modalBlazorEmail') {
            var fb = document.getElementById('modalBlazorEmailFeedback');
            if (checkTimers['email']) clearTimeout(checkTimers['email']);
            doCheck('email', target, fb, true);
        }
    });

    // 3. SUBMIT GUARD & AJAX FORM SUBMISSION
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || form.id !== 'modalBlazorRegisterForm') return;

        var phoneInput = document.getElementById('modalBlazorPhone');
        var userInput = document.getElementById('modalBlazorUser');
        var emailInput = document.getElementById('modalBlazorEmail');
        var regAlert = document.getElementById('modalBlazorRegAlert');
        var btn = document.getElementById('modalBlazorRegBtn');

        // Chặn submit nếu có trường trùng lặp
        if (checkStatus.phone === false) {
            e.preventDefault();
            if (phoneInput) {
                phoneInput.focus();
                phoneInput.classList.add('modal-check-invalid', 'is-invalid');
            }
            return;
        }

        if (checkStatus.username === false) {
            e.preventDefault();
            if (userInput) {
                userInput.focus();
                userInput.classList.add('modal-check-invalid', 'is-invalid');
            }
            return;
        }

        if (checkStatus.email === false) {
            e.preventDefault();
            if (emailInput) {
                emailInput.focus();
                emailInput.classList.add('modal-check-invalid', 'is-invalid');
            }
            return;
        }

        // Xử lý gửi AJAX mượt mà không tải lại trang khi có lỗi
        e.preventDefault();

        if (btn) {
            btn.disabled = true;
            btn.innerText = 'Đang đăng ký...';
        }
        if (regAlert) {
            regAlert.classList.add('d-none');
            regAlert.innerText = '';
        }

        var formData = new FormData(form);

        fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            }
        })
        .then(function (res) {
            var contentType = res.headers.get('content-type') || '';
            if (contentType.indexOf('application/json') !== -1) {
                return res.json();
            }
            if (res.redirected) {
                window.location.href = res.url;
                return null;
            }
            return res.text().then(function (text) {
                try {
                    return JSON.parse(text);
                } catch (e) {
                    window.location.reload();
                    return null;
                }
            });
        })
        .then(function (data) {
            if (!data) return;
            if (data.success) {
                window.location.href = data.redirectUrl || '/';
            } else {
                if (regAlert) {
                    regAlert.innerText = data.message || 'Đăng ký không thành công. Vui lòng kiểm tra lại!';
                    regAlert.classList.remove('d-none');
                } else {
                    alert(data.message || 'Đăng ký không thành công!');
                }
            }
        })
        .catch(function (err) {
            if (regAlert) {
                regAlert.innerText = 'Đã xảy ra lỗi kết nối. Vui lòng thử lại!';
                regAlert.classList.remove('d-none');
            }
        })
        .finally(function () {
            if (btn) {
                btn.disabled = false;
                btn.innerText = 'Đăng Ký Tài Khoản BomChelsea';
            }
        });
    });

    window.initAuthModalCheck = function () {
        // Ready
    };
})();

