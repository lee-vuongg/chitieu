// ho-so.js: hồ sơ hiển thị cho quản trị viên (tên bot cố định, tên thật và tỷ lệ chi tiêu khi đồng ý).
// Đặt trong src/ChiTieu/wwwroot/js/. Không gửi số tiền, ghi chú hay tên danh mục.
window.chiTieuHoSo = (() => {
    const KHOA = 'chitieu-ho-so';
    const KHOA_ID = 'chitieu-tk-id';
    // Thay TAI-KHOAN bằng tên tài khoản Cloudflare (giống analytics.js)
    const URL_HO_SO = 'https://chitieu-thongke.liguowang-apps.workers.dev/profile';
    const VERSION = '1.0';

    function doc() {
        try { return JSON.parse(localStorage.getItem(KHOA)) || {}; }
        catch { return {}; }
    }

    function ghi(o) {
        localStorage.setItem(KHOA, JSON.stringify(o));
    }

    function maId() {
        let id = localStorage.getItem(KHOA_ID);
        if (!id) {
            id = crypto.randomUUID ? crypto.randomUUID() : Date.now() + '-' + Math.random().toString(36).slice(2);
            localStorage.setItem(KHOA_ID, id);
        }
        return id;
    }

    // Tên bot cố định cho mỗi máy: Bot-7F3A
    function tenBot() {
        const h = doc();
        if (!h.bot) {
            h.bot = 'Bot-' + Math.random().toString(36).slice(2, 6).toUpperCase();
            ghi(h);
        }
        return h.bot;
    }

    // Máy đang dùng, ví dụ: "iPhone · iOS 18.2" hoặc "Android 14" hoặc "Máy tính"
    function may() {
        const ua = navigator.userAgent || '';
        if (/iPhone|iPad|iPod/.test(ua)) {
            const m = ua.match(/OS (\d+)[_.](\d+)/);
            return 'iPhone' + (m ? ' · iOS ' + m[1] + '.' + m[2] : '');
        }
        if (/Android/.test(ua)) {
            const m = ua.match(/Android (\d+(?:\.\d+)?)/);
            return 'Android' + (m ? ' ' + m[1] : '');
        }
        return 'Máy tính';
    }

    function thongTin() {
        const h = doc();
        return { ten: h.ten || null, dongYTen: !!h.dongYTen, dongYTyLe: !!h.dongYTyLe, bot: tenBot() };
    }

    function luu(t) {
        const h = doc();
        h.dongYTen = !!t.dongYTen;
        h.ten = h.dongYTen ? String(t.ten || '').trim().slice(0, 40) || null : null;
        h.dongYTyLe = !!t.dongYTyLe;
        ghi(h);
    }

    // tyLe: [{ danhMuc, phanTram }] hoặc null. Chỉ gửi khi người dùng đồng ý riêng.
    async function dongBo(tyLe) {
        const h = doc();
        const body = {
            id: maId(),
            bot: tenBot(),
            dongYTen: !!h.dongYTen,
            ten: h.dongYTen ? h.ten || null : null,
            may: may(),
            dongYTyLe: !!h.dongYTyLe,
            tyLe: h.dongYTyLe && Array.isArray(tyLe) ? tyLe : null,
            v: VERSION
        };
        try {
            await fetch(URL_HO_SO, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
                keepalive: true
            });
        } catch { /* mất mạng thì lần mở sau sẽ đồng bộ lại */ }
    }

    return { thongTin, luu, dongBo };
})();
