// Họ và tên: Lại Anh Quân
// Mã sinh viên: 23103100255
// Nội dung thực hiện: AJAX dùng chung cho biểu mẫu CRUD, danh sách và thao tác quản trị
(() => {
  'use strict';
  if (window.__libraryAjaxLoaded) return;
  window.__libraryAjaxLoaded = true;

  const lists = new Map();

  function getForm(list) {
    return list.dataset.ajaxForm ? document.querySelector(list.dataset.ajaxForm) : list.querySelector('form[data-ajax-filter-form]');
  }

  function stateForForm(form) {
    for (const [list, state] of lists) {
      if (list.contains(form) || (state.formSelector && form.matches(state.formSelector))) return { list, state };
    }
    return null;
  }

  function makeUrl(form, submitter) {
    const data = new FormData(form);
    if (submitter && submitter.name) data.set(submitter.name, submitter.value);
    const params = new URLSearchParams(data);
    const query = params.toString();
    return (form.getAttribute('action') || location.pathname) + (query ? '?' + query : '');
  }

  function syncForm(form, url) {
    if (!form) return;
    const params = new URL(url, location.href).searchParams;
    for (const field of form.elements) {
      if (!field.name) continue;
      const values = params.getAll(field.name);
      if (field.type === 'checkbox' || field.type === 'radio') field.checked = values.includes(field.value);
      else field.value = params.get(field.name) ?? field.dataset.default ?? '';
    }
  }

  function restoreField(list, caret) {
    const field = list.querySelector('[name="' + CSS.escape(caret.name) + '"]');
    if (!field) return;
    field.focus();
    // Nếu người dùng gõ thêm trong lúc chờ, đưa lại giá trị mới nhất và tìm lại.
    if (field.value !== caret.value) {
      field.value = caret.value;
      field.dispatchEvent(new Event('input', { bubbles: true }));
    }
    try { field.setSelectionRange(caret.start, caret.end); } catch (_) { /* ô không hỗ trợ con trỏ */ }
  }

  function updateTabs(list, url) {
    const tabs = list.dataset.ajaxTabs && document.querySelector(list.dataset.ajaxTabs);
    if (!tabs) return;
    const selected = new URL(url, location.href).searchParams.get('trangThai') || '';
    tabs.querySelectorAll('a.quick-tab-ban-doc').forEach((tab) => {
      const fromHref = new URL(tab.href, location.href).searchParams.get('trangThai') || '';
      const active = fromHref === selected || (tab.dataset.giaTri || '') === selected;
      tab.classList.remove('btn-primary', 'btn-warning', 'btn-light', 'border');
      if (active) tab.classList.add(tab.dataset.activeClass || 'btn-primary');
      else tab.classList.add('btn-light', 'border');
    });
  }

  function extractContent(list, html) {
    const selector = list.dataset.ajaxDocumentSelector;
    if (!selector) return html;
    const doc = new DOMParser().parseFromString(html, 'text/html');
    const fragment = doc.querySelector(selector);
    if (!fragment) throw new Error('Expected AJAX results were not returned.');
    return fragment.innerHTML;
  }

  async function loadList(list, url, options = {}) {
    const state = lists.get(list);
    if (!state) return;
    if (state.request) state.request.abort();
    const request = new AbortController();
    state.request = request;
    list.classList.add('is-loading');
    list.setAttribute('aria-busy', 'true');
    try {
      const response = await fetch(url, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin',
        signal: request.signal
      });
      if (!response.ok) throw new Error('HTTP ' + response.status);
      const html = await response.text();
      // Giữ focus và con trỏ của ô đang gõ: vùng kết quả (kèm form lọc) bị thay bằng HTML mới.
      const typing = document.activeElement;
      const caret = typing && list.contains(typing) && typing.name && typing.matches('[data-ajax-search], [data-ajax-input]')
        ? { name: typing.name, value: typing.value, start: typing.selectionStart, end: typing.selectionEnd }
        : null;
      list.innerHTML = extractContent(list, html);
      if (caret) restoreField(list, caret);
      state.url = url;
      if (options.push) {
        const replaceTyping = options.typing && history.state && history.state.libraryAjax && history.state.typing;
        const historyState = { libraryAjax: true, typing: Boolean(options.typing) };
        if (replaceTyping) history.replaceState(historyState, '', url);
        else history.pushState(historyState, '', url);
      }
      if (options.sync) syncForm(getForm(list), url);
      updateTabs(list, url);
      if (list.dataset.ajaxBadge) {
        const meta = document.querySelector(list.dataset.ajaxMeta || '[data-total-count]');
        const badge = document.querySelector(list.dataset.ajaxBadge);
        if (meta && badge) badge.textContent = (meta.dataset.totalCount || '0') + (list.dataset.ajaxBadgeSuffix || '');
      }
    } catch (error) {
      if (error.name !== 'AbortError') location.assign(url);
    } finally {
      if (state.request === request) {
        state.request = null;
        list.classList.remove('is-loading');
        list.removeAttribute('aria-busy');
      }
    }
  }

  function clearForm(form) {
    if (!form) return;
    for (const field of form.querySelectorAll('input[name], select[name], textarea[name]')) {
      if (field.type === 'hidden') continue;
      if (field.type === 'checkbox' || field.type === 'radio') field.checked = false;
      else if (field.tagName === 'SELECT') {
        // Ô chọn không có lựa chọn rỗng: về mặc định (hoặc lựa chọn đầu) thay vì để trống.
        field.value = field.dataset.default ?? '';
        if (field.selectedIndex < 0) field.selectedIndex = 0;
      }
      else field.value = '';
    }
  }

  function showAlert(selector, message, type) {
    const box = selector && document.querySelector(selector);
    if (!box) return;
    box.innerHTML = '';
    const alert = document.createElement('div');
    alert.className = 'alert alert-' + type + ' alert-dismissible fade show';
    alert.setAttribute('role', 'alert');
    alert.append(document.createTextNode(message));
    const close = document.createElement('button');
    close.type = 'button';
    close.className = 'btn-close';
    close.dataset.bsDismiss = 'alert';
    close.setAttribute('aria-label', 'Close');
    alert.append(close);
    box.append(alert);
    alert.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }

  // List declarations are data-driven so one handler supports the staff list screens.
  document.querySelectorAll('[data-ajax-list]').forEach((list) => {
    const state = { formSelector: list.dataset.ajaxForm || null, url: location.pathname + location.search, request: null };
    lists.set(list, state);
  });

  document.addEventListener('submit', (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== 'get' || form.matches('[data-ajax-crud], [data-ajax-action]')) return;
    const owner = stateForForm(form);
    if (!owner || (owner.state.formSelector ? !form.matches(owner.state.formSelector) : !form.matches('[data-ajax-filter-form]'))) return;
    event.preventDefault();
    loadList(owner.list, makeUrl(form, event.submitter), { push: true });
  });

  document.addEventListener('change', (event) => {
    const form = event.target.form;
    if (!form) return;
    const owner = stateForForm(form);
    if (owner && owner.list.hasAttribute('data-ajax-change') && !event.target.matches('[data-ajax-search], [data-ajax-input]'))
      loadList(owner.list, makeUrl(form), { push: true });
  });

  document.addEventListener('input', (event) => {
    const field = event.target;
    if (!field.matches('[data-ajax-search], [data-ajax-input]') || !field.form) return;
    const owner = stateForForm(field.form);
    if (!owner) return;
    clearTimeout(owner.state.timer);
    const minimum = Number(field.dataset.ajaxSearchMin || field.dataset.ajaxInputMin || 0);
    if (field.value.trim().length > 0 && field.value.trim().length < minimum) return;
    owner.state.timer = setTimeout(() => loadList(owner.list, makeUrl(field.form), { push: true, typing: true }), Number(field.dataset.ajaxSearchDelay || 350));
  });

  document.addEventListener('click', (event) => {
    const clear = event.target.closest('[data-ajax-clear]');
    if (!clear) return;
    const owner = stateForForm(clear.closest('form'));
    if (!owner) return;
    event.preventDefault();
    const form = clear.closest('form');
    clearForm(form);
    loadList(owner.list, makeUrl(form), { push: true });
  });

  // Optional client-side checks and confirmation text for confirm-modal buttons.
  // Capture runs before forms.js opens the modal, so the shared modal keeps its existing behavior.
  document.addEventListener('click', (event) => {
    const trigger = event.target.closest('[data-confirm-form]');
    if (!trigger) return;
    const form = document.querySelector(trigger.dataset.confirmForm);
    if (!form) return;

    if (trigger.dataset.ajaxConfirmSetField) {
      const field = form.querySelector(trigger.dataset.ajaxConfirmSetField);
      if (field) field.value = trigger.dataset.ajaxConfirmSetValue || '';
    }

    if (trigger.dataset.ajaxConfirmRequired) {
      const field = form.querySelector(trigger.dataset.ajaxConfirmRequired);
      const error = trigger.dataset.ajaxConfirmError && document.querySelector(trigger.dataset.ajaxConfirmError);
      if (field && !field.value.trim()) {
        if (error) error.textContent = trigger.dataset.ajaxConfirmRequiredMessage || 'Vui lòng điền trường bắt buộc.';
        field.classList.add('is-invalid');
        field.focus();
        event.preventDefault();
        event.stopImmediatePropagation();
        return;
      }
      if (field) field.classList.remove('is-invalid');
      if (error) error.textContent = '';
    }

    if (trigger.dataset.ajaxConfirmRadio) {
      const selected = Array.from(form.querySelectorAll('input[type="radio"]'))
        .find((radio) => radio.name === trigger.dataset.ajaxConfirmRadio && radio.checked);
      if (!selected) {
        showAlert(trigger.dataset.ajaxConfirmAlert, 'Vui lòng chọn tình trạng sách.', 'danger');
        event.preventDefault();
        event.stopImmediatePropagation();
        return;
      }
      const spans = selected.closest('label')?.querySelectorAll('span') || [];
      const template = trigger.dataset.ajaxConfirmTemplate || trigger.dataset.confirmMessage || '';
      trigger.dataset.confirmMessage = template
        .replace('{status}', spans[0]?.textContent.trim() || '')
        .replace('{amount}', spans[1]?.textContent.trim() || '');
    }
  }, true);

  document.addEventListener('click', (event) => {
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    const external = event.target.closest('[data-ajax-list-target]');
    if (external) {
      const list = document.querySelector(external.dataset.ajaxListTarget);
      const state = list && lists.get(list);
      if (!state || !external.href) return;
      event.preventDefault();
      if (external.dataset.ajaxClearForm) clearForm(document.querySelector(external.dataset.ajaxClearForm));
      loadList(list, external.href, { push: true, sync: true });
      return;
    }

    const list = event.target.closest('[data-ajax-list]');
    if (!list) return;
    const state = lists.get(list);
    const link = event.target.closest('[data-ajax-link], a.myloans-tab-link, nav[aria-label="Phân trang"] a, .pagination a, a.filter-card-link');
    if (!state || !link || !list.contains(link) || !link.href) return;
    if (link.closest('.disabled') || link.getAttribute('aria-disabled') === 'true') { event.preventDefault(); return; }
    event.preventDefault();
    loadList(list, link.href, { push: true, sync: true });
  });

  // Existing CRUD forms use the shared success redirect / validation-fragment contract.
  document.addEventListener('submit', async (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.matches('[data-ajax-crud="form"]')) return;
    event.preventDefault();
    const buttons = Array.from(form.querySelectorAll('button[type="submit"], input[type="submit"], button[type="button"]'));
    buttons.forEach((button) => { button.disabled = true; });
    try {
      const response = await fetch(form.action, {
        method: form.method || 'POST', body: new FormData(form), credentials: 'same-origin',
        headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json, text/html;q=0.9' }
      });
      const contentType = response.headers.get('content-type') || '';
      if (contentType.includes('application/json')) {
        const result = await response.json();
        if (result.success === false && result.message) {
          const card = form.closest('.app-form-card, .app-card');
          if (card) {
            card.querySelector('.ajax-crud-error')?.remove();
            const alert = document.createElement('div');
            alert.className = 'alert alert-danger ajax-crud-error';
            alert.setAttribute('role', 'alert');
            alert.textContent = result.message;
            card.prepend(alert);
            card.scrollIntoView({ behavior: 'smooth', block: 'start' });
          }
          return;
        }
        if (result.redirectUrl) { location.assign(result.redirectUrl); return; }
      } else {
        if (!response.ok) throw new Error('HTTP ' + response.status);
        const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
        const newCard = doc.querySelector('.app-form-card, .app-card');
        const oldCard = form.closest('.app-form-card, .app-card');
        if (newCard && oldCard) {
          oldCard.replaceWith(newCard);
          if (window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
            jQuery(newCard).find('form').each(function () {
              jQuery(this).removeData('validator').removeData('unobtrusiveValidation');
              jQuery.validator.unobtrusive.parse(this);
            });
          }
          return;
        }
      }
      location.reload();
    } catch (error) {
      console.error('CRUD AJAX failed', error);
      location.reload();
    } finally {
      buttons.forEach((button) => { button.disabled = false; });
    }
  });

  // JSON action forms can refresh their list and report the server message declaratively.
  document.addEventListener('submit', async (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.matches('[data-ajax-action="json"]')) return;
    event.preventDefault();
    const button = form.querySelector('button[type="submit"]');
    if (button) button.disabled = true;
    if (form.dataset.ajaxHideModal === 'true' && window.bootstrap) {
      const modal = form.closest('.modal');
      if (modal) bootstrap.Modal.getInstance(modal)?.hide();
    }
    try {
      const response = await fetch(form.action, {
        method: form.method || 'POST', body: new FormData(form), credentials: 'same-origin',
        headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }
      });
      if (!response.ok) throw new Error('HTTP ' + response.status);
      const result = await response.json();
      const list = form.dataset.ajaxRefresh && document.querySelector(form.dataset.ajaxRefresh);
      const state = list && lists.get(list);
      if (state) await loadList(list, state.url, { push: false });
      if (result.success && result.redirectUrl && form.dataset.ajaxRedirect !== 'false') {
        location.assign(result.redirectUrl);
        return;
      }
      const type = result.success ? 'success' : 'danger';
      showAlert(form.dataset.ajaxAlert, result.message || (result.success ? 'Thao tác thành công.' : 'Thao tác không thành công.'), type);
    } catch (error) {
      console.error('AJAX action failed', error);
      location.reload();
    } finally {
      if (button) button.disabled = false;
    }
  });

  window.addEventListener('popstate', () => {
    for (const [list, state] of lists) loadList(list, location.pathname + location.search, { push: false, sync: true });
  });
})();
