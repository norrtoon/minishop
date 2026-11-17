import { api, session, ApiError, setUnauthorizedHandler } from './api.js';
import { cart } from './cart.js';

const app = document.getElementById('app');

const esc = (v) => String(v ?? '').replace(/[&<>"']/g, c =>
  ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

const money = (v) => new Intl.NumberFormat('ru-RU', { style: 'currency', currency: 'RUB', maximumFractionDigits: 0 }).format(v);
const dateTime = (v) => new Date(v).toLocaleString('ru-RU', { dateStyle: 'medium', timeStyle: 'short' });
const date = (v) => new Date(v).toLocaleDateString('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' });

function plural(n, one, few, many) {
  const m10 = n % 10, m100 = n % 100;
  if (m10 === 1 && m100 !== 11) return one;
  if (m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14)) return few;
  return many;
}
const reviewsWord = (n) => `${n} ${plural(n, 'отзыв', 'отзыва', 'отзывов')}`;

const stars = (rating, cls = '') =>
  `<span class="stars ${cls}" style="--r:${Number(rating) || 0}" aria-label="Оценка ${rating} из 5">★★★★★</span>`;

const STATUS = {
  New: { label: 'Новый', cls: 'new' },
  Processing: { label: 'В сборке', cls: 'processing' },
  Shipped: { label: 'Отправлен', cls: 'shipped' },
  Completed: { label: 'Получен', cls: 'completed' },
  Cancelled: { label: 'Отменён', cls: 'cancelled' }
};
const statusBadge = (s) => `<span class="badge badge--${STATUS[s]?.cls}">${STATUS[s]?.label ?? esc(s)}</span>`;

const CATEGORY_ICONS = { 'Электроника': '🎧', 'Книги': '📚', 'Одежда': '👕', 'Дом': '🏠' };
const iconFor = (categoryName) => CATEGORY_ICONS[categoryName] || '📦';

function toast(message, type = 'ok') {
  const el = document.createElement('div');
  el.className = `toast toast--${type}`;
  el.textContent = message;
  document.getElementById('toasts').append(el);
  setTimeout(() => el.classList.add('toast--hide'), 3200);
  setTimeout(() => el.remove(), 3600);
}

function showError(err) {
  if (err instanceof ApiError) toast(`${err.title}: ${err.message}`, 'error');
  else { console.error(err); toast('Что-то пошло не так', 'error'); }
}

const modal = document.getElementById('modal');
function openModal(html, wide = false) {
  document.getElementById('modal-body').innerHTML = html;
  modal.querySelector('.modal__dialog').classList.toggle('modal__dialog--wide', wide);
  modal.hidden = false;
}
function closeModal() { modal.hidden = true; }
modal.addEventListener('click', e => { if (e.target.closest('[data-close]')) closeModal(); });
document.addEventListener('keydown', e => { if (e.key === 'Escape' && !modal.hidden) closeModal(); });

function readForm(form) {
  const data = {};
  for (const el of form.elements) {
    if (!el.name || el.type === 'file') continue;
    if (el.type === 'checkbox') data[el.name] = el.checked;
    else if (el.type === 'radio') {
      if (el.checked) data[el.name] = Number(el.value);
      else if (!(el.name in data)) data[el.name] = null;
    }
    else if (el.dataset.number !== undefined) data[el.name] = el.value === '' ? null : Number(el.value);
    else data[el.name] = el.value;
  }
  return data;
}

function clearErrors(form) {
  form.querySelectorAll('.field__error').forEach(e => e.remove());
  form.querySelectorAll('.is-invalid').forEach(e => e.classList.remove('is-invalid'));
  form.querySelector('.form__error')?.remove();
}

function showFormErrors(form, err) {
  clearErrors(form);
  const general = [];
  if (err instanceof ApiError && err.fieldErrors) {
    for (const [key, messages] of Object.entries(err.fieldErrors)) {
      const input = [...form.elements].find(el => el.name && el.name.toLowerCase() === key.toLowerCase());
      if (input) {
        input.classList.add('is-invalid');
        const hint = document.createElement('div');
        hint.className = 'field__error';
        hint.textContent = messages.join(' ');
        input.closest('.field')?.append(hint);
      } else {
        general.push(...messages);
      }
    }
  } else {
    general.push(err instanceof ApiError ? err.message : 'Неизвестная ошибка');
  }
  if (general.length) {
    const box = document.createElement('div');
    box.className = 'form__error';
    box.textContent = general.join(' ');
    form.prepend(box);
  }
}

function onSubmit(form, handler) {
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const button = form.querySelector('[type=submit]');
    button.disabled = true;
    clearErrors(form);
    try {
      await handler(readForm(form));
    } catch (err) {
      showFormErrors(form, err);
    } finally {
      button.disabled = false;
    }
  });
}

const field = (label, inner) => `<label class="field"><span class="field__label">${label}</span>${inner}</label>`;

