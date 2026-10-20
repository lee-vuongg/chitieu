# Chi tiêu — sổ chi tiêu cá nhân (PWA)

Ứng dụng quản lý chi tiêu cho một người dùng, viết bằng **C# + Blazor WebAssembly (.NET 9)**, chạy hoàn toàn trên trình duyệt, dữ liệu lưu trong **IndexedDB** của máy. Không có backend. Dùng được khi mất mạng.

- Gõ câu tự nhiên: “ăn phở 50k”, “đổ xăng 80 nghìn”, “lương 10tr”, “hôm qua cà phê 35k” → lưu bằng 1 thao tác.
- Lịch sử, tổng quan tháng (donut, cột theo ngày, so với tháng trước), ngân sách + cảnh báo 80% / 100% / dự báo, mục tiêu tiết kiệm có confetti.
- Sao lưu JSON qua Share Sheet của iOS, nhập lại (gộp hoặc ghi đè), nhắc sao lưu sau 14 ngày.
- AI Gemini **tùy chọn** qua Cloudflare Worker. Không cài AI, app vẫn chạy 100%.

## Cấu trúc

```
ChiTieu.sln
src/ChiTieu.Core/     Logic thuần, không phụ thuộc trình duyệt: Models, ParserService, NganSachService,
                      MucTieuService, ThongKeService, BackupCore, AiCore, DinhDang
src/ChiTieu/          Ứng dụng Blazor WASM: Services (DbService, GiaoDichService, BackupService, AiService…),
                      Components, Pages, Layout, wwwroot (css, js/db.js, manifest, service worker, icon)
tests/ChiTieu.Tests/  xUnit: parser (hơn 60 ca), ngân sách/dự báo, mục tiêu, thống kê, backup, định dạng
worker/               Cloudflare Worker giữ API key Gemini
tools/sinh-icon.py    Sinh lại icon PNG
```

Logic tính toán nằm trong `ChiTieu.Core` để test được mà không cần trình duyệt; các file `.razor` chỉ hiển thị.

## Chạy trên máy (Windows)

Cần .NET SDK 9 (`dotnet --version` ra 9.x).

```powershell
cd ChiTieu
dotnet restore
dotnet build
dotnet test
dotnet watch --project src/ChiTieu
```

Mở địa chỉ `http://localhost:xxxx` hiện ra. Khi chạy `dotnet watch`, service worker **không** cache (để thấy code mới ngay). Chế độ offline chỉ hoạt động với bản publish.

## Publish và đưa lên Cloudflare Pages

```powershell
dotnet publish src/ChiTieu -c Release -o publish
```

Thư mục web để triển khai là `publish/wwwroot`.

**Cách 1 — kéo thả (nhanh nhất):**
1. Vào Cloudflare Dashboard → *Workers & Pages* → *Create* → *Pages* → *Upload assets*.
2. Đặt tên dự án (ví dụ `chitieu`), kéo thả thư mục `publish/wwwroot`, bấm *Deploy*.
3. Địa chỉ có dạng `https://chitieu.pages.dev` (đã là HTTPS).

**Cách 2 — wrangler:**
```powershell
npx wrangler pages deploy publish/wwwroot --project-name chitieu
```

Lưu ý khi dùng Cloudflare Pages:
- **base href**: đã để `<base href="/" />` vì Pages phục vụ ở gốc tên miền. Không cần sửa gì. (Nếu sau này đặt ở thư mục con như GitHub Pages `/ten-repo/` thì phải sửa `<base href>` trong `index.html` **và** biến `base` trong `service-worker.published.js`.)
- **404.html / routing SPA**: không cần. Cloudflare Pages tự trả `index.html` cho mọi đường dẫn nếu dự án **không có** file `404.html`, nên mở thẳng `/lich-su` vẫn chạy. Service worker cũng tự trả `index.html` cho điều hướng khi offline.
- File `_headers` (đã có trong `wwwroot`) bắt trình duyệt luôn kiểm tra bản mới của `service-worker.js` và `index.html`.
- Mỗi lần triển khai bản mới, người dùng sẽ thấy banner “Có phiên bản mới — Cập nhật” khi mở lại app.

## Thử trên iPhone (cần HTTPS)

Service worker và “Thêm vào Màn hình chính” chỉ hoạt động trên **HTTPS**.

1. Triển khai lên Cloudflare Pages như trên, mở địa chỉ `https://…pages.dev` bằng **Safari** (không phải Chrome).
2. Bấm nút **Chia sẻ** → **Thêm vào Màn hình chính** → **Thêm**. (App cũng tự hiện hướng dẫn này một lần.)
3. Mở từ biểu tượng trên màn hình chính: chạy full màn hình.
4. **Mở app một lần khi có mạng** để tải xong dữ liệu offline, sau đó bật Chế độ máy bay và mở lại để kiểm tra.
5. Đóng hẳn app rồi mở lại: dữ liệu vẫn còn.

