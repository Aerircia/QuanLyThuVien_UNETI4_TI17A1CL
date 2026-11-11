// Họ và tên: Phùng Ngọc Bảo
// Mã sinh viên: 23103100278
// Nội dung thực hiện: Cập nhật trang thông báo, danh sách và đánh dấu đã đọc
(() => {
  'use strict';
  // Header có chiều cao cố định, ô tìm kiếm được đặt absolute
  const publicHeader = document.getElementById('appPublicHeader');
  const homeHeader = document.querySelector('[data-home-search="true"]');
  const applyHeaderState = () => {
    const scrollTop = window.scrollY || document.documentElement.scrollTop || 0;
    publicHeader?.classList.toggle('is-scrolled', scrollTop > 6);
    homeHeader?.classList.toggle('is-search-visible', scrollTop >= 420);
  };
  let headerFramePending = false;
  const scheduleHeaderState = () => {
    if (headerFramePending) return;
    headerFramePending = true;
    window.requestAnimationFrame(() => {
      applyHeaderState();
      headerFramePending = false;
    });
  };
  applyHeaderState();
  window.addEventListener('scroll', scheduleHeaderState, { passive: true });
  window.addEventListener('resize', scheduleHeaderState, { passive: true });
  window.addEventListener('pageshow', applyHeaderState);

  // Nút Trợ giúp: mở modal bằng Bootstrap API sau khi bundle đã được tải
  document.addEventListener('click', (event) => {
    const trigger = event.target.closest('[data-help-trigger]');
    if (!trigger) return;

    const modalElement = document.getElementById('appHelpModal');
    if (!modalElement) {
      console.error('Không tìm thấy #appHelpModal. Kiểm tra partial _HelpModalPartial.cshtml trong _Layout.cshtml.');
      return;
    }

    if (!window.bootstrap || typeof window.bootstrap.Modal !== 'function') {
      console.error('Bootstrap Modal chưa được tải. Kiểm tra bootstrap.bundle.min.js trong _Layout.cshtml.');
      return;
    }

    event.preventDefault();
    window.bootstrap.Modal.getOrCreateInstance(modalElement).show();
  });

  // Animation xuất hiện theo vùng nhìn, có fallback cho trình duyệt không hỗ trợ
  const isNotificationPage = /^\/ThongBao(?:\/|$)/i.test(window.location.pathname);
  const motionTargets = isNotificationPage ? [] : document.querySelectorAll(
    '.app-public-content .reader-page > *, .app-public-content .reader-page .reader-reveal, ' +
    '.app-public-content .reader-page .reader-notification-row, ' +
    '.app-public-content .lp-hero, .app-public-content .lp-how, .app-public-content .lp-section, ' +
    '.app-public-content .lp-band, .app-public-content .reveal'
  );
  if (motionTargets.length) {
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (!reduceMotion && 'IntersectionObserver' in window) {
      document.documentElement.classList.add('reader-motion-ready');
      const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          entry.target.classList.add('is-visible');
          observer.unobserve(entry.target);
        });
      }, { threshold: 0.08, rootMargin: '0px 0px -35px 0px' });
      motionTargets.forEach((element, index) => {
        element.classList.add('reader-reveal');
        element.style.setProperty('--reader-delay', `${Math.min(index, 8) * 35}ms`);
        observer.observe(element);
      });
    } else {
      motionTargets.forEach((element) => element.classList.add('is-visible'));
    }
  }


  // Lưu cài đặt thông báo ngay trên trang
  const settingsForm = document.querySelector('[data-notification-settings-form]');
  if (settingsForm) {
    const settingsStatus = settingsForm.querySelector('[data-notification-settings-status]');
    const settingsReset = settingsForm.querySelector('[data-notification-settings-reset]');
    const saveSettings = async () => {
      const token = settingsForm.querySelector('input[name="__RequestVerificationToken"]')?.value;
      const fields = [
        'BatThongBaoWeb', 'BatThongBaoEmail', 'NhomMuonTra',
        'NhomGiaHan', 'NhomTienPhat', 'NhomHeThong'
      ];
      const body = new URLSearchParams();
      fields.forEach((name) => {
        const control = settingsForm.elements.namedItem(name);
        body.set(name, control?.checked ? 'true' : 'false');
      });
      if (token) body.set('__RequestVerificationToken', token);
      const submit = settingsForm.querySelector('button[type="submit"]');
      if (submit) submit.disabled = true;
      if (settingsStatus) settingsStatus.textContent = 'Đang lưu...';
      try {
        const response = await fetch(settingsForm.action, {
          method: 'POST',
          headers: {
            'X-Requested-With': 'XMLHttpRequest',
            'Accept': 'application/json',
            'Content-Type': 'application/x-www-form-urlencoded'
          },
          body,
          credentials: 'same-origin'
        });
        const result = await response.json();
        if (!response.ok || !result.success) throw new Error(result.message || 'Không thể lưu cài đặt.');
        if (settingsStatus) settingsStatus.textContent = result.message || 'Đã lưu cài đặt.';
      } catch (error) {
        if (settingsStatus) settingsStatus.textContent = error.message || 'Có lỗi khi lưu cài đặt.';
      } finally {
        if (submit) submit.disabled = false;
      }
    };

    settingsForm.addEventListener('submit', (event) => {
      event.preventDefault();
      void saveSettings();
    });
    settingsReset?.addEventListener('click', () => {
      settingsForm.querySelectorAll('input[type="checkbox"]').forEach((input) => { input.checked = true; });
      void saveSettings();
    });
  }

  const widget = document.querySelector('[data-notification-widget]');
  if (!widget) return;

  const badge = widget.querySelector('[data-notification-count]');
  const list = widget.querySelector('[data-notification-list]');
  const readAllButton = widget.querySelector('[data-notification-read-all]');
  const token = document.querySelector('#notificationTokenForm input[name="__RequestVerificationToken"]')?.value;
  const headers = { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' };
  if (token) headers['RequestVerificationToken'] = token;

  let previousCount = Number.parseInt(String(badge?.textContent || '0').replace('+', ''), 10) || 0;
  const updateCount = (count) => {
    if (!badge) return;
    const value = Number(count || 0);
    badge.textContent = value > 9 ? '9+' : String(value);
    badge.hidden = value <= 0;
    if (value > previousCount) {
      badge.classList.remove('is-bumping');
      void badge.offsetWidth;
      badge.classList.add('is-bumping');
      const trigger = widget.querySelector('.app-notification-trigger');
      trigger?.classList.remove('is-ringing');
      void trigger?.offsetWidth;
      trigger?.classList.add('is-ringing');
    }
    previousCount = value;
    if (readAllButton) readAllButton.disabled = value <= 0;
  };

  const formatDate = (value) => {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    return new Intl.DateTimeFormat('vi-VN', {
      hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric'
    }).format(date);
  };

  const createIcon = (isFine) => {
    const wrapper = document.createElement('span');
    wrapper.className = `app-notification-icon${isFine ? ' is-fine' : ''}`;
    wrapper.setAttribute('aria-hidden', 'true');
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    svg.setAttribute('focusable', 'false');
    const shape = document.createElementNS('http://www.w3.org/2000/svg', isFine ? 'rect' : 'path');
    if (isFine) {
      shape.setAttribute('x', '3'); shape.setAttribute('y', '5');
      shape.setAttribute('width', '18'); shape.setAttribute('height', '14');
      shape.setAttribute('rx', '2');
    } else {
      shape.setAttribute('d', 'M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4');
    }
    svg.append(shape);
    if (isFine) {
      const lines = document.createElementNS('http://www.w3.org/2000/svg', 'path');
      lines.setAttribute('d', 'M7 9h10M7 13h5');
      svg.append(lines);
    }
    wrapper.append(svg);
    return wrapper;
  };

  const renderList = (items) => {
    if (!list) return;
    list.replaceChildren();

    if (!Array.isArray(items) || items.length === 0) {
      const empty = document.createElement('div');
      empty.className = 'app-notification-empty';
      const symbol = document.createElement('span');
      symbol.textContent = '✓';
      const title = document.createElement('strong');
      title.textContent = 'Bạn đã cập nhật!';
      const description = document.createElement('small');
      description.textContent = 'Chưa có thông báo mới.';
      empty.append(symbol, title, description);
      list.append(empty);
      return;
    }

    items.forEach((item) => {
      const row = document.createElement('a');
      const safeTarget = typeof item.duongDan === 'string' && item.duongDan.startsWith('/') && !item.duongDan.startsWith('//')
        ? item.duongDan : '/PhieuMuon/MyLoans';
      row.href = typeof item.openUrl === 'string' && item.openUrl.startsWith('/') && !item.openUrl.startsWith('//')
        ? item.openUrl : safeTarget;
      row.className = `app-notification-item${item.daDoc ? '' : ' is-unread'}`;
      row.dataset.notificationItem = '';
      row.dataset.notificationId = String(item.id);
      row.append(createIcon(item.loai === 'fine'));

      const copy = document.createElement('span');
      copy.className = 'app-notification-copy';
      const title = document.createElement('strong');
      title.textContent = item.tieuDe || 'Thông báo thư viện';
      const body = document.createElement('span');
      body.textContent = item.noiDung || '';
      const date = document.createElement('small');
      date.textContent = formatDate(item.ngayTao);
      copy.append(title, body, date);
      row.append(copy);

      if (!item.daDoc) {
        const dot = document.createElement('i');
        dot.className = 'app-notification-dot';
        dot.setAttribute('aria-label', 'Chưa đọc');
        row.append(dot);
      }
      list.append(row);
    });
  };

  let refreshInFlight = false;
  let refreshTimer = null;

  const refreshBell = async () => {
    if (document.visibilityState === 'hidden' || refreshInFlight) return;
    refreshInFlight = true;
    try {
      const response = await fetch(widget.dataset.bellUrl, {
        headers,
        cache: 'no-store',
        credentials: 'same-origin'
      });
      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          console.warn(`Không lấy được thông báo: HTTP ${response.status}. Hãy đăng nhập lại hoặc kiểm tra quyền truy cập.`);
        }
        return;
      }
      const contentType = response.headers.get('content-type') || '';
      if (!contentType.includes('application/json')) {
        console.warn('Endpoint chuông thông báo không trả về JSON. Kiểm tra route /ThongBao/Bell.');
        return;
      }
      const data = await response.json();
      updateCount(data.chuaDoc);
      renderList(data.danhSach);
    } catch (error) {
      console.debug('Không thể cập nhật thông báo lúc này.', error);
    } finally {
      refreshInFlight = false;
    }
  };

  const stopRefreshTimer = () => {
    if (refreshTimer !== null) {
      window.clearTimeout(refreshTimer);
      refreshTimer = null;
    }
  };

  const scheduleNextRefresh = () => {
    stopRefreshTimer();
    if (document.visibilityState === 'hidden') return;
    // Chờ lần gọi hiện tại kết thúc rồi mới đếm 30 giây tiếp theo, không tạo request chồng nhau
    refreshTimer = window.setTimeout(async () => {
      await refreshBell();
      scheduleNextRefresh();
    }, 30000);
  };

  const refreshAndSchedule = async () => {
    await refreshBell();
    scheduleNextRefresh();
  };

  widget.addEventListener('show.bs.dropdown', () => {
    // Khi người dùng mở thông báo, lấy dữ liệu mới ngay thay vì chờ chu kỳ polling
    void refreshBell();
  });

  widget.addEventListener('click', async (event) => {
    const item = event.target.closest('[data-notification-item]');
    if (item) {
      const id = item.dataset.notificationId;
      if (!id || !token) return;
      event.preventDefault();
      try {
        const body = new URLSearchParams({ id });
        const response = await fetch(widget.dataset.readUrl, {
          method: 'POST',
          headers: { ...headers, 'Content-Type': 'application/x-www-form-urlencoded' },
          body,
          credentials: 'same-origin'
        });
        if (response.ok) {
          const result = await response.json();
          updateCount(result.chuaDoc);
          item.classList.remove('is-unread');
          item.querySelector('.app-notification-dot')?.remove();
        }
      } catch (error) {
        console.warn('Không thể đánh dấu thông báo đã đọc.', error);
      }
      window.location.assign(item.href);
      return;
    }

    if (event.target.closest('[data-notification-read-all]')) {
      if (!token || !readAllButton || readAllButton.disabled) return;
      readAllButton.disabled = true;
      try {
        const response = await fetch(widget.dataset.readAllUrl, {
          method: 'POST',
          headers,
          credentials: 'same-origin'
        });
        if (!response.ok) throw new Error('Yêu cầu không thành công');
        const result = await response.json();
        updateCount(result.chuaDoc);
        list?.querySelectorAll('.app-notification-item.is-unread').forEach((row) => {
          row.classList.remove('is-unread');
          row.querySelector('.app-notification-dot')?.remove();
        });
      } catch (error) {
        console.warn('Không thể đánh dấu tất cả thông báo đã đọc.', error);
        readAllButton.disabled = false;
      }
    }
  });

  // Danh sách ban đầu đã được render từ server trong partial, tránh gọi API trùng ngay khi tải trang
  scheduleNextRefresh();
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
      stopRefreshTimer();
      return;
    }
    void refreshAndSchedule();
  });
  window.addEventListener('pagehide', stopRefreshTimer);
  window.addEventListener('pageshow', (event) => {
    // Chỉ làm mới ngay khi trang được khôi phục từ back-forward cache
    if (event.persisted && document.visibilityState === 'visible') void refreshAndSchedule();
  });
})();
