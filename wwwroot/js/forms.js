// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Hiện/ẩn mật khẩu, chặn bấm gửi form nhiều lần, hộp thoại xác nhận (data-confirm-*)
(() => {
  'use strict';

  let pendingForm = null;

  const confirmEl = document.getElementById('confirmModal');
  const confirmOk = document.getElementById('confirmOk');

  document.addEventListener('click', (e) => {
    // Nút Hiện/Ẩn mật khẩu: data-toggle-password="#idInput"
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

    // Nút cần xác nhận: data-confirm-form="#formId" + data-confirm-title/message/ok/class
    const trigger = e.target.closest('[data-confirm-form]');
    if (trigger && confirmEl && confirmOk && window.bootstrap) {
      pendingForm = document.querySelector(trigger.dataset.confirmForm);
      if (!pendingForm) return;
      document.getElementById('confirmTitle').textContent = trigger.dataset.confirmTitle || 'Xác nhận';
      document.getElementById('confirmMessage').textContent = trigger.dataset.confirmMessage || 'Bạn có chắc chắn?';
      confirmOk.textContent = trigger.dataset.confirmOk || 'Đồng ý';
      confirmOk.className = 'btn ' + (trigger.dataset.confirmClass || 'btn-primary');
      bootstrap.Modal.getOrCreateInstance(confirmEl).show();
    }
  });

  if (confirmOk) {
    confirmOk.addEventListener('click', () => {
      if (!pendingForm) return;
      confirmOk.disabled = true; // tránh bấm hai lần khi đang gửi
      pendingForm.submit();
    });
  }

  // Chặn bấm gửi nhiều lần cho form POST. Đợi hết vòng submit để biết validation có chặn hay không.
  document.addEventListener('submit', (e) => {
    const form = e.target;
    if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== 'post') return;
    setTimeout(() => {
      if (e.defaultPrevented) return;
      form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach((b) => { b.disabled = true; });
    }, 0);
  });

  // Quay lại bằng nút Back (bfcache): bật lại các nút đã bị khóa.
  window.addEventListener('pageshow', (e) => {
    if (!e.persisted) return;
    document.querySelectorAll('button[type="submit"]').forEach((b) => { b.disabled = false; });
    if (confirmOk) confirmOk.disabled = false;
  });
})();
