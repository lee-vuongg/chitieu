using Microsoft.JSInterop;

namespace ChiTieu.Services;

public class AmThanh
{
    private readonly IJSRuntime _js;
    public AmThanh(IJSRuntime js) => _js = js;

    /// <summary>kieu: "luu", "xoa", "canhBao", "mucTieu"</summary>
    public async Task PhatAsync(string kieu)
    {
        try { await _js.InvokeVoidAsync("chiTieuAm.phat", kieu); }
        catch { /* không có âm thanh thì bỏ qua */ }
    }

    public async Task<bool> DangTatAsync()
    {
        try { return await _js.InvokeAsync<bool>("chiTieuAm.tatAm"); }
        catch { return false; }
    }

    public async Task DatTatAsync(bool tat)
    {
        try { await _js.InvokeVoidAsync("chiTieuAm.datTatAm", tat); }
        catch { }
    }
}