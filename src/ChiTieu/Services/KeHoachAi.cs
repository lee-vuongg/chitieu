namespace ChiTieu.Services;

/// <summary>Một hạn mức AI đề xuất cho một danh mục (null = ngân sách tổng).</summary>
public record AiHanMuc(string? DanhMucId, long SoTien, string? GhiChu);

/// <summary>Kết quả kế hoạch chi tiêu AI trả về.</summary>
public record AiKeHoach(string? TomTat, List<AiHanMuc>? HanMuc, List<string>? LoiKhuyen);