Thử nhanh trong lúc code, không cần deploy: `dotnet watch` rồi dùng `npx cloudflared tunnel --url http://localhost:PORT` để có địa chỉ HTTPS tạm. Cách này chỉ để xem giao diện, service worker không cache ở chế độ dev.

Lưu ý: Safari có thể xóa dữ liệu web của trang không dùng suốt nhiều tuần nếu app **chưa được thêm vào Màn hình chính**. App đã xin chế độ lưu bền và nhắc sao lưu sau 14 ngày, nhưng vẫn nên sao lưu định kỳ.

## Sao lưu và khôi phục

- **Sao lưu**: *Cài đặt → Sao lưu ra file*. iPhone mở Share Sheet, chọn *Lưu vào Tệp* (iCloud Drive) hoặc gửi email/Zalo cho chính mình. Nếu trình duyệt không hỗ trợ chia sẻ file thì file `chitieu-backup-….json` được tải về.
- **Khôi phục**: *Cài đặt → Khôi phục từ file*, chọn file `.json`, rồi chọn:
  - **Gộp**: giữ dữ liệu hiện có, chỉ thêm phần chưa có (theo mã nhận dạng).
  - **Ghi đè**: thay toàn bộ bằng nội dung file (có bước xác nhận).
- App nhắc sao lưu khi đã quá 14 ngày (hoặc chưa từng sao lưu) và có dữ liệu.

## AI Gemini (tùy chọn)

AI chỉ dùng khi: đã cấu hình, có mạng, và parser offline **không chắc** (độ tin cậy dưới 0,7). Mọi lỗi (mất mạng, hết quota, sai mã) đều quay về parser offline và cho chọn danh mục bằng tay.

Dữ liệu gửi đi:
- **Phân tích câu nhập**: chính câu người dùng gõ (ví dụ “bé tí 70k”) + danh sách danh mục + ngày hôm nay.
- **Hỏi đáp / Tổng kết tháng**: chỉ số liệu tổng hợp (tổng chi theo ngày × danh mục, theo tháng), **không** gửi ghi chú hay mã giao dịch.

### Cài Worker

```powershell
cd worker
npm install -g wrangler        # hoặc dùng npx wrangler ...
npx wrangler login
npx wrangler deploy
npx wrangler secret put GEMINI_API_KEY    # dán API key lấy từ https://aistudio.google.com/apikey
npx wrangler secret put APP_SECRET        # tự đặt một chuỗi dài, ví dụ 24 ký tự ngẫu nhiên
npx wrangler secret put ALLOWED_ORIGIN    # ví dụ https://chitieu.pages.dev  (nhiều địa chỉ: ngăn cách bằng dấu phẩy)
```

Sau `deploy` bạn nhận địa chỉ dạng `https://chitieu-ai.<tài-khoản>.workers.dev`. Trong app: *Cài đặt → Trợ lý AI* nhập địa chỉ đó và `APP_SECRET`, bấm **Lưu**.

Worker kiểm tra: đúng `Origin` (CORS không dùng dấu `*`), đúng mã bí mật (`Authorization: Bearer …`), rồi mới gọi Gemini bằng key lưu trong secret. Mã bí mật cũng được lưu trong IndexedDB của máy và đi vào bản sao lưu, vì vậy hãy cất file sao lưu cẩn thận.

Nếu cần đổi model, sửa hằng số `MODEL` ở đầu `worker/worker.js`.

## Giả định và giới hạn

- Hạn mức ngân sách là hạn mức **lặp lại mỗi tháng** (đặt một lần, áp dụng các tháng sau).
- Dự báo cuối tháng = tổng chi đến nay ÷ số ngày đã qua × số ngày trong tháng, chỉ tính từ ngày thứ 3 của tháng.
- “Hôm nay” tính theo múi giờ của trình duyệt (Blazor WASM mặc định là UTC nên app lấy độ lệch múi giờ từ JS).
- Số không có đơn vị như “phở 50” hiểu là 50 nghìn nhưng độ tin cậy thấp. Số từ 1.000 trở lên như “1.250.000” giữ nguyên.
- Hiệu ứng đếm số tiền dùng một hàm JS nhỏ (CSS không định dạng được dấu chấm hàng nghìn); mọi hiệu ứng khác là CSS thuần và tôn trọng `prefers-reduced-motion`.
- Nhãn thanh điều hướng dưới cỡ 15px (để 5 mục vừa màn hình 390px); mọi chữ khác từ 16px.
- Rung (`navigator.vibrate`) iOS Safari không hỗ trợ nên bị bỏ qua êm trên iPhone.
