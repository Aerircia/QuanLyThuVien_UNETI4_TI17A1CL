// Họ và tên: Vũ Ngọc Hương Giang
// Mã sinh viên: 23103100280
// Nội dung thực hiện: gửi nút nhắc nhở bằng AJAX, gửi bằng fetch, báo kết quả bằng thông báo nhỏ.
(function () {
  'use strict';

  var modalEl = null, modalInst = null, current = null;

  function ensureModal() {
    if (modalEl) return;
    modalEl = document.createElement('div');
    modalEl.className = 'modal fade';
    modalEl.tabIndex = -1;
    modalEl.innerHTML =
      '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">' +
      '<div class="modal-header"><h5 class="modal-title"></h5><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Đóng"></button></div>' +
      '<div class="modal-body"><p class="mb-0" data-role="msg"></p></div>' +
      '<div class="modal-footer"><button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Hủy</button>' +
      '<button type="button" class="btn btn-primary" data-role="ok">Gửi nhắc</button></div></div></div>';
    document.body.appendChild(modalEl);
    modalInst = new bootstrap.Modal(modalEl);
    modalEl.querySelector('[data-role="ok"]').addEventListener('click', function () {
      var f = current;
      modalInst.hide();
      if (f) send(f);
    });
  }

  function toast(message, ok) {
    var host = document.getElementById('nhac-toast-host');
    if (!host) {
      host = document.createElement('div');
      host.id = 'nhac-toast-host';
      host.className = 'toast-container position-fixed top-0 end-0 p-3';
      host.style.zIndex = '1100';
      document.body.appendChild(host);
    }
    var el = document.createElement('div');
    el.className = 'toast align-items-center border-0 text-white ' + (ok ? 'bg-success' : 'bg-danger');
    el.setAttribute('role', ok ? 'status' : 'alert');
    el.innerHTML = '<div class="d-flex"><div class="toast-body"></div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Đóng"></button></div>';
    el.querySelector('.toast-body').textContent = message;
    host.appendChild(el);
    var t = new bootstrap.Toast(el, { delay: ok ? 3500 : 6000 });
    el.addEventListener('hidden.bs.toast', function () { el.remove(); });
    t.show();
  }

  async function send(form) {
    var btn = form.querySelector('button[type="submit"]');
    var label = btn ? btn.textContent : '';
    if (btn) { btn.disabled = true; btn.textContent = 'Đang gửi...'; }
    try {
      var r = await fetch(form.action, {
        method: 'POST',
        body: new FormData(form),
        headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' },
        credentials: 'same-origin'
      });
      var type = r.headers.get('content-type') || '';
      if (type.indexOf('application/json') < 0) {
        toast(r.redirected ? 'Phiên đăng nhập đã hết hạn. Vui lòng tải lại trang.' : 'Không gửi được nhắc nhở (mã ' + r.status + ').', false);
        if (btn) { btn.disabled = false; btn.textContent = label; }
        return;
      }
      var j = await r.json();
      toast(j.message || (j.success ? 'Đã gửi nhắc nhở.' : 'Không gửi được nhắc nhở.'), !!j.success);
      
      if (btn) {
        var daNhacRoi = (!j.success && j.message && j.message.toLowerCase().indexOf('đã nhắc') !== -1);
        var khoaNut = j.success || daNhacRoi;
        btn.disabled = khoaNut;
        btn.textContent = khoaNut ? 'Đã nhắc' : label;
        if (khoaNut) {
            btn.className = btn.className.replace(/btn-outline-\w+/, 'btn-secondary');
        }
      }
    } catch (e) {
      toast('Không kết nối được máy chủ. Vui lòng thử lại.', false);
      if (btn) { btn.disabled = false; btn.textContent = label; }
    }
  }

  document.addEventListener('submit', function (e) {
    var form = e.target;
    if (!(form instanceof HTMLFormElement) || !form.hasAttribute('data-nhac')) return;
    e.preventDefault();
    ensureModal();
    current = form;
    modalEl.querySelector('.modal-title').textContent = form.getAttribute('data-nhac-title') || 'Xác nhận';
    modalEl.querySelector('[data-role="msg"]').textContent = form.getAttribute('data-nhac-confirm') || 'Gửi nhắc nhở?';
    modalInst.show();
  });
})();