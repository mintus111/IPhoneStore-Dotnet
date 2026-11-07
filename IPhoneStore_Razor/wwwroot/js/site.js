// Apple iStore JavaScript Helpers

function showToast(message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = 'apple-toast';
    
    let iconSvg = '<svg width="20" height="20" fill="none" stroke="#2997ff" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>';
    if (type === 'success') {
        iconSvg = '<svg width="20" height="20" fill="none" stroke="#30d158" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>';
    } else if (type === 'error') {
        iconSvg = '<svg width="20" height="20" fill="none" stroke="#ff453a" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>';
    }

    toast.innerHTML = `${iconSvg} <span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.transition = 'all 0.4s ease';
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(10px)';
        setTimeout(() => toast.remove(), 400);
    }, 3200);
}

// Thêm nhanh vào giỏ hàng qua AJAX (dùng cho Razor & MVC)
function addToCartAjax(url, productId, token) {
    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('quantity', 1);

    const headers = {};
    if (token) {
        headers['RequestVerificationToken'] = token;
    }

    fetch(url, {
        method: 'POST',
        headers: headers,
        body: formData
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            showToast(data.message || 'Đã thêm iPhone vào giỏ hàng!', 'success');
            const badge = document.getElementById('cart-count-badge');
            if (badge) {
                badge.innerText = data.cartCount;
                badge.classList.remove('d-none');
            }
        } else {
            showToast(data.message || 'Có lỗi xảy ra!', 'error');
        }
    })
    .catch(err => {
        console.error(err);
        showToast('Không thể kết nối đến máy chủ!', 'error');
    });
}

function updateCartBadge(count) {
    const badge = document.getElementById('cart-count-badge');
    if (badge) {
        badge.innerText = count;
        badge.classList.toggle('d-none', !(count > 0));
    }
}
