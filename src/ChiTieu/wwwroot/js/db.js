// Lớp IndexedDB mỏng cho Blazor. Mỗi "store" là một bảng, khóa chính là trường "id".
// Dữ liệu truyền qua lại dưới dạng chuỗi JSON để C# tự tuần tự hóa.
(function () {
  const TEN_DB = 'chitieu-db';
  const PHIEN_BAN = 1;
  const STORES = ['giaoDich', 'danhMuc', 'nganSach', 'mucTieu', 'tuKhoaHoc', 'caiDat'];
  let dbPromise = null;

  // Chuyển lỗi của trình duyệt thành mã ngắn để C# hiển thị thông báo thân thiện.
  function maLoi(e) {
    const ten = (e && (e.name || (e.target && e.target.error && e.target.error.name))) || '';
    if (ten === 'QuotaExceededError') return 'HET_DUNG_LUONG';
    if (ten === 'SecurityError' || ten === 'InvalidStateError' || ten === 'UnknownError') return 'BI_CHAN';
    return 'LOI:' + (ten || String(e));
  }

  function mo() {
    if (dbPromise) return dbPromise;
    dbPromise = new Promise((resolve, reject) => {
      if (!('indexedDB' in window) || !window.indexedDB) { reject(new Error('BI_CHAN')); return; }
      let req;
      try { req = indexedDB.open(TEN_DB, PHIEN_BAN); } catch (e) { reject(new Error(maLoi(e))); return; }
      req.onupgradeneeded = () => {
        const db = req.result;
        for (const s of STORES) {
          if (!db.objectStoreNames.contains(s)) db.createObjectStore(s, { keyPath: 'id' });
        }
      };
      req.onsuccess = () => {
        const db = req.result;
        db.onversionchange = () => { db.close(); dbPromise = null; };
        resolve(db);
      };
      req.onerror = () => reject(new Error(maLoi(req.error)));
      req.onblocked = () => reject(new Error('BI_CHAN'));
    });
    dbPromise.catch(() => { dbPromise = null; });
    return dbPromise;
  }

  function chay(store, che, fn) {
    return mo().then(db => new Promise((resolve, reject) => {
      let tx;
      try { tx = db.transaction(store, che); } catch (e) { reject(new Error(maLoi(e))); return; }
      let kq;
      tx.oncomplete = () => resolve(kq);
      tx.onerror = () => reject(new Error(maLoi(tx.error)));
      tx.onabort = () => reject(new Error(maLoi(tx.error)));
      try { kq = fn(tx.objectStore(store), (v) => { kq = v; }); } catch (e) { reject(new Error(maLoi(e))); }
    })).catch(e => { throw new Error(e && e.message ? e.message : maLoi(e)); });
  }

  window.chitieuDb = {
    // Trả về mảng JSON chuỗi của cả store.
    layTatCa: (store) => chay(store, 'readonly', (os, dat) => {
      const r = os.getAll();
      r.onsuccess = () => dat(JSON.stringify(r.result));
    }),
    luu: (store, json) => chay(store, 'readwrite', (os) => { os.put(JSON.parse(json)); }),
    luuNhieu: (store, jsonMang) => chay(store, 'readwrite', (os) => {
      for (const o of JSON.parse(jsonMang)) os.put(o);
    }),
    xoa: (store, id) => chay(store, 'readwrite', (os) => { os.delete(id); }),
    xoaHet: (store) => chay(store, 'readwrite', (os) => { os.clear(); }),
    // Ghi đè cả store trong MỘT transaction để không bị mất dữ liệu nửa chừng.
    thayThe: (store, jsonMang) => chay(store, 'readwrite', (os) => {
      os.clear();
      for (const o of JSON.parse(jsonMang)) os.put(o);
    }),
    // Xin trình duyệt giữ dữ liệu lâu dài (Safari có thể xóa dữ liệu web nếu lâu không mở).
    xinGiuLau: async () => {
      try { return navigator.storage && navigator.storage.persist ? await navigator.storage.persist() : false; }
      catch { return false; }
    },
    uocLuong: async () => {
      try {
        if (!navigator.storage || !navigator.storage.estimate) return '';
        const e = await navigator.storage.estimate();
        return JSON.stringify({ dung: e.usage || 0, tong: e.quota || 0 });
      } catch { return ''; }
    }
  };
})();