function renderNav() {
  const route = location.hash.slice(2).split('?')[0];
  const link = (href, text, key) =>
    `<a href="#/${href}" class="nav__link ${route === key ? 'is-active' : ''}">${text}</a>`;

  const count = cart.count();
  let html = link('', 'Каталог', '') +
    link('cart', `Корзина${count ? ` <span class="pill">${count}</span>` : ''}`, 'cart');

  if (session.isLoggedIn) {
    html += link('orders', 'Мои заказы', 'orders');
    if (session.isAdmin) html += link('admin', 'Админка', 'admin');
    html += `<span class="nav__user" title="${esc(session.user?.email)}">${esc(session.user?.name)}${session.isAdmin ? ' <span class="pill pill--dark">admin</span>' : ''}</span>
             <button class="btn btn--ghost btn--sm" id="logout">Выйти</button>`;
  } else {
    html += link('login', 'Войти', 'login');
  }

  const nav = document.getElementById('nav');
  nav.innerHTML = html;
  nav.querySelector('#logout')?.addEventListener('click', () => {
    session.clear();
    toast('Вы вышли из аккаунта');
    location.hash = '#/';
    renderNav();
  });
}

cart.onChange(renderNav);

setUnauthorizedHandler(() => {
  session.clear();
  renderNav();
  toast('Сессия истекла, войдите снова', 'error');
  location.hash = '#/login';
});

let categoriesCache = [];
const catalogState = { search: '', categoryId: '', minPrice: '', maxPrice: '', inStockOnly: false, sort: 'name', page: 1 };

async function catalogPage() {
  try { categoriesCache = await api.categories.list(); } catch (e) { showError(e); }

  app.innerHTML = `
    <section class="hero"><h1>Каталог</h1></section>
    <div class="catalog">
      <aside class="filters card">
        <form id="filters">
          ${field('Поиск', `<input name="search" type="search" placeholder="Название или описание" value="${esc(catalogState.search)}">`)}
          ${field('Категория', `<select name="categoryId">
              <option value="">Все категории</option>
              ${categoriesCache.map(c => `<option value="${c.id}" ${String(c.id) === String(catalogState.categoryId) ? 'selected' : ''}>${esc(c.name)} (${c.productCount})</option>`).join('')}
            </select>`)}
          <div class="field-row">
            ${field('Цена от', `<input name="minPrice" type="number" min="0" placeholder="0" value="${esc(catalogState.minPrice)}">`)}
            ${field('до', `<input name="maxPrice" type="number" min="0" placeholder="∞" value="${esc(catalogState.maxPrice)}">`)}
          </div>
          ${field('Сортировка', `<select name="sort">
              <option value="name">По названию</option>
              <option value="price_asc">Сначала дешёвые</option>
              <option value="price_desc">Сначала дорогие</option>
              <option value="rating">По рейтингу</option>
              <option value="newest">Сначала новые</option>
            </select>`)}
          <label class="check"><input type="checkbox" name="inStockOnly" ${catalogState.inStockOnly ? 'checked' : ''}> Только в наличии</label>
          <button type="reset" class="btn btn--ghost btn--block">Сбросить</button>
        </form>
      </aside>
      <section>
        <div id="filter-error"></div>
        <div id="products" class="grid"></div>
        <div id="pager" class="pager"></div>
      </section>
    </div>`;

  const form = document.getElementById('filters');
  form.sort.value = catalogState.sort;

  let timer;
  form.addEventListener('input', () => {
    clearTimeout(timer);
    timer = setTimeout(() => { Object.assign(catalogState, readForm(form), { page: 1 }); loadProducts(); }, 300);
  });
  form.addEventListener('reset', () => setTimeout(() => {
    Object.assign(catalogState, readForm(form), { sort: 'name', page: 1 });
    loadProducts();
  }));

  document.getElementById('products').addEventListener('click', onProductClick);
  loadProducts();
}

async function loadProducts() {
  const grid = document.getElementById('products');
  const errorBox = document.getElementById('filter-error');
  if (!grid) return;
  grid.classList.add('is-loading');
  errorBox.innerHTML = '';

  try {
    const result = await api.products.list({ ...catalogState, pageSize: 9 });
    grid.innerHTML = result.items.length
      ? result.items.map(productCard).join('')
      : `<div class="empty">Ничего не найдено</div>`;
    renderPager(result);
  } catch (err) {
    const msgs = err.fieldErrors ? [...new Set(Object.values(err.fieldErrors).flat())] : [err.message];
    errorBox.innerHTML = `<div class="alert">${msgs.map(esc).join('<br>')}</div>`;
    grid.innerHTML = '';
    document.getElementById('pager').innerHTML = '';
  } finally {
    grid.classList.remove('is-loading');
  }
}

const productImage = (p) => p.imageUrl
  ? `<img src="${esc(p.imageUrl)}" alt="" loading="lazy">`
  : `<span>${iconFor(p.categoryName)}</span>`;

