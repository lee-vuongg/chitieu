// am-thanh.js: âm thanh ngắn tạo bằng Web Audio, không cần file âm thanh.
// iOS chỉ cho phát âm thanh sau một lần chạm của người dùng, nên file này mở khóa âm thanh ở lần chạm đầu tiên.
window.chiTieuAm = (() => {
    const KHOA_TAT = 'chitieu-am-tat';
    let ctx = null;

    function nguCanh() {
        if (!ctx) {
            const Lop = window.AudioContext || window.webkitAudioContext;
            if (!Lop) return null;
            ctx = new Lop();
        }
        if (ctx.state === 'suspended') ctx.resume();
        return ctx;
    }

    // Phát một đoạn im lặng rất ngắn ngay trong thao tác chạm để iOS cho phép âm thanh về sau
    function mokKhoa() {
        const c = nguCanh();
        if (!c) return;
        const dem = c.createBuffer(1, 1, 22050);
        const nguon = c.createBufferSource();
        nguon.buffer = dem;
        nguon.connect(c.destination);
        nguon.start(0);
    }

    // Chỉ cần mở khóa một lần; lần chạm đầu tiên trên trang là đủ
    ['pointerdown', 'touchend', 'click'].forEach(su => {
        document.addEventListener(su, mokKhoa, { once: true, passive: true });
    });

    function tatAm() {
        return localStorage.getItem(KHOA_TAT) === '1';
    }

    function datTatAm(tat) {
        localStorage.setItem(KHOA_TAT, tat ? '1' : '0');
    }

    function mot(c, tanSo, batDau, thoiLuong, kieuSong, am) {
        const dao = c.createOscillator();
        const gain = c.createGain();
        dao.type = kieuSong;
        dao.frequency.value = tanSo;
        gain.gain.setValueAtTime(0.0001, batDau);
        gain.gain.exponentialRampToValueAtTime(am, batDau + 0.012);
        gain.gain.exponentialRampToValueAtTime(0.0001, batDau + thoiLuong);
        dao.connect(gain);
        gain.connect(c.destination);
        dao.start(batDau);
        dao.stop(batDau + thoiLuong + 0.03);
    }

    // Mỗi phần tử: [tần số Hz, thời điểm bắt đầu (giây), kiểu sóng]
    const MAU = {
        luu: [[523.25, 0, 'sine'], [659.25, 0.11, 'sine']],
        xoa: [[392.0, 0, 'sine'], [293.66, 0.12, 'sine']],
        canhBao: [[880, 0, 'triangle'], [880, 0.2, 'triangle'], [880, 0.4, 'triangle']],
        mucTieu: [[523.25, 0, 'sine'], [659.25, 0.1, 'sine'], [783.99, 0.2, 'sine'], [1046.5, 0.32, 'sine']]
    };

    // kieu: 'luu' | 'xoa' | 'canhBao' | 'mucTieu'
    function phat(kieu) {
        if (tatAm()) return;
        const c = nguCanh();
        if (!c) return;
        const t0 = c.currentTime + 0.02;
        (MAU[kieu] || []).forEach(([tanSo, tre, kieuSong]) => {
            mot(c, tanSo, t0 + tre, 0.26, kieuSong, 0.14);
        });
    }

    return { phat, tatAm, datTatAm };
})();