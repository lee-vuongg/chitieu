using ChiTieu.Models;

namespace ChiTieu.Services;

/// <summary>
/// Bộ nhớ đệm toàn bộ dữ liệu của một người dùng (rất nhỏ nên giữ hết trong RAM),
/// ghi xuống IndexedDB qua DbService. Các trang lắng nghe sự kiện ThayDoi để vẽ lại.
/// </summary>
public class KhoDuLieu
{
    private readonly DbService _db;
    public KhoDuLieu(DbService db) => _db = db;

    public List<GiaoDich> DsGiaoDich { get; private set; } = new();
    public List<DanhMuc> DsDanhMuc { get; private set; } = new();
    public List<NganSach> DsNganSach { get; private set; } = new();
    public List<MucTieu> DsMucTieu { get; private set; } = new();
    public List<TuKhoaHoc> DsTuKhoaHoc { get; private set; } = new();
    public CaiDat CaiDat { get; private set; } = new();

    public bool DaTai { get; private set; }
    /// <summary>Thông báo lỗi tiếng Việt nếu không đọc được IndexedDB.</summary>
    public string? Loi { get; private set; }

    public event Action? ThayDoi;
    public void BaoThayDoi() => ThayDoi?.Invoke();

    public async Task TaiAsync()
    {
        Loi = null;
        try
        {
            DsGiaoDich = await _db.LayTatCaAsync<GiaoDich>(DbService.GiaoDich);
            DsDanhMuc = await _db.LayTatCaAsync<DanhMuc>(DbService.DanhMuc);
            DsNganSach = await _db.LayTatCaAsync<NganSach>(DbService.NganSach);
            DsMucTieu = await _db.LayTatCaAsync<MucTieu>(DbService.MucTieu);
            DsTuKhoaHoc = await _db.LayTatCaAsync<TuKhoaHoc>(DbService.TuKhoaHoc);
            var cd = await _db.LayTatCaAsync<CaiDat>(DbService.CaiDat);

            if (DsDanhMuc.Count == 0)
            {
                DsDanhMuc = DanhMucMacDinh.TaoMoi();
                await _db.LuuNhieuAsync(DbService.DanhMuc, DsDanhMuc);
            }
            if (cd.Count == 0)
            {
                CaiDat = new CaiDat();
                await _db.LuuAsync(DbService.CaiDat, CaiDat);
            }
            else CaiDat = cd[0];

            await _db.XinGiuLauAsync(); // nhờ Safari đừng tự xóa dữ liệu
        }
        catch (DuLieuException e)
        {
            Loi = e.Message;
        }
        DaTai = true;
        BaoThayDoi();
    }

    /// <summary>Thay toàn bộ dữ liệu trong RAM (dùng sau khi nhập backup).</summary>
    public void GanTatCa(BackupData d)
    {
        DsGiaoDich = d.GiaoDich;
        DsDanhMuc = d.DanhMuc;
        DsNganSach = d.NganSach;
        DsMucTieu = d.MucTieu;
        DsTuKhoaHoc = d.TuKhoaHoc;
    }

    public BackupData ChupDuLieu() => new()
    {
        GiaoDich = DsGiaoDich.ToList(),
        DanhMuc = DsDanhMuc.ToList(),
        NganSach = DsNganSach.ToList(),
        MucTieu = DsMucTieu.ToList(),
        TuKhoaHoc = DsTuKhoaHoc.ToList()
    };

    public DanhMuc? TimDanhMuc(string? id) => id == null ? null : DsDanhMuc.FirstOrDefault(d => d.Id == id);

    public string TenDanhMuc(string? id) => id == null ? "Tổng tháng" : TimDanhMuc(id)?.Ten ?? "Khác";

    public Dictionary<string, string> TuKhoaDaHoc() =>
        DsTuKhoaHoc.ToDictionary(t => t.Id, t => t.DanhMucId);

    public IEnumerable<DanhMuc> DanhMucTheoLoai(LoaiGiaoDich loai) => DsDanhMuc.Where(d => d.Loai == loai);
}
