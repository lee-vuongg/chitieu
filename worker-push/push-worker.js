// Cloudflare Worker: lưu đăng ký nhận thông báo và gửi nhắc hằng ngày.
// Thông báo gửi đi không có nội dung (empty push), chỉ xác thực bằng VAPID.
// Máy chủ KHÔNG nhận và KHÔNG lưu dữ liệu chi tiêu nào, chỉ lưu địa chỉ thiết bị nhận thông báo.

const enc = new TextEncoder();

function b64u(buf) {
    const bytes = new Uint8Array(buf);
    let s = '';
    for (const b of bytes) s += String.fromCharCode(b);
    return btoa(s).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

async function khoaTu(endpoint) {
    const hash = await crypto.subtle.digest('SHA-256', enc.encode(endpoint));
    return 'sub:' + [...new Uint8Array(hash)].map(b => b.toString(16).padStart(2, '0')).join('');
}

// JWT ký ES256 theo chuẩn VAPID (RFC 8292)
async function taoJwt(aud, env) {
    const jwk = JSON.parse(env.VAPID_PRIVATE_JWK);
    const khoa = await crypto.subtle.importKey('jwk', jwk, { name: 'ECDSA', namedCurve: 'P-256' }, false, ['sign']);
    const dau = b64u(enc.encode(JSON.stringify({ typ: 'JWT', alg: 'ES256' })));
    const than = b64u(enc.encode(JSON.stringify({
        aud,
        exp: Math.floor(Date.now() / 1000) + 12 * 3600,
        sub: env.VAPID_SUBJECT
    })));
    const noiDung = `${dau}.${than}`;
    const chuKy = await crypto.subtle.sign({ name: 'ECDSA', hash: 'SHA-256' }, khoa, enc.encode(noiDung));
    return `${noiDung}.${b64u(chuKy)}`;
}

// Trả về { status, noiDung } để ghi log đầy đủ khi có lỗi
async function guiNhac(endpoint, env) {
    const jwt = await taoJwt(new URL(endpoint).origin, env);
    const res = await fetch(endpoint, {
        method: 'POST',
        headers: {
            TTL: '3600',
            Authorization: `vapid t=${jwt}, k=${env.VAPID_PUBLIC}`
        }
    });
    let noiDung = '';
    if (res.status !== 201) {
        try { noiDung = await res.text(); } catch { /* bỏ qua */ }
    }
    return { status: res.status, noiDung };
}

async function guiTatCa(env) {
    let tong = 0, thanhCong = 0, loi = 0, xoa = 0;
    let cursor;
    do {
        const ds = await env.SUBS.list({ prefix: 'sub:', cursor });
        for (const k of ds.keys) {
            const raw = await env.SUBS.get(k.name);
            if (!raw) continue;
            tong++;
            const { endpoint } = JSON.parse(raw);
            try {
                const { status, noiDung } = await guiNhac(endpoint, env);
                if (status === 201) {
                    thanhCong++;
                    console.log('Gửi thành công:', endpoint.slice(0, 50));
                } else {
                    loi++;
                    console.log('Apple trả về mã:', status, noiDung, endpoint.slice(0, 50));
                    // 404/410: thiết bị đã gỡ đăng ký hoặc xóa app, bỏ khỏi danh sách
                    if (status === 404 || status === 410) {
                        await env.SUBS.delete(k.name);
                        xoa++;
                    }
                }
            } catch (e) {
                loi++;
                console.log('Lỗi gửi thông báo:', e.message);
            }
        }
        cursor = ds.list_complete ? undefined : ds.cursor;
    } while (cursor);
    console.log(`Tổng kết: ${tong} thiết bị, ${thanhCong} thành công, ${loi} lỗi, ${xoa} đã xóa`);
}

export default {
    async fetch(request, env) {
        const origin = request.headers.get('Origin') || '';
        const dungOrigin = origin === env.ALLOWED_ORIGIN;
        const cors = {
            'Access-Control-Allow-Origin': dungOrigin ? origin : 'null',
            'Access-Control-Allow-Methods': 'POST, OPTIONS',
            'Access-Control-Allow-Headers': 'Content-Type'
        };

        if (request.method === 'OPTIONS') return new Response(null, { headers: cors });
        if (request.method !== 'POST') return new Response('Not found', { status: 404 });
        if (!dungOrigin) return new Response('Forbidden', { status: 403 });

        const duong = new URL(request.url).pathname;

        let body;
        try { body = await request.json(); }
        catch { return new Response('Bad request', { status: 400, headers: cors }); }

        const endpoint = body?.endpoint;
        if (typeof endpoint !== 'string' || !endpoint.startsWith('https://') || endpoint.length > 1000) {
            return new Response('Bad request', { status: 400, headers: cors });
        }
        const khoa = await khoaTu(endpoint);

        if (duong === '/subscribe') {
            await env.SUBS.put(khoa, JSON.stringify({ endpoint, luc: Date.now() }));
            return new Response('ok', { headers: cors });
        }
        if (duong === '/unsubscribe') {
            await env.SUBS.delete(khoa);
            return new Response('ok', { headers: cors });
        }
        return new Response('Not found', { status: 404, headers: cors });
    },

    async scheduled(event, env, ctx) {
        ctx.waitUntil(guiTatCa(env));
    }
};