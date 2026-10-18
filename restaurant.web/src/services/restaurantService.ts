import apiClient from './apiClient';
import type { RestaurantDto } from '../types/api';

let pending: { token: string | null; promise: Promise<RestaurantDto> } | null = null;

export function getCurrentRestaurant(): Promise<RestaurantDto> {
  const token = localStorage.getItem('token');
  if (pending?.token === token) return pending.promise;
  const promise = apiClient.get<RestaurantDto>('/api/restaurants/current').then(restaurant => {
    if (localStorage.getItem('token') !== token) throw new Error('تغيرت جلسة المستخدم. أعد محاولة الطباعة.');
    if (!restaurant.name?.trim()) throw new Error('اسم المطعم غير محدد في الإعدادات.');
    return restaurant;
  }).finally(() => {
    if (pending?.promise === promise) pending = null;
  });
  pending = { token, promise };
  return promise;
}

export function invalidateRestaurantInfo(): void {
  pending = null;
}
