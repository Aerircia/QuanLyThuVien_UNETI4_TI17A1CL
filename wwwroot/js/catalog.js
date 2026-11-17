// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Duyệt kho sách bằng AJAX (không tải lại trang): tìm kiếm trực tiếp, lọc, sắp xếp, phân trang, lịch sử trình duyệt
(() => {
  'use strict';

  const form = document.getElementById('catalogForm');
  const results = document.getElementById('catalogResults');

  // Không có fetch thì để form GET và liên kết hoạt động như bình thường.
  if (!form || !results || !window.fetch || !window.URLSearchParams) return;
  document.documentElement.classList.add('js-catalog');

  const base = form.getAttribute('action') || location.pathname;
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const DEBOUNCE_MS = 350;

  let timer = null;
  let current = null;                       // AbortController của yêu cầu đang chạy
  let lastUrl = location.pathname + location.search;

  history.replaceState({ catalog: true }, '', location.href);

  // ---------- Dựng URL từ trạng thái của form (bỏ ô trống và giá trị mặc định) ----------
  function buildUrl() {
    const params = new URLSearchParams();
    for (const el of form.elements) {
      if (!el.name || el.disabled || el.type === 'submit' || el.type === 'button') continue;
      if ((el.type === 'radio' || el.type === 'checkbox') && !el.checked) continue;
      const value = (el.value || '').trim();
      if (value === '' || value === (el.dataset.default ?? '')) continue;
      params.set(el.name, value);
    }
    const qs = params.toString();
    return base + (qs ? '?' + qs : '');
  }

  // ---------- Đồng bộ ngược: URL -> các ô trong form (khi bấm chip, Back/Forward, xóa lọc) ----------
  function syncForm(url) {
    const params = new URL(url, location.href).searchParams;
    for (const el of form.elements) {
      if (!el.name || el.type === 'submit' || el.type === 'button') continue;
      const value = params.get(el.name);
      if (el.type === 'radio') el.checked = (value ?? '') === el.value;
      else el.value = value ?? el.dataset.default ?? '';
    }
  }

  function selectedCategory() {
    const checked = form.querySelector('input[name="maTheLoai"]:checked');
    return checked ? checked.value : '';
  }

  function updateChrome() {
    // Viên thuốc thể loại (mobile)
    const cat = selectedCategory();
    document.querySelectorAll('[data-pill]').forEach((p) => p.classList.toggle('on', (p.dataset.cat || '') === cat));

    // Số bộ lọc đang bật trên nút "Bộ lọc"
    const val = (name) => { const c = form.elements[name]; return c ? (c.value || '') : ''; };
    let n = 0;
    ['maTheLoai', 'tacGia', 'tonKho', 'trangThai'].forEach((k) => { if (val(k)) n += 1; });
    if (val('namTu') || val('namDen')) n += 1;
    if (val('giaTu') || val('giaDen')) n += 1;
    const badge = document.getElementById('filterCount');
    if (badge) { badge.textContent = String(n); badge.hidden = n === 0; }
  }

  // ---------- Tải kết quả ----------
  async function load(url, opts = {}) {
    const { mode = 'push', sync = false, scroll = false, typing = false } = opts;
    // Hủy request cũ trước cả khi bỏ qua URL đã tải; nếu không response cũ
    // vẫn có thể ghi đè trạng thái người dùng vừa khôi phục.
    if (current) {
      current.abort();
      current = null;
    }
    if (mode !== 'pop' && url === lastUrl) {
      results.classList.remove('is-loading');
      results.removeAttribute('aria-busy');
      return;
    }

    const mine = new AbortController();
    current = mine;
    results.classList.add('is-loading');
    results.setAttribute('aria-busy', 'true');

    try {
      const res = await fetch(url, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin',
        signal: mine.signal
      });
      if (!res.ok) throw new Error('HTTP ' + res.status);
      const html = await res.text();

      results.innerHTML = html;
      lastUrl = url;

      // Gõ liên tục chỉ thay mục lịch sử đang gõ, không thêm mục mới cho mỗi lần dừng tay.
      if (mode === 'push') {
        const replaceTyping = typing && history.state && history.state.typing;
        const state = { catalog: true, typing };
        if (replaceTyping) history.replaceState(state, '', url);
        else history.pushState(state, '', url);
      }

      if (sync) syncForm(url);
      updateChrome();

      if (scroll) {
        results.scrollIntoView({ block: 'start', behavior: reduced ? 'auto' : 'smooth' });
        results.focus({ preventScroll: true });
      }
    } catch (err) {
      if (err.name === 'AbortError') return;
      location.href = url;                  // lỗi mạng hoặc server: về cách tải trang bình thường
    } finally {
      if (current === mine) {
        results.classList.remove('is-loading');
        results.removeAttribute('aria-busy');
      }
    }
  }

  const run = (opts) => { clearTimeout(timer); load(buildUrl(), opts); };

  // ---------- Form: gửi, đổi giá trị, gõ ----------
  form.addEventListener('submit', (e) => {
    e.preventDefault();
    run({ mode: 'push' });
  });

  form.addEventListener('change', (e) => {
    if (!e.target.name) return;
    run({ mode: 'push' });
  });

  form.addEventListener('input', (e) => {
    const el = e.target;
    if (!el.matches('input:not([type="radio"]):not([type="checkbox"]):not([type="hidden"])')) return;

    // Tìm kiếm: chỉ chạy khi trống (để xóa bộ lọc) hoặc từ 2 ký tự trở lên.
    if (el.name === 'search' && el.value.trim().length === 1) return;
    // Năm: chờ đủ 4 chữ số để không lọc theo "19" khi đang gõ "1990".
    if ((el.name === 'namTu' || el.name === 'namDen') && el.value !== '' && el.value.length !== 4) return;

    clearTimeout(timer);
    timer = setTimeout(() => load(buildUrl(), { mode: 'push', typing: true }), DEBOUNCE_MS);
  });

  // ---------- Liên kết: chip, kiểu xem, phân trang, viên thuốc, xóa lọc ----------
  document.addEventListener('click', (e) => {
    if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;

    const link = e.target.closest('a[data-ajax], #catalogResults nav[aria-label="Phân trang"] a');
    if (link) {
      e.preventDefault();
      if (link.closest('.disabled')) return;
      load(link.pathname + link.search, { mode: 'push', sync: true, scroll: true });
      return;
    }

    const pill = e.target.closest('[data-pill]');
    if (pill) {
      e.preventDefault();
      const radio = form.querySelector('input[name="maTheLoai"][value="' + (pill.dataset.cat || '') + '"]');
      if (radio) { radio.checked = true; run({ mode: 'push' }); }
      return;
    }

    if (e.target.closest('#clearFilters')) {
      e.preventDefault();
      form.querySelectorAll('[data-filter]').forEach((el) => {
        if (el.type === 'radio') el.checked = el.value === '';
        else el.value = '';
      });
      run({ mode: 'push' });
    }
  });

  // ---------- Back / Forward ----------
  window.addEventListener('popstate', (e) => {
    if (!e.state || !e.state.catalog) return;
    clearTimeout(timer);
    load(location.pathname + location.search, { mode: 'pop', sync: true });
  });
})();