function productCard(p) {
  const inCart = cart.quantityOf(p.id);
  const stock = p.stock === 0
    ? `<span class="stock stock--out">Нет в наличии</span>`
    : `<span class="stock">${p.stock <= 5 ? `Осталось ${p.stock}` : 'В наличии'}</span>`;
  const rating = p.reviewCount
    ? `<span class="rating-inline">${stars(p.rating)} <span class="muted small">${p.rating.toFixed(1)} · ${p.reviewCount}</span></span>`
    : '';
  return `
    <article class="product card">
      <button class="product__image cat-${p.categoryId % 6}" data-action="details" data-id="${p.id}" aria-label="Подробнее">
        ${productImage(p)}
      </button>
      <div class="product__body">
        <div class="product__cat">${esc(p.categoryName)}</div>
        <h3 class="product__name"><a href="#" data-action="details" data-id="${p.id}">${esc(p.name)}</a></h3>
        <div class="product__meta">${rating}${stock}</div>
        <div class="product__footer">
          <strong class="price">${money(p.price)}</strong>
          <button class="btn btn--sm" data-action="add" data-id="${p.id}" ${p.stock === 0 || inCart >= p.stock ? 'disabled' : ''}>
            ${inCart ? `В корзине: ${inCart}` : 'В корзину'}
          </button>
        </div>
      </div>
    </article>`;
}

function renderPager({ page, totalPages, totalCount }) {
  const pager = document.getElementById('pager');
  if (totalPages <= 1) { pager.innerHTML = `<span class="muted">Найдено: ${totalCount}</span>`; return; }
  let html = '';
  for (let i = 1; i <= totalPages; i++)
    html += `<button class="pager__btn ${i === page ? 'is-active' : ''}" data-page="${i}">${i}</button>`;
  pager.innerHTML = html + `<span class="muted">Найдено: ${totalCount}</span>`;
  pager.querySelectorAll('[data-page]').forEach(b => b.addEventListener('click', () => {
    catalogState.page = Number(b.dataset.page);
    loadProducts();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }));
}

async function onProductClick(e) {
  const btn = e.target.closest('[data-action]');
  if (!btn) return;
  e.preventDefault();
  const id = Number(btn.dataset.id);

  try {
    if (btn.dataset.action === 'add') {
      const product = await api.products.get(id);
      cart.add(product);
      toast(`«${product.name}» добавлен в корзину`);
      loadProducts();
    } else if (btn.dataset.action === 'details') {
      await openProduct(id);
    }
  } catch (err) { showError(err); }
}

async function openProduct(id) {
  const [p, reviews] = await Promise.all([api.products.get(id), api.reviews.forProduct(id)]);

  openModal(`
    <div class="details">
      <div class="details__image cat-${p.categoryId % 6}">${productImage(p)}</div>
      <div>
        <div class="product__cat">${esc(p.categoryName)}</div>
        <h2>${esc(p.name)}</h2>
        ${p.reviewCount ? `<div class="rating-inline">${stars(p.rating)} <span class="muted small">${p.rating.toFixed(1)} · ${reviewsWord(p.reviewCount)}</span></div>` : ''}
        <p>${esc(p.description || 'Описание отсутствует.')}</p>
        <p class="muted">На складе: ${p.stock} шт.</p>
        <div class="product__footer">
          <strong class="price price--lg">${money(p.price)}</strong>
          <button class="btn" id="details-add" ${p.stock === 0 ? 'disabled' : ''}>В корзину</button>
        </div>
      </div>
    </div>
    <section class="reviews" id="reviews"></section>`, true);

  document.getElementById('details-add').addEventListener('click', () => {
    cart.add(p);
    toast(`«${p.name}» добавлен в корзину`);
    closeModal();
    loadProducts();
  });

  renderReviews(p.id, reviews);
}

