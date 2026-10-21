// push.js: bật/tắt nhắc chi tiêu hằng ngày. Đặt trong src/ChiTieu/wwwroot/js/
window.chiTieuPush = (() => {
    // Thay bằng VAPID_PUBLIC đã tạo ở bước sinh khóa (phải khớp với Worker)
    const VAPID_PUBLIC = 'BBObXAWSK7WQq3lHNtn64lNs7v5pmiwQEJvc-mNzT-5fAyq3ON_t_BymOfBV_FMQO0RO3F-MOjXp3S1f7F2t86s';
    // Thay bằng địa chỉ Worker sau khi deploy
  const WORKER = 'https://chitieu-nhac.liguowang-apps.workers.dev';

    function coHoTro() {
        return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
    }

    // Chuyển khóa base64url sang Uint8Array cho applicationServerKey
    function doiKhoa(b64u) {
        const pad = '='.repeat((4 - (b64u.length % 4)) % 4);
        const raw = atob((b64u + pad).replace(/-/g, '+').replace(/_/g, '/'));
        return Uint8Array.from(raw, c => c.charCodeAt(0));
    }

    async function dang() {
        if (!coHoTro()) return false;
        const reg = await navigator.serviceWorker.ready;
        return !!(await reg.pushManager.getSubscription());
    }

    // Phải được gọi từ thao tác bấm của người dùng (iOS yêu cầu điều này)
    async function bat() {
        if (!coHoTro()) {
            return { ok: false, loi: 'Thiết bị này chưa hỗ trợ thông báo. Trên iPhone, hãy thêm app vào Màn hình chính rồi mở từ biểu tượng.' };
        }
        const quyen = await Notification.requestPermission();
        if (quyen !== 'granted') {
            return { ok: false, loi: 'Chưa được cấp quyền thông báo. Hãy bật lại trong Cài đặt của điện thoại.' };
        }

        const reg = await navigator.serviceWorker.ready;
        let sub = await reg.pushManager.getSubscription();
        if (!sub) {
            sub = await reg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: doiKhoa(VAPID_PUBLIC)
            });
        }

        const res = await fetch(WORKER + '/subscribe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ endpoint: sub.endpoint })
        });
        if (!res.ok) {
            return { ok: false, loi: 'Không lưu được đăng ký nhắc. Hãy thử lại sau.' };
        }
        return { ok: true, loi: null };
    }

    async function tat() {
        if (!coHoTro()) return true;
        const reg = await navigator.serviceWorker.ready;
        const sub = await reg.pushManager.getSubscription();
        if (!sub) return true;
        try {
            await fetch(WORKER + '/unsubscribe', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ endpoint: sub.endpoint })
            });
        } catch { /* vẫn hủy đăng ký trên máy */ }
        await sub.unsubscribe();
        return true;
    }

    return { coHoTro, dang, bat, tat };
})();
