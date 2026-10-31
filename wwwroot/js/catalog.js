// Họ và tên: <điền tên>
// Mã sinh viên: <điền MSSV>
// Nội dung thực hiện: Form tìm kiếm kho sách: bỏ tham số rỗng khỏi URL, tự gửi khi đổi thể loại/sắp xếp, xóa điều kiện nâng cao
(() => {
  'use strict';

  const form = document.getElementById('catalogForm');
  if (!form) return;

  // URL gọn: không gửi các ô để trống.
  form.addEventListener('submit', () => {
    [...form.elements].forEach((el) => {
      if (el.name && el.type !== 'submit' && el.type !== 'button' && el.value === '') el.disabled = true;
    });
  });

  form.querySelectorAll('[data-autosubmit]').forEach((el) => {
    el.addEventListener('change', () => form.requestSubmit());
  });

  const reset = document.getElementById('advReset');
  if (reset) {
    reset.addEventListener('click', () => {
      form.querySelectorAll('#advModal input, #advModal select').forEach((el) => { el.value = ''; });
    });
  }

  // Quay lại bằng nút Back: bật lại các ô đã bị khóa lúc gửi.
  window.addEventListener('pageshow', () => {
    [...form.elements].forEach((el) => { el.disabled = false; });
  });
})();
