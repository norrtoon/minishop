
const CART_KEY = 'minishop.cart';
const listeners = new Set();

function load() {
  try { return JSON.parse(localStorage.getItem(CART_KEY) || '[]'); } catch { return []; }
}

function save(items) {
  try { localStorage.setItem(CART_KEY, JSON.stringify(items)); } catch { }
  listeners.forEach(fn => fn());
}

export const cart = {
  items: () => load(),

  count: () => load().reduce((s, i) => s + i.quantity, 0),

  total: () => load().reduce((s, i) => s + i.price * i.quantity, 0),

  quantityOf: (productId) => load().find(i => i.productId === productId)?.quantity || 0,

  add(product, qty = 1) {
    const items = load();
    const existing = items.find(i => i.productId === product.id);
    if (existing) {
      existing.quantity = Math.min(existing.quantity + qty, product.stock);
    } else {
      items.push({
        productId: product.id,
        name: product.name,
        price: product.price,
        stock: product.stock,
        categoryName: product.categoryName,
        quantity: Math.min(qty, product.stock)
      });
    }
    save(items);
  },

  setQuantity(productId, qty) {
    let items = load();
    const item = items.find(i => i.productId === productId);
    if (!item) return;
    item.quantity = Math.max(1, Math.min(qty, item.stock || 100));
    save(items);
  },

  remove(productId) {
    save(load().filter(i => i.productId !== productId));
  },

  clear() { save([]); },

  onChange(fn) { listeners.add(fn); }
};
