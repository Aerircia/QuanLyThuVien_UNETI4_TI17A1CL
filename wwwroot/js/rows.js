// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: Hàng cuộn ngang ([data-rail]): nút mũi tên trái/phải, kéo chuột để cuộn, ẩn nút khi không còn gì để cuộn
(() => {
  'use strict';

  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  document.querySelectorAll('[data-rail]').forEach((wrap) => {
    const track = wrap.querySelector('.rail-track');
    if (!track) return;

    const prev = wrap.querySelector('.rail-arrow.prev');
    const next = wrap.querySelector('.rail-arrow.next');

    const update = () => {
      const max = track.scrollWidth - track.clientWidth;
      wrap.classList.toggle('no-scroll', max <= 4);
      wrap.classList.toggle('at-start', track.scrollLeft <= 4);
      wrap.classList.toggle('at-end', track.scrollLeft >= max - 4);
    };

    const by = (dir) => track.scrollBy({
      left: dir * Math.max(160, track.clientWidth * 0.8),
      behavior: reduced ? 'auto' : 'smooth'
    });

    if (prev) prev.addEventListener('click', () => by(-1));
    if (next) next.addEventListener('click', () => by(1));
    track.addEventListener('scroll', update, { passive: true });
    if (window.ResizeObserver) new ResizeObserver(update).observe(track);
    window.addEventListener('load', update);
    update();

    // Kéo chuột để cuộn (cảm ứng đã cuộn được sẵn). Kéo xong thì chặn cú click để không mở nhầm liên kết.
    let drag = null;
    let moved = false;

    track.addEventListener('pointerdown', (e) => {
      if (e.pointerType !== 'mouse' || e.button !== 0) return;
      drag = { x: e.clientX, left: track.scrollLeft, active: false };
    });

    track.addEventListener('pointermove', (e) => {
      if (!drag) return;
      const dx = e.clientX - drag.x;
      if (!drag.active && Math.abs(dx) > 6) {
        drag.active = true;
        track.classList.add('dragging');
        track.setPointerCapture(e.pointerId);
      }
      if (drag.active) track.scrollLeft = drag.left - dx;
    });

    const end = () => {
      if (!drag) return;
      if (drag.active) {
        moved = true;
        setTimeout(() => { moved = false; }, 0);
      }
      track.classList.remove('dragging');
      drag = null;
    };

    track.addEventListener('pointerup', end);
    track.addEventListener('pointercancel', end);
    track.addEventListener('click', (e) => {
      if (moved) { e.preventDefault(); e.stopPropagation(); }
    }, true);
    track.addEventListener('dragstart', (e) => e.preventDefault());
  });
})();
