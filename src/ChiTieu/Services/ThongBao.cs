using ChiTieu.Models;
using MudBlazor;

namespace ChiTieu.Services;

/// <summary>Thông báo ngắn trong app (không phải push). Xóa xong có nút "Hoàn tác".</summary>
public class ThongBao
{
    private readonly ISnackbar _snack;
    private readonly GiaoDichService _giaoDich;

    public ThongBao(ISnackbar snack, GiaoDichService giaoDich)
    {
        _snack = snack; _giaoDich = giaoDich;
    }

    public void Xong(string noiDung) => _snack.Add(noiDung, Severity.Success);
    public void Loi(string noiDung) => _snack.Add(noiDung, Severity.Error);
    public void Thong(string noiDung) => _snack.Add(noiDung, Severity.Normal);

    public void DaXoa(GiaoDich g)
    {
        var ten = string.IsNullOrWhiteSpace(g.GhiChu) ? "khoản này" : g.GhiChu;
        _snack.Add($"Đã xóa {ten}", Severity.Normal, cfg =>
        {
            cfg.Action = "Hoàn tác";
            cfg.OnClick = async _ =>
            {
                try
                {
                    await _giaoDich.KhoiPhucAsync(g);
                    Xong("Đã khôi phục");
                }
                catch (DuLieuException e) { Loi(e.Message); }
            };
        });
    }
}
