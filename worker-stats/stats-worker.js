// Cloudflare Worker: thống kê ẩn danh.
// Chỉ đếm lượt dùng tính năng theo ngày. KHÔNG nhận số tiền, ghi chú, danh mục hay bất kỳ nội dung chi tiêu nào.
// Mã thiết bị được băm một chiều (có muối bí mật), không lưu mã gốc.

const SU_KIEN = new Set([
    'mo_app', 'luu_giao_dich', 'xoa_giao_dich', 'dung_mic', 'dung_pdf',
    'dung_ai', 'bat_nhac', 'dung_sao_luu'
]);
const NEN_TANG = new Set(['ios', 'android', 'web']);
const NGAY_TOI_DA = 366;
const REGEX_NGAY = /^\d{4}-\d{2}-\d{2}$/;

async function bam(chuoi, muoi) {
    const h = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(muoi + '|' + chuoi));
    return [...new Uint8Array(h)].map(b => b.toString(16).padStart(2, '0')).join('');
}

// Trả về khoảng ngày [tu, den] (yyyy-MM-dd). Mặc định là 30 ngày gần nhất tính đến hôm nay (UTC).
function layKhoangNgay(params) {
    const hom = new Date().toISOString().slice(0, 10);
    const tuParam = params.get('tu');
    const denParam = params.get('den');
    const den = denParam || hom;
    if (!REGEX_NGAY.test(den)) return null;
    const tu = tuParam || new Date(Date.parse(den + 'T00:00:00Z') - 29 * 864e5).toISOString().slice(0, 10);
    if (!REGEX_NGAY.test(tu)) return null;
    if (tu > den) return null;
    const soNgay = (Date.parse(den + 'T00:00:00Z') - Date.parse(tu + 'T00:00:00Z')) / 864e5 + 1;
    if (soNgay > NGAY_TOI_DA) return null;
    return { tu, den };
}

export default {
    async fetch(request, env) {
        const url = new URL(request.url);
        const origin = request.headers.get('Origin') || '';
        const dungOrigin = origin === env.ALLOWED_ORIGIN;
        const headers = {
            'Access-Control-Allow-Origin': dungOrigin ? origin : 'null',
            'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
            'Access-Control-Allow-Headers': 'Content-Type'
        };

        if (request.method === 'OPTIONS') return new Response(null, { headers });

        // Ghi nhận một sự kiện từ app
        if (request.method === 'POST' && url.pathname === '/event') {
            if (!dungOrigin) return new Response('Forbidden', { status: 403 });

            let b;
            try { b = await request.json(); }
            catch { return new Response('Bad request', { status: 400, headers }); }

            if (!SU_KIEN.has(b?.ev) || !NEN_TANG.has(b?.p) || typeof b?.id !== 'string' || b.id.length < 8 || b.id.length > 64) {
                return new Response('Bad request', { status: 400, headers });
            }

            const ngay = new Date().toISOString().slice(0, 10);
            const maThietBi = await bam(b.id, env.MUOI);
            const maNgay = await bam(b.id + '|' + ngay, env.MUOI);

            await env.DB.batch([
                env.DB.prepare('INSERT OR IGNORE INTO thiet_bi (ma, lan_dau) VALUES (?, ?)').bind(maThietBi, ngay),
                env.DB.prepare('INSERT OR IGNORE INTO nguoi_dung (ngay, ma) VALUES (?, ?)').bind(ngay, maNgay),
                env.DB.prepare(
                    'INSERT INTO su_kien (ngay, ev, nen, so) VALUES (?, ?, ?, 1) ' +
                    'ON CONFLICT(ngay, ev, nen) DO UPDATE SET so = so + 1'
                ).bind(ngay, b.ev, b.p)
            ]);

            return new Response('ok', { headers });
        }

        // Xem số liệu theo khoảng ngày: ?token=...&tu=yyyy-MM-dd&den=yyyy-MM-dd (mặc định 30 ngày, tối đa 366 ngày)
        if (request.method === 'GET' && url.pathname === '/stats') {
            if (url.searchParams.get('token') !== env.ADMIN_TOKEN) {
                return new Response('Forbidden', { status: 403 });
            }

            const khoang = layKhoangNgay(url.searchParams);
            if (!khoang) {
                return new Response('Khoảng ngày không hợp lệ (tối đa 366 ngày)', { status: 400, headers });
            }
            const { tu, den } = khoang;

            const [tong, hoatDong, suKien] = await Promise.all([
                env.DB.prepare('SELECT COUNT(*) AS so FROM thiet_bi').first(),
                env.DB.prepare(
                    'SELECT ngay, COUNT(*) AS so FROM nguoi_dung WHERE ngay >= ? AND ngay <= ? GROUP BY ngay ORDER BY ngay'
                ).bind(tu, den).all(),
                env.DB.prepare(
                    'SELECT ev, nen, SUM(so) AS so FROM su_kien WHERE ngay >= ? AND ngay <= ? GROUP BY ev, nen ORDER BY so DESC'
                ).bind(tu, den).all()
            ]);

            return Response.json({
                tuNgay: tu,
                denNgay: den,
                tongThietBi: tong.so,
                nguoiDungHoatDongTheoNgay: hoatDong.results,
                suKienTheoNen: suKien.results
            }, { headers: { ...headers, 'Content-Type': 'application/json' } });
        }

        return new Response('Not found', { status: 404 });
    }
};