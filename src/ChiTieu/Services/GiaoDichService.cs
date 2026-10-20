using ChiTieu.Models;

namespace ChiTieu.Services;

/// <summary>Thêm/sửa/xóa giao dịch, phân tích câu nhập nhanh và "học" danh mục.</summary>
public class GiaoDichService
{
    private readonly KhoDuLieu _kho;
    private readonly DbService _db;
    private readonly ParserService _parser;
    private readonly NganSachService _nganSach;
    private readonly DongHo _dongHo;

    public GiaoDichService(KhoDuLieu kho, DbService db, ParserService parser, NganSachService nganSach, DongHo dongHo)
    {
        _kho = kho; _db = db; _parser = parser; _nganSach = nganSach; _dongHo = dongHo;
    }

    public KetQuaParse Phan(string cau) =>
        _parser.Parse(cau, _dongHo.HomNay, _kho.DsDanhMuc, _kho.TuKhoaDaHoc());

    public async Task<GiaoDich> ThemAsync(GiaoDich g)
    {
        Chuan(g);
        await _db.LuuAsync(DbService.GiaoDich, g);
        _kho.DsGiaoDich.Add(g);
        _kho.BaoThayDoi();
        return g;
    }

    public async Task SuaAsync(GiaoDich g)
    {
        Chuan(g);
        await _db.LuuAsync(DbService.GiaoDich, g);
        var i = _kho.DsGiaoDich.FindIndex(x => x.Id == g.Id);
        if (i >= 0) _kho.DsGiaoDich[i] = g; else _kho.DsGiaoDich.Add(g);
        _kho.BaoThayDoi();
    }

    /// <summary>Xóa và trả lại bản đã xóa để có thể hoàn tác.</summary>
    public async Task<GiaoDich?> XoaAsync(Guid id)
    {
        var g = _kho.DsGiaoDich.FirstOrDefault(x => x.Id == id);
        if (g == null) return null;
        await _db.XoaAsync(DbService.GiaoDich, id.ToString());
        _kho.DsGiaoDich.Remove(g);
        _kho.BaoThayDoi();
        return g;
    }

    public Task KhoiPhucAsync(GiaoDich g) => ThemAsync(g);

    /// <summary>Người dùng sửa danh mục cho một ghi chú -> nhớ để lần sau tự dùng.</summary>
    public async Task HocDanhMucAsync(string ghiChu, string danhMucId)
    {
        var khoa = ParserService.TrichKhoaHoc(ghiChu);
        if (khoa.Length < 2) return;
        var cu = _kho.DsTuKhoaHoc.FirstOrDefault(t => t.Id == khoa);
        if (cu != null && cu.DanhMucId == danhMucId) return;
        var moi = new TuKhoaHoc { Id = khoa, DanhMucId = danhMucId };
        await _db.LuuAsync(DbService.TuKhoaHoc, moi);
        _kho.DsTuKhoaHoc.RemoveAll(t => t.Id == khoa);
        _kho.DsTuKhoaHoc.Add(moi);
    }

    public List<CanhBaoNganSach> CanhBaoSauKhiLuu(GiaoDich g) =>
        _nganSach.CanhBaoChoKhoanChi(_kho.DsNganSach, _kho.DsGiaoDich, g, _dongHo.HomNay, _kho.TenDanhMuc);

    private static void Chuan(GiaoDich g)
    {
        if (g.SoTien <= 0) throw new ArgumentException("Số tiền phải lớn hơn 0.");
        g.GhiChu = (g.GhiChu ?? "").Trim();
        g.Version = PhienBan.HienTai;
    }
}
