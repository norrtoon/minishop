const TOKEN_KEY = 'minishop.token';
const USER_KEY = 'minishop.user';

export class ApiError extends Error {
  constructor(status, problem) {
    super(problem?.detail || problem?.title || `Ошибка ${status}`);
    this.status = status;
    this.title = problem?.title || 'Ошибка';
    this.fieldErrors = problem?.errors || null;
  }
}

export const session = {
  get token() { return safeGet(TOKEN_KEY); },
  get user() {
    try { return JSON.parse(safeGet(USER_KEY) || 'null'); } catch { return null; }
  },
  get isLoggedIn() { return !!this.token; },
  get isAdmin() { return this.user?.role === 'Admin'; },
  save(auth) {
    safeSet(TOKEN_KEY, auth.token);
    safeSet(USER_KEY, JSON.stringify(auth.user));
  },
  clear() {
    safeRemove(TOKEN_KEY);
    safeRemove(USER_KEY);
  }
};

function safeGet(k) { try { return localStorage.getItem(k); } catch { return null; } }
function safeSet(k, v) { try { localStorage.setItem(k, v); } catch { } }
function safeRemove(k) { try { localStorage.removeItem(k); } catch { } }

let onUnauthorized = () => {};
export function setUnauthorizedHandler(fn) { onUnauthorized = fn; }

async function request(method, url, body) {
  const isForm = body instanceof FormData;
  const headers = { 'Accept': 'application/json' };
  if (body !== undefined && !isForm) headers['Content-Type'] = 'application/json';
  if (session.token) headers['Authorization'] = `Bearer ${session.token}`;

  let response;
  try {
    response = await fetch(url, {
      method,
      headers,
      body: body === undefined ? undefined : isForm ? body : JSON.stringify(body)
    });
  } catch {
    throw new ApiError(0, { title: 'Нет связи', detail: 'Сервер недоступен.' });
  }

  if (response.status === 204) return null;

  const isJson = (response.headers.get('content-type') || '').includes('json');
  const data = isJson ? await response.json() : null;

  if (!response.ok) {
    if (response.status === 401 && session.token) onUnauthorized();
    throw new ApiError(response.status, data);
  }
  return data;
}

function qs(params) {
  const p = new URLSearchParams();
  Object.entries(params || {}).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '' && v !== false) p.append(k, v);
  });
  const s = p.toString();
  return s ? `?${s}` : '';
}

export const api = {
  auth: {
    login: (dto) => request('POST', '/api/auth/login', dto),
    register: (dto) => request('POST', '/api/auth/register', dto),
    me: () => request('GET', '/api/auth/me')
  },
  categories: {
    list: () => request('GET', '/api/categories'),
    create: (dto) => request('POST', '/api/categories', dto),
    update: (id, dto) => request('PUT', `/api/categories/${id}`, dto),
    remove: (id) => request('DELETE', `/api/categories/${id}`)
  },
  products: {
    list: (query) => request('GET', `/api/products${qs(query)}`),
    get: (id) => request('GET', `/api/products/${id}`),
    create: (dto) => request('POST', '/api/products', dto),
    update: (id, dto) => request('PUT', `/api/products/${id}`, dto),
    remove: (id) => request('DELETE', `/api/products/${id}`),
    uploadImage: (id, file) => {
      const form = new FormData();
      form.append('file', file);
      return request('POST', `/api/products/${id}/image`, form);
    },
    removeImage: (id) => request('DELETE', `/api/products/${id}/image`)
  },
  reviews: {
    forProduct: (productId) => request('GET', `/api/products/${productId}/reviews`),
    create: (productId, dto) => request('POST', `/api/products/${productId}/reviews`, dto),
    update: (id, dto) => request('PUT', `/api/reviews/${id}`, dto),
    remove: (id) => request('DELETE', `/api/reviews/${id}`),
    latest: () => request('GET', '/api/reviews')
  },
  orders: {
    my: () => request('GET', '/api/orders/my'),
    all: (status) => request('GET', `/api/orders${qs({ status })}`),
    create: (dto) => request('POST', '/api/orders', dto),
    cancel: (id) => request('POST', `/api/orders/${id}/cancel`),
    setStatus: (id, status) => request('PATCH', `/api/orders/${id}/status`, { status }),
    remove: (id) => request('DELETE', `/api/orders/${id}`)
  }
};
