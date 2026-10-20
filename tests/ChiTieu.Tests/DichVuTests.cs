using ChiTieu.Models;
using ChiTieu.Services;
using Xunit;

namespace ChiTieu.Tests;

public class NganSachServiceTests
{
    // 21/10/2026: tháng 10 có 31 ngày -> dự báo = daChi / 21 * 31
    private static readonly DateOnly HomNay = new(2026, 10, 21);
    private readonly NganSachService _s = new();

    private static GiaoDich Chi(string dm, long tien, int ngay = 10, int thang = 10) =>
        new() { Loai = LoaiGiaoDich.Chi, DanhMucId = dm, SoTien = tien, Ngay = new DateOnly(2026, thang, ngay) };

    [Theory]
    [InlineData(0, 1000, MucCanhBao.BinhThuong)]
    [InlineData(799, 1000, MucCanhBao.BinhThuong)]
    [InlineData(800, 1000, MucCanhBao.Vang)]
    [InlineData(1000, 1000, MucCanhBao.Vang)]   // vừa chạm hạn mức: chưa gọi là vượt
    [InlineData(1001, 1000, MucCanhBao.Do)]
    [InlineData(500, 0, MucCanhBao.BinhThuong)] // không đặt hạn mức
    public void XacDinhMuc_TheoNguong(long daChi, long hanMuc, MucCanhBao mong)
    {
        Assert.Equal(mong, NganSachService.XacDinhMuc(daChi, hanMuc));
    }

    [Fact]
    public void DuBao_TheoTocDoTrungBinh()
    {
        Assert.Equal(3_100_000L, NganSachService.DuBaoCuoiThang(2_100_000, HomNay, 2026, 10));
    }

    [Fact]
    public void DuBao_BangKhong_KhiKhongPhaiThangDangChay()
    {
        Assert.Equal(0L, NganSachService.DuBaoCuoiThang(2_100_000, HomNay, 2026, 9));
    }

    [Fact]
    public void DuBao_BangKhong_KhiQuaItNgay()
    {
        Assert.Equal(0L, NganSachService.DuBaoCuoiThang(500_000, new DateOnly(2026, 10, 2), 2026, 10));
    }

    [Fact]
    public void TinhMot_CoDuBaoVuot_KhiChuaChamHanMuc()
    {
        var ns = new NganSach { HanMuc = 3_000_000 };
        var tt = _s.TinhMot(ns, new[] { Chi("an-uong", 2_100_000) }, HomNay, 2026, 10);
        Assert.Equal(MucCanhBao.BinhThuong, tt.Muc);
        Assert.Equal(3_100_000L, tt.DuBaoCuoiThang);
        Assert.Equal(100_000L, tt.DuBaoVuot);
        Assert.True(tt.CoDuBaoVuot);
    }

    [Fact]
    public void TinhMot_KhongDuBao_KhiDaVuot()
    {
        var ns = new NganSach { HanMuc = 1_000_000 };
        var tt = _s.TinhMot(ns, new[] { Chi("an-uong", 1_500_000) }, HomNay, 2026, 10);
        Assert.Equal(MucCanhBao.Do, tt.Muc);
        Assert.Equal(0L, tt.DuBaoVuot);
    }

    [Fact]
    public void TinhMot_ChiTinhDanhMucCuaNo()
    {
        var ns = new NganSach { DanhMucId = "an-uong", HanMuc = 1_000_000 };
        var gd = new[] { Chi("an-uong", 300_000), Chi("di-chuyen", 900_000), Chi("an-uong", 100_000, thang: 9) };
        var tt = _s.TinhMot(ns, gd, HomNay, 2026, 10);
        Assert.Equal(300_000L, tt.DaChi);
    }

    [Fact]
    public void TinhMot_KhongTinhKhoanThu()
    {
        var ns = new NganSach { HanMuc = 1_000_000 };
        var thu = new GiaoDich { Loai = LoaiGiaoDich.Thu, SoTien = 5_000_000, Ngay = new DateOnly(2026, 10, 5) };
        Assert.Equal(0L, _s.TinhMot(ns, new[] { thu }, HomNay, 2026, 10).DaChi);
    }

