// Các hàm tiện ích nhỏ cho Blazor gọi qua JS interop.
(function () {
  const giamChuyenDong = () => window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  function dinhDangSo(n) {
    return Math.abs(Math.round(n)).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  }

  const MAU_NEN = { sang: '#F4F0E6', toi: '#12161B' };

  window.chitieuUtil = {
    // Độ lệch múi giờ (phút) của trình duyệt, giống Date.getTimezoneOffset(): UTC+7 -> -420
    lechMuiGio: () => new Date().getTimezoneOffset(),

    coMang: () => navigator.onLine !== false,

    // Rung nhẹ khi bấm nút. iOS Safari không hỗ trợ thì bỏ qua.
    rung: (ms) => { try { if (navigator.vibrate) navigator.vibrate(ms); } catch (e) { } },

    heThongToi: () => window.matchMedia('(prefers-color-scheme: dark)').matches,

    theoDoiHeThong: (ref) => {
      const mq = window.matchMedia('(prefers-color-scheme: dark)');
      const fn = (e) => ref.invokeMethodAsync('HeThongDoi', e.matches).catch(() => { });
      if (mq.addEventListener) mq.addEventListener('change', fn); else if (mq.addListener) mq.addListener(fn);
    },

    datChuDe: (toi) => {
      document.documentElement.dataset.theme = toi ? 'dark' : 'light';
      const meta = document.querySelector('meta[name="theme-color"]');
      if (meta) meta.setAttribute('content', toi ? MAU_NEN.toi : MAU_NEN.sang);
    },

    // Hiệu ứng số đếm tăng dần. Sửa nodeValue của text node sẵn có để không làm Blazor mất tham chiếu.
    demSo: (el, tu, den, dau, hau) => {
      if (!el) return;
      let nut = el.firstChild;
      if (!nut) { nut = document.createTextNode(''); el.appendChild(nut); }
      const hienThi = (v) => { nut.nodeValue = dau + dinhDangSo(v) + hau; };
      if (el._demId) cancelAnimationFrame(el._demId);
      if (giamChuyenDong() || tu === den) { hienThi(den); return; }
      const batDau = performance.now(), thoiGian = 750;
      const buoc = (t) => {
        const p = Math.min(1, (t - batDau) / thoiGian);
        const e = 1 - Math.pow(1 - p, 3); // easeOutCubic
        hienThi(tu + (den - tu) * e);
        if (p < 1) el._demId = requestAnimationFrame(buoc); else hienThi(den);
      };
      el._demId = requestAnimationFrame(buoc);
    },

    // Xuất file backup: ưu tiên Share Sheet của iOS, không được thì tải file về.
    // Trả về "share" | "download" | "huy"
    chiaSeFile: async (ten, noiDung) => {
      const file = new File([noiDung], ten, { type: 'application/json' });
      try {
        if (navigator.canShare && navigator.canShare({ files: [file] })) {
          await navigator.share({ files: [file], title: 'Sao lưu chi tiêu' });
          return 'share';
        }
      } catch (e) {
        if (e && e.name === 'AbortError') return 'huy'; // người dùng đóng Share Sheet
        // lỗi khác (ví dụ NotAllowedError) -> thử tải file
      }
      try {
        const url = URL.createObjectURL(file);
        const a = document.createElement('a');
        a.href = url; a.download = ten;
        document.body.appendChild(a); a.click(); a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 30000);
        return 'download';
      } catch (e) { return 'huy'; }
    },

    // Hướng dẫn "Thêm vào Màn hình chính": chỉ cần khi đang mở trong Safari iOS (chưa cài).
    canHuongDanIos: () => {
      const ua = navigator.userAgent || '';
      const laIos = /iPhone|iPad|iPod/.test(ua) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
      const daCai = window.navigator.standalone === true || window.matchMedia('(display-mode: standalone)').matches;
      return laIos && !daCai;
    },

    // Đăng ký service worker và báo khi có phiên bản mới (iPhone hay giữ app chạy rất lâu).
    dangKySw: async () => {
      if (!('serviceWorker' in navigator)) return;
      try {
        const reg = await navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' });
        const baoMoi = (sw) => {
          if (!sw || document.getElementById('banner-cap-nhat')) return;
          const b = document.createElement('div');
          b.id = 'banner-cap-nhat';
          b.innerHTML = '<span>Có phiên bản mới</span><button type="button">Cập nhật</button>';
          b.querySelector('button').onclick = () => sw.postMessage('SKIP_WAITING');
          document.body.appendChild(b);
        };
        if (reg.waiting && navigator.serviceWorker.controller) baoMoi(reg.waiting);
        reg.addEventListener('updatefound', () => {
          const sw = reg.installing;
          if (!sw) return;
          sw.addEventListener('statechange', () => {
            if (sw.state === 'installed' && navigator.serviceWorker.controller) baoMoi(sw);
          });
        });
        let daTai = false;
        navigator.serviceWorker.addEventListener('controllerchange', () => {
          if (daTai) return; daTai = true; location.reload();
        });
        // Kiểm tra bản mới mỗi khi mở lại app
        document.addEventListener('visibilitychange', () => { if (!document.hidden) reg.update().catch(() => { }); });
      } catch (e) { /* không đăng ký được thì app vẫn chạy bình thường */ }
    }
  };

  // Hiệu ứng nhấn: gợn sóng (ripple) từ điểm chạm + rung nhẹ. Dùng ủy quyền sự kiện nên mọi nút đều có.
  document.addEventListener('pointerdown', (e) => {
    const nut = e.target.closest && e.target.closest('.nut, .nut-them');
    if (!nut || nut.disabled) return;
    try { if (navigator.vibrate) navigator.vibrate(8); } catch (err) { }
    if (giamChuyenDong()) return;
    const r = nut.getBoundingClientRect();
    const d = Math.max(r.width, r.height);
    const gon = document.createElement('span');
    gon.className = 'gon';
    gon.style.cssText = 'width:' + d + 'px;height:' + d + 'px;left:' + (e.clientX - r.left - d / 2) + 'px;top:' + (e.clientY - r.top - d / 2) + 'px';
    nut.appendChild(gon);
    setTimeout(() => gon.remove(), 600);
  }, { passive: true });

  window.chitieuUtil.dangKySw();
})();
