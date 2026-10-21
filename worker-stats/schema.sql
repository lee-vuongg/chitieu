-- Thống kê ẩn danh: không có số tiền, ghi chú hay danh mục.

-- Tổng số thiết bị đã từng dùng app (mã băm, không đảo ngược được)
CREATE TABLE IF NOT EXISTS thiet_bi (
    ma TEXT PRIMARY KEY,
    lan_dau TEXT NOT NULL
);

-- Người dùng hoạt động theo ngày (mã băm kèm ngày, không liên kết được giữa các ngày)
CREATE TABLE IF NOT EXISTS nguoi_dung (
    ngay TEXT NOT NULL,
    ma TEXT NOT NULL,
    PRIMARY KEY (ngay, ma)
);

-- Số lượt dùng từng tính năng theo ngày và nền tảng
CREATE TABLE IF NOT EXISTS su_kien (
    ngay TEXT NOT NULL,
    ev TEXT NOT NULL,
    nen TEXT NOT NULL,
    so INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (ngay, ev, nen)
);