    [Fact]
    public void TaoCanhBao_BaMuc_DoTruocVangSauDo()
    {
        var tt = new List<TinhTrangNganSach>
        {
            new() { DanhMucId = "an-uong", HanMuc = 1000, DaChi = 850, Muc = MucCanhBao.Vang },
            new() { DanhMucId = "di-chuyen", HanMuc = 1000, DaChi = 1200, Muc = MucCanhBao.Do },
            new() { DanhMucId = null, HanMuc = 3_000_000, DaChi = 2_100_000, Muc = MucCanhBao.BinhThuong,
                    DuBaoCuoiThang = 3_100_000, DuBaoVuot = 100_000 },
        };
        var cb = _s.TaoCanhBao(tt, id => id ?? "tổng");
        Assert.Equal(3, cb.Count);
        Assert.Equal(MucCanhBao.Do, cb[0].Muc);
        Assert.Contains(cb, c => c.LaDuBao && c.NoiDung.Contains("cuối tháng sẽ vượt") && c.NoiDung.Contains("100.000 đ"));
        Assert.Contains(cb, c => c.Muc == MucCanhBao.Vang && !c.LaDuBao && c.NoiDung.Contains("85%"));
    }

    [Fact]
    public void CanhBaoChoKhoanChi_ChiLienQuanDanhMucVuaLuu()
    {
        var nganSach = new[]
        {
            new NganSach { DanhMucId = null, HanMuc = 10_000_000 },
            new NganSach { DanhMucId = "an-uong", HanMuc = 500_000 },
            new NganSach { DanhMucId = "di-chuyen", HanMuc = 100_000 },
        };
        var vuaLuu = Chi("an-uong", 150_000, 21);
        var gd = new[] { Chi("an-uong", 300_000), vuaLuu, Chi("di-chuyen", 400_000) };
        var cb = _s.CanhBaoChoKhoanChi(nganSach, gd, vuaLuu, HomNay, id => id ?? "tổng");
        Assert.Contains(cb, c => c.DanhMucId == "an-uong" && c.Muc == MucCanhBao.Vang && !c.LaDuBao);
        Assert.DoesNotContain(cb, c => c.DanhMucId == "di-chuyen");
    }

    [Fact]
    public void CanhBaoChoKhoanChi_KhoanThuKhongCanhBao()
    {
        var thu = new GiaoDich { Loai = LoaiGiaoDich.Thu, SoTien = 100, Ngay = HomNay };
        var cb = _s.CanhBaoChoKhoanChi(new[] { new NganSach { HanMuc = 1 } }, new[] { thu }, thu, HomNay, id => "x");
        Assert.Empty(cb);
    }

    [Fact]
    public void TinhTatCa_BoQuaHanMucKhong_VaTongLenDau()
    {
        var ds = new[]
        {
            new NganSach { DanhMucId = "an-uong", HanMuc = 100 },
            new NganSach { DanhMucId = "di-chuyen", HanMuc = 0 },
            new NganSach { DanhMucId = null, HanMuc = 1000 },
        };
        var kq = _s.TinhTatCa(ds, Array.Empty<GiaoDich>(), HomNay, 2026, 10);
        Assert.Equal(2, kq.Count);
        Assert.Null(kq[0].DanhMucId);
    }
}

public class MucTieuServiceTests
{
    private static readonly DateOnly HomNay = new(2026, 10, 21);
    private readonly MucTieuService _s = new();

    [Fact]
    public void SoThangConLai_LamTronLen()
    {
        Assert.Equal(6, MucTieuService.SoThangConLai(HomNay, new DateOnly(2027, 4, 21)));
        Assert.Equal(1, MucTieuService.SoThangConLai(HomNay, HomNay.AddDays(3)));
        Assert.Equal(0, MucTieuService.SoThangConLai(HomNay, HomNay));
    }

