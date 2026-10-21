// Cloudflare Worker: thống kê và hồ sơ người dùng.
// Không nhận số tiền, ghi chú hay tên danh mục. Tỷ lệ chi tiêu (phần trăm) chỉ được lưu khi người dùng đồng ý riêng.
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

function layKhoangNgay(params) {
    const hom = new Date().toISOString().slice(0, 10);
    const den = params.get('den') || hom;
    if (!REGEX_NGAY.test(den)) return null;
    const tu = params.get('tu') || new Date(Date.parse(den + 'T00:00:00Z') - 29 * 864e5).toISOString().slice(0, 10);
    if (!REGEX_NGAY.test(tu) || tu > den) return null;
    const soNgay = (Date.parse(den + 'T00:00:00Z') - Date.parse(tu + 'T00:00:00Z')) / 864e5 + 1;
    if (soNgay > NGAY_TOI_DA) return null;
    return { tu, den };
}

function hopLe(chuoi, toiDa) {
    return typeof chuoi === 'string' && chuoi.length >= 1 && chuoi.length <= toiDa;
}

// Kiểm tra và làm sạch tỷ lệ chi tiêu: chỉ phần trăm và tên danh mục, không có số tiền
function lamSachTyLe(mang) {
    if (!Array.isArray(mang)) return null;
    const ketQua = [];
    for (const m of mang.slice(0, 12)) {
        if (!hopLe(m?.danhMuc, 40) || !Number.isInteger(m?.phanTram) || m.phanTram < 0 || m.phanTram > 100) return null;
        ketQua.push({ danhMuc: m.danhMuc, phanTram: m.phanTram });
    }
    return ketQua;
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

        // ----- App gửi lên -----
        if (request.method === 'POST' && (url.pathname === '/event' || url.pathname === '/profile')) {
            if (!dungOrigin) return new Response('Forbidden', { status: 403 });

            let b;
            try { b = await request.json(); }
            catch { return new Response('Bad request', { status: 400, headers }); }

            if (typeof b?.id !== 'string' || b.id.length < 8 || b.id.length > 64) {
                return new Response('Bad request', { status: 400, headers });
            }

            const ngay = new Date().toISOString().slice(0, 10);
            const maThietBi = await bam(b.id, env.MUOI);

            if (url.pathname === '/event') {
                if (!SU_KIEN.has(b.ev) || !NEN_TANG.has(b.p)) return new Response('Bad request', { status: 400, headers });
                const maNgay = await bam(b.id + '|' + ngay, env.MUOI);
                await env.DB.batch([
                    env.DB.prepare('INSERT OR IGNORE INTO thiet_bi (ma, lan_dau) VALUES (?, ?)').bind(maThietBi, ngay),
                    env.DB.prepare('INSERT OR IGNORE INTO nguoi_dung (ngay, ma) VALUES (?, ?)').bind(ngay, maNgay),
                    env.DB.prepare(
                        'INSERT INTO su_kien (ngay, ev, nen, so) VALUES (?, ?, ?, 1) ' +
                        'ON CONFLICT(ngay, ev, nen) DO UPDATE SET so = so + 1'
                    ).bind(ngay, b.ev, b.p),
                    env.DB.prepare(
                        'INSERT INTO luot_ma (ngay, ma, ev, so) VALUES (?, ?, ?, 1) ' +
                        'ON CONFLICT(ngay, ma, ev) DO UPDATE SET so = so + 1'
                    ).bind(ngay, maThietBi, b.ev)
                ]);
                return new Response('ok', { headers });
            }

            // /profile: hồ sơ hiển thị với quản trị viên
            if (!hopLe(b.bot, 32)) return new Response('Bad request', { status: 400, headers });
            const dongYTen = b.dongYTen === true;
            const ten = dongYTen && hopLe(b.ten, 40) ? b.ten.trim() : null;
            const may = hopLe(b.may, 60) ? b.may : null;
            const dongYTyLe = b.dongYTyLe === true;
            const tyLe = dongYTyLe ? lamSachTyLe(b.tyLe) : null;

            await env.DB.prepare(
                'INSERT INTO ho_so (ma, bot, ten, may, tyle_json, cap_nhat) VALUES (?, ?, ?, ?, ?, ?) ' +
                'ON CONFLICT(ma) DO UPDATE SET bot = excluded.bot, ten = excluded.ten, may = excluded.may, ' +
                'tyle_json = excluded.tyle_json, cap_nhat = excluded.cap_nhat'
            ).bind(maThietBi, b.bot, ten, may, tyLe ? JSON.stringify(tyLe) : null, ngay).run();

            return new Response('ok', { headers });
        }

        // ----- Quản trị: đọc số liệu -----
        if (request.method === 'GET' && (url.pathname === '/stats' || url.pathname === '/users')) {
            if (url.searchParams.get('token') !== env.ADMIN_TOKEN) return new Response('Forbidden', { status: 403 });

            const khoang = layKhoangNgay(url.searchParams);
            if (!khoang) return new Response('Khoảng ngày không hợp lệ (tối đa 366 ngày)', { status: 400, headers });
            const { tu, den } = khoang;
                      // Dữ liệu đã được bảo vệ bằng mã quản trị, nên cho phép đọc từ mọi nguồn
            const jsonHeaders = { 'Access-Control-Allow-Origin': '*', 'Content-Type': 'application/json' };

            if (url.pathname === '/stats') {
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
                    tuNgay: tu, denNgay: den,
                    tongThietBi: tong.so,
                    nguoiDungHoatDongTheoNgay: hoatDong.results,
                    suKienTheoNen: suKien.results
                }, { headers: jsonHeaders });
            }

            // /users: danh sách người dùng có hoạt động trong khoảng
            const [nguoiRows, suKienRows] = await Promise.all([
                env.DB.prepare(
                    'SELECT l.ma, MAX(l.ngay) AS lan_cuoi, SUM(l.so) AS luot, h.bot, h.ten, h.may, h.tyle_json ' +
                    'FROM luot_ma l LEFT JOIN ho_so h ON h.ma = l.ma ' +
                    'WHERE l.ngay >= ? AND l.ngay <= ? GROUP BY l.ma ORDER BY luot DESC'
                ).bind(tu, den).all(),
                env.DB.prepare(
                    'SELECT ma, ev, SUM(so) AS so FROM luot_ma WHERE ngay >= ? AND ngay <= ? GROUP BY ma, ev'
                ).bind(tu, den).all()
            ]);

            const suKienTheoMa = {};
            for (const r of suKienRows.results) {
                (suKienTheoMa[r.ma] ??= {})[r.ev] = r.so;
            }

            const nguoi = nguoiRows.results.map(r => ({
                bot: r.bot || 'Chưa đặt tên',
                ten: r.ten || null,
                may: r.may || null,
                lanCuoi: r.lan_cuoi,
                luot: r.luot,
                tyLe: r.tyle_json ? JSON.parse(r.tyle_json) : null,
                suKien: suKienTheoMa[r.ma] || {}
            }));

            return Response.json({ tuNgay: tu, denNgay: den, nguoi }, { headers: jsonHeaders });
        }

        return new Response('Not found', { status: 404 });
    }
};
