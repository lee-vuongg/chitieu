-- Bổ sung cho thống kê: lượt dùng theo từng thiết bị (mã băm) và hồ sơ hiển thị

-- Lượt dùng theo ngày, theo thiết bị và tính năng (để xem từng người dùng làm gì)
CREATE TABLE IF NOT EXISTS luot_ma (
    ngay TEXT NOT NULL,
    ma TEXT NOT NULL,
    ev TEXT NOT NULL,
    so INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (ngay, ma, ev)
);

-- Hồ sơ hiển thị: tên bot (luôn có), tên thật (chỉ khi đồng ý), máy đang dùng,
-- tỷ lệ chi tiêu theo danh mục (JSON, chỉ khi đồng ý riêng)
CREATE TABLE IF NOT EXISTS ho_so (
    ma TEXT PRIMARY KEY,
    bot TEXT NOT NULL,
    ten TEXT,
    may TEXT,
    tyle_json TEXT,
    cap_nhat TEXT NOT NULL
);
