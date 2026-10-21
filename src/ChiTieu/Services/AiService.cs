using System.Net.Http.Json;
using ChiTieu.Models;
using Microsoft.JSInterop;

namespace ChiTieu.Services;

public record KetQuaAi<T>(bool Ok, T? Data, string? Loi);

/// <summary>
/// Gọi thẳng Google Gemini từ trình duyệt bằng API key người dùng nhập. AI chỉ là phần tùy chọn:
/// mọi lỗi đều trả về Ok=false kèm thông báo thân thiện, app tự quay về parser offline.
/// </summary>
public class AiService
{
    private readonly HttpClient _http;
    private readonly KhoDuLieu _kho;
    private readonly DongHo _dongHo;
    private readonly IJSRuntime _js;
    private readonly ThongKeService _thongKe;
    private readonly QuanLyNganSach _nganSach;

    public AiService(HttpClient http, KhoDuLieu kho, DongHo dongHo, IJSRuntime js,
        ThongKeService thongKe, QuanLyNganSach nganSach)
    {
        _http = http; _kho = kho; _dongHo = dongHo; _js = js; _thongKe = thongKe; _nganSach = nganSach;
    }

    private const string Model = "gemini-2.5-flash"; // đổi nếu Google đổi tên model
    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/models/" + Model + ":generateContent";

    // AiMa = Gemini API key. AiUrl chỉ còn là dấu vết của cấu hình Worker cũ: nếu còn giá trị thì coi như chưa cài.
    public bool DaCauHinh =>
        string.IsNullOrWhiteSpace(_kho.CaiDat.AiUrl) && !string.IsNullOrWhiteSpace(_kho.CaiDat.AiMa);

    public async Task<bool> CoMangAsync()
    {
        try { return await _js.InvokeAsync<bool>("chitieuUtil.coMang"); }
        catch { return false; }
    }

    /// <summary>Chỉ nên gọi khi parser tự tin thấp. Trả Ok=false nếu không dùng được AI (cho chọn danh mục tay).</summary>
    public async Task<KetQuaAi<KetQuaParse>> PhanTichAsync(string cau, KetQuaParse local)
    {
        if (!DaCauHinh) return new(false, null, null);
        if (!await CoMangAsync()) return new(false, null, "Đang mất mạng, dùng cách phân tích có sẵn.");

        var dsDm = string.Join("; ", _kho.DsDanhMuc.Select(d => $"{d.Id}: {d.Ten} [{(d.Loai == LoaiGiaoDich.Thu ? "thu" : "chi")}]"));
        var cauCat = cau.Length > 200 ? cau[..200] : cau;
        var prompt = $"Bạn tách một câu nhập chi tiêu tiếng Việt thành dữ liệu. Hôm nay là {_dongHo.HomNay:yyyy-MM-dd}.\n" +
                     $"Danh mục hợp lệ (id: tên [loại]): {dsDm}.\n" +
                     "Quy đổi: k/nghìn/ngàn=1.000, tr/triệu/củ=1.000.000. Chọn đúng một danhMucId trong danh sách, loai là \"thu\" hoặc \"chi\", ngay dạng yyyy-MM-dd.\n" +
                     $"Câu: \"{cauCat}\"";
        var schema = new
        {
            type = "OBJECT",
            properties = new
            {
                loai = new { type = "STRING" },
                soTien = new { type = "INTEGER" },
                danhMucId = new { type = "STRING" },
                ghiChu = new { type = "STRING" },
                ngay = new { type = "STRING" }
            }
        };
        var (ok, json, loi) = await GoiAsync(prompt, schema, 10);
        if (!ok) return new(false, null, loi);

        var ai = AiCore.DocKetQuaParse(json ?? "");
        if (ai == null) return new(false, null, "AI trả lời không đúng định dạng. Dùng cách phân tích có sẵn.");

        // Chỉ nhận phần hợp lệ; phần nào thiếu thì giữ kết quả của parser offline.
        var dm = _kho.TimDanhMuc(ai.DanhMucId);
        var soTien = ai.SoTien is > 0 ? ai.SoTien : local.SoTien;
        if (soTien == null && dm == null) return new(false, null, "AI không hiểu được câu này.");
        var loai = dm?.Loai ?? (string.Equals(ai.Loai, "thu", StringComparison.OrdinalIgnoreCase) ? LoaiGiaoDich.Thu : local.Loai);
        var ngay = DateOnly.TryParse(ai.Ngay, out var n) && n <= _dongHo.HomNay.AddDays(1) ? n : local.Ngay;

        return new(true, new KetQuaParse
        {
            Loai = loai,
            SoTien = soTien,
            Ngay = ngay,
            GhiChu = string.IsNullOrWhiteSpace(ai.GhiChu) ? local.GhiChu : ai.GhiChu.Trim(),
            DanhMucId = dm?.Id ?? local.DanhMucId,
            DoTinCay = dm != null && soTien != null ? 0.85 : 0.5,
            TimThayDanhMuc = dm != null,
            TimThayNgay = local.TimThayNgay
        }, null);
    }

