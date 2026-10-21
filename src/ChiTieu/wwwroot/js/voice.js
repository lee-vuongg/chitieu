// voice.js: nhận giọng nói tiếng Việt bằng Web Speech API (Chrome, Edge, Safari iOS 14.5+).
// Phải gọi từ một thao tác bấm nút của người dùng, và trang phải chạy trên HTTPS hoặc localhost.
window.chiTieuVoice = (() => {
    const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
    let recog = null;

    function coHoTro() {
        return !!SR;
    }

    // dotnetRef: DotNetObjectReference của component Blazor
    // Gọi ngược lại: NhanKetQua(text, laKetQuaCuoi), NhanLoi(thongBao), KetThuc()
    function batDau(dotnetRef) {
        if (!SR) {
            dotnetRef.invokeMethodAsync('NhanLoi', 'Trình duyệt này không hỗ trợ nhận giọng nói.');
            return;
        }

        // Hủy phiên cũ (nếu còn) nhưng bỏ qua các sự kiện của nó
        if (recog) {
            const cu = recog;
            recog = null;
            try { cu.abort(); } catch { }
        }

        const phien = new SR();
        recog = phien;
        phien.lang = 'vi-VN';
        phien.interimResults = true;
        phien.continuous = false;
        phien.maxAlternatives = 1;

        phien.onresult = e => {
            if (recog !== phien) return;
            let text = '';
            let cuoi = false;
            for (const r of e.results) {
                text += r[0].transcript;
                if (r.isFinal) cuoi = true;
            }
            dotnetRef.invokeMethodAsync('NhanKetQua', text.trim(), cuoi);
        };

        phien.onerror = e => {
            // "aborted" là do chính app hoặc người dùng hủy, không phải lỗi
            if (e.error === 'aborted' || recog !== phien) return;
            const thongBao = e.error === 'not-allowed'
                ? 'Chưa cấp quyền micro cho trình duyệt.'
                : e.error === 'no-speech'
                    ? 'Không nghe thấy gì, thử lại nhé.'
                    : 'Lỗi nhận giọng nói: ' + e.error;
            dotnetRef.invokeMethodAsync('NhanLoi', thongBao);
        };

        phien.onend = () => {
            if (recog !== phien) return;
            recog = null;
            dotnetRef.invokeMethodAsync('KetThuc');
        };

        try {
            phien.start();
        } catch (err) {
            recog = null;
            dotnetRef.invokeMethodAsync('NhanLoi', 'Không bật được micro. Hãy thử lại.');
        }
    }

    function dung() {
        if (recog) recog.stop();
    }

    return { coHoTro, batDau, dung };
})();