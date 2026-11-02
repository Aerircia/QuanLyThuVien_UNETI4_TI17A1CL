// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Hiện/ẩn mật khẩu, chặn bấm gửi form nhiều lần, hộp thoại xác nhận (data-confirm-*)
(() => {
  'use strict';

  let pendingForm = null;

  const getConfirmEl = () => document.getElementById('confirmModal');
  const getConfirmOk = () => document.getElementById('confirmOk');

  document.addEventListener('click', (e) => {
    // 1. Nút Hiện/Ẩn mật khẩu: data-toggle-password="#idInput"
    const toggle = e.target.closest('[data-toggle-password]');
    if (toggle) {
      const input = document.querySelector(toggle.dataset.togglePassword);
      if (!input) return;
      const show = input.type === 'password';
      input.type = show ? 'text' : 'password';
      toggle.textContent = show ? 'Ẩn' : 'Hiện';
      toggle.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
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

    pendingForm.submit();
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

  // 5. Quay lại bằng nút Back (bfcache): bật lại các nút đã bị khóa.
  window.addEventListener('pageshow', (e) => {
    if (!e.persisted) return;
    document.querySelectorAll('button[type="submit"]').forEach((b) => { b.disabled = false; });
    const confirmOk = getConfirmOk();
    if (confirmOk) confirmOk.disabled = false;
  });
})();
