// Service worker bản publish: cache toàn bộ file của app để dùng được khi mất mạng hoàn toàn.
// service-worker-assets.js được .NET sinh ra lúc publish, liệt kê mọi file kèm mã băm.
self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
// Người dùng bấm "Cập nhật" ở banner thì mới chuyển sang phiên bản mới
self.addEventListener('message', event => { if (event.data === 'SKIP_WAITING') self.skipWaiting(); });

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.svg$/, /\.blat$/, /\.dat$/, /\.webmanifest$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

// Cloudflare Pages / tên miền riêng phục vụ ở gốc "/". Nếu đặt ở thư mục con (GitHub Pages) phải đổi cả <base href>.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

// Trang gốc được lưu dưới khóa "/" (không dùng "index.html" vì Pages redirect /index.html sang /)
const shellKey = new URL('/', self.origin).href;

async function onInstall(event) {
    const cache = await caches.open(cacheName);

    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => asset.url !== 'index.html')
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await cache.addAll(assetsRequests);

    // Lưu trang gốc trực tiếp từ "/" để không lấy phải phản hồi redirect
    const shell = await fetch(shellKey, { cache: 'no-cache' });
    if (shell.ok) await cache.put(shellKey, shell);
}

async function onActivate(event) {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

// Tạo bản sao phản hồi không mang trạng thái redirect (Safari từ chối phản hồi kiểu này khi mở trang)
async function khongRedirect(response) {
    const blob = await response.blob();
    return new Response(blob, {
        status: response.status,
        statusText: response.statusText,
        headers: response.headers
    });
}

async function onFetch(event) {
    let cachedResponse = null;
    const isNavigate = event.request.mode === 'navigate'
        && !manifestUrlList.some(url => url === event.request.url);

    if (event.request.method === 'GET') {
        const cache = await caches.open(cacheName);
        // Mọi đường dẫn điều hướng (vd /lich-su) trả về trang gốc để Blazor tự định tuyến, kể cả khi offline
        cachedResponse = await cache.match(isNavigate ? shellKey : event.request);
    }
    try {
        if (cachedResponse) {
            return isNavigate ? await khongRedirect(cachedResponse) : cachedResponse;
        }
        const networkResponse = await fetch(event.request);
        return isNavigate && networkResponse.redirected
            ? await khongRedirect(networkResponse)
            : networkResponse;
    } catch (e) {
        // Không có mạng và cũng không có trong cache (ví dụ gọi AI): trả lỗi mạng để app tự xử lý
        return new Response('', { status: 503, statusText: 'Offline' });
    }
}