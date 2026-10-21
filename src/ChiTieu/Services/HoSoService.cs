using ChiTieu.Models;
using Microsoft.JSInterop;

namespace ChiTieu.Services;

/// <summary>Thông tin hồ sơ hiển thị với quản trị viên (lấy từ ho-so.js).</summary>
public record ThongTinHoSo(string? Ten, bool DongYTen, bool DongYTyLe, string? Bot);

/// <summary>Tỷ lệ chi tiêu của một danh mục trong 30 ngày (chỉ phần trăm, không có số tiền).</summary>
public record TyLeDanhMuc(string DanhMuc, int PhanTram);

public class HoSoService
{
    private readonly IJSRuntime _js;
    private readonly KhoDuLieu _kho;
    private readonly DongHo _dongHo;

    public HoSoService(IJSRuntime js, KhoDuLieu kho, DongHo dongHo)
    {
        _js = js; _kho = kho; _dongHo = dongHo;
    }

    public async Task<ThongTinHoSo> LayAsync()
    {
        try { return await _js.InvokeAsync<ThongTinHoSo>("chiTieuHoSo.thongTin"); }
        catch { return new ThongTinHoSo(null, false, false, null); }
    }

    public async Task LuuAsync(string? ten, bool dongYTen, bool dongYTyLe)
    {
        try { await _js.InvokeVoidAsync("chiTieuHoSo.luu", new { ten, dongYTen, dongYTyLe }); }
        catch { /* không ảnh hưởng app */ }
    }

    /// <summary>Gửi hồ sơ lên máy chủ. Tỷ lệ chi tiêu chỉ được tính và gửi khi người dùng đã đồng ý riêng.</summary>
    public async Task DongBoAsync()
    {
        try
        {
            var t = await LayAsync();
            object? tyLe = t.DongYTyLe ? TyLeChi() : null;
            await _js.InvokeVoidAsync("chiTieuHoSo.dongBo", tyLe);
        }
        catch { /* mất mạng thì bỏ qua */ }
    }

    private List<object> TyLeChi()
    {
        var hom = _dongHo.HomNay;
        var tu = hom.AddDays(-29);
        var chi = _kho.DsGiaoDich
            .Where(g => g.Loai == LoaiGiaoDich.Chi && g.Ngay >= tu && g.Ngay <= hom)
            .ToList();

        var tong = chi.Sum(g => g.SoTien);
        if (tong <= 0) return new List<object>();

        return chi
            .GroupBy(g => _kho.TenDanhMuc(g.DanhMucId))
            .Select(gr => new TyLeDanhMuc(
                gr.Key,
                (int)Math.Round(gr.Sum(x => x.SoTien) * 100.0 / tong, MidpointRounding.AwayFromZero)))
            .OrderByDescending(x => x.PhanTram)
            .Take(12)
            .Cast<object>()
            .ToList();
    }
}
