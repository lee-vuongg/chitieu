using System.Text.Json;
using System.Text.Json.Serialization;
using ChiTieu.Models;

namespace ChiTieu.Services;

/// <summary>Kết quả AI phân tích câu nhập. Các trường đều có thể thiếu.</summary>
public class AiParseKetQua
{
    [JsonPropertyName("loai")] public string? Loai { get; set; }
    [JsonPropertyName("soTien")] public long? SoTien { get; set; }
    [JsonPropertyName("danhMucId")] public string? DanhMucId { get; set; }
    [JsonPropertyName("ghiChu")] public string? GhiChu { get; set; }
    [JsonPropertyName("ngay")] public string? Ngay { get; set; }
}

/// <summary>
/// Dựng dữ liệu gửi lên Worker. QUAN TRỌNG: chỉ gửi số liệu tổng hợp theo danh mục,
/// không gửi ghi chú/danh sách giao dịch thô.
/// </summary>
public static class AiCore
{
    public static object TomTatTheoDanhMuc(IEnumerable<GiaoDich> gd, IReadOnlyList<DanhMuc> dm, DateOnly tu, DateOnly den)
    {
        var tenDm = dm.ToDictionary(d => d.Id, d => d.Ten);
        var trongKhoang = gd.Where(g => g.Ngay >= tu && g.Ngay <= den).ToList();
        return new
        {
            tuNgay = tu.ToString("yyyy-MM-dd"),
            denNgay = den.ToString("yyyy-MM-dd"),
            tongThu = trongKhoang.Where(g => g.Loai == LoaiGiaoDich.Thu).Sum(g => g.SoTien),
            tongChi = trongKhoang.Where(g => g.Loai == LoaiGiaoDich.Chi).Sum(g => g.SoTien),
            chiTheoDanhMuc = trongKhoang.Where(g => g.Loai == LoaiGiaoDich.Chi)
                .GroupBy(g => g.DanhMucId)
                .Select(x => new { danhMuc = tenDm.GetValueOrDefault(x.Key, "Khác"), soTien = x.Sum(g => g.SoTien), soLan = x.Count() })
                .OrderByDescending(x => x.soTien).ToList()
        };
    }

    /// <summary>
    /// Dữ liệu cho hỏi đáp: tổng chi theo (ngày, danh mục) trong 90 ngày gần nhất.
    /// Có đủ độ chi tiết để trả lời "tuần rồi tiêu bao nhiêu tiền xăng" nhưng KHÔNG gửi ghi chú.
    /// </summary>
    public static object DuLieuHoiDap(IEnumerable<GiaoDich> gd, IReadOnlyList<DanhMuc> dm, DateOnly homNay)
    {
        var (tu, den) = KhoangHoiDap(homNay);
        var tenDm = dm.ToDictionary(d => d.Id, d => d.Ten);
        var trongKhoang = gd.Where(g => g.Ngay >= tu && g.Ngay <= den).ToList();
        return new
        {
            homNay = homNay.ToString("yyyy-MM-dd"),
            thuHomNay = DinhDang.Thu(homNay),
            tuNgay = tu.ToString("yyyy-MM-dd"),
            theoNgay = trongKhoang
                .GroupBy(g => new { g.Ngay, g.DanhMucId, g.Loai })
                .OrderBy(x => x.Key.Ngay)
                .Select(x => new
                {
                    ngay = x.Key.Ngay.ToString("yyyy-MM-dd"),
                    loai = x.Key.Loai == LoaiGiaoDich.Thu ? "thu" : "chi",
                    danhMuc = tenDm.GetValueOrDefault(x.Key.DanhMucId, "Khác"),
                    soTien = x.Sum(g => g.SoTien)
                }).ToList()
        };
    }

    /// <summary>Số liệu gửi cho "Tổng kết tháng bằng AI": tháng này, tháng trước và tình trạng ngân sách.</summary>
    public static object TomTatThang(IEnumerable<GiaoDich> gd, IReadOnlyList<DanhMuc> dm,
        IEnumerable<TinhTrangNganSach> nganSach, int nam, int thang)
    {
        var ds = gd.ToList();
        var dau = new DateOnly(nam, thang, 1);
        var cuoi = new DateOnly(nam, thang, DateTime.DaysInMonth(nam, thang));
        var truoc = dau.AddMonths(-1);
        var tenDm = dm.ToDictionary(d => d.Id, d => d.Ten);
        return new
        {
            thang = $"{thang:00}/{nam}",
            thangNay = TomTatTheoDanhMuc(ds, dm, dau, cuoi),
            thangTruoc = TomTatTheoDanhMuc(ds, dm, truoc, new DateOnly(truoc.Year, truoc.Month, DateTime.DaysInMonth(truoc.Year, truoc.Month))),
            nganSach = nganSach.Select(n => new
            {
                phamVi = n.DanhMucId == null ? "Tổng tháng" : tenDm.GetValueOrDefault(n.DanhMucId, "Khác"),
                hanMuc = n.HanMuc,
                daChi = n.DaChi
            }).ToList()
        };
    }

    /// <summary>Khoảng ngày để trả lời câu hỏi thông thường: gửi theo 90 ngày gần nhất.</summary>
    public static (DateOnly Tu, DateOnly Den) KhoangHoiDap(DateOnly homNay) => (homNay.AddDays(-90), homNay);

    public static string TaoMaLoiThanThien(int? httpStatus) => httpStatus switch
    {
        400 or 401 or 403 => "API key chưa đúng hoặc chưa được phép dùng. Hãy kiểm tra lại trong Cài đặt.",
        429 => "AI đã hết lượt dùng trong lúc này. Ứng dụng dùng cách phân tích có sẵn.",
        >= 500 => "Máy chủ AI đang bận. Ứng dụng dùng cách phân tích có sẵn.",
        _ => "Không kết nối được AI. Ứng dụng dùng cách phân tích có sẵn."
    };

    public static bool UrlHopLe(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.IsLoopback);

    public static AiParseKetQua? DocKetQuaParse(string json)
    {
        try { return JsonSerializer.Deserialize<AiParseKetQua>(json, JsonCfg.Opt); }
        catch (JsonException) { return null; }
    }
}