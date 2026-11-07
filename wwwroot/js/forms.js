// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Hiện/ẩn mật khẩu, chặn bấm gửi form nhiều lần, hộp thoại xác nhận (data-confirm-*)
(() => {
  'use strict';

  // Chặn nạp hai lần (layout và view cùng nhúng file này sẽ làm nút hiện/ẩn bật rồi tắt ngay).
  if (window.__formsJsLoaded) return;
  window.__formsJsLoaded = true;

  let pendingForm = null;

  const getConfirmEl = () => document.getElementById('confirmModal');
  const getConfirmOk = () => document.getElementById('confirmOk');

  // ---------- Hiện/ẩn mật khẩu ----------
  // Đổi icon bằng JS để không phụ thuộc vào CSS :last-child.
  const setPwState = (btn, show) => {
    btn.classList.toggle('is-visible', show);
    btn.setAttribute('aria-pressed', String(show));
    btn.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
    const icons = btn.querySelectorAll('.ico');
    if (icons.length >= 2) {
      icons[0].style.display = show ? 'none' : 'block';
      icons[1].style.display = show ? 'block' : 'none';
    }
  };

  // Đồng bộ icon theo type thực tế của ô nhập (lúc tải trang và khi quay lại bằng nút Back).
  const syncPasswordButtons = () => {
    document.querySelectorAll('[data-toggle-password]').forEach((btn) => {
      const input = document.querySelector(btn.dataset.togglePassword);
      if (input) setPwState(btn, input.type !== 'password');
    });
  };

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', syncPasswordButtons);
  else syncPasswordButtons();

  document.addEventListener('click', (e) => {
    // 1. Nút Hiện/Ẩn mật khẩu: data-toggle-password="#idInput"
    const toggle = e.target.closest('[data-toggle-password]');
    if (toggle) {
      const input = document.querySelector(toggle.dataset.togglePassword);
      if (!input) return;
      const show = input.type === 'password';
      const start = input.selectionStart;
      const end = input.selectionEnd;
      input.type = show ? 'text' : 'password';
      setPwState(toggle, show);
      input.focus({ preventScroll: true });
      try { input.setSelectionRange(start, end); } catch { /* một số trình duyệt không hỗ trợ */ }
      return;
    }

    // 2. Nút cần xác nhận: data-confirm-form="#formId" + data-confirm-title/message/ok/class
    const trigger = e.target.closest('[data-confirm-form]');
    if (trigger && window.bootstrap) {
      const confirmEl = getConfirmEl();
      const confirmOk = getConfirmOk();
      if (!confirmEl || !confirmOk) return;

      pendingForm = document.querySelector(trigger.dataset.confirmForm);
      if (!pendingForm) return;

      // Kiểm tra tính hợp lệ của form (nếu có các trường required hoặc validation) trước khi mở modal
      if (window.jQuery && jQuery(pendingForm).valid && !jQuery(pendingForm).valid()) {
        const bad = pendingForm.querySelector('.input-validation-error');
        if (bad) bad.focus();
        return;
      }
      if (pendingForm.checkValidity && !pendingForm.checkValidity()) {
        pendingForm.reportValidity();
        return;
      }

      const titleEl = document.getElementById('confirmTitle');
      const msgEl = document.getElementById('confirmMessage');
      if (titleEl) titleEl.textContent = trigger.dataset.confirmTitle || 'Xác nhận';
      if (msgEl) msgEl.textContent = trigger.dataset.confirmMessage || 'Bạn có chắc chắn?';
      confirmOk.textContent = trigger.dataset.confirmOk || 'Đồng ý';
      confirmOk.className = 'btn ' + (trigger.dataset.confirmClass || 'btn-primary');
      confirmOk.disabled = false;

      bootstrap.Modal.getOrCreateInstance(confirmEl).show();
    }
  });

  // 3. Xử lý khi bấm nút "Đồng ý" trong hộp thoại xác nhận
  document.addEventListener('click', (e) => {
    const okBtn = e.target.closest('#confirmOk');
    if (!okBtn || !pendingForm) return;

    okBtn.disabled = true; // tránh bấm hai lần khi đang gửi

    const confirmEl = getConfirmEl();
    if (confirmEl && window.bootstrap) {
      bootstrap.Modal.getInstance(confirmEl)?.hide();
    }

    if (typeof pendingForm.requestSubmit === 'function') {
      pendingForm.requestSubmit();
    } else {
      pendingForm.submit();
    }
  });

  // 4. Chặn bấm gửi nhiều lần cho form POST. Đợi hết vòng submit để biết validation có chặn hay không.
  document.addEventListener('submit', (e) => {
    const form = e.target;
    if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== 'post') return;
    setTimeout(() => {
      if (e.defaultPrevented) return;
      form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach((b) => { b.disabled = true; });
    }, 0);
  });

  // 5. Quay lại bằng nút Back (bfcache): bật lại các nút đã bị khóa, đồng bộ lại icon mật khẩu.
  window.addEventListener('pageshow', (e) => {
    syncPasswordButtons();
    if (!e.persisted) return;
    document.querySelectorAll('button[type="submit"]').forEach((b) => { b.disabled = false; });
    const confirmOk = getConfirmOk();
    if (confirmOk) confirmOk.disabled = false;
  });

  // Gửi CRUD mà không tải lại trang; thành công điều hướng về danh sách, lỗi trả lại nội dung form để hiện validation.
  document.addEventListener('submit', async (e) => {
    const form = e.target;
    if (!(form instanceof HTMLFormElement) || !form.matches('[data-ajax-crud="form"]')) return;

    e.preventDefault();
    const submitButtons = Array.from(form.querySelectorAll('button[type="submit"], input[type="submit"], button[type="button"]'));
    submitButtons.forEach((button) => { button.disabled = true; });

    try {
      const response = await fetch(form.action, {
        method: form.method || 'POST',
        body: new FormData(form),
        headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json, text/html;q=0.9' }
      });

      const contentType = response.headers.get('content-type') || '';
      if (contentType.includes('application/json')) {
        const result = await response.json();
        if (result.success === false && result.message) {
          // Thao tác bị chặn phía server: hiện thông báo ngay trong card, giữ form để sửa và gửi lại.
          const card = form.closest('.app-form-card, .app-card');
          if (card) {
            card.querySelector('.ajax-crud-error')?.remove();
            const alertEl = document.createElement('div');
            alertEl.className = 'alert alert-danger ajax-crud-error';
            alertEl.setAttribute('role', 'alert');
            alertEl.textContent = result.message;
            card.prepend(alertEl);
            card.scrollIntoView({ behavior: 'smooth', block: 'start' });
          }
          return;
        }
        if (result.redirectUrl) {
          window.location.assign(result.redirectUrl);
          return;
        }
      } else {
        const html = await response.text();
        const parsed = new DOMParser().parseFromString(html, 'text/html');
        // Các view CRUD dùng .app-card, một số trang (TheLoai/Delete) dùng .app-form-card.
        const newCard = parsed.querySelector('.app-form-card, .app-card');
        const currentCard = form.closest('.app-form-card, .app-card');
        if (newCard && currentCard) {
          currentCard.replaceWith(newCard);
          // DOMParser không thực thi script; tạo lại để script nhúng trong card (nếu có) chạy được.
          newCard.querySelectorAll('script').forEach((oldScript) => {
            const script = document.createElement('script');
            if (oldScript.src) script.src = oldScript.src;
            else script.textContent = oldScript.textContent;
            oldScript.replaceWith(script);
          });
          return;
        }
        // Không tìm thấy card để thay (trang lỗi 500, trang lạ): nạp lại để hiện lỗi thay vì im lặng.
        window.location.reload();
        return;
      }
    } catch (error) {
      console.error('CRUD AJAX failed', error);
      window.location.reload();
      return;
    } finally {
      submitButtons.forEach((button) => { button.disabled = false; });
    }
  });
})();
