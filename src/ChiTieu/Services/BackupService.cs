using ChiTieu.Models;
using Microsoft.JSInterop;

namespace ChiTieu.Services;

public enum KetQuaXuat { DaChiaSe, DaTaiXuong, Huy }

/// <summary>Xuất/nhập JSON. Logic kiểm tra và gộp nằm ở BackupCore (Core, có test).</summary>
public class BackupService
{
    private readonly KhoDuLieu _kho;
    private readonly DbService _db;
    private readonly CaiDatService _caiDat;
    private readonly IJSRuntime _js;
    private readonly DongHo _dongHo;

    public BackupService(KhoDuLieu kho, DbService db, CaiDatService caiDat, IJSRuntime js, DongHo dongHo)
    {
        _kho = kho; _db = db; _caiDat = caiDat; _js = js; _dongHo = dongHo;
    }

    public bool CoDuLieu => _kho.DsGiaoDich.Count > 0 || _kho.DsMucTieu.Count > 0;

    public bool CanNhac => BackupCore.CanNhacBackup(_kho.CaiDat.BackupLanCuoi, DateTime.UtcNow, CoDuLieu);

    public int? SoNgayTuLanCuoi => _kho.CaiDat.BackupLanCuoi == null
        ? null : (int)(DateTime.UtcNow - _kho.CaiDat.BackupLanCuoi.Value).TotalDays;

    /// <summary>Mở Share Sheet của iOS; nếu trình duyệt không hỗ trợ thì tải file xuống.</summary>
    public async Task<KetQuaXuat> XuatAsync()
    {
        var json = BackupCore.XuatJson(_kho.ChupDuLieu());
        var ten = BackupCore.TenFile(_dongHo.BayGio);
        var kq = await _js.InvokeAsync<string>("chitieuUtil.chiaSeFile", ten, json);
        if (kq == "huy") return KetQuaXuat.Huy;
        _kho.CaiDat.BackupLanCuoi = DateTime.UtcNow;
        await _caiDat.LuuAsync();
        return kq == "share" ? KetQuaXuat.DaChiaSe : KetQuaXuat.DaTaiXuong;
    }

    /// <summary>Đọc và kiểm tra file (chưa ghi gì). Ném InvalidDataException nếu file hỏng.</summary>
    public BackupData Doc(string json) => BackupCore.DocJson(json);

    public async Task NhapAsync(BackupData file, CheDoNhap che)
    {
        var ketQua = BackupCore.GopHoacGhiDe(_kho.ChupDuLieu(), file, che);
        if (ketQua.DanhMuc.Count == 0) ketQua.DanhMuc = DanhMucMacDinh.TaoMoi();

        // Mỗi store được thay trong một transaction riêng; lỗi giữa chừng sẽ ném DuLieuException.
        await _db.ThayTheAsync(DbService.GiaoDich, ketQua.GiaoDich);
        await _db.ThayTheAsync(DbService.DanhMuc, ketQua.DanhMuc);
        await _db.ThayTheAsync(DbService.NganSach, ketQua.NganSach);
        await _db.ThayTheAsync(DbService.MucTieu, ketQua.MucTieu);
        await _db.ThayTheAsync(DbService.TuKhoaHoc, ketQua.TuKhoaHoc);

        _kho.GanTatCa(ketQua);
        _kho.BaoThayDoi();
    }
}
