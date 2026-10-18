import type { OrderCheckoutCreateDto } from '../types/api';

export interface PendingSale<TCart> {
  id: string;
  payload: OrderCheckoutCreateDto;
  cart: TCart[];
}
const key = (userId: string) => `pending-sale:${userId}`;

export function readPendingSale<TCart>(userId: string): PendingSale<TCart> | null {
  try {
    const raw = localStorage.getItem(key(userId));
    if (!raw) return null;
    const saved = JSON.parse(raw) as PendingSale<TCart>;
    if (!saved.id || saved.payload?.clientRequestId !== saved.id || !Array.isArray(saved.cart)) return null;
    return saved;
  } catch { return null; }
}

export function savePendingSale<TCart>(userId: string, sale: PendingSale<TCart>): void {
  // Persist before sending: a storage failure must not create an unrecoverable payment.
  localStorage.setItem(key(userId), JSON.stringify(sale));
}
export function clearPendingSale(userId: string): void { localStorage.removeItem(key(userId)); }
