using ChiTieu.Models;

namespace ChiTieu.Services;

public enum MucCanhBao
{
    BinhThuong = 0,
    /// <summary>Chạm 80% hạn mức.</summary>
    Vang = 1,
    /// <summary>Vượt 100% hạn mức.</summary>
    Do = 2
}

public class TinhTrangNganSach
{
    /// <summary>null = ngân sách tổng của tháng.</summary>
    public string? DanhMucId { get; init; }
    public long HanMuc { get; init; }
    public long DaChi { get; init; }
    public double TyLe => HanMuc <= 0 ? 0 : (double)DaChi / HanMuc;
    public long ConLai => HanMuc - DaChi;
    public MucCanhBao Muc { get; init; }
    /// <summary>Dự báo tổng chi cuối tháng nếu giữ tốc độ hiện tại (0 nếu không phải tháng đang chạy).</summary>
    public long DuBaoCuoiThang { get; init; }
    /// <summary>Dự báo sẽ vượt hạn mức bao nhiêu (0 nếu không vượt).</summary>
    public long DuBaoVuot { get; init; }
    public bool CoDuBaoVuot => DuBaoVuot > 0;
}

public class CanhBaoNganSach
{
    public MucCanhBao Muc { get; init; }
    /// <summary>true nếu là cảnh báo dự báo (vẫn chưa chạm 100%).</summary>
    public bool LaDuBao { get; init; }
    public string? DanhMucId { get; init; }
    public string TieuDe { get; init; } = "";
    public string NoiDung { get; init; } = "";
}

/// <summary>Tính toán ngân sách, cảnh báo và dự báo. Thuần logic, không đụng dữ liệu nên dễ test.</summary>
public class NganSachService
{
    public const double NguongVang = 0.8;
    public const double NguongDo = 1.0;
    /// <summary>Cần ít nhất ngần này ngày dữ liệu mới dự báo, tránh báo bậy ngày đầu tháng.</summary>
    public const int SoNgayToiThieuDuBao = 3;

    public static MucCanhBao XacDinhMuc(long daChi, long hanMuc)
    {
        if (hanMuc <= 0) return MucCanhBao.BinhThuong;
        var tyLe = (double)daChi / hanMuc;
        if (tyLe >= NguongDo + 1e-9 || daChi > hanMuc) return MucCanhBao.Do;
        if (tyLe >= NguongVang - 1e-9) return MucCanhBao.Vang;
        return MucCanhBao.BinhThuong;
    }

    /// <summary>Dự báo tổng chi cuối tháng theo tốc độ trung bình/ngày. Trả 0 nếu chưa đủ dữ liệu.</summary>
    public static long DuBaoCuoiThang(long daChi, DateOnly homNay, int nam, int thang)
    {
        if (homNay.Year != nam || homNay.Month != thang) return 0;
        if (homNay.Day < SoNgayToiThieuDuBao) return 0;
        var soNgay = DateTime.DaysInMonth(nam, thang);
        return (long)Math.Round((decimal)daChi / homNay.Day * soNgay, MidpointRounding.AwayFromZero);
    }

    public static long TongChi(IEnumerable<GiaoDich> gd, int nam, int thang, string? danhMucId = null) =>
        gd.Where(g => g.Loai == LoaiGiaoDich.Chi && g.Ngay.Year == nam && g.Ngay.Month == thang
                      && (danhMucId == null || g.DanhMucId == danhMucId))
          .Sum(g => g.SoTien);

    public TinhTrangNganSach TinhMot(NganSach ns, IEnumerable<GiaoDich> gd, DateOnly homNay, int nam, int thang)
    {
        var daChi = TongChi(gd, nam, thang, ns.DanhMucId);
        var duBao = DuBaoCuoiThang(daChi, homNay, nam, thang);
        var vuot = ns.HanMuc > 0 && daChi <= ns.HanMuc && duBao > ns.HanMuc ? duBao - ns.HanMuc : 0;
        return new TinhTrangNganSach
        {
            DanhMucId = ns.DanhMucId,
            HanMuc = ns.HanMuc,
            DaChi = daChi,
            Muc = XacDinhMuc(daChi, ns.HanMuc),
            DuBaoCuoiThang = duBao,
            DuBaoVuot = vuot
        };
    }

    public List<TinhTrangNganSach> TinhTatCa(IEnumerable<NganSach> nganSach, IEnumerable<GiaoDich> gd,
        DateOnly homNay, int nam, int thang)
    {
        var ds = gd as IList<GiaoDich> ?? gd.ToList();
        return nganSach.Where(n => n.HanMuc > 0)
            .Select(n => TinhMot(n, ds, homNay, nam, thang))
            .OrderBy(t => t.DanhMucId != null)          // ngân sách tổng lên đầu
            .ThenByDescending(t => t.TyLe)
            .ToList();
    }

    /// <summary>Danh sách cảnh báo của tháng; đỏ trước, vàng, rồi dự báo.</summary>
    public List<CanhBaoNganSach> TaoCanhBao(IEnumerable<TinhTrangNganSach> tinhTrang,
        Func<string?, string> tenDanhMuc)
    {
        var kq = new List<CanhBaoNganSach>();
        foreach (var t in tinhTrang)
        {
            var ten = t.DanhMucId == null ? "ngân sách tháng" : $"mục {tenDanhMuc(t.DanhMucId)}";
            if (t.Muc == MucCanhBao.Do)
            {
                kq.Add(new CanhBaoNganSach
                {
                    Muc = MucCanhBao.Do, DanhMucId = t.DanhMucId,
                    TieuDe = "Đã vượt ngân sách",
                    NoiDung = $"Bạn đã chi {DinhDang.Tien(t.DaChi)}, vượt {ten} {DinhDang.Tien(t.DaChi - t.HanMuc)}."
                });
            }
            else if (t.Muc == MucCanhBao.Vang)
            {
                kq.Add(new CanhBaoNganSach
                {
                    Muc = MucCanhBao.Vang, DanhMucId = t.DanhMucId,
                    TieuDe = "Sắp hết ngân sách",
                    NoiDung = $"Đã dùng {DinhDang.PhanTram(t.TyLe)} {ten}. Còn lại {DinhDang.Tien(t.ConLai)}."
                });
            }
            if (t.CoDuBaoVuot)
            {
                kq.Add(new CanhBaoNganSach
                {
                    Muc = MucCanhBao.Vang, LaDuBao = true, DanhMucId = t.DanhMucId,
                    TieuDe = "Dự báo sẽ vượt",
                    NoiDung = $"Với tốc độ này cuối tháng sẽ vượt {ten} {DinhDang.Tien(t.DuBaoVuot)}."
                });
            }
        }
        return kq.OrderByDescending(c => c.Muc).ThenBy(c => c.LaDuBao).ToList();
    }

    /// <summary>Cảnh báo liên quan đến một khoản chi vừa lưu: ngân sách tổng và ngân sách của danh mục đó.</summary>
    public List<CanhBaoNganSach> CanhBaoChoKhoanChi(IEnumerable<NganSach> nganSach, IEnumerable<GiaoDich> gd,
        GiaoDich vuaLuu, DateOnly homNay, Func<string?, string> tenDanhMuc)
    {
        if (vuaLuu.Loai != LoaiGiaoDich.Chi) return new();
        var lienQuan = nganSach.Where(n => n.DanhMucId == null || n.DanhMucId == vuaLuu.DanhMucId);
        var tt = TinhTatCa(lienQuan, gd, homNay, vuaLuu.Ngay.Year, vuaLuu.Ngay.Month);
        return TaoCanhBao(tt, tenDanhMuc);
    }
}
