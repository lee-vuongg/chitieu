namespace ChiTieu.Models;

/// <summary>Phiên bản schema dữ liệu hiện tại, dùng cho migrate sau này.</summary>
public static class PhienBan
{
    public const int HienTai = 1;
}

public enum LoaiGiaoDich
{
    Chi = 0,
    Thu = 1
}

public enum ChuDe
{
    HeThong = 0,
    Sang = 1,
    Toi = 2
}

public class GiaoDich
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public LoaiGiaoDich Loai { get; set; } = LoaiGiaoDich.Chi;
    /// <summary>Số tiền VND, luôn dương.</summary>
    public long SoTien { get; set; }
    public string DanhMucId { get; set; } = "khac-chi";
    public string GhiChu { get; set; } = "";
    public DateOnly Ngay { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateTime TaoLuc { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = PhienBan.HienTai;
}

public class DanhMuc
{
    public string Id { get; set; } = "";
    public string Ten { get; set; } = "";
    public LoaiGiaoDich Loai { get; set; } = LoaiGiaoDich.Chi;
    public string Icon { get; set; } = "🏷️";
    public string Mau { get; set; } = "#9E9E9E";
    /// <summary>Từ khóa (viết thường, có thể có dấu) để đoán danh mục.</summary>
    public List<string> TuKhoa { get; set; } = new();
    public int Version { get; set; } = PhienBan.HienTai;
}

public class NganSach
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>null = ngân sách tổng của tháng.</summary>
    public string? DanhMucId { get; set; }
    public long HanMuc { get; set; }
    public int Version { get; set; } = PhienBan.HienTai;
}

public class NapTien
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long SoTien { get; set; }
    public DateOnly Ngay { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string GhiChu { get; set; } = "";
}

public class MucTieu
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Ten { get; set; } = "";
    public long SoTienMucTieu { get; set; }
    public DateOnly Han { get; set; } = DateOnly.FromDateTime(DateTime.Today).AddMonths(6);
    public DateTime TaoLuc { get; set; } = DateTime.UtcNow;
    public bool DaChucMung { get; set; }
    public List<NapTien> LichSuNap { get; set; } = new();
    public int Version { get; set; } = PhienBan.HienTai;
}

/// <summary>Từ khóa đã học: người dùng sửa danh mục cho ghi chú nào thì lần sau dùng lại.</summary>
public class TuKhoaHoc
{
    /// <summary>Từ khóa đã chuẩn hóa (không dấu, chữ thường) - khóa chính.</summary>
    public string Id { get; set; } = "";
    public string DanhMucId { get; set; } = "";
    public int Version { get; set; } = PhienBan.HienTai;
}

public class CaiDat
{
    public string Id { get; set; } = "main";
    public ChuDe ChuDe { get; set; } = ChuDe.HeThong;
    public string AiUrl { get; set; } = "";
    public string AiMa { get; set; } = "";
    public DateTime? BackupLanCuoi { get; set; }
    public bool DaHienHuongDanIos { get; set; }
    public DateTime TaoLuc { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = PhienBan.HienTai;
}

public class BackupData
{
    public int Version { get; set; } = PhienBan.HienTai;
    public DateTime XuatLuc { get; set; } = DateTime.UtcNow;
    public List<GiaoDich> GiaoDich { get; set; } = new();
    public List<DanhMuc> DanhMuc { get; set; } = new();
    public List<NganSach> NganSach { get; set; } = new();
    public List<MucTieu> MucTieu { get; set; } = new();
    public List<TuKhoaHoc> TuKhoaHoc { get; set; } = new();
}