function renderReviews(productId, data, editing = null) {
  const box = document.getElementById('reviews');
  if (!box) return;

  const bar = (n) => {
    const count = data.distribution[n - 1];
    const pct = data.reviewCount ? Math.round(count / data.reviewCount * 100) : 0;
    return `<div class="bar"><span>${n}★</span><div class="bar__track"><div class="bar__fill" style="width:${pct}%"></div></div><span class="muted">${count}</span></div>`;
  };

  let formBlock = '';
  if (editing) formBlock = reviewForm(editing);
  else if (data.canReview) formBlock = reviewForm(null);
  else if (data.cannotReviewReason) formBlock = `<p class="muted small">${esc(data.cannotReviewReason)}</p>`;

  box.innerHTML = `
    <h3>Отзывы</h3>
    <div class="rating-summary">
      <div class="rating-summary__score">
        <div class="rating-big">${data.reviewCount ? data.rating.toFixed(1) : '—'}</div>
        ${stars(data.rating)}
        <div class="muted small">${reviewsWord(data.reviewCount)}</div>
      </div>
      <div class="bars">${[5, 4, 3, 2, 1].map(bar).join('')}</div>
    </div>
    ${formBlock}
    <div class="review-list">
      ${data.items.map(r => reviewItem(r, data)).join('') || '<p class="muted">Пока никто не оставил отзыв.</p>'}
    </div>`;

  const form = box.querySelector('#review-form');
  if (form) {
    onSubmit(form, async (dto) => {
      if (editing) await api.reviews.update(editing.id, dto);
      else await api.reviews.create(productId, dto);
      toast(editing ? 'Отзыв обновлён' : 'Спасибо за отзыв!');
      renderReviews(productId, await api.reviews.forProduct(productId));
      loadProducts();
    });
    box.querySelector('[data-cancel-edit]')?.addEventListener('click', () => renderReviews(productId, data));
  }

  box.querySelectorAll('[data-edit-review]').forEach(b => b.addEventListener('click', () => {
    const review = data.items.find(r => r.id === Number(b.dataset.editReview));
    renderReviews(productId, data, review);
    box.querySelector('#review-form textarea')?.focus();
  }));

  box.querySelectorAll('[data-delete-review]').forEach(b => b.addEventListener('click', async () => {
    if (!confirm('Удалить отзыв?')) return;
    try {
      await api.reviews.remove(b.dataset.deleteReview);
      toast('Отзыв удалён');
      renderReviews(productId, await api.reviews.forProduct(productId));
      loadProducts();
    } catch (err) { showError(err); }
  }));
}

function reviewForm(review) {
  const current = review?.rating ?? 0;
  return `
    <form id="review-form" class="review-form" novalidate>
      <div class="field">
        <span class="field__label">${review ? 'Ваша оценка' : 'Оцените товар'}</span>
        <div class="star-input">
          ${[5, 4, 3, 2, 1].map(n => `
            <input type="radio" id="star-${n}" name="rating" value="${n}" ${n === current ? 'checked' : ''}>
            <label for="star-${n}" title="${n}">★</label>`).join('')}
        </div>
      </div>
      ${field('Комментарий', `<textarea name="text" rows="3" maxlength="1000" placeholder="Что понравилось, что нет?">${esc(review?.text)}</textarea>`)}
      <div class="review-form__actions">
        ${review ? '<button type="button" class="btn btn--ghost" data-cancel-edit>Отмена</button>' : ''}
        <button type="submit" class="btn">${review ? 'Сохранить' : 'Отправить отзыв'}</button>
      </div>
    </form>`;
}

function reviewItem(r, data) {
  const mine = r.id === data.myReviewId;
  const actions = [];
  if (mine) actions.push(`<button class="link-btn" data-edit-review="${r.id}">Изменить</button>`);
  if (mine || session.isAdmin) actions.push(`<button class="link-btn link-btn--danger" data-delete-review="${r.id}">Удалить</button>`);

  return `
    <article class="review ${mine ? 'review--mine' : ''}">
      <header class="review__head">
        <strong>${esc(r.userName)}${mine ? ' <span class="pill pill--dark">вы</span>' : ''}</strong>
        ${stars(r.rating, 'stars--sm')}
        <span class="muted small">${date(r.createdAt)}${r.updatedAt ? ' · изменён' : ''}</span>
      </header>
      ${r.text ? `<p>${esc(r.text)}</p>` : ''}
      ${actions.length ? `<div class="review__actions">${actions.join('')}</div>` : ''}
    </article>`;
}

function cartPage() {
  const items = cart.items();
  if (!items.length) {
    app.innerHTML = `<section class="hero"><h1>Корзина</h1></section>
      <div class="empty card">Корзина пуста. <a href="#/">Перейти в каталог</a></div>`;
    return;
  }

  app.innerHTML = `
    <section class="hero"><h1>Корзина</h1></section>
    <div class="cart-layout">
      <div class="card table-wrap">
        <table class="table">
          <thead><tr><th>Товар</th><th>Цена</th><th>Кол-во</th><th>Сумма</th><th></th></tr></thead>
          <tbody>
            ${items.map(i => `
              <tr>
                <td><strong>${esc(i.name)}</strong><div class="muted small">${esc(i.categoryName)}</div></td>
                <td>${money(i.price)}</td>
                <td>
                  <div class="qty">
                    <button class="qty__btn" data-qty="${i.productId}" data-delta="-1">−</button>
                    <span>${i.quantity}</span>
                    <button class="qty__btn" data-qty="${i.productId}" data-delta="1" ${i.quantity >= i.stock ? 'disabled' : ''}>+</button>
                  </div>
                </td>
                <td>${money(i.price * i.quantity)}</td>
                <td><button class="btn btn--ghost btn--sm" data-remove="${i.productId}">Удалить</button></td>
              </tr>`).join('')}
          </tbody>
        </table>
        <div class="cart-total">Итого: <strong>${money(cart.total())}</strong></div>
      </div>

      <div class="card">
        <h2>Оформление</h2>
        ${session.isLoggedIn ? `
          <form id="checkout" novalidate>
            ${field('Адрес доставки', `<input name="address" placeholder="Город, улица, дом, квартира">`)}
            ${field('Комментарий', `<textarea name="comment" rows="3" placeholder="Необязательно"></textarea>`)}
            <button type="submit" class="btn btn--block">Оформить заказ</button>
          </form>`
        : `<p>Чтобы оформить заказ, <a href="#/login">войдите</a> или <a href="#/register">зарегистрируйтесь</a>.</p>`}
      </div>
    </div>`;

  app.querySelectorAll('[data-qty]').forEach(b => b.addEventListener('click', () => {
    const id = Number(b.dataset.qty);
    cart.setQuantity(id, cart.quantityOf(id) + Number(b.dataset.delta));
    cartPage();
  }));
  app.querySelectorAll('[data-remove]').forEach(b => b.addEventListener('click', () => {
    cart.remove(Number(b.dataset.remove));
    cartPage();
  }));

  const form = document.getElementById('checkout');
  if (form) onSubmit(form, async ({ address, comment }) => {
    const order = await api.orders.create({
      address,
      comment,
      items: cart.items().map(i => ({ productId: i.productId, quantity: i.quantity }))
    });
    cart.clear();
    toast(`Заказ №${order.id} оформлен на ${money(order.total)}`);
    location.hash = '#/orders';
  });
}

async function ordersPage() {
  app.innerHTML = `<section class="hero"><h1>Мои заказы</h1></section><div id="orders" class="stack"><div class="muted">Загрузка…</div></div>`;
  try {
    const orders = await api.orders.my();
    const box = document.getElementById('orders');
    box.innerHTML = orders.length ? orders.map(o => orderCard(o, false)).join('')
      : `<div class="empty card">Заказов пока нет. <a href="#/">В каталог</a></div>`;

    box.querySelectorAll('[data-cancel]').forEach(b => b.addEventListener('click', async () => {
      if (!confirm('Отменить заказ?')) return;
      try {
        await api.orders.cancel(b.dataset.cancel);
        toast('Заказ отменён');
        ordersPage();
      } catch (err) { showError(err); }
    }));

    box.querySelectorAll('[data-review-product]').forEach(b => b.addEventListener('click', () =>
      openProduct(Number(b.dataset.reviewProduct)).catch(showError)));
  } catch (err) { showError(err); }
}

function orderCard(o, adminMode) {
  const statusControl = adminMode ? `
    <select class="status-select" data-status="${o.id}">
      ${Object.entries(STATUS).map(([k, v]) => `<option value="${k}" ${k === o.status ? 'selected' : ''}>${v.label}</option>`).join('')}
    </select>` : statusBadge(o.status);

  const actions = [];
  if (!adminMode && o.status === 'New') actions.push(`<button class="btn btn--ghost btn--sm" data-cancel="${o.id}">Отменить</button>`);
  if (adminMode) actions.push(`<button class="btn btn--danger btn--sm" data-delete-order="${o.id}">Удалить</button>`);

  const canReview = !adminMode && o.status === 'Completed';
  return `
    <article class="card order">
      <header class="order__head">
        <div>
          <strong>Заказ №${o.id}</strong> <span class="muted">· ${dateTime(o.createdAt)}</span>
          ${adminMode ? `<div class="muted small">${esc(o.userName)} · ${esc(o.userEmail)}</div>` : ''}
        </div>
        ${statusControl}
      </header>
      <ul class="order__items">
        ${o.items.map(i => `
          <li>
            <span>${esc(i.productName)} × ${i.quantity}
              ${canReview && i.productId ? `<button class="link-btn" data-review-product="${i.productId}">отзыв</button>` : ''}
            </span>
            <span>${money(i.lineTotal)}</span>
          </li>`).join('')}
      </ul>
      <footer class="order__foot">
        <div class="muted small">${esc(o.address)}${o.comment ? ` · ${esc(o.comment)}` : ''}</div>
        <div class="order__total">${actions.join(' ')} <strong>${money(o.total)}</strong></div>
      </footer>
    </article>`;
}

function loginPage() {
  app.innerHTML = `
    <div class="auth card">
      <h1>Вход</h1>
      <form id="login" novalidate>
        ${field('Email', `<input name="email" type="email" autocomplete="username">`)}
        ${field('Пароль', `<input name="password" type="password" autocomplete="current-password">`)}
        <button type="submit" class="btn btn--block">Войти</button>
      </form>
      <p class="muted">Нет аккаунта? <a href="#/register">Регистрация</a></p>
      <div class="demo">
        <div class="muted small">Тестовые аккаунты:</div>
        <button class="chip" data-demo="admin@shop.local|Admin123">admin@shop.local / Admin123</button>
        <button class="chip" data-demo="user@shop.local|User123">user@shop.local / User123</button>
      </div>
    </div>`;

  const form = document.getElementById('login');
  app.querySelectorAll('[data-demo]').forEach(b => b.addEventListener('click', () => {
    [form.email.value, form.password.value] = b.dataset.demo.split('|');
  }));

  onSubmit(form, async (dto) => {
    const auth = await api.auth.login(dto);
    session.save(auth);
    toast(`Добро пожаловать, ${auth.user.name}!`);
    renderNav();
    location.hash = auth.user.role === 'Admin' ? '#/admin' : '#/';
  });
}

