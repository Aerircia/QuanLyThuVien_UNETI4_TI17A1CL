// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: 6 ô nhập OTP gộp vào một trường ẩn (MaOtp), dán mã, đếm ngược nút gửi lại mã
(() => {
  'use strict';

  const boxes = [...document.querySelectorAll('.otp-box')];
  const hidden = document.getElementById('MaOtp');

  if (boxes.length && hidden) {
    const sync = () => { hidden.value = boxes.map((b) => b.value).join(''); };

    boxes.forEach((box, i) => {
      box.addEventListener('input', () => {
        box.value = box.value.replace(/\D/g, '').slice(-1);
        if (box.value && boxes[i + 1]) boxes[i + 1].focus();
        sync();
      });

      box.addEventListener('keydown', (e) => {
        if (e.key === 'Backspace' && !box.value && boxes[i - 1]) boxes[i - 1].focus();
        if (e.key === 'ArrowLeft' && boxes[i - 1]) boxes[i - 1].focus();
        if (e.key === 'ArrowRight' && boxes[i + 1]) boxes[i + 1].focus();
      });

      box.addEventListener('paste', (e) => {
        const digits = ((e.clipboardData && e.clipboardData.getData('text')) || '').replace(/\D/g, '').slice(0, boxes.length);
        if (!digits) return;
        e.preventDefault();
        boxes.forEach((b, k) => { b.value = digits[k] || ''; });
        boxes[Math.min(digits.length, boxes.length - 1)].focus();
        sync();
      });
    });

    boxes[0].focus();
  }

  // Đếm ngược trước khi cho gửi lại mã (server vẫn kiểm tra lại thời gian chờ).
  const btn = document.getElementById('resendBtn');
  if (btn) {
    const label = btn.dataset.label || 'Gửi lại mã';
    let left = parseInt(btn.dataset.cooldown || '0', 10) || 0;
    const tick = () => {
      if (left > 0) {
        btn.disabled = true;
        btn.textContent = label + ' (' + left + 's)';
        left -= 1;
        setTimeout(tick, 1000);
      } else {
        btn.disabled = false;
        btn.textContent = label;
      }
    };
    tick();
  }
})();
