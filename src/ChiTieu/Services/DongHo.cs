using Microsoft.JSInterop;

namespace ChiTieu.Services;

/// <summary>
/// Blazor WASM không biết múi giờ của trình duyệt (DateTime.Now ra giờ UTC),
/// nên "hôm nay" phải tính từ độ lệch múi giờ lấy bằng JS, nếu không từ 0h-7h sáng sẽ bị lệch một ngày.
/// </summary>
public class DongHo
{
    private readonly IJSRuntime _js;
    private int _lechPhut; // giống Date.getTimezoneOffset(): UTC+7 -> -420
    private bool _daLay;

    public DongHo(IJSRuntime js) => _js = js;

    public async Task KhoiTaoAsync()
    {
        try
        {
            _lechPhut = await _js.InvokeAsync<int>("chitieuUtil.lechMuiGio");
            _daLay = true;
        }
        catch { _lechPhut = 0; }
    }

    public DateTime BayGio => DateTime.UtcNow.AddMinutes(-_lechPhut);
    public DateOnly HomNay => DateOnly.FromDateTime(BayGio);
    public bool DaLayMuiGio => _daLay;
}
