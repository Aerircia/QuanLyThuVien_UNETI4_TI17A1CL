// Họ và tên: Vũ Tiến Thành
// Mã sinh viên: 23103100257
// Nội dung thực hiện: Điều hướng dùng chung theo hợp đồng data-app-* trong tài liệu site.css
// - data-app-sidebar-toggle / data-app-sidebar-close: sidebar khung nhân viên (class app-sidebar-open trên body)
// - data-app-nav-toggle + #appPublicNav: menu công khai (class is-open)
// Ngưỡng mobile: 900px (khớp media query trong site.css).
(function () {
  'use strict';

  function setSidebarOpen(open) {
    document.body.classList.toggle('app-sidebar-open', open);
    var btn = document.querySelector('[data-app-sidebar-toggle]');
    if (btn) {
      btn.setAttribute('aria-expanded', String(open));
      btn.setAttribute('aria-label', open ? 'Đóng menu điều hướng' : 'Mở menu điều hướng');
    }
  }

  function setNavOpen(open) {
    var nav = document.getElementById('appPublicNav');
    if (nav) nav.classList.toggle('is-open', open);
    var btn = document.querySelector('[data-app-nav-toggle]');
    if (btn) {
      btn.setAttribute('aria-expanded', String(open));
      btn.setAttribute('aria-label', open ? 'Đóng menu' : 'Mở menu');
    }
  }

  document.querySelector('[data-app-sidebar-toggle]')?.addEventListener('click', function () {
    setSidebarOpen(!document.body.classList.contains('app-sidebar-open'));
  });

  // Dùng querySelectorAll để mọi nút đóng (nút X và lớp mờ) đều hoạt động.
  document.querySelectorAll('[data-app-sidebar-close]').forEach(function (el) {
    el.addEventListener('click', function () { setSidebarOpen(false); });
  });

  document.querySelector('[data-app-nav-toggle]')?.addEventListener('click', function () {
    var nav = document.getElementById('appPublicNav');
    setNavOpen(!(nav && nav.classList.contains('is-open')));
  });

  // Bấm vào liên kết trong menu công khai thì tự đóng menu (màn hình nhỏ).
  document.getElementById('appPublicNav')?.addEventListener('click', function (e) {
    if (e.target.closest('a') && window.innerWidth <= 900) setNavOpen(false);
  });

  // Esc đóng cả hai menu.
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
      setSidebarOpen(false);
      setNavOpen(false);
    }
  });

  // Vượt 900px thì dọn trạng thái sidebar còn sót.
  window.matchMedia('(min-width: 901px)').addEventListener('change', function (e) {
    if (e.matches) setSidebarOpen(false);
  });
})();
