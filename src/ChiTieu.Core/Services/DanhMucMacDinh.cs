using ChiTieu.Models;

namespace ChiTieu.Services;

/// <summary>Danh mục và bảng từ khóa mặc định (người dùng có thể chỉnh trong Cài đặt).</summary>
public static class DanhMucMacDinh
{
    public const string KhacChi = "khac-chi";
    public const string KhacThu = "thu-khac";

    private static DanhMuc Tao(string id, string ten, LoaiGiaoDich loai, string icon, string mau, params string[] tuKhoa) =>
        new() { Id = id, Ten = ten, Loai = loai, Icon = icon, Mau = mau, TuKhoa = tuKhoa.ToList() };

    public static List<DanhMuc> TaoMoi() => new()
    {
        Tao("an-uong", "Ăn uống", LoaiGiaoDich.Chi, "🍜", "#F4511E",
            "phở", "bún", "bún bò", "bánh mì", "cơm", "cà phê", "cafe", "trà sữa", "trà đá", "trà chanh",
            "nước ngọt", "nước mía", "bia", "rượu", "nhậu", "ăn sáng", "ăn trưa", "ăn tối", "ăn vặt",
            "ăn uống", "quán ăn", "nhà hàng", "lẩu", "nướng", "xôi", "cháo", "hủ tiếu", "mì tôm", "chợ",
            "đi chợ", "rau", "thịt", "trứng", "hải sản", "sữa tươi", "sữa chua", "sữa bột", "bánh", "kem",
            "pizza", "gà rán", "snack"),
        Tao("di-chuyen", "Di chuyển", LoaiGiaoDich.Chi, "🛵", "#039BE5",
            "xăng", "đổ xăng", "xăng xe", "grab", "gojek", "xanh sm", "xe ôm", "taxi", "gửi xe", "đậu xe",
            "vé xe", "xe buýt", "buýt", "vé tàu", "vé máy bay", "máy bay", "sửa xe", "rửa xe", "thay nhớt",
            "nhớt", "vá xe", "cầu đường", "phí cầu đường", "bảo dưỡng xe"),
        Tao("mua-sam", "Mua sắm", LoaiGiaoDich.Chi, "🛍️", "#8E24AA",
            "quần áo", "áo", "quần", "giày", "dép", "túi xách", "mua sắm", "shopee", "lazada", "tiki",
            "điện thoại", "tai nghe", "laptop", "mỹ phẩm", "kem dưỡng", "đồ gia dụng", "nồi cơm"),
        Tao("hoa-don", "Hóa đơn & nhà cửa", LoaiGiaoDich.Chi, "🏠", "#FB8C00",
            "tiền điện", "tiền nước", "điện nước", "internet", "wifi", "tiền nhà", "thuê nhà", "tiền thuê",
            "gas", "tiền rác", "phí chung cư", "chung cư", "sửa nhà", "tiền điện thoại", "cước điện thoại",
            "nạp điện thoại", "nạp tiền", "truyền hình", "bảo hiểm"),
        Tao("suc-khoe", "Sức khỏe", LoaiGiaoDich.Chi, "💊", "#E53935",
            "thuốc", "bác sĩ", "khám bệnh", "khám", "bệnh viện", "viện phí", "nha khoa", "răng", "vitamin",
            "xét nghiệm", "tiêm", "phòng khám", "gym", "yoga", "cắt tóc", "gội đầu", "làm tóc"),
        Tao("giai-tri", "Giải trí", LoaiGiaoDich.Chi, "🎬", "#00897B",
            "xem phim", "phim", "rạp", "karaoke", "game", "du lịch", "đi chơi", "netflix", "youtube",
            "spotify", "bóng đá", "picnic", "câu cá", "cầu lông", "tennis", "bida", "golf"),
        Tao("hoc-tap", "Học tập", LoaiGiaoDich.Chi, "📚", "#3949AB",
            "học phí", "học", "sách", "vở", "bút", "khóa học", "gia sư", "sách vở", "lệ phí thi"),
        Tao("qua-tang", "Quà tặng & biếu", LoaiGiaoDich.Chi, "🎁", "#D81B60",
            "mua quà", "tặng quà", "quà sinh nhật", "biếu", "mừng cưới", "đám cưới", "đám tang", "phúng",
            "lì xì"),
        Tao(KhacChi, "Khác", LoaiGiaoDich.Chi, "🏷️", "#757575",
            "thuốc lá", "từ thiện", "ủng hộ", "phạt"),
        Tao("luong", "Lương", LoaiGiaoDich.Thu, "💼", "#2E7D32",
            "lương", "tiền lương", "nhận lương", "lương tháng", "tiền công", "làm thêm", "tiền làm thêm",
            "freelance"),
        Tao("thuong", "Thưởng", LoaiGiaoDich.Thu, "🏆", "#43A047",
            "thưởng", "tiền thưởng", "bonus", "hoa hồng"),
        Tao(KhacThu, "Thu nhập khác", LoaiGiaoDich.Thu, "💰", "#66BB6A",
            "tiền lãi", "lãi suất", "lãi tiết kiệm", "lãi ngân hàng", "cổ tức", "hoàn tiền", "cashback",
            "tiền bán", "bán đồ", "bán hàng", "thu nhập", "cho thuê", "tiền cho thuê", "nhận tiền", "nhận",
            "được cho", "được tặng", "mẹ cho", "bố cho", "con cho", "con gửi", "cháu cho", "mừng tuổi",
            "được lì xì", "nhận lì xì"),
    };

    public static List<DanhMuc> TatCa => TaoMoi();
}
