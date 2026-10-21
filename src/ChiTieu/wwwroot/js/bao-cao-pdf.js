// bao-cao-pdf.js: xuất báo cáo thu chi ra PDF bằng hộp thoại in của trình duyệt.
// Trên iPhone: trong hộp thoại in, chọn Chia sẻ → Lưu vào Tệp để có file PDF.
window.chiTieuBaoCao = (() => {
    const esc = s => String(s ?? '').replace(/[&<>"']/g, c =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

    const tien = n => Number(n || 0).toLocaleString('vi-VN') + ' đ';

    // du lieu: { tieuDe, tu, den, giaoDich: [{ ngay, danhMuc, ghiChu, soTien, loai }] }
    // ngay, tu, den dạng yyyy-MM-dd; loai của giao dịch là 'thu' hoặc 'chi'
    function inPDF(du) {
        const dsLoc = (du.giaoDich || [])
            .filter(g => g.ngay >= du.tu && g.ngay <= du.den)
            .sort((a, b) => a.ngay.localeCompare(b.ngay));

        const tongThu = dsLoc.filter(g => g.loai === 'thu').reduce((s, g) => s + g.soTien, 0);
        const tongChi = dsLoc.filter(g => g.loai === 'chi').reduce((s, g) => s + g.soTien, 0);

        const chiTheoDm = {};
        dsLoc.filter(g => g.loai === 'chi').forEach(g => {
            const k = g.danhMuc || 'Khác';
            chiTheoDm[k] = (chiTheoDm[k] || 0) + g.soTien;
        });
        const dmRows = Object.entries(chiTheoDm)
            .sort((a, b) => b[1] - a[1])
            .map(([k, v]) => `<tr><td>${esc(k)}</td><td class="r">${tien(v)}</td></tr>`)
            .join('') || '<tr><td colspan="2">Không có khoản chi</td></tr>';

        const gdRows = dsLoc.map(g => `
            <tr>
                <td>${esc(g.ngay)}</td>
                <td>${g.loai === 'thu' ? 'Thu' : 'Chi'}</td>
                <td>${esc(g.danhMuc)}</td>
                <td class="ghichu">${esc(g.ghiChu)}</td>
                <td class="r ${g.loai === 'thu' ? 'thu' : 'chi'}">${tien(g.soTien)}</td>
            </tr>`).join('') || '<tr><td colspan="5">Không có giao dịch trong khoảng này</td></tr>';

        const html = `<!doctype html>
<html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>${esc(du.tieuDe)}</title>
<style>
  @page { size: A4; margin: 16mm; }
  body { font-family: Arial, "Segoe UI", Roboto, sans-serif; color: #1D2430; font-size: 12px; }
  h1 { font-size: 20px; margin: 0 0 4px; }
  .phu { color: #55606F; margin-bottom: 16px; }
  .tong { display: flex; gap: 12px; margin-bottom: 18px; }
  .tong > div { flex: 1; border: 1px solid #ddd; border-radius: 8px; padding: 10px; }
  .tong b { display: block; font-size: 16px; margin-top: 4px; }
  h2 { font-size: 14px; margin: 18px 0 6px; }
  table { width: 100%; border-collapse: collapse; }
  th, td { border-bottom: 1px solid #e5e5e5; padding: 6px 4px; text-align: left; vertical-align: top; }
  th { background: #f4f0e6; font-weight: 700; }
  .r { text-align: right; white-space: nowrap; }
  .thu { color: #17803F; }
  .chi { color: #D9481C; }
  .ghichu { word-break: break-word; min-width: 120px; }
  @media screen and (max-width: 600px) {
    body { font-size: 15px; padding: 12px; }
    h1 { font-size: 22px; }
    .tong { flex-direction: column; }
    th, td { font-size: 14px; }
    .r { white-space: normal; }
  }
</style></head>
<body>
  <h1>${esc(du.tieuDe)}</h1>
  <div class="phu">Từ ${esc(du.tu)} đến ${esc(du.den)}</div>

  <div class="tong">
    <div>Tổng thu<b class="thu">${tien(tongThu)}</b></div>
    <div>Tổng chi<b class="chi">${tien(tongChi)}</b></div>
    <div>Chênh lệch<b>${tien(tongThu - tongChi)}</b></div>
  </div>

  <h2>Chi theo danh mục</h2>
  <table><thead><tr><th>Danh mục</th><th class="r">Số tiền</th></tr></thead>
  <tbody>${dmRows}</tbody></table>

  <h2>Chi tiết giao dịch</h2>
  <table><thead><tr><th>Ngày</th><th>Loại</th><th>Danh mục</th><th>Ghi chú</th><th class="r">Số tiền</th></tr></thead>
  <tbody>${gdRows}</tbody></table>
</body></html>`;

        const w = window.open('', '_blank');
        if (!w) {
            alert('Trình duyệt đang chặn cửa sổ mới. Hãy cho phép pop-up cho trang này rồi thử lại.');
            return;
        }
        w.document.open();
        w.document.write(html);
        w.document.close();
        w.focus();
        setTimeout(() => w.print(), 300);
    }

    return { inPDF };
})();