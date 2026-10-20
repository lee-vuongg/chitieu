using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChiTieu.Models;

namespace ChiTieu.Services;

public class KetQuaParse
{
    public LoaiGiaoDich Loai { get; init; } = LoaiGiaoDich.Chi;
    /// <summary>null nếu không tìm thấy số tiền.</summary>
    public long? SoTien { get; init; }
    public DateOnly Ngay { get; init; }
    public string GhiChu { get; init; } = "";
    public string DanhMucId { get; init; } = DanhMucMacDinh.KhacChi;
    /// <summary>0..1. Dưới 0,7 nghĩa là nên nhờ AI (nếu có) hoặc cho chọn tay.</summary>
    public double DoTinCay { get; init; }
    public bool TimThayDanhMuc { get; init; }
    public bool TimThayNgay { get; init; }
}

/// <summary>
/// Parser tiếng Việt chạy hoàn toàn offline.
/// Mọi so khớp làm trên bản "không dấu, chữ thường, GIỮ NGUYÊN độ dài" nên vị trí
/// trùng với chuỗi gốc, nhờ đó cắt được số tiền/ngày ra khỏi ghi chú mà vẫn giữ dấu.
/// </summary>
public class ParserService
{
    public const double NguongTinCay = 0.7;

    private const string MauSo = @"(?<so>\d{1,3}(?:[.,]\d{3})+|\d+(?:[.,]\d+)?)";

    // số + đơn vị (+ phần lẻ dính liền "1tr2" hoặc "rưỡi")
    private static readonly Regex RxDonVi = new(
        @"(?<![a-z0-9.,])" + MauSo +
        @"\s*(?<dv>trieu|tr|cu|nghin|ngan|ty|k|vnd|dong|d)(?![a-z])" +
        @"(?:(?<le>\d{1,3})(?!\d)|\s*(?<ruoi>ruoi)(?![a-z]))?",
        RegexOptions.Compiled);

    private static readonly Regex RxSoTran = new(
        @"(?<![a-z0-9.,/])" + MauSo + @"(?![a-z0-9/])", RegexOptions.Compiled);

    private static readonly Regex RxNgayThang = new(
        @"(?<![\d/])(\d{1,2})/(\d{1,2})(?:/(\d{4}|\d{2}))?(?![\d/])", RegexOptions.Compiled);

    private static readonly Regex RxKia = new(@"\bhom\s+kia\b", RegexOptions.Compiled);
    private static readonly Regex RxQua = new(@"\b(?:hom\s+qua|(?:toi|chieu|sang|trua|dem)\s+qua)\b", RegexOptions.Compiled);
    private static readonly Regex RxNay = new(@"\b(?:hom\s+nay|(?:sang|trua|chieu|toi|dem)\s+nay)\b", RegexOptions.Compiled);

    private static readonly Regex RxThu = new(
        @"\b(?:thu\s*(?<t>hai|ba|tu|nam|sau|bay|[2-7])|chu\s*nhat|cn)\b" +
        @"(?!\s*(?:k|tr|trieu|nghin|ngan|cu|ty|d|vnd|dong|ruoi)\b)" +
        @"(?:\s+tuan\s+(?<w>truoc|roi|qua|nay))?",
        RegexOptions.Compiled);

    private static readonly Regex RxTuanTruoc = new(@"\btuan\s+(?:truoc|roi|qua)\b", RegexOptions.Compiled);
    private static readonly Regex RxNgayD = new(@"\bngay\s+(\d{1,2})\b(?!\s*/)", RegexOptions.Compiled);

    private static readonly Regex RxGoiYThu = new(
        @"^thu\b|\b(?:nhan|duoc\s+cho|duoc\s+tang|hoan\s+tien)\b", RegexOptions.Compiled);

    /// <summary>Vùng làm việc: chuỗi không dấu + mặt nạ các đoạn đã bị cắt.</summary>
    private sealed class Vung
    {
        public readonly char[] LamViec;
        public readonly bool[] DaXoa;
        public Vung(string ascii) { LamViec = ascii.ToCharArray(); DaXoa = new bool[ascii.Length]; }
        public string Chuoi() => new(LamViec);
        public void Xoa(int bd, int dai)
        {
            for (var i = bd; i < bd + dai && i < LamViec.Length; i++) { LamViec[i] = ' '; DaXoa[i] = true; }
        }
    }