function registerPage() {
  app.innerHTML = `
    <div class="auth card">
      <h1>Регистрация</h1>
      <form id="register" novalidate>
        ${field('Имя', `<input name="name" autocomplete="name">`)}
        ${field('Email', `<input name="email" type="email" autocomplete="email">`)}
        ${field('Пароль', `<input name="password" type="password" autocomplete="new-password">`)}
        ${field('Повторите пароль', `<input name="confirmPassword" type="password" autocomplete="new-password">`)}
        <button type="submit" class="btn btn--block">Создать аккаунт</button>
      </form>
      <p class="muted">Уже есть аккаунт? <a href="#/login">Войти</a></p>
    </div>`;

  onSubmit(document.getElementById('register'), async (dto) => {
    const auth = await api.auth.register(dto);
    session.save(auth);
    toast('Аккаунт создан!');
    renderNav();
    location.hash = '#/';
  });
}

let adminTab = 'products';

function adminPage() {
  app.innerHTML = `
    <section class="hero"><h1>Админ-панель</h1></section>
    <div class="tabs">
      <button class="tab" data-tab="products">Товары</button>
      <button class="tab" data-tab="categories">Категории</button>
      <button class="tab" data-tab="orders">Заказы</button>
      <button class="tab" data-tab="reviews">Отзывы</button>
    </div>
    <div id="admin-content"></div>`;

  app.querySelectorAll('[data-tab]').forEach(b => b.addEventListener('click', () => { adminTab = b.dataset.tab; showAdminTab(); }));
  showAdminTab();
}

function showAdminTab() {
  app.querySelectorAll('[data-tab]').forEach(b => b.classList.toggle('is-active', b.dataset.tab === adminTab));
  ({ products: adminProducts, categories: adminCategories, orders: adminOrders, reviews: adminReviews })[adminTab]();
}

const adminProductsState = { search: '', page: 1 };

async function adminProducts() {
  const box = document.getElementById('admin-content');
  try {
    categoriesCache = await api.categories.list();
    const result = await api.products.list({ search: adminProductsState.search, page: adminProductsState.page, pageSize: 20 });
    box.innerHTML = `
      <div class="toolbar">
        <input id="admin-search" type="search" placeholder="Поиск товара…" value="${esc(adminProductsState.search)}">
        <button class="btn" id="add-product">+ Добавить товар</button>
      </div>
      <div class="card table-wrap">
        <table class="table">
          <thead><tr><th></th><th>Название</th><th>Категория</th><th>Цена</th><th>Остаток</th><th>Рейтинг</th><th></th></tr></thead>
          <tbody>
            ${result.items.map(p => `
              <tr>
                <td><div class="thumb cat-${p.categoryId % 6}">${productImage(p)}</div></td>
                <td><strong>${esc(p.name)}</strong><div class="muted small">id ${p.id}</div></td>
                <td>${esc(p.categoryName)}</td>
                <td>${money(p.price)}</td>
                <td>${p.stock === 0 ? '<span class="stock--out">0</span>' : p.stock}</td>
                <td>${p.reviewCount ? `${p.rating.toFixed(1)} <span class="muted small">(${p.reviewCount})</span>` : '<span class="muted">—</span>'}</td>
                <td class="actions">
                  <button class="btn btn--ghost btn--sm" data-edit="${p.id}">Изменить</button>
                  <button class="btn btn--danger btn--sm" data-del="${p.id}">Удалить</button>
                </td>
              </tr>`).join('') || `<tr><td colspan="7" class="empty">Нет товаров</td></tr>`}
          </tbody>
        </table>
      </div>
      <div class="pager">${Array.from({ length: result.totalPages }, (_, i) =>
        `<button class="pager__btn ${i + 1 === result.page ? 'is-active' : ''}" data-p="${i + 1}">${i + 1}</button>`).join('')}
        <span class="muted">Всего: ${result.totalCount}</span></div>`;

    let timer;
    box.querySelector('#admin-search').addEventListener('input', (e) => {
      clearTimeout(timer);
      timer = setTimeout(() => { adminProductsState.search = e.target.value; adminProductsState.page = 1; adminProducts(); }, 300);
    });
    box.querySelectorAll('[data-p]').forEach(b => b.addEventListener('click', () => { adminProductsState.page = Number(b.dataset.p); adminProducts(); }));
    box.querySelector('#add-product').addEventListener('click', () => productForm(null));
    box.querySelectorAll('[data-edit]').forEach(b => b.addEventListener('click', async () => {
      try { productForm(await api.products.get(b.dataset.edit)); } catch (err) { showError(err); }
    }));
    box.querySelectorAll('[data-del]').forEach(b => b.addEventListener('click', async () => {
      if (!confirm('Удалить товар? Его отзывы тоже удалятся.')) return;
      try { await api.products.remove(b.dataset.del); toast('Товар удалён'); adminProducts(); } catch (err) { showError(err); }
    }));
  } catch (err) { showError(err); }
}