    /// <summary>Hỏi đáp: chỉ gửi tổng chi theo (ngày, danh mục), không gửi ghi chú.</summary>
    public async Task<KetQuaAi<string>> HoiAsync(string cauHoi)
    {
        if (!DaCauHinh) return new(false, null, "Chưa cài AI. Vào Cài đặt để nhập địa chỉ và mã bí mật.");
        if (!await CoMangAsync()) return new(false, null, "Đang mất mạng nên chưa hỏi được.");
        var duLieu = AiCore.DuLieuHoiDap(_kho.DsGiaoDich, _kho.DsDanhMuc, _dongHo.HomNay);
        var cauCat = cauHoi.Length > 300 ? cauHoi[..300] : cauHoi;
        var prompt = "Bạn là trợ lý chi tiêu cho một người lớn tuổi. Trả lời ngắn gọn, dễ hiểu bằng tiếng Việt, số tiền dạng 1.250.000 đ.\n" +
                     $"Chỉ dùng số liệu dưới đây (tổng chi theo ngày và danh mục, hôm nay là {_dongHo.HomNay:yyyy-MM-dd} - {DinhDang.Thu(_dongHo.HomNay)}). Nếu không đủ dữ liệu hãy nói rõ.\n" +
                     "\"Tuần rồi\" là tuần từ thứ hai đến chủ nhật liền trước tuần hiện tại.\n" +
                     $"Dữ liệu: {System.Text.Json.JsonSerializer.Serialize(duLieu, JsonCfg.Opt)}\n" +
                     $"Câu hỏi: \"{cauCat}\"";
        return await TraLoiAsync(prompt);
    }

    public async Task<KetQuaAi<string>> TongKetThangAsync(int nam, int thang)
    {
        if (!DaCauHinh) return new(false, null, "Chưa cài AI. Vào Cài đặt để nhập địa chỉ và mã bí mật.");
        if (!await CoMangAsync()) return new(false, null, "Đang mất mạng nên chưa tổng kết được.");
        var duLieu = AiCore.TomTatThang(_kho.DsGiaoDich, _kho.DsDanhMuc, _nganSach.TinhTrang(nam, thang), nam, thang);
        var prompt = "Bạn là trợ lý chi tiêu cho một người lớn tuổi. Viết tổng kết tháng bằng tiếng Việt, thân thiện, tối đa 6 câu:\n" +
                     "tổng thu/chi, khoản chi lớn nhất, so với tháng trước, tình hình ngân sách, và 1 lời khuyên nhỏ. Số tiền dạng 1.250.000 đ.\n" +
                     $"Dữ liệu: {System.Text.Json.JsonSerializer.Serialize(duLieu, JsonCfg.Opt)}";
        return await TraLoiAsync(prompt);
    }

    private async Task<KetQuaAi<string>> TraLoiAsync(string prompt)
    {
        var (ok, text, loi) = await GoiAsync(prompt, null, 25);
        if (!ok) return new(false, null, loi);
        if (string.IsNullOrWhiteSpace(text))
            return new KetQuaAi<string>(false, null, "AI chưa trả lời được. Hãy thử lại sau.");
        return new KetQuaAi<string>(true, text.Trim(), null);
    }