    public KetQuaParse Parse(string? text, DateOnly homNay,
        IReadOnlyList<DanhMuc>? danhMuc = null,
        IReadOnlyDictionary<string, string>? daHoc = null)
    {
        danhMuc ??= DanhMucMacDinh.TatCa;
        var goc = (text ?? "").Normalize(NormalizationForm.FormC);
        var vung = new Vung(VanBan.ChuanHoaDoDai(goc));

        var ngay = TimNgay(vung, homNay, out var coNgay);
        var tien = TimTien(vung, out var diemTien, out var nhieuNhom);

        var ghiChuAscii = VanBan.GonKhoangTrang(vung.Chuoi());
        var ghiChu = DungGhiChu(goc, vung.DaXoa);

        var (dm, diemDm) = DoanDanhMuc(ghiChuAscii, danhMuc, daHoc);
        LoaiGiaoDich loai;
        string dmId;
        double diemCuoiDm;
        var timThayDm = dm != null;
        if (dm != null)
        {
            loai = dm.Loai; dmId = dm.Id; diemCuoiDm = diemDm;
        }
        else if (RxGoiYThu.IsMatch(ghiChuAscii))
        {
            loai = LoaiGiaoDich.Thu; dmId = DanhMucMacDinh.KhacThu; diemCuoiDm = 0.3;
        }
        else
        {
            loai = LoaiGiaoDich.Chi; dmId = DanhMucMacDinh.KhacChi; diemCuoiDm = 0.1;
        }

        double tinCay = 0;
        if (tien.HasValue)
        {
            tinCay = diemTien + diemCuoiDm;
            if (nhieuNhom) tinCay -= 0.2;
            tinCay = Math.Clamp(tinCay, 0, 1);
        }

        return new KetQuaParse
        {
            Loai = loai,
            SoTien = tien,
            Ngay = ngay,
            GhiChu = ghiChu,
            DanhMucId = dmId,
            DoTinCay = Math.Round(tinCay, 3),
            TimThayDanhMuc = timThayDm,
            TimThayNgay = coNgay
        };
    }

    // ------------------------------------------------------------------ NGÀY

    private static DateOnly TimNgay(Vung v, DateOnly homNay, out bool co)
    {
        co = false;
        var s = v.Chuoi();

        // dd/MM hoặc dd/MM/yyyy
        for (var m = RxNgayThang.Match(s); m.Success; m = m.NextMatch())
        {
            var d = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            var mo = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            var coNam = m.Groups[3].Success;
            var y = homNay.Year;
            if (coNam)
            {
                y = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                if (y < 100) y += 2000;
            }
            if (mo < 1 || mo > 12 || y < 1 || y > 9999 || d < 1 || d > DateTime.DaysInMonth(y, mo)) continue;
            var dt = new DateOnly(y, mo, d);
            // Không ghi năm mà ngày rơi vào tương lai -> hiểu là năm trước
            if (!coNam && dt > homNay && d <= DateTime.DaysInMonth(y - 1, mo)) dt = new DateOnly(y - 1, mo, d);
            v.Xoa(m.Index, m.Length);
            co = true;
            return dt;
        }

        var kq = homNay;
        Match mm;
        if ((mm = RxKia.Match(s)).Success) { v.Xoa(mm.Index, mm.Length); co = true; return homNay.AddDays(-2); }
        if ((mm = RxQua.Match(s)).Success) { v.Xoa(mm.Index, mm.Length); co = true; return homNay.AddDays(-1); }
        if ((mm = RxNay.Match(s)).Success) { v.Xoa(mm.Index, mm.Length); co = true; return homNay; }

        if ((mm = RxThu.Match(s)).Success)
        {
            var idx = 6; // chủ nhật
            if (mm.Groups["t"].Success)
            {
                idx = mm.Groups["t"].Value switch
                {
                    "hai" or "2" => 0,
                    "ba" or "3" => 1,
                    "tu" or "4" => 2,
                    "nam" or "5" => 3,
                    "sau" or "6" => 4,
                    "bay" or "7" => 5,
                    _ => 0
                };
            }
            var idxNay = ((int)homNay.DayOfWeek + 6) % 7; // thứ hai = 0
            var dauTuan = homNay.AddDays(-idxNay);
            if (mm.Groups["w"].Success)
            {
                kq = mm.Groups["w"].Value == "nay" ? dauTuan.AddDays(idx) : dauTuan.AddDays(-7 + idx);
            }
            else
            {
                // Không nói tuần nào: lấy lần gần nhất không vượt quá hôm nay
                kq = homNay.AddDays(-((idxNay - idx + 7) % 7));
            }
            v.Xoa(mm.Index, mm.Length);
            co = true;
            return kq;
        }

        if ((mm = RxTuanTruoc.Match(s)).Success) { v.Xoa(mm.Index, mm.Length); co = true; return homNay.AddDays(-7); }

        if ((mm = RxNgayD.Match(s)).Success)
        {
            var d = int.Parse(mm.Groups[1].Value, CultureInfo.InvariantCulture);
            var dt = ThuNgayTrongThang(homNay, d);
            if (dt != null) { v.Xoa(mm.Index, mm.Length); co = true; return dt.Value; }
        }

        return homNay;
    }

