using ChiTieu.Models;
using ChiTieu.Services;
using Xunit;

namespace ChiTieu.Tests;

public class ParserServiceTests
{
    // Thứ tư 21/10/2026 — cố định để test ngày tương đối không bị lệch theo ngày chạy.
    private static readonly DateOnly HomNay = new(2026, 10, 21);
    private readonly ParserService _p = new();

    private KetQuaParse P(string s) => _p.Parse(s, HomNay);

    [Theory]
    [InlineData("ăn phở 50k", 50_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("đổ xăng 80 nghìn", 80_000L, LoaiGiaoDich.Chi, "di-chuyen")]
    [InlineData("lương 10tr", 10_000_000L, LoaiGiaoDich.Thu, "luong")]
    [InlineData("hôm qua cà phê 35k", 35_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("mua quà 1,5 triệu", 1_500_000L, LoaiGiaoDich.Chi, "qua-tang")]
    [InlineData("ăn tối 1.250.000", 1_250_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("tiền điện 650.000", 650_000L, LoaiGiaoDich.Chi, "hoa-don")]
    [InlineData("lương 1 củ rưỡi", 1_500_000L, LoaiGiaoDich.Thu, "luong")]
    [InlineData("xem phim 120k", 120_000L, LoaiGiaoDich.Chi, "giai-tri")]
    [InlineData("bác sĩ 300k", 300_000L, LoaiGiaoDich.Chi, "suc-khoe")]
    [InlineData("grab 45k", 45_000L, LoaiGiaoDich.Chi, "di-chuyen")]
    [InlineData("mua quần áo 400k", 400_000L, LoaiGiaoDich.Chi, "mua-sam")]
    [InlineData("học phí 2tr", 2_000_000L, LoaiGiaoDich.Chi, "hoc-tap")]
    [InlineData("mừng tuổi 500k", 500_000L, LoaiGiaoDich.Thu, "thu-khac")]
    [InlineData("mẹ cho 200k", 200_000L, LoaiGiaoDich.Thu, "thu-khac")]
    [InlineData("thưởng tết 5tr", 5_000_000L, LoaiGiaoDich.Thu, "thuong")]
    [InlineData("lãi tiết kiệm 300k", 300_000L, LoaiGiaoDich.Thu, "thu-khac")]
    [InlineData("thu tiền nợ 2tr", 2_000_000L, LoaiGiaoDich.Thu, "thu-khac")]
    [InlineData("2 triệu 500 nghìn tiền nhà", 2_500_000L, LoaiGiaoDich.Chi, "hoa-don")]
    [InlineData("1tr2 tiền điện", 1_200_000L, LoaiGiaoDich.Chi, "hoa-don")]
    [InlineData("mua bia 2 thùng 300k", 300_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("tiền thuê nhà 5tr", 5_000_000L, LoaiGiaoDich.Chi, "hoa-don")]
    [InlineData("tiền điện thoại 200k", 200_000L, LoaiGiaoDich.Chi, "hoa-don")]
    [InlineData("đi chợ 120k", 120_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("ăn sáng 45k", 45_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("Ăn phở 50K", 50_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("an pho 50k", 50_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("50k phở", 50_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("đổ xăng 80.000đ", 80_000L, LoaiGiaoDich.Chi, "di-chuyen")]
    [InlineData("lương 10.000.000", 10_000_000L, LoaiGiaoDich.Thu, "luong")]
    [InlineData("cafe 25k", 25_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("đi taxi 150k", 150_000L, LoaiGiaoDich.Chi, "di-chuyen")]
    [InlineData("nhậu 500k", 500_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("tiền nước 90k", 90_000L, LoaiGiaoDich.Chi, "hoa-don")]
    [InlineData("tiền xăng 100 nghìn", 100_000L, LoaiGiaoDich.Chi, "di-chuyen")]
    [InlineData("gửi xe 5k", 5_000L, LoaiGiaoDich.Chi, "di-chuyen")]
    [InlineData("mua thuốc 85k", 85_000L, LoaiGiaoDich.Chi, "suc-khoe")]
    [InlineData("cắt tóc 60k", 60_000L, LoaiGiaoDich.Chi, "suc-khoe")]
    [InlineData("bán đồ cũ 1tr", 1_000_000L, LoaiGiaoDich.Thu, "thu-khac")]
    [InlineData("tiền lãi 150k", 150_000L, LoaiGiaoDich.Thu, "thu-khac")]
    [InlineData("nạp điện thoại 100k", 100_000L, LoaiGiaoDich.Chi, "hoa-don")]
    [InlineData("đóng phí chung cư 1tr2", 1_200_000L, LoaiGiaoDich.Chi, "hoa-don")]
    [InlineData("đi karaoke 350k", 350_000L, LoaiGiaoDich.Chi, "giai-tri")]
    [InlineData("sữa chua 20k", 20_000L, LoaiGiaoDich.Chi, "an-uong")]
    [InlineData("mua sách 150k", 150_000L, LoaiGiaoDich.Chi, "hoc-tap")]
    public void Parse_TachTienLoaiVaDanhMuc(string cau, long soTien, LoaiGiaoDich loai, string danhMucId)
    {
        var kq = P(cau);
        Assert.Equal(soTien, kq.SoTien);
        Assert.Equal(loai, kq.Loai);
        Assert.Equal(danhMucId, kq.DanhMucId);
    }

    [Theory]
    [InlineData("ăn phở 50k", 2026, 10, 21, false)]
    [InlineData("hôm nay ăn phở 50k", 2026, 10, 21, true)]
    [InlineData("hôm qua ăn phở 50k", 2026, 10, 20, true)]
    [InlineData("hôm kia ăn phở 50k", 2026, 10, 19, true)]
    [InlineData("15/10 điện nước 800k", 2026, 10, 15, true)]
    [InlineData("20/10/2026 xăng 80k", 2026, 10, 20, true)]
    [InlineData("5/9/25 ăn 30k", 2025, 9, 5, true)]
    [InlineData("25/12 quà 1tr", 2025, 12, 25, true)]   // chưa tới ngày trong năm nay -> hiểu là năm trước
    [InlineData("thứ hai tuần trước ăn trưa 45k", 2026, 10, 12, true)]
    [InlineData("thứ hai ăn phở 50k", 2026, 10, 19, true)]
    [InlineData("thứ 6 tuần trước nhậu 500k", 2026, 10, 16, true)]
    [InlineData("chủ nhật đi chợ 200k", 2026, 10, 18, true)]
    [InlineData("thứ tư ăn phở 50k", 2026, 10, 21, true)]
    [InlineData("thứ năm ăn phở 50k", 2026, 10, 15, true)]
    [InlineData("tuần trước đổ xăng 100k", 2026, 10, 14, true)]
    [InlineData("ngày 15 tiền điện 700k", 2026, 10, 15, true)]
    [InlineData("sáng nay ăn bún bò 40k", 2026, 10, 21, true)]
    [InlineData("tối qua nhậu 300k", 2026, 10, 20, true)]
    public void Parse_TachNgay(string cau, int nam, int thang, int ngay, bool timThay)
    {
        var kq = P(cau);
        Assert.Equal(new DateOnly(nam, thang, ngay), kq.Ngay);
        Assert.Equal(timThay, kq.TimThayNgay);
    }

    [Fact]
    public void GhiChu_BoSoTienVaNgayNhungGiuDau()
    {
        Assert.Equal("Ăn phở", P("ăn phở 50k").GhiChu);
        Assert.Equal("Cà phê", P("hôm qua cà phê 35k").GhiChu);
        Assert.Equal("Điện nước", P("15/10 điện nước 800k").GhiChu);
    }

    [Fact]
    public void DoTinCay_CaoKhiDuTienVaDanhMuc()
    {
        var kq = P("ăn phở 50k");
        Assert.True(kq.DoTinCay >= ParserService.NguongTinCay);
        Assert.True(kq.TimThayDanhMuc);
    }

    [Fact]
    public void DoTinCay_ThapKhiKhongBietDanhMuc()
    {
        var kq = P("trả góp 3tr");
        Assert.Equal(3_000_000L, kq.SoTien);
        Assert.Equal(DanhMucMacDinh.KhacChi, kq.DanhMucId);
        Assert.False(kq.TimThayDanhMuc);
        Assert.True(kq.DoTinCay < ParserService.NguongTinCay);
    }

    [Fact]
    public void DoTinCay_ThapKhiSoKhongCoDonVi()
    {
        var kq = P("cà phê 25");
        Assert.Equal(25_000L, kq.SoTien); // hiểu là 25 nghìn
        Assert.True(kq.DoTinCay < ParserService.NguongTinCay);
    }

    [Fact]
    public void DoTinCay_GiamKhiCoNhieuSoTien()
    {
        var kq = P("ăn phở 50k bia 30k");
        Assert.Equal(50_000L, kq.SoTien);
        Assert.True(kq.DoTinCay < P("ăn phở 50k").DoTinCay);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc xyz")]
    public void KhongCoSoTien_TraNull(string cau)
    {
        var kq = P(cau);
        Assert.Null(kq.SoTien);
        Assert.Equal(0, kq.DoTinCay);
    }

    [Fact]
    public void Null_KhongNemLoi()
    {
        Assert.Null(_p.Parse(null, HomNay).SoTien);
    }

    [Fact]
    public void Emoji_KhongNemLoi()
    {
        var kq = P("ăn phở 😋 50k");
        Assert.Equal(50_000L, kq.SoTien);
        Assert.Equal("an-uong", kq.DanhMucId);
    }

    [Fact]
    public void CuCai_KhongPhaiTien()
    {
        // "củ" là triệu, nhưng "2 củ cải" là rau củ chứ không phải 2 triệu
        var kq = P("mua 2 củ cải 10k");
        Assert.Equal(10_000L, kq.SoTien);
    }

    [Fact]
    public void DaHoc_UuTienHonBangTuKhoaMacDinh()
    {
        var daHoc = new Dictionary<string, string> { ["ca phe"] = "giai-tri" };
        var kq = _p.Parse("cà phê 30k", HomNay, null, daHoc);
        Assert.Equal("giai-tri", kq.DanhMucId);
    }

    [Fact]
    public void DaHoc_DoanDuocTuKhongCoTrongBang()
    {
        var daHoc = new Dictionary<string, string> { ["be ti"] = "giai-tri" };
        var kq = _p.Parse("bé tí 70k", HomNay, null, daHoc);
        Assert.Equal("giai-tri", kq.DanhMucId);
        Assert.True(kq.DoTinCay >= ParserService.NguongTinCay);
    }

    [Fact]
    public void TrichKhoaHoc_LayToiDa3TuKhongDau()
    {
        Assert.Equal("ca phe sang", ParserService.TrichKhoaHoc("Cà phê sáng nay rất ngon"));
        Assert.Equal("be ti", ParserService.TrichKhoaHoc("Bé Tí"));
    }

    [Fact]
    public void TuKhoaTuyChinh_DuocDung()
    {
        var dm = DanhMucMacDinh.TaoMoi();
        dm.First(d => d.Id == "giai-tri").TuKhoa.Add("câu lạc bộ");
        var kq = _p.Parse("câu lạc bộ 100k", HomNay, dm);
        Assert.Equal("giai-tri", kq.DanhMucId);
    }
}