function productForm(p) {
  let current = p;
  const isUploaded = (url) => url?.startsWith('/uploads/');

  openModal(`
    <h2>${p ? 'Редактирование товара' : 'Новый товар'}</h2>
    <form id="product-form" novalidate>
      ${field('Название', `<input name="name" value="${esc(p?.name)}">`)}
      ${field('Описание', `<textarea name="description" rows="3">${esc(p?.description)}</textarea>`)}
      <div class="field-row">
        ${field('Цена, ₽', `<input name="price" type="number" step="0.01" data-number value="${p?.price ?? ''}">`)}
        ${field('Остаток', `<input name="stock" type="number" data-number value="${p?.stock ?? 0}">`)}
      </div>
      ${field('Категория', `<select name="categoryId" data-number>
          <option value="0">— выберите —</option>
          ${categoriesCache.map(c => `<option value="${c.id}" ${c.id === p?.categoryId ? 'selected' : ''}>${esc(c.name)}</option>`).join('')}
        </select>`)}
      <div class="field">
        <span class="field__label">Изображение</span>
        <div class="upload">
          <div class="upload__preview cat-${(p?.categoryId ?? 0) % 6}" id="image-preview">${p ? productImage(p) : '📷'}</div>
          <div class="upload__controls">
            <input type="file" id="image-file" accept="image/jpeg,image/png,image/webp,image/gif">
            <div class="muted small">JPG, PNG, WEBP или GIF, до 2 МБ</div>
            ${isUploaded(p?.imageUrl) ? '<button type="button" class="link-btn link-btn--danger" id="remove-image">Удалить изображение</button>' : ''}
          </div>
        </div>
      </div>
      ${field('Или ссылка на картинку', `<input name="imageUrl" placeholder="https://…" value="${esc(isUploaded(p?.imageUrl) ? '' : p?.imageUrl)}">`)}
      <button type="submit" class="btn btn--block">${p ? 'Сохранить' : 'Создать'}</button>
    </form>`);

  const form = document.getElementById('product-form');
  const fileInput = document.getElementById('image-file');

  fileInput.addEventListener('change', () => {
    const file = fileInput.files[0];
    if (file) document.getElementById('image-preview').innerHTML = `<img src="${URL.createObjectURL(file)}" alt="">`;
  });

  document.getElementById('remove-image')?.addEventListener('click', async () => {
    try {
      current = await api.products.removeImage(current.id);
      toast('Изображение удалено');
      productForm(current);
      adminProducts();
    } catch (err) { showError(err); }
  });

  onSubmit(form, async (dto) => {
    if (!dto.imageUrl) dto.imageUrl = isUploaded(current?.imageUrl) ? current.imageUrl : null;

    current = current
      ? await api.products.update(current.id, dto)
      : await api.products.create(dto);

    const file = fileInput.files[0];
    if (file) current = await api.products.uploadImage(current.id, file);

    closeModal();
    toast(p ? 'Товар обновлён' : 'Товар создан');
    adminProducts();
  });
}

async function adminCategories() {
  const box = document.getElementById('admin-content');
  try {
    categoriesCache = await api.categories.list();
    box.innerHTML = `
      <div class="toolbar"><span></span><button class="btn" id="add-cat">+ Добавить категорию</button></div>
      <div class="card table-wrap">
        <table class="table">
          <thead><tr><th>ID</th><th>Название</th><th>Описание</th><th>Товаров</th><th></th></tr></thead>
          <tbody>
            ${categoriesCache.map(c => `
              <tr>
                <td class="muted">${c.id}</td>
                <td><strong>${esc(c.name)}</strong></td>
                <td class="muted">${esc(c.description)}</td>
                <td>${c.productCount}</td>
                <td class="actions">
                  <button class="btn btn--ghost btn--sm" data-edit="${c.id}">Изменить</button>
                  <button class="btn btn--danger btn--sm" data-del="${c.id}">Удалить</button>
                </td>
              </tr>`).join('')}
          </tbody>
        </table>
      </div>`;

    box.querySelector('#add-cat').addEventListener('click', () => categoryForm(null));
    box.querySelectorAll('[data-edit]').forEach(b => b.addEventListener('click', () =>
      categoryForm(categoriesCache.find(c => c.id === Number(b.dataset.edit)))));
    box.querySelectorAll('[data-del]').forEach(b => b.addEventListener('click', async () => {
      if (!confirm('Удалить категорию?')) return;
      try { await api.categories.remove(b.dataset.del); toast('Категория удалена'); adminCategories(); } catch (err) { showError(err); }
    }));
  } catch (err) { showError(err); }
}

