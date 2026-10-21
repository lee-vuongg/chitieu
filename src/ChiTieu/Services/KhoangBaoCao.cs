namespace ChiTieu.Services;

/// <summary>Tính khoảng ngày cho báo cáo PDF và lập kế hoạch AI. Tuần bắt đầu từ thứ hai.</summary>
public static class KhoangBaoCao
{
    public const string Tuan = "tuan";
    public const string Thang = "thang";
    public const string TuyChon = "tuychon";

    public static (DateOnly Tu, DateOnly Den) Tinh(string loai, DateOnly homNay, DateOnly? tuNgay = null, DateOnly? denNgay = null)
    {
        switch (loai)
        {
            case Tuan:
            {
                var thu = ((int)homNay.DayOfWeek + 6) % 7; // thứ hai = 0
                var tu = homNay.AddDays(-thu);
                return (tu, tu.AddDays(6));
            }
            case Thang:
            {
                var dau = new DateOnly(homNay.Year, homNay.Month, 1);
                var cuoi = new DateOnly(homNay.Year, homNay.Month, DateTime.DaysInMonth(homNay.Year, homNay.Month));
                return (dau, cuoi);
            }
            default:
            {
                var a = tuNgay ?? homNay;
                var b = denNgay ?? homNay;
                return a <= b ? (a, b) : (b, a);
            }
        }
    }
}
