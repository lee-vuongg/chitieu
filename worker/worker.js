// Cloudflare Worker trung gian: giữ API key Gemini bí mật, app chỉ gọi Worker này.
// Secrets cần đặt (xem README.md):  GEMINI_API_KEY, APP_SECRET, ALLOWED_ORIGIN
const MODEL = 'gemini-2.5-flash';

export default {
  async fetch(request, env) {
    const origin = request.headers.get('Origin') || '';
    // ALLOWED_ORIGIN có thể là nhiều địa chỉ ngăn cách bằng dấu phẩy (bản chạy thật + localhost để thử)
    const choPhep = (env.ALLOWED_ORIGIN || '').split(',').map(s => s.trim()).filter(Boolean);
    const hopLe = origin !== '' && choPhep.includes(origin);
    // CORS đúng origin: chỉ trả header cho trang web của bạn, không dùng dấu *
    const cors = {
      'Access-Control-Allow-Origin': hopLe ? origin : 'null',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type',
      'Access-Control-Allow-Methods': 'POST, OPTIONS',
      'Access-Control-Max-Age': '86400',
      'Vary': 'Origin',
    };
    const json = (obj, status = 200) =>
      new Response(JSON.stringify(obj), { status, headers: { ...cors, 'Content-Type': 'application/json; charset=utf-8' } });

    if (request.method === 'OPTIONS') return new Response(null, { status: 204, headers: cors });
    if (request.method !== 'POST') return json({ loi: 'Method not allowed' }, 405);
    if (!hopLe) return json({ loi: 'Origin không được phép' }, 403);
    if (request.headers.get('Authorization') !== `Bearer ${env.APP_SECRET}`) return json({ loi: 'Sai mã bí mật' }, 401);

    let body;
    try { body = await request.json(); } catch { return json({ loi: 'JSON không hợp lệ' }, 400); }

    let prompt, schema = null;
    if (body.action === 'parse') {
      prompt = `Bạn tách một câu nhập chi tiêu tiếng Việt thành dữ liệu. Hôm nay là ${body.homNay}.
Danh mục hợp lệ (id: tên [loại]): ${(body.danhMuc || []).map(d => `${d.id}: ${d.ten} [${d.loai}]`).join('; ')}.
Quy đổi: k/nghìn/ngàn=1.000, tr/triệu/củ=1.000.000. Chọn đúng một danhMucId trong danh sách, loai là "thu" hoặc "chi", ngay dạng yyyy-MM-dd.
Câu: "${String(body.cau || '').slice(0, 200)}"`;
      schema = {
        type: 'OBJECT',
        properties: {
          loai: { type: 'STRING' }, soTien: { type: 'INTEGER' }, danhMucId: { type: 'STRING' },
          ghiChu: { type: 'STRING' }, ngay: { type: 'STRING' },
        },
      };
    } else if (body.action === 'ask') {
      prompt = `Bạn là trợ lý chi tiêu cho một người lớn tuổi. Trả lời ngắn gọn, dễ hiểu bằng tiếng Việt, số tiền dạng 1.250.000 đ.
Chỉ dùng số liệu dưới đây (tổng chi theo ngày và danh mục, hôm nay là ${body.duLieu?.homNay} - ${body.duLieu?.thuHomNay}). Nếu không đủ dữ liệu hãy nói rõ.
"Tuần rồi" là tuần từ thứ hai đến chủ nhật liền trước tuần hiện tại.
Dữ liệu: ${JSON.stringify(body.duLieu)}
Câu hỏi: "${String(body.cauHoi || '').slice(0, 300)}"`;
    } else if (body.action === 'summary') {
      prompt = `Bạn là trợ lý chi tiêu cho một người lớn tuổi. Viết tổng kết tháng bằng tiếng Việt, thân thiện, tối đa 6 câu:
tổng thu/chi, khoản chi lớn nhất, so với tháng trước, tình hình ngân sách, và 1 lời khuyên nhỏ. Số tiền dạng 1.250.000 đ.
Dữ liệu: ${JSON.stringify(body.duLieu)}`;
    } else {
      return json({ loi: 'action không hợp lệ' }, 400);
    }

    const payload = { contents: [{ parts: [{ text: prompt }] }] };
    if (schema) payload.generationConfig = { responseMimeType: 'application/json', responseSchema: schema };

    const res = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/${MODEL}:generateContent`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'x-goog-api-key': env.GEMINI_API_KEY },
      body: JSON.stringify(payload),
    });
    // Chuyển nguyên mã lỗi (đặc biệt 429 hết quota) để app báo thân thiện và tự quay về parser offline
    if (!res.ok) return json({ loi: 'Gemini lỗi' }, res.status === 429 ? 429 : 502);

    const data = await res.json();
    const text = data?.candidates?.[0]?.content?.parts?.[0]?.text ?? '';
    if (schema) {
      try { return json(JSON.parse(text)); } catch { return json({ loi: 'AI trả về sai định dạng' }, 502); }
    }
    return json({ traLoi: text });
  },
};