function categoryForm(c) {
  openModal(`
    <h2>${c ? 'Редактирование категории' : 'Новая категория'}</h2>
    <form id="cat-form" novalidate>
      ${field('Название', `<input name="name" value="${esc(c?.name)}">`)}
      ${field('Описание', `<textarea name="description" rows="3">${esc(c?.description)}</textarea>`)}
      <button type="submit" class="btn btn--block">${c ? 'Сохранить' : 'Создать'}</button>
    </form>`);

  onSubmit(document.getElementById('cat-form'), async (dto) => {
    if (c) await api.categories.update(c.id, dto);
    else await api.categories.create(dto);
    closeModal();
    toast(c ? 'Категория обновлена' : 'Категория создана');
    adminCategories();
  });
}

let adminOrderFilter = '';

async function adminOrders() {
  const box = document.getElementById('admin-content');
  try {
    const orders = await api.orders.all(adminOrderFilter);
    box.innerHTML = `
      <div class="toolbar">
        <select id="order-filter">
          <option value="">Все статусы</option>
          ${Object.entries(STATUS).map(([k, v]) => `<option value="${k}" ${k === adminOrderFilter ? 'selected' : ''}>${v.label}</option>`).join('')}
        </select>
        <span class="muted">Заказов: ${orders.length}</span>
      </div>
      <div class="stack">${orders.map(o => orderCard(o, true)).join('') || '<div class="empty card">Заказов нет</div>'}</div>`;

    box.querySelector('#order-filter').addEventListener('change', (e) => { adminOrderFilter = e.target.value; adminOrders(); });

    box.querySelectorAll('[data-status]').forEach(sel => sel.addEventListener('change', async () => {
      try {
        await api.orders.setStatus(sel.dataset.status, sel.value);
        toast(`Статус заказа №${sel.dataset.status} изменён`);
      } catch (err) { showError(err); }
      adminOrders();
    }));

    box.querySelectorAll('[data-delete-order]').forEach(b => b.addEventListener('click', async () => {
      if (!confirm('Удалить заказ?')) return;
      try { await api.orders.remove(b.dataset.deleteOrder); toast('Заказ удалён'); adminOrders(); } catch (err) { showError(err); }
    }));
  } catch (err) { showError(err); }
}

async function adminReviews() {
  const box = document.getElementById('admin-content');
  try {
    const reviews = await api.reviews.latest();
    box.innerHTML = `
      <div class="toolbar"><span class="muted">Последние отзывы: ${reviews.length}</span></div>
      <div class="card table-wrap">
        <table class="table">
          <thead><tr><th>Товар</th><th>Автор</th><th>Оценка</th><th>Текст</th><th>Дата</th><th></th></tr></thead>
          <tbody>
            ${reviews.map(r => `
              <tr>
                <td><strong>${esc(r.productName)}</strong></td>
                <td>${esc(r.userName)}</td>
                <td>${stars(r.rating, 'stars--sm')}</td>
                <td class="muted cell-text">${esc(r.text) || '—'}</td>
                <td class="muted small">${date(r.createdAt)}</td>
                <td class="actions"><button class="btn btn--danger btn--sm" data-del="${r.id}">Удалить</button></td>
              </tr>`).join('') || `<tr><td colspan="6" class="empty">Отзывов нет</td></tr>`}
          </tbody>
        </table>
      </div>`;

    box.querySelectorAll('[data-del]').forEach(b => b.addEventListener('click', async () => {
      if (!confirm('Удалить отзыв?')) return;
      try { await api.reviews.remove(b.dataset.del); toast('Отзыв удалён'); adminReviews(); } catch (err) { showError(err); }
    }));
  } catch (err) { showError(err); }
}

const routes = {
  '': { view: catalogPage },
  'cart': { view: cartPage },
  'orders': { view: ordersPage, auth: true },
  'admin': { view: adminPage, admin: true },
  'login': { view: loginPage },
  'register': { view: registerPage }
};

function router() {
  const path = location.hash.replace(/^#\/?/, '').split('?')[0];
  const route = routes[path];
  closeModal();
  renderNav();

  if (!route) {
    app.innerHTML = `<div class="empty card"><h1>404</h1><p>Страница не найдена.</p><a href="#/">На главную</a></div>`;
    return;
  }
  if ((route.auth || route.admin) && !session.isLoggedIn) {
    toast('Сначала войдите в аккаунт', 'error');
    location.hash = '#/login';
    return;
  }
  if (route.admin && !session.isAdmin) {
    app.innerHTML = `<div class="empty card"><h1>403</h1><p>Доступ только для администратора.</p></div>`;
    return;
  }
  route.view();
}

window.addEventListener('hashchange', router);
router();
