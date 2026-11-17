// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Các tương tác điều hướng dùng chung cho toàn bộ Razor views
(() => {
  'use strict';

  const sidebarToggle = document.querySelector('[data-app-sidebar-toggle]');
  const sidebarClose = document.querySelector('[data-app-sidebar-close]');
  const sidebar = document.getElementById('appSidebar');
  const publicNavToggle = document.querySelector('[data-app-nav-toggle]');
  const publicNav = document.getElementById('appPublicNav');

  const setSidebarOpen = (isOpen) => {
    document.body.classList.toggle('app-sidebar-open', isOpen);
    const isMobile = window.matchMedia('(max-width: 900px)').matches;
    if (sidebar) {
      sidebar.inert = isMobile && !isOpen;
      sidebar.setAttribute('aria-hidden', String(isMobile && !isOpen));
    }
    if (sidebarToggle) {
      sidebarToggle.setAttribute('aria-expanded', String(isOpen));
      sidebarToggle.setAttribute('aria-label', isOpen ? 'Đóng menu điều hướng' : 'Mở menu điều hướng');
    }
  };

  setSidebarOpen(false);

  sidebarToggle?.addEventListener('click', () => {
    setSidebarOpen(!document.body.classList.contains('app-sidebar-open'));
  });

  sidebarClose?.addEventListener('click', () => setSidebarOpen(false));

  sidebar?.querySelectorAll('a[href]').forEach((link) => {
    link.addEventListener('click', () => {
      if (window.matchMedia('(max-width: 900px)').matches) setSidebarOpen(false);
    });
  });

  publicNavToggle?.addEventListener('click', () => {
    if (!publicNav) return;
    const isOpen = publicNav.classList.toggle('is-open');
    publicNavToggle.setAttribute('aria-expanded', String(isOpen));
    publicNavToggle.setAttribute('aria-label', isOpen ? 'Đóng menu' : 'Mở menu');
  });

  // Thu gọn menu mobile sau khi chọn trang
  publicNav?.querySelectorAll('a[href]').forEach((link) => {
    link.addEventListener('click', () => {
      if (window.matchMedia('(max-width: 900px)').matches) {
        publicNav.classList.remove('is-open');
        publicNavToggle?.setAttribute('aria-expanded', 'false');
      }
    });
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      setSidebarOpen(false);
      publicNav?.classList.remove('is-open');
      publicNavToggle?.setAttribute('aria-expanded', 'false');
    }
  });

  // Dọn trạng thái menu mobile còn sót khi chuyển sang màn hình lớn hơn 900px
  window.matchMedia('(min-width: 901px)').addEventListener('change', (event) => {
    if (event.matches) setSidebarOpen(false);
  });
})();
