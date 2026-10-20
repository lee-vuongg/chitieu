using ChiTieu.Models;

namespace ChiTieu.Services;

public class DongDanhMuc
{
    public string DanhMucId { get; init; } = "";
    public long SoTien { get; init; }
    public double TyLe { get; init; }
}

public class TongQuanThang
{
    public int Nam { get; init; }
    public int Thang { get; init; }
    public long Thu { get; init; }
    public long Chi { get; init; }
    public long ConLai => Thu - Chi;
    public List<DongDanhMuc> ChiTheoDanhMuc { get; init; } = new();
    /// <summary>Chi theo từng ngày trong tháng; chỉ số 0 = ngày 1.</summary>
    public long[] ChiTheoNgay { get; init; } = Array.Empty<long>();
    public long ChiThangTruoc { get; init; }
    /// <summary>% tăng/giảm so với tháng trước (0,12 = tăng 12%). null nếu tháng trước không có chi.</summary>
    public double? ThayDoiSoVoiThangTruoc { get; init; }
}

public class ThongKeService
{
    public TongQuanThang TongQuan(IEnumerable<GiaoDich> gd, int nam, int thang)
    {
        var ds = gd as IList<GiaoDich> ?? gd.ToList();
        var thangNay = ds.Where(g => g.Ngay.Year == nam && g.Ngay.Month == thang).ToList();
        var chi = thangNay.Where(g => g.Loai == LoaiGiaoDich.Chi).ToList();
        var tongChi = chi.Sum(g => g.SoTien);

        var theoDm = chi.GroupBy(g => g.DanhMucId)
            .Select(x => new DongDanhMuc
            {
                DanhMucId = x.Key,
                SoTien = x.Sum(g => g.SoTien),
                TyLe = tongChi == 0 ? 0 : (double)x.Sum(g => g.SoTien) / tongChi
            })
            .OrderByDescending(d => d.SoTien).ToList();

        var theoNgay = new long[DateTime.DaysInMonth(nam, thang)];
        foreach (var g in chi) theoNgay[g.Ngay.Day - 1] += g.SoTien;

        var truoc = new DateOnly(nam, thang, 1).AddMonths(-1);
        var chiTruoc = NganSachService.TongChi(ds, truoc.Year, truoc.Month);

        return new TongQuanThang
        {
            Nam = nam,
            Thang = thang,
            Thu = thangNay.Where(g => g.Loai == LoaiGiaoDich.Thu).Sum(g => g.SoTien),
            Chi = tongChi,
            ChiTheoDanhMuc = theoDm,
            ChiTheoNgay = theoNgay,
            ChiThangTruoc = chiTruoc,
            ThayDoiSoVoiThangTruoc = chiTruoc == 0 ? null : (double)(tongChi - chiTruoc) / chiTruoc
        };
    }

    /// <summary>Tổng chi trong khoảng ngày [tu, den], có thể lọc theo danh mục. Dùng cho hỏi đáp AI.</summary>
    public static long ChiTrongKhoang(IEnumerable<GiaoDich> gd, DateOnly tu, DateOnly den, string? danhMucId = null) =>
        gd.Where(g => g.Loai == LoaiGiaoDich.Chi && g.Ngay >= tu && g.Ngay <= den
                      && (danhMucId == null || g.DanhMucId == danhMucId))
          .Sum(g => g.SoTien);
}
