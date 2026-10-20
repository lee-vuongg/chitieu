using System.Text.Json;
using System.Text.Json.Serialization;
using ChiTieu.Models;

namespace ChiTieu.Services;

/// <summary>Cấu hình JSON dùng chung cho IndexedDB, backup và gửi AI (camelCase, enum dạng chữ).</summary>
public static class JsonCfg
{
    public static readonly JsonSerializerOptions Opt = Tao(false);
    public static readonly JsonSerializerOptions OptDep = Tao(true);

    private static JsonSerializerOptions Tao(bool dep)
    {
        // UnsafeRelaxed: không escape tiếng Việt thành \uXXXX, file backup dễ đọc và nhỏ hơn.
        // An toàn vì JSON này không bao giờ nhúng thẳng vào HTML.
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = dep,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }
}

public enum CheDoNhap { Gop, GhiDe }

/// <summary>Logic backup thuần: tuần tự hóa, kiểm tra, gộp. Phần chia sẻ file nằm ở app.</summary>
public static class BackupCore
{
    public static string XuatJson(BackupData d) => JsonSerializer.Serialize(d, JsonCfg.OptDep);

    /// <summary>Đọc và kiểm tra file backup. Ném InvalidDataException với thông điệp tiếng Việt nếu hỏng.</summary>
    public static BackupData DocJson(string json)
    {
        BackupData? d;
        try { d = JsonSerializer.Deserialize<BackupData>(json, JsonCfg.Opt); }
        catch (JsonException) { throw new InvalidDataException("File không đúng định dạng sao lưu của ứng dụng."); }
        if (d == null) throw new InvalidDataException("File sao lưu trống.");
        if (d.Version > PhienBan.HienTai)
            throw new InvalidDataException("File sao lưu được tạo bởi phiên bản mới hơn. Hãy cập nhật ứng dụng.");
        if (d.GiaoDich.Any(g => g.SoTien <= 0 || g.Id == Guid.Empty))
            throw new InvalidDataException("File sao lưu có giao dịch không hợp lệ.");
        return d;
    }

    /// <summary>Gộp: giữ dữ liệu hiện có, chỉ thêm phần chưa có (theo Id). Ghi đè: dùng nguyên dữ liệu trong file.</summary>
    public static BackupData GopHoacGhiDe(BackupData hienCo, BackupData file, CheDoNhap che)
    {
        if (che == CheDoNhap.GhiDe) return file;
        return new BackupData
        {
            Version = PhienBan.HienTai,
            GiaoDich = hienCo.GiaoDich.Concat(file.GiaoDich.Where(g => hienCo.GiaoDich.All(x => x.Id != g.Id))).ToList(),
            DanhMuc = hienCo.DanhMuc.Concat(file.DanhMuc.Where(g => hienCo.DanhMuc.All(x => x.Id != g.Id))).ToList(),
            NganSach = hienCo.NganSach.Concat(file.NganSach.Where(g => hienCo.NganSach.All(x =>
                x.Id != g.Id && x.DanhMucId != g.DanhMucId))).ToList(),
            MucTieu = hienCo.MucTieu.Concat(file.MucTieu.Where(g => hienCo.MucTieu.All(x => x.Id != g.Id))).ToList(),
            TuKhoaHoc = hienCo.TuKhoaHoc.Concat(file.TuKhoaHoc.Where(g => hienCo.TuKhoaHoc.All(x => x.Id != g.Id))).ToList()
        };
    }

    /// <summary>Quá 14 ngày chưa backup (hoặc chưa từng) thì nhắc. Chưa có dữ liệu thì không nhắc.</summary>
    public static bool CanNhacBackup(DateTime? lanCuoi, DateTime bayGio, bool coDuLieu)
    {
        if (!coDuLieu) return false;
        return lanCuoi == null || (bayGio - lanCuoi.Value).TotalDays > 14;
    }

    public static string TenFile(DateTime bayGio) => $"chitieu-backup-{bayGio:yyyyMMdd-HHmm}.json";
}
