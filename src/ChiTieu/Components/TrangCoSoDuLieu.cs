using ChiTieu.Services;
using Microsoft.AspNetCore.Components;

namespace ChiTieu.Components;

/// <summary>Lớp nền cho các trang: tự vẽ lại mỗi khi dữ liệu thay đổi và tự hủy đăng ký khi rời trang.</summary>
public abstract class TrangCoSoDuLieu : ComponentBase, IDisposable
{
    [Inject] protected KhoDuLieu Kho { get; set; } = default!;
    [Inject] protected DongHo Dong { get; set; } = default!;
    [Inject] protected ThongBao Bao { get; set; } = default!;
    [Inject] protected PhanHoi Rung { get; set; } = default!;

    protected override void OnInitialized()
    {
        Kho.ThayDoi += DuLieuDoi;
        KhoiTao();
    }

    /// <summary>Trang ghi đè để khởi tạo trạng thái.</summary>
    protected virtual void KhoiTao() { }

    private void DuLieuDoi() => _ = InvokeAsync(StateHasChanged);

    public virtual void Dispose() => Kho.ThayDoi -= DuLieuDoi;

    /// <summary>Chạy một thao tác ghi dữ liệu; lỗi lưu hiện thành thông báo tiếng Việt thay vì làm sập app.</summary>
    protected async Task AnToanAsync(Func<Task> viec)
    {
        try { await viec(); }
        catch (DuLieuException e) { Bao.Loi(e.Message); }
        catch (ArgumentException e) { Bao.Loi(e.Message); }
    }
}