    [Fact]
    public void TinhTienDo_MoiThangCanDeDanh()
    {
        var m = new MucTieu { SoTienMucTieu = 6_000_000, Han = new DateOnly(2027, 4, 21) };
        m.LichSuNap.Add(new NapTien { SoTien = 1_200_000 });
        var td = _s.TinhTienDo(m, HomNay);
        Assert.Equal(1_200_000L, td.DaNap);
        Assert.Equal(4_800_000L, td.ConThieu);
        Assert.Equal(0.2, td.TyLe, 3);
        Assert.Equal(800_000L, td.CanMoiThang);
        Assert.False(td.DaDat);
        Assert.False(td.QuaHan);
    }

    [Fact]
    public void TinhTienDo_QuaHan_CanNapHetPhanThieu()
    {
        var m = new MucTieu { SoTienMucTieu = 1_000_000, Han = HomNay.AddDays(-5) };
        m.LichSuNap.Add(new NapTien { SoTien = 400_000 });
        var td = _s.TinhTienDo(m, HomNay);
        Assert.True(td.QuaHan);
        Assert.Equal(600_000L, td.CanMoiThang);
    }

    [Fact]
    public void NapThem_BaoDatMucTieuDungMotLan()
    {
        var m = new MucTieu { SoTienMucTieu = 1_000_000, Han = new DateOnly(2027, 1, 1) };
        Assert.False(_s.NapThem(m, 600_000, HomNay));
        Assert.True(_s.NapThem(m, 500_000, HomNay));   // vừa vượt mục tiêu -> chúc mừng
        Assert.False(_s.NapThem(m, 100_000, HomNay));  // nạp thêm nữa thì không chúc mừng lại
        var td = _s.TinhTienDo(m, HomNay);
        Assert.True(td.DaDat);
        Assert.Equal(1.0, td.TyLe, 3);
        Assert.Equal(0L, td.CanMoiThang);
    }

    [Fact]
    public void NapThem_SoTienKhongHopLe_NemLoi()
    {
        var m = new MucTieu { SoTienMucTieu = 1000 };
        Assert.Throws<ArgumentException>(() => _s.NapThem(m, 0, HomNay));
        Assert.Throws<ArgumentException>(() => _s.NapThem(m, -5, HomNay));
    }
}

public class ThongKeServiceTests
{
    private static GiaoDich G(LoaiGiaoDich l, string dm, long t, int thang, int ngay) =>
        new() { Loai = l, DanhMucId = dm, SoTien = t, Ngay = new DateOnly(2026, thang, ngay) };

    [Fact]
    public void TongQuan_TinhThuChiVaSoSanhThangTruoc()
    {
        var gd = new[]
        {
            G(LoaiGiaoDich.Chi, "an-uong", 100_000, 10, 3),
            G(LoaiGiaoDich.Chi, "di-chuyen", 300_000, 10, 5),
            G(LoaiGiaoDich.Thu, "luong", 1_000_000, 10, 1),
            G(LoaiGiaoDich.Chi, "an-uong", 200_000, 9, 15),
        };
        var t = new ThongKeService().TongQuan(gd, 2026, 10);
        Assert.Equal(1_000_000L, t.Thu);
        Assert.Equal(400_000L, t.Chi);
        Assert.Equal(600_000L, t.ConLai);
        Assert.Equal(31, t.ChiTheoNgay.Length);
        Assert.Equal(100_000L, t.ChiTheoNgay[2]);
        Assert.Equal(300_000L, t.ChiTheoNgay[4]);
        Assert.Equal("di-chuyen", t.ChiTheoDanhMuc[0].DanhMucId);
        Assert.Equal(0.75, t.ChiTheoDanhMuc[0].TyLe, 3);
        Assert.Equal(1.0, t.ThayDoiSoVoiThangTruoc!.Value, 3); // 200k -> 400k = +100%
    }

    [Fact]
    public void TongQuan_KhongCoThangTruoc_ThayDoiLaNull()
    {
        var t = new ThongKeService().TongQuan(new[] { G(LoaiGiaoDich.Chi, "an-uong", 100, 10, 3) }, 2026, 10);
        Assert.Null(t.ThayDoiSoVoiThangTruoc);
    }

