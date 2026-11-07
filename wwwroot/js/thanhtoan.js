// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Xây dựng JavaScript trang thanh toán tiền phạt
// Chọn khoản phạt + tính tổng (MyFines, ThuPhatBanDoc); trang kết quả tự hỏi trạng thái khi đang chờ thanh toán.
(() => {
  'use strict';

  // ---------- Chọn khoản phạt ----------
  const form = document.querySelector('[data-pay-form]');
  if (form) {
    const checks = [...form.querySelectorAll('[data-fine-check]')];
    const all = form.querySelector('[data-fine-all]');
    const totalEl = form.querySelector('[data-pay-total]');
    const countEl = form.querySelector('[data-pay-count]');
    const submit = form.querySelector('[data-pay-submit]');
    const fmt = new Intl.NumberFormat('vi-VN');

    // Số tiền chỉ để hiển thị; server tính lại từ cơ sở dữ liệu khi thanh toán.
    const update = () => {
      const chosen = checks.filter((c) => c.checked && !c.disabled);
      const total = chosen.reduce((s, c) => s + (Number(c.dataset.amount) || 0), 0);
      if (totalEl) totalEl.textContent = fmt.format(total) + ' ₫';
      if (countEl) countEl.textContent = String(chosen.length);
      if (submit) submit.disabled = chosen.length === 0;
      if (all) {
        const enabled = checks.filter((c) => !c.disabled);
        all.checked = enabled.length > 0 && enabled.every((c) => c.checked);
        all.indeterminate = chosen.length > 0 && !all.checked;
      }
    };

    checks.forEach((c) => c.addEventListener('change', update));
    if (all) all.addEventListener('change', () => {
      checks.forEach((c) => { if (!c.disabled) c.checked = all.checked; });
      update();
    });
    window.addEventListener('pageshow', update);   // giữ đúng tổng khi quay lại bằng nút Back
    update();
  }

  // ---------- Trang kết quả: hỏi trạng thái định kỳ ----------
  const box = document.querySelector('[data-pay-status-url]');
  if (box) {
    const url = box.dataset.payStatusUrl;
    const label = box.querySelector('[data-pay-status-label]');
    const expires = box.dataset.expires ? Date.parse(box.dataset.expires) : 0;
    let stopped = false;

    const tick = async () => {
      if (stopped) return;
      try {
        const res = await fetch(url, { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (res.ok) {
          const data = await res.json();
          if (data.final) { stopped = true; location.reload(); return; }
          if (label && data.status === 'Error') label.textContent = data.label;
        }
      } catch { /* mạng lỗi: thử lại ở lần sau */ }
      // Quá hạn: Status ở trên đã nhờ server đóng giao dịch; nếu vẫn chưa xong thì dừng hỏi, tránh vòng lặp tải lại.
      if (expires && Date.now() > expires + 30000) {
        stopped = true;
        if (label) label.textContent = 'Chưa nhận được kết quả. Hãy tải lại trang sau ít phút.';
        return;
      }
      setTimeout(tick, 3000);
    };
    setTimeout(tick, 3000);
  }
})();
