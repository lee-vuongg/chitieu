using ChiTieu.Models;

namespace ChiTieu.Services;

/// <summary>Lưu ngân sách (tính toán nằm ở NganSachService trong Core).</summary>
public class QuanLyNganSach
{
    private readonly KhoDuLieu _kho;
    private readonly DbService _db;
    private readonly NganSachService _tinh;
    private readonly DongHo _dongHo;

    public QuanLyNganSach(KhoDuLieu kho, DbService db, NganSachService tinh, DongHo dongHo)
    {
        _kho = kho; _db = db; _tinh = tinh; _dongHo = dongHo;
    }

    public long HanMucCuaDanhMuc(string? danhMucId) =>
        _kho.DsNganSach.FirstOrDefault(n => n.DanhMucId == danhMucId)?.HanMuc ?? 0;

    /// <summary>Đặt hạn mức; hạn mức &lt;= 0 nghĩa là bỏ ngân sách đó. danhMucId = null là ngân sách tổng.</summary>
    public async Task DatHanMucAsync(string? danhMucId, long hanMuc)
    {
        var cu = _kho.DsNganSach.FirstOrDefault(n => n.DanhMucId == danhMucId);
        if (hanMuc <= 0)
        {
            if (cu == null) return;
            await _db.XoaAsync(DbService.NganSach, cu.Id.ToString());
            _kho.DsNganSach.Remove(cu);
        }
        else
        {
            var ns = cu ?? new NganSach { DanhMucId = danhMucId };
            ns.HanMuc = hanMuc;
            ns.Version = PhienBan.HienTai;
            await _db.LuuAsync(DbService.NganSach, ns);
            if (cu == null) _kho.DsNganSach.Add(ns);
        }
        _kho.BaoThayDoi();
    }

    public List<TinhTrangNganSach> TinhTrang(int nam, int thang) =>
        _tinh.TinhTatCa(_kho.DsNganSach, _kho.DsGiaoDich, _dongHo.HomNay, nam, thang);

    /// <summary>Cảnh báo của tháng hiện tại (đỏ, vàng, dự báo) để hiện nổi bật ở màn hình chính.</summary>
    public List<CanhBaoNganSach> CanhBaoThangNay()
    {
        var hn = _dongHo.HomNay;
        return _tinh.TaoCanhBao(TinhTrang(hn.Year, hn.Month), _kho.TenDanhMuc);
    }
}

public class QuanLyMucTieu
{
    private readonly KhoDuLieu _kho;
    private readonly DbService _db;
    private readonly MucTieuService _tinh;
    private readonly DongHo _dongHo;

    public QuanLyMucTieu(KhoDuLieu kho, DbService db, MucTieuService tinh, DongHo dongHo)
    {
        _kho = kho; _db = db; _tinh = tinh; _dongHo = dongHo;
    }

    public TienDoMucTieu TienDo(MucTieu m) => _tinh.TinhTienDo(m, _dongHo.HomNay);

    public async Task LuuAsync(MucTieu m)
    {
        if (string.IsNullOrWhiteSpace(m.Ten)) throw new ArgumentException("Hãy đặt tên cho mục tiêu.");
        if (m.SoTienMucTieu <= 0) throw new ArgumentException("Số tiền mục tiêu phải lớn hơn 0.");
        m.Ten = m.Ten.Trim();
        m.Version = PhienBan.HienTai;
        await _db.LuuAsync(DbService.MucTieu, m);
        if (!_kho.DsMucTieu.Any(x => x.Id == m.Id)) _kho.DsMucTieu.Add(m);
        _kho.BaoThayDoi();
    }

    public async Task XoaAsync(Guid id)
    {
        await _db.XoaAsync(DbService.MucTieu, id.ToString());
        _kho.DsMucTieu.RemoveAll(x => x.Id == id);
        _kho.BaoThayDoi();
    }

    /// <summary>Nạp thêm tiền. Trả true nếu lần nạp này vừa đạt mục tiêu (để bắn confetti).</summary>
    public async Task<bool> NapThemAsync(MucTieu m, long soTien)
    {
        // Làm trên bản sao để nếu lưu lỗi thì dữ liệu trong RAM không bị lệch so với IndexedDB.
        var ban = new MucTieu
        {
            Id = m.Id, Ten = m.Ten, SoTienMucTieu = m.SoTienMucTieu, Han = m.Han, TaoLuc = m.TaoLuc,
            DaChucMung = m.DaChucMung, Version = m.Version, LichSuNap = m.LichSuNap.ToList()
        };
        var vuaDat = _tinh.NapThem(ban, soTien, _dongHo.HomNay);
        await _db.LuuAsync(DbService.MucTieu, ban);
        m.LichSuNap = ban.LichSuNap;
        m.DaChucMung = ban.DaChucMung;
        _kho.BaoThayDoi();
        return vuaDat;
    }
}