    [Fact]
    public void TongQuan_ThangRong()
    {
        var t = new ThongKeService().TongQuan(Array.Empty<GiaoDich>(), 2026, 2);
        Assert.Equal(0L, t.Chi);
        Assert.Equal(28, t.ChiTheoNgay.Length);
        Assert.Empty(t.ChiTheoDanhMuc);
    }

    [Fact]
    public void ChiTrongKhoang_LocTheoDanhMuc()
    {
        var gd = new[]
        {
            G(LoaiGiaoDich.Chi, "di-chuyen", 80_000, 10, 12),
            G(LoaiGiaoDich.Chi, "di-chuyen", 70_000, 10, 14),
            G(LoaiGiaoDich.Chi, "an-uong", 50_000, 10, 13),
            G(LoaiGiaoDich.Chi, "di-chuyen", 99_000, 10, 20),
        };
        var tong = ThongKeService.ChiTrongKhoang(gd, new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 18), "di-chuyen");
        Assert.Equal(150_000L, tong);
    }
}

public class BackupCoreTests
{
    private static BackupData Mau() => new()
    {
        GiaoDich = { new GiaoDich { SoTien = 50_000, GhiChu = "Ăn phở", DanhMucId = "an-uong", Ngay = new DateOnly(2026, 10, 1) } },
        DanhMuc = DanhMucMacDinh.TaoMoi(),
        NganSach = { new NganSach { DanhMucId = "an-uong", HanMuc = 1_000_000 } },
        MucTieu = { new MucTieu { Ten = "Du lịch", SoTienMucTieu = 5_000_000 } },
    };

    [Fact]
    public void XuatRoiDoc_GiuNguyenDuLieu()
    {
        var goc = Mau();
        var doc = BackupCore.DocJson(BackupCore.XuatJson(goc));
        Assert.Equal(goc.GiaoDich[0].Id, doc.GiaoDich[0].Id);
        Assert.Equal("Ăn phở", doc.GiaoDich[0].GhiChu);
        Assert.Equal(new DateOnly(2026, 10, 1), doc.GiaoDich[0].Ngay);
        Assert.Equal(LoaiGiaoDich.Chi, doc.GiaoDich[0].Loai);
        Assert.Equal(goc.DanhMuc.Count, doc.DanhMuc.Count);
        Assert.Equal("Du lịch", doc.MucTieu[0].Ten);
    }

    [Theory]
    [InlineData("không phải json")]
    [InlineData("null")]
    public void DocJson_FileHong_NemLoiThanThien(string noiDung)
    {
        Assert.Throws<InvalidDataException>(() => BackupCore.DocJson(noiDung));
    }

    [Fact]
    public void DocJson_PhienBanMoiHon_NemLoi()
    {
        var d = Mau(); d.Version = PhienBan.HienTai + 1;
        Assert.Throws<InvalidDataException>(() => BackupCore.DocJson(BackupCore.XuatJson(d)));
    }

    [Fact]
    public void DocJson_GiaoDichSaiSoTien_NemLoi()
    {
        var d = Mau(); d.GiaoDich[0].SoTien = 0;
        Assert.Throws<InvalidDataException>(() => BackupCore.DocJson(BackupCore.XuatJson(d)));
    }

    [Fact]
    public void Gop_KhongNhanDoi_GhiDe_DungDuLieuFile()
    {
        var hienCo = Mau();
        var file = new BackupData();
        file.GiaoDich.Add(hienCo.GiaoDich[0]);                       // trùng Id
        file.GiaoDich.Add(new GiaoDich { SoTien = 70_000 });         // mới
        var gop = BackupCore.GopHoacGhiDe(hienCo, file, CheDoNhap.Gop);
        Assert.Equal(2, gop.GiaoDich.Count);
        Assert.Single(gop.NganSach);                                  // giữ nguyên

        var ghiDe = BackupCore.GopHoacGhiDe(hienCo, file, CheDoNhap.GhiDe);
        Assert.Same(file, ghiDe);
    }

