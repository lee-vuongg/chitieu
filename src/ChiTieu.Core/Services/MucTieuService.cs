using ChiTieu.Models;

namespace ChiTieu.Services;

public class TienDoMucTieu
{
    public long DaNap { get; init; }
    public long ConThieu { get; init; }
    /// <summary>0..1 (đã kẹp).</summary>
    public double TyLe { get; init; }
    public bool DaDat { get; init; }
    public bool QuaHan { get; init; }
    public int SoThangConLai { get; init; }
    /// <summary>Mỗi tháng cần để dành bao nhiêu để kịp hạn.</summary>
    public long CanMoiThang { get; init; }
}

public class MucTieuService
{
    public static long DaNap(MucTieu m) => m.LichSuNap.Sum(n => n.SoTien);

    /// <summary>Số tháng còn lại, làm tròn lên, tối thiểu 1 nếu chưa quá hạn.</summary>
    public static int SoThangConLai(DateOnly homNay, DateOnly han)
    {
        if (han <= homNay) return 0;
        var ngay = han.DayNumber - homNay.DayNumber;
        return Math.Max(1, (int)Math.Ceiling(ngay / 30.4375));
    }

    public TienDoMucTieu TinhTienDo(MucTieu m, DateOnly homNay)
    {
        var daNap = DaNap(m);
        var conThieu = Math.Max(0, m.SoTienMucTieu - daNap);
        var daDat = m.SoTienMucTieu > 0 && daNap >= m.SoTienMucTieu;
        var thang = SoThangConLai(homNay, m.Han);
        long can;
        if (daDat) can = 0;
        else if (thang == 0) can = conThieu; // quá hạn: cần nạp hết phần còn thiếu
        else can = (long)Math.Ceiling((decimal)conThieu / thang);
        return new TienDoMucTieu
        {
            DaNap = daNap,
            ConThieu = conThieu,
            TyLe = m.SoTienMucTieu <= 0 ? 0 : Math.Clamp((double)daNap / m.SoTienMucTieu, 0, 1),
            DaDat = daDat,
            QuaHan = !daDat && m.Han < homNay,
            SoThangConLai = thang,
            CanMoiThang = can
        };
    }

    /// <summary>Nạp thêm tiền. Trả true nếu lần nạp này vừa làm đạt mục tiêu (để bắn confetti một lần).</summary>
    public bool NapThem(MucTieu m, long soTien, DateOnly ngay, string ghiChu = "")
    {
        if (soTien <= 0) throw new ArgumentException("Số tiền nạp phải lớn hơn 0.", nameof(soTien));
        var truoc = DaNap(m) >= m.SoTienMucTieu;
        m.LichSuNap.Add(new NapTien { SoTien = soTien, Ngay = ngay, GhiChu = ghiChu });
        var sau = DaNap(m) >= m.SoTienMucTieu;
        var vuaDat = !truoc && sau && !m.DaChucMung;
        if (vuaDat) m.DaChucMung = true;
        return vuaDat;
    }
}
