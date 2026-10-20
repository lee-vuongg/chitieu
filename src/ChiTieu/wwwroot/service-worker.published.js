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

async function onInstall(event) {
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // Mọi đường dẫn điều hướng (vd /lich-su) trả về index.html để Blazor tự định tuyến, kể cả khi offline
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);
        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }
    try {
        return cachedResponse || await fetch(event.request);
    } catch (e) {
        // Không có mạng và cũng không có trong cache (ví dụ gọi AI): trả lỗi mạng để app tự xử lý
        return new Response('', { status: 503, statusText: 'Offline' });
    }
}