    /// <summary>"ngày 15" -> ngày 15 của tháng này, nếu còn ở tương lai thì lấy tháng trước.</summary>
    private static DateOnly? ThuNgayTrongThang(DateOnly homNay, int d)
    {
        if (d < 1 || d > 31) return null;
        var y = homNay.Year; var m = homNay.Month;
        if (d > DateTime.DaysInMonth(y, m)) return null;
        var dt = new DateOnly(y, m, d);
        if (dt > homNay)
        {
            var truoc = homNay.AddMonths(-1);
            if (d > DateTime.DaysInMonth(truoc.Year, truoc.Month)) return null;
            dt = new DateOnly(truoc.Year, truoc.Month, d);
        }
        return dt;
    }

    // ------------------------------------------------------------------ TIỀN

    private static long Boi(string dv) => dv switch
    {
        "trieu" or "tr" or "cu" => 1_000_000,
        "nghin" or "ngan" or "k" => 1000,
        "ty" => 1_000_000_000,
        _ => 1 // d, vnd, dong
    };

    private static bool ThuSo(string t, out decimal kq)
    {
        var s = Regex.IsMatch(t, @"^\d{1,3}([.,]\d{3})+$")
            ? t.Replace(".", "").Replace(",", "")   // 1.250.000 -> dấu phân cách hàng nghìn
            : t.Replace(',', '.');                  // 1,5 -> số thập phân
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out kq);
    }

    private sealed class Doan
    {
        public int Bd, Kt;
        public long GiaTri;
        public long BoiCuoi;
    }

    private static long? TimTien(Vung v, out double diem, out bool nhieuNhom)
    {
        diem = 0; nhieuNhom = false;
        var s = v.Chuoi();

        var doan = new List<Doan>();
        foreach (Match m in RxDonVi.Matches(s))
        {
            var dv = m.Groups["dv"].Value;
            // "củ" là triệu, nhưng "2 củ cải" thì không phải tiền
            if (dv == "cu")
            {
                var conLai = s[(m.Index + m.Length)..].TrimStart();
                if (conLai.Length > 0 && char.IsLetter(conLai[0]) && !conLai.StartsWith("ruoi", StringComparison.Ordinal)) continue;
            }
            if (!ThuSo(m.Groups["so"].Value, out var so)) continue;
            var boi = Boi(dv);
            var gt = so * boi;
            if (boi > 1)
            {
                if (m.Groups["le"].Success)
                {
                    var le = m.Groups["le"].Value;
                    var mau = 1m;
                    for (var i = 0; i < le.Length; i++) mau *= 10;
                    gt += boi * decimal.Parse(le, CultureInfo.InvariantCulture) / mau; // 1tr2 = 1,2 triệu
                }
                if (m.Groups["ruoi"].Success) gt += boi / 2m;                           // 2 triệu rưỡi
            }
            if (gt <= 0 || gt > 1_000_000_000_000m) continue;
            doan.Add(new Doan
            {
                Bd = m.Index,
                Kt = m.Index + m.Length,
                GiaTri = (long)Math.Round(gt, MidpointRounding.AwayFromZero),
                BoiCuoi = boi
            });
        }

        if (doan.Count > 0)
        {
            // Gộp các đoạn liền kề giảm dần: "2 triệu 500 nghìn"
            var nhom = new List<Doan>();
            var cur = doan[0];
            for (var i = 1; i < doan.Count; i++)
            {
                var ke = doan[i];
                var giua = s.Substring(cur.Kt, ke.Bd - cur.Kt);
                if (string.IsNullOrWhiteSpace(giua) && ke.BoiCuoi < cur.BoiCuoi)
                {
                    cur.GiaTri += ke.GiaTri; cur.Kt = ke.Kt; cur.BoiCuoi = ke.BoiCuoi;
                }
                else { nhom.Add(cur); cur = ke; }
            }
            nhom.Add(cur);

            var chon = nhom[0];
            nhieuNhom = nhom.Count > 1;
            v.Xoa(chon.Bd, chon.Kt - chon.Bd);
            diem = 0.5;
            return chon.GiaTri;
        }

        // Không có đơn vị: số trần
        Match? tot = null; decimal totGt = 0;
        foreach (Match m in RxSoTran.Matches(s))
        {
            if (!ThuSo(m.Groups["so"].Value, out var gt) || gt <= 0 || gt > 1_000_000_000_000m) continue;
            if (tot == null || gt > totGt) { tot = m; totGt = gt; }
        }
        if (tot == null) return null;

        v.Xoa(tot.Index, tot.Length);
        if (totGt >= 1000)
        {
            diem = 0.45;
            return (long)Math.Round(totGt, MidpointRounding.AwayFromZero);
        }
        diem = 0.15; // "phở 50" -> hiểu là 50 nghìn nhưng không chắc, để độ tin cậy thấp
        return (long)Math.Round(totGt * 1000, MidpointRounding.AwayFromZero);
    }

    // -------------------------------------------------------------- GHI CHÚ

    private static string DungGhiChu(string goc, bool[] daXoa)
    {
        var sb = new StringBuilder(goc.Length);
        for (var i = 0; i < goc.Length; i++) sb.Append(daXoa[i] ? ' ' : goc[i]);
        var s = VanBan.GonKhoangTrang(sb.ToString()).Trim(' ', ',', '.', ';', ':', '-', '–', '+');
        s = VanBan.GonKhoangTrang(s);
        if (s.Length == 0) return "";
        return char.ToUpperInvariant(s[0]) + s[1..];
    }

    // ------------------------------------------------------------- DANH MỤC

    /// <summary>Đoán danh mục từ ghi chú: ưu tiên từ khóa đã học, sau đó bảng từ khóa (khớp dài nhất thắng).</summary>
    public (DanhMuc? DanhMuc, double DiemTinCay) DoanDanhMuc(string ghiChu,
        IReadOnlyList<DanhMuc>? danhMuc = null,
        IReadOnlyDictionary<string, string>? daHoc = null)
    {
        danhMuc ??= DanhMucMacDinh.TatCa;
        var a = VanBan.GonKhoangTrang(VanBan.ChuanHoa(ghiChu));
        if (a.Length == 0) return (null, 0);

        if (daHoc != null)
        {
            DanhMuc? tot = null; var dai = 0;
            foreach (var kv in daHoc)
            {
                var khoa = VanBan.GonKhoangTrang(VanBan.ChuanHoa(kv.Key));
                if (khoa.Length <= dai || !VanBan.ChuaTu(a, khoa)) continue;
                var dm = danhMuc.FirstOrDefault(x => x.Id == kv.Value);
                if (dm == null) continue;
                tot = dm; dai = khoa.Length;
            }
            if (tot != null) return (tot, 0.5);
        }

        DanhMuc? chon = null; var daiNhat = 0; var nhieuTu = false;
        foreach (var dm in danhMuc)
        {
            foreach (var kw in dm.TuKhoa)
            {
                var k = VanBan.GonKhoangTrang(VanBan.ChuanHoa(kw));
                if (k.Length <= daiNhat || !VanBan.ChuaTu(a, k)) continue;
                chon = dm; daiNhat = k.Length; nhieuTu = k.Contains(' ');
            }
        }
        if (chon == null) return (null, 0);
        return (chon, nhieuTu ? 0.475 : 0.45);
    }

    /// <summary>Khóa dùng để "học" danh mục: tối đa 3 từ đầu của ghi chú, không dấu.</summary>
    public static string TrichKhoaHoc(string ghiChu)
    {
        var a = VanBan.ChuanHoa(ghiChu);
        var sb = new StringBuilder(a.Length);
        foreach (var c in a) sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        var tu = VanBan.GonKhoangTrang(sb.ToString()).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', tu.Take(3));
    }
}
