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
function addToCartAjax(urlOrProductId, productIdOrElem, token) {
    let url = '/Home/AddToCart';
    let productId = urlOrProductId;
    let buttonElem = null;

    // Phân loại tham số linh hoạt
    if (typeof urlOrProductId === 'string' && isNaN(Number(urlOrProductId))) {
        // Gọi theo kiểu: addToCartAjax(url, productId, token)
        url = urlOrProductId;
        productId = productIdOrElem;
    } else {
        // Gọi theo kiểu: addToCartAjax(productId, this)
        productId = urlOrProductId;
        if (productIdOrElem && productIdOrElem.nodeType) {
            buttonElem = productIdOrElem;
        }
    }

    // Tự động tìm Antiforgery Token nếu chưa được truyền
    if (!token) {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput) {
            token = tokenInput.value;
        }
    }

    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('quantity', 1);
    if (token) {
        formData.append('__RequestVerificationToken', token);
    }

    const headers = {};
    if (token) {
        headers['RequestVerificationToken'] = token;
    }

    let originalHtml = '';
    if (buttonElem) {
        buttonElem.disabled = true;
        originalHtml = buttonElem.innerHTML;
        buttonElem.innerHTML = 'Đang thêm...';
    }

    fetch(url, {
        method: 'POST',
        headers: headers,
        body: formData
    })
    .then(async res => {
        if (!res.ok) {
            const errText = await res.text().catch(() => '');
            console.error('Fetch error:', res.status, errText);
            throw new Error(`Server returned HTTP ${res.status}`);
        }
        return res.json();
    })
    .then(data => {
        const isSuccess = data.success ?? data.Success;
        const msg = data.message ?? data.Message;
        const count = data.cartCount ?? data.CartCount;

        if (isSuccess) {
            showToast(msg || 'Đã thêm iPhone vào giỏ hàng!', 'success');
            if (count !== undefined) {
                updateCartBadge(count);
            }
        } else {
            showToast(msg || 'Có lỗi xảy ra!', 'error');
        }
    })
    .catch(err => {
        console.error(err);
        showToast('Không thể kết nối đến máy chủ!', 'error');
    })
    .finally(() => {
        if (buttonElem) {
            buttonElem.disabled = false;
            buttonElem.innerHTML = originalHtml;
        }
    });
}

function updateCartBadge(count) {
    const badge = document.getElementById('cart-count-badge');
    if (badge) {
        badge.innerText = count;
        badge.classList.toggle('d-none', !(count > 0));
    }
}
