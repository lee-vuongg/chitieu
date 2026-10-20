// Khi chạy `dotnet watch` (môi trường dev) không cache gì để tránh thấy code cũ.
// Bản chạy thật nằm trong service-worker.published.js.
self.addEventListener('fetch', () => { });