    /// <summary>Gọi Gemini. Có schema thì ép trả JSON theo schema; không có thì trả văn bản thường. Trả về phần text của câu trả lời.</summary>
    private async Task<(bool Ok, string? Text, string? Loi)> GoiAsync(string prompt, object? schema, int giayToiDa)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(giayToiDa));
            object body = schema == null
                ? new { contents = new[] { new { parts = new[] { new { text = prompt } } } } }
                : new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new { responseMimeType = "application/json", responseSchema = schema }
                };
            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(body, options: JsonCfg.Opt)
            };
            req.Headers.TryAddWithoutValidation("x-goog-api-key", _kho.CaiDat.AiMa.Trim());
            using var res = await _http.SendAsync(req, cts.Token);
            if (!res.IsSuccessStatusCode) return (false, null, AiCore.TaoMaLoiThanThien((int)res.StatusCode));

            using var doc = System.Text.Json.JsonDocument.Parse(await res.Content.ReadAsStringAsync(cts.Token));
            var text = doc.RootElement.TryGetProperty("candidates", out var cs) && cs.GetArrayLength() > 0
                       && cs[0].TryGetProperty("content", out var ct) && ct.TryGetProperty("parts", out var ps) && ps.GetArrayLength() > 0
                       && ps[0].TryGetProperty("text", out var t) ? t.GetString() : null;
            return (true, text, null);
        }
        catch (System.Text.Json.JsonException)
        {
            return (false, null, "AI trả lời không đúng định dạng. Hãy thử lại sau.");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return (false, null, AiCore.TaoMaLoiThanThien(null));
        }
    }
    /// <summary>Lập kế hoạch chi tiêu cho khoảng ngày. Chỉ gửi số liệu tổng hợp theo danh mục.</summary>
    public async Task<KetQuaAi<AiKeHoach>> KeHoachAsync(DateOnly tu, DateOnly den, string? ghiChu)
    {
        if (!DaCauHinh) return new(false, null, "Chưa cài AI. Vào Cài đặt để nhập mã AI.");
        if (!await CoMangAsync()) return new(false, null, "Đang mất mạng nên chưa lập kế hoạch được.");

        var soNgay = Math.Max(1, den.DayNumber - tu.DayNumber + 1);
        var trongKhoang = _kho.DsGiaoDich.Where(g => g.Ngay >= tu && g.Ngay <= den).ToList();

        var thu = trongKhoang.Where(g => g.Loai == LoaiGiaoDich.Thu).Sum(g => g.SoTien);
        var chiTheoDm = trongKhoang
            .Where(g => g.Loai == LoaiGiaoDich.Chi)
            .GroupBy(g => g.DanhMucId)
            .Select(gr => new { danhMuc = _kho.TenDanhMuc(gr.Key), tong = gr.Sum(x => x.SoTien) })
            .OrderByDescending(x => x.tong)
            .ToList();
        var chi = chiTheoDm.Sum(x => x.tong);

        var hanMuc = _kho.DsNganSach
            .Select(n => new { danhMucId = n.DanhMucId, tenDanhMuc = _kho.TenDanhMuc(n.DanhMucId), hanMucThang = n.HanMuc })
            .ToList();

        var ghi = (ghiChu ?? "").Trim();
        if (ghi.Length > 300) ghi = ghi[..300];

        var prompt = "Bạn là trợ lý lập kế hoạch chi tiêu cho một người lớn tuổi, viết bằng tiếng Việt, ngắn gọn, dễ hiểu.\n" +
                     $"Khoảng thời gian: {tu:dd/MM/yyyy} đến {den:dd/MM/yyyy} ({soNgay} ngày).\n" +
                     $"Tổng thu: {thu} đ. Tổng chi: {chi} đ.\n" +
                     $"Chi theo danh mục: {System.Text.Json.JsonSerializer.Serialize(chiTheoDm, JsonCfg.Opt)}\n" +
                     $"Hạn mức đã đặt (theo tháng): {System.Text.Json.JsonSerializer.Serialize(hanMuc, JsonCfg.Opt)}\n" +
                     "Hãy đề xuất hạn mức chi cho từng danh mục trong khoảng này, tóm tắt ngắn, và 2 đến 3 lời khuyên." +
                     (ghi.Length > 0 ? $"\nNgười dùng dặn thêm: \"{ghi}\"" : "");

        var schema = new
        {
            type = "OBJECT",
            properties = new
            {
                tomTat = new { type = "STRING" },
                hanMuc = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            danhMucId = new { type = "STRING" },
                            soTien = new { type = "INTEGER" },
                            ghiChu = new { type = "STRING" }
                        }
                    }
                },
                loiKhuyen = new { type = "ARRAY", items = new { type = "STRING" } }
            }
        };

        var (ok, json, loi) = await GoiAsync(prompt, schema, 30);
        if (!ok) return new(false, null, loi);

        try
        {
            var kq = System.Text.Json.JsonSerializer.Deserialize<AiKeHoach>(json ?? "", JsonCfg.Opt);
            if (kq == null) return new(false, null, "AI chưa trả lời được. Hãy thử lại.");
            return new(true, kq, null);
        }
        catch (System.Text.Json.JsonException)
        {
            return new(false, null, "AI trả lời không đúng định dạng. Hãy thử lại.");
        }
    }
}