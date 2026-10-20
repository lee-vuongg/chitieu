using System.Globalization;
using System.Text;

namespace ChiTieu.Services;

/// <summary>Định dạng hiển thị: tiền VND 1.250.000 đ, ngày dd/MM/yyyy.</summary>
public static class DinhDang
{
    private static readonly string[] TenThu =
        { "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy" };

    public static string Tien(long soTien)
    {
        var s = Math.Abs(soTien).ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');
        return (soTien < 0 ? "-" : "") + s + " đ";
    }

    /// <summary>Chỉ phần số: 1.250.000 (dùng cho hiệu ứng đếm số).</summary>
    public static string So(long soTien) =>
        Math.Abs(soTien).ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');

    /// <summary>Rút gọn cho nhãn biểu đồ: 1,2tr, 350k.</summary>
    public static string TienRutGon(long soTien)
    {
        var a = Math.Abs(soTien);
        string r;
        if (a >= 1_000_000)
        {
            var tr = a / 1_000_000m;
            r = tr.ToString(tr % 1 == 0 ? "0" : "0.#", CultureInfo.InvariantCulture).Replace('.', ',') + "tr";
        }
        else if (a >= 1000)
        {
            r = (a / 1000m).ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',') + "k";
        }
        else r = a.ToString(CultureInfo.InvariantCulture);
        return (soTien < 0 ? "-" : "") + r;
    }

    public static string Ngay(DateOnly d) => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string Thang(int nam, int thang) => $"Tháng {thang:00}/{nam}";

    public static string Thu(DateOnly d) => TenThu[(int)d.DayOfWeek];

    /// <summary>Nhãn nhóm trong lịch sử: Hôm nay / Hôm qua / Thứ hai, 12/10/2026.</summary>
    public static string NhanNgay(DateOnly d, DateOnly homNay)
    {
        if (d == homNay) return "Hôm nay";
        if (d == homNay.AddDays(-1)) return "Hôm qua";
        return $"{Thu(d)}, {Ngay(d)}";
    }

    public static string PhanTram(double tyLe) =>
        (tyLe * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
}

/// <summary>Chuẩn hóa chữ: bỏ dấu, hạ chữ thường nhưng GIỮ NGUYÊN độ dài chuỗi.</summary>
public static class VanBan
{
    public static string ChuanHoaDoDai(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            var c = char.ToLowerInvariant(ch);
            if (c == 'đ') { sb.Append('d'); continue; }
            // Emoji (cặp surrogate) không được Normalize riêng lẻ vì sẽ ném lỗi
            if (c < 128 || char.IsSurrogate(c)) { sb.Append(c); continue; }
            var d = c.ToString().Normalize(NormalizationForm.FormD);
            sb.Append(d[0] < 128 ? d[0] : c);
        }
        return sb.ToString();
    }

    /// <summary>Hạ chữ thường + bỏ dấu (đã FormC để mỗi ký tự có dấu là 1 char).</summary>
    public static string ChuanHoa(string s) => ChuanHoaDoDai((s ?? "").Normalize(NormalizationForm.FormC));

    public static string GonKhoangTrang(string s)
    {
        var sb = new StringBuilder(s.Length);
        var truocLaCach = true;
        foreach (var c in s)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!truocLaCach) sb.Append(' ');
                truocLaCach = true;
            }
            else { sb.Append(c); truocLaCach = false; }
        }
        return sb.ToString().Trim();
    }

    /// <summary>Chuỗi chứa "từ" key (ranh giới là ký tự không phải chữ/số).</summary>
    public static bool ChuaTu(string text, string key)
    {
        if (key.Length == 0) return false;
        var i = 0;
        while ((i = text.IndexOf(key, i, StringComparison.Ordinal)) >= 0)
        {
            var truoc = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
            var j = i + key.Length;
            var sau = j >= text.Length || !char.IsLetterOrDigit(text[j]);
            if (truoc && sau) return true;
            i++;
        }
        return false;
    }
}
