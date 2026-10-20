using ChiTieu.Models;
using Microsoft.JSInterop;

namespace ChiTieu.Services;

/// <summary>Trạng thái bottom sheet nhập nhanh dùng chung (nút + giữa, bấm vào dòng lịch sử để sửa).</summary>
public class SheetService
{
    public bool Mo { get; private set; }
    /// <summary>null = thêm mới.</summary>
    public GiaoDich? DangSua { get; private set; }
    public event Action? Doi;

    public void MoMoi() { DangSua = null; Mo = true; Doi?.Invoke(); }
    public void MoSua(GiaoDich g) { DangSua = g; Mo = true; Doi?.Invoke(); }
    public void Dong() { Mo = false; Doi?.Invoke(); }
}

/// <summary>Rung nhẹ khi bấm nút (iOS Safari không hỗ trợ vibrate thì bỏ qua êm).</summary>
public class PhanHoi
{
    private readonly IJSRuntime _js;
    public PhanHoi(IJSRuntime js) => _js = js;

    public async Task RungAsync(int ms = 12)
    {
        try { await _js.InvokeVoidAsync("chitieuUtil.rung", ms); } catch { }
    }
}
