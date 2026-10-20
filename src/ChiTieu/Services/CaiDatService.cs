using ChiTieu.Models;
using Microsoft.JSInterop;

namespace ChiTieu.Services;

/// <summary>Cài đặt người dùng, danh mục/từ khóa và giao diện sáng/tối.</summary>
public class CaiDatService : IAsyncDisposable
{
    private readonly KhoDuLieu _kho;
    private readonly DbService _db;
    private readonly IJSRuntime _js;
    private DotNetObjectReference<CaiDatService>? _ref;
    private bool _heThongToi;

    public CaiDatService(KhoDuLieu kho, DbService db, IJSRuntime js)
    {
        _kho = kho; _db = db; _js = js;
    }

    public event Action? GiaoDienDoi;

    public CaiDat Hien => _kho.CaiDat;

    /// <summary>true nếu đang hiển thị nền tối (theo lựa chọn tay hoặc theo hệ thống).</summary>
    public bool DangToi => Hien.ChuDe switch
    {
        ChuDe.Toi => true,
        ChuDe.Sang => false,
        _ => _heThongToi
    };

    public async Task KhoiTaoGiaoDienAsync()
    {
        try
        {
            _heThongToi = await _js.InvokeAsync<bool>("chitieuUtil.heThongToi");
            _ref ??= DotNetObjectReference.Create(this);
            await _js.InvokeVoidAsync("chitieuUtil.theoDoiHeThong", _ref);
        }
        catch { /* không có thì cứ để sáng */ }
        await ApDungAsync();
    }

    [JSInvokable]
    public async Task HeThongDoi(bool toi)
    {
        _heThongToi = toi;
        if (Hien.ChuDe == ChuDe.HeThong) await ApDungAsync();
    }

    public async Task DatChuDeAsync(ChuDe chuDe)
    {
        Hien.ChuDe = chuDe;
        await LuuAsync();
        await ApDungAsync();
    }

    private async Task ApDungAsync()
    {
        try { await _js.InvokeVoidAsync("chitieuUtil.datChuDe", DangToi); } catch { }
        GiaoDienDoi?.Invoke();
    }

    public async Task LuuAsync()
    {
        Hien.Version = PhienBan.HienTai;
        await _db.LuuAsync(DbService.CaiDat, Hien);
        _kho.BaoThayDoi();
    }

    public async Task LuuDanhMucAsync(DanhMuc dm)
    {
        await _db.LuuAsync(DbService.DanhMuc, dm);
        _kho.BaoThayDoi();
    }

    /// <summary>Thêm danh mục mới do người dùng tự tạo.</summary>
    public async Task ThemDanhMucAsync(string ten, LoaiGiaoDich loai, string icon)
    {
        ten = ten.Trim();
        if (ten.Length == 0) throw new ArgumentException("Hãy nhập tên danh mục.");
        var id = "tuy-" + Guid.NewGuid().ToString("N")[..8];
        var dm = new DanhMuc
        {
            Id = id, Ten = ten, Loai = loai, Icon = string.IsNullOrWhiteSpace(icon) ? "🏷️" : icon.Trim(),
            Mau = loai == LoaiGiaoDich.Thu ? "#43A047" : "#8D6E63"
        };
        await _db.LuuAsync(DbService.DanhMuc, dm);
        _kho.DsDanhMuc.Add(dm);
        _kho.BaoThayDoi();
    }

    public async Task DatLaiTuKhoaMacDinhAsync()
    {
        foreach (var mac in DanhMucMacDinh.TaoMoi())
        {
            var dm = _kho.DsDanhMuc.FirstOrDefault(d => d.Id == mac.Id);
            if (dm == null) continue;
            dm.TuKhoa = mac.TuKhoa;
            await _db.LuuAsync(DbService.DanhMuc, dm);
        }
        _kho.BaoThayDoi();
    }

    public async ValueTask DisposeAsync()
    {
        _ref?.Dispose();
        await Task.CompletedTask;
    }
}