    [Fact]
    public void Gop_KhongTaoHaiNganSachChoCungDanhMuc()
    {
        var hienCo = Mau();
        var file = new BackupData();
        file.NganSach.Add(new NganSach { DanhMucId = "an-uong", HanMuc = 9 });
        var gop = BackupCore.GopHoacGhiDe(hienCo, file, CheDoNhap.Gop);
        Assert.Single(gop.NganSach);
        Assert.Equal(1_000_000L, gop.NganSach[0].HanMuc);
    }

    [Fact]
    public void CanNhacBackup_Sau14Ngay()
    {
        var bayGio = new DateTime(2026, 10, 21);
        Assert.True(BackupCore.CanNhacBackup(null, bayGio, true));
        Assert.True(BackupCore.CanNhacBackup(bayGio.AddDays(-15), bayGio, true));
        Assert.False(BackupCore.CanNhacBackup(bayGio.AddDays(-14), bayGio, true));
        Assert.False(BackupCore.CanNhacBackup(bayGio.AddDays(-1), bayGio, true));
        Assert.False(BackupCore.CanNhacBackup(null, bayGio, false)); // chưa có dữ liệu thì khỏi nhắc
    }
}

public class DinhDangTests
{
    [Theory]
    [InlineData(1_250_000L, "1.250.000 đ")]
    [InlineData(0L, "0 đ")]
    [InlineData(999L, "999 đ")]
    [InlineData(-50_000L, "-50.000 đ")]
    public void Tien(long so, string mong) => Assert.Equal(mong, DinhDang.Tien(so));

    [Theory]
    [InlineData(1_200_000L, "1,2tr")]
    [InlineData(2_000_000L, "2tr")]
    [InlineData(350_000L, "350k")]
    [InlineData(500L, "500")]
    public void TienRutGon(long so, string mong) => Assert.Equal(mong, DinhDang.TienRutGon(so));

    [Fact]
    public void Ngay_DinhDangDdMMyyyy() => Assert.Equal("05/10/2026", DinhDang.Ngay(new DateOnly(2026, 10, 5)));

    [Fact]
    public void NhanNgay_HomNayHomQua()
    {
        var hn = new DateOnly(2026, 10, 21);
        Assert.Equal("Hôm nay", DinhDang.NhanNgay(hn, hn));
        Assert.Equal("Hôm qua", DinhDang.NhanNgay(hn.AddDays(-1), hn));
        Assert.Equal("Thứ hai, 19/10/2026", DinhDang.NhanNgay(new DateOnly(2026, 10, 19), hn));
    }
}

public class AiCoreTests
{
    [Fact]
    public void TomTat_KhongChuaGhiChuHayIdGiaoDich()
    {
        var gd = new[]
        {
            new GiaoDich { SoTien = 50_000, GhiChu = "BÍ MẬT-khám bệnh ông X", DanhMucId = "an-uong", Ngay = new DateOnly(2026, 10, 3) }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(
            AiCore.TomTatTheoDanhMuc(gd, DanhMucMacDinh.TaoMoi(), new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)),
            JsonCfg.Opt);
        Assert.DoesNotContain("BÍ MẬT", json);
        Assert.DoesNotContain(gd[0].Id.ToString(), json);
        Assert.Contains("Ăn uống", json);
        Assert.Contains("50000", json);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(null)]
    public void MaLoi_CoThongDiepTiengViet(int? status)
    {
        Assert.False(string.IsNullOrWhiteSpace(AiCore.TaoMaLoiThanThien(status)));
    }

    [Theory]
    [InlineData("https://abc.workers.dev", true)]
    [InlineData("http://abc.example.com", false)]
    [InlineData("không phải url", false)]
    [InlineData("", false)]
    public void UrlHopLe(string url, bool mong) => Assert.Equal(mong, AiCore.UrlHopLe(url));

    [Fact]
    public void DocKetQuaParse_JsonHong_TraNull()
    {
        Assert.Null(AiCore.DocKetQuaParse("{hỏng"));
        Assert.Equal(50_000L, AiCore.DocKetQuaParse("{\"soTien\":50000,\"danhMucId\":\"an-uong\"}")!.SoTien);
    }
}
