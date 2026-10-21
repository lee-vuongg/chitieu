using Microsoft.JSInterop;

namespace ChiTieu.Services;

public class SuKienService
{
    private readonly IJSRuntime _js;
    public SuKienService(IJSRuntime js) => _js = js;

    public async Task GhiAsync(string ev)
    {
        try { await _js.InvokeVoidAsync("chiTieuTK.ghi", ev); }
        catch { /* không ảnh hưởng app */ }
    }
}