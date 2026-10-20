using System.Text.Json;
using Microsoft.JSInterop;

namespace ChiTieu.Services;

/// <summary>Lỗi lưu/đọc dữ liệu, thông điệp đã là tiếng Việt thân thiện để hiện thẳng cho người dùng.</summary>
public class DuLieuException : Exception
{
    public DuLieuException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>Lớp mỏng gọi wwwroot/js/db.js (IndexedDB). Mỗi store lưu một loại đối tượng, khóa chính là "id".</summary>
public class DbService
{
    public const string GiaoDich = "giaoDich";
    public const string DanhMuc = "danhMuc";
    public const string NganSach = "nganSach";
    public const string MucTieu = "mucTieu";
    public const string TuKhoaHoc = "tuKhoaHoc";
    public const string CaiDat = "caiDat";
    public static readonly string[] TatCaStore = { GiaoDich, DanhMuc, NganSach, MucTieu, TuKhoaHoc, CaiDat };

    private readonly IJSRuntime _js;
    public DbService(IJSRuntime js) => _js = js;

    public async Task<List<T>> LayTatCaAsync<T>(string store)
    {
        try
        {
            var json = await _js.InvokeAsync<string>("chitieuDb.layTatCa", store);
            return JsonSerializer.Deserialize<List<T>>(string.IsNullOrEmpty(json) ? "[]" : json, JsonCfg.Opt) ?? new();
        }
        catch (JSException e) { throw Dich(e); }
        catch (JsonException e) { throw new DuLieuException("Dữ liệu lưu trong máy bị lỗi, không đọc được.", e); }
    }

    public Task LuuAsync<T>(string store, T item) =>
        Chay(() => _js.InvokeVoidAsync("chitieuDb.luu", store, JsonSerializer.Serialize(item, JsonCfg.Opt)).AsTask());

    public Task LuuNhieuAsync<T>(string store, IEnumerable<T> items) =>
        Chay(() => _js.InvokeVoidAsync("chitieuDb.luuNhieu", store, JsonSerializer.Serialize(items, JsonCfg.Opt)).AsTask());

    public Task XoaAsync(string store, string id) =>
        Chay(() => _js.InvokeVoidAsync("chitieuDb.xoa", store, id).AsTask());

    /// <summary>Thay toàn bộ nội dung một store trong một transaction (dùng khi nhập backup ghi đè).</summary>
    public Task ThayTheAsync<T>(string store, IEnumerable<T> items) =>
        Chay(() => _js.InvokeVoidAsync("chitieuDb.thayThe", store, JsonSerializer.Serialize(items, JsonCfg.Opt)).AsTask());

    public async Task<bool> XinGiuLauAsync()
    {
        try { return await _js.InvokeAsync<bool>("chitieuDb.xinGiuLau"); }
        catch { return false; }
    }

    private static async Task Chay(Func<Task> viec)
    {
        try { await viec(); }
        catch (JSException e) { throw Dich(e); }
    }

    private static DuLieuException Dich(JSException e)
    {
        var m = e.Message ?? "";
        if (m.Contains("HET_DUNG_LUONG"))
            return new DuLieuException("Bộ nhớ trên điện thoại đã đầy nên chưa lưu được. Hãy giải phóng bớt dung lượng rồi thử lại.", e);
        if (m.Contains("BI_CHAN"))
            return new DuLieuException("Trình duyệt đang chặn việc lưu dữ liệu (thường do chế độ riêng tư). Hãy mở bằng Safari thường rồi thử lại.", e);
        return new DuLieuException("Không lưu được dữ liệu. Hãy thử lại, nếu vẫn lỗi hãy đóng và mở lại ứng dụng.", e);
    }
}
