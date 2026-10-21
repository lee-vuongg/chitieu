// analytics.js: gửi sự kiện ẩn danh để đếm lượt dùng tính năng. Đặt trong src/ChiTieu/wwwroot/js/
// Không gửi số tiền, ghi chú, danh mục. Chỉ gửi tên sự kiện, nền tảng và mã thiết bị ngẫu nhiên.
window.chiTieuTK = (() => {
    // Thay bằng địa chỉ Worker thống kê sau khi deploy
   const URL_THU = 'https://chitieu-thongke.liguowang-apps.workers.dev/event';
    const VERSION = '1.0';
    const KHOA_ID = 'chitieu-tk-id';
    const KHOA_TAT = 'chitieu-tk-tat';

    function maThietBi() {
        let id = localStorage.getItem(KHOA_ID);
        if (!id) {
            id = (crypto.randomUUID ? crypto.randomUUID() : Date.now() + '-' + Math.random().toString(36).slice(2));
            localStorage.setItem(KHOA_ID, id);
        }
        return id;
    }

    function nenTang() {
        const ua = navigator.userAgent || '';
        if (/iPhone|iPad|iPod/.test(ua)) return 'ios';
        if (/Android/.test(ua)) return 'android';
        return 'web';
    }

    function dangTat() {
        return localStorage.getItem(KHOA_TAT) === '1';
    }

    function tat(bat) {
        localStorage.setItem(KHOA_TAT, bat ? '1' : '0');
    }

    // Gửi không chặn giao diện. keepalive giúp gửi được cả khi người dùng vừa rời app.
    function ghi(ev) {
        if (dangTat()) return;
        try {
            fetch(URL_THU, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ ev, p: nenTang(), v: VERSION, id: maThietBi() }),
                keepalive: true
            }).catch(() => { /* mất mạng thì bỏ qua, không ảnh hưởng app */ });
        } catch { /* bỏ qua */ }
    }

    return { ghi, tat, dangTat };
})();
