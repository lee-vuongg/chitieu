#!/usr/bin/env python3
"""Sinh icon PWA (PNG) cho app Chi tiêu. Chạy: python tools/sinh-icon.py
Cần Pillow:  pip install pillow
Kết quả ghi vào src/ChiTieu/wwwroot/ (icon-180, icon-192, icon-512, icon-512-maskable) và icon.svg.
Hình: nền mực tối + ba cột tăng dần (xanh = thu, vàng = cảnh báo, cam đỏ = chi), đúng bảng màu của app.
"""
import os
from PIL import Image, ImageDraw

NEN = (29, 36, 48)
CAC_COT = [((47, 191, 113), 0.34), ((242, 176, 30), 0.52), ((226, 85, 43), 0.74)]
SS = 4  # vẽ gấp 4 lần rồi thu nhỏ để mép mịn


def ve(kich_thuoc, bo_goc, vung_an_toan):
    """bo_goc: tỉ lệ bo góc nền (0 = vuông). vung_an_toan: tỉ lệ cạnh chứa hình (maskable dùng ~0.6)."""
    s = kich_thuoc * SS
    anh = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(anh)
    if bo_goc > 0:
        d.rounded_rectangle([0, 0, s - 1, s - 1], radius=int(s * bo_goc), fill=NEN)
    else:
        d.rectangle([0, 0, s, s], fill=NEN)

    vung = s * vung_an_toan
    le = (s - vung) / 2
    rong_cot = vung * 0.22
    khoang = vung * 0.11
    tong = rong_cot * 3 + khoang * 2
    x0 = le + (vung - tong) / 2
    day = le + vung * 0.88
    cao_toi_da = vung * 0.80
    for i, (mau, ti_le) in enumerate(CAC_COT):
        x = x0 + i * (rong_cot + khoang)
        cao = cao_toi_da * ti_le / 0.74
        d.rounded_rectangle([x, day - cao, x + rong_cot, day], radius=int(rong_cot * 0.32), fill=mau)
    # đường nền mảnh
    d.rounded_rectangle([x0 - vung * 0.03, day + vung * 0.03, x0 + tong + vung * 0.03, day + vung * 0.055],
                        radius=int(vung * 0.01), fill=(244, 240, 230))
    return anh.resize((kich_thuoc, kich_thuoc), Image.LANCZOS)


def main():
    goc = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src", "ChiTieu", "wwwroot")
    goc = os.path.normpath(goc)
    ve(192, 0.22, 0.62).save(os.path.join(goc, "icon-192.png"))
    ve(512, 0.22, 0.62).save(os.path.join(goc, "icon-512.png"))
    # maskable: nền tràn hết, hình nằm trong vùng an toàn giữa (80% đường kính)
    ve(512, 0, 0.52).save(os.path.join(goc, "icon-512-maskable.png"))
    # iOS tự bo góc nên dùng ảnh vuông, không trong suốt
    nen = Image.new("RGB", (180, 180), NEN)
    nen.paste(ve(180, 0, 0.62), (0, 0), ve(180, 0, 0.62))
    nen.save(os.path.join(goc, "icon-180.png"))
    print("Đã sinh icon vào", goc)


if __name__ == "__main__":
    main()
