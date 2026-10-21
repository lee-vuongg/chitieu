// cap-nhat.js: hiện banner "Có phiên bản mới" và chuyển sang bản mới khi bấm Cập nhật.
(function () {
    if (!('serviceWorker' in navigator)) return;

    let daTaiLai = false;

    function taoBanner() {
        let banner = document.getElementById('banner-cap-nhat');
        if (banner) return banner;
        banner = document.createElement('div');
        banner.id = 'banner-cap-nhat';
        banner.style.display = 'none';
        banner.innerHTML = '<span>Có phiên bản mới</span><button type="button">Cập nhật</button>';
        document.body.appendChild(banner);
        banner.querySelector('button').addEventListener('click', kichHoat);
        return banner;
    }

    function hienBanner() {
        taoBanner().style.display = 'flex';
    }

    async function kichHoat() {
        const reg = await navigator.serviceWorker.getRegistration();
        if (!reg || !reg.waiting) {
            location.reload();
            return;
        }

        const cho = reg.waiting;
        cho.addEventListener('statechange', () => {
            if (cho.state === 'activated') location.reload();
        });
        cho.postMessage('SKIP_WAITING');

        // Dự phòng: nếu trình duyệt không báo, vẫn tải lại sau 3 giây
        setTimeout(() => location.reload(), 3000);
    }

    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (daTaiLai) return;
        daTaiLai = true;
        location.reload();
    });

    async function theoDoi() {
        const reg = await navigator.serviceWorker.getRegistration();
        if (!reg) return;

        if (reg.waiting) hienBanner();

        reg.addEventListener('updatefound', () => {
            const moi = reg.installing;
            if (!moi) return;
            moi.addEventListener('statechange', () => {
                if (moi.state === 'installed' && navigator.serviceWorker.controller) hienBanner();
            });
        });

        // Kiểm tra bản mới mỗi giờ khi app đang mở
        setInterval(() => reg.update(), 60 * 60 * 1000);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', theoDoi);
    } else {
        theoDoi();
    }
})();