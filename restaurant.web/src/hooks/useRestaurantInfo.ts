import { useEffect, useState } from 'react';
import { useAuth } from './useAuth';
import { getCurrentRestaurant, invalidateRestaurantInfo } from '../services/restaurantService';
import type { RestaurantDto } from '../types/api';

export function useRestaurantInfo() {
  const { user } = useAuth();
  const [restaurant, setRestaurant] = useState<RestaurantDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  useEffect(() => {
    let active = true;
    let generation = 0;
    const reload = () => {
      const request = ++generation;
      setIsLoading(true);
      getCurrentRestaurant().then(value => {
        if (active && request === generation) { setRestaurant(value); setError(null); }
      }).catch((cause: unknown) => {
        if (active && request === generation) { setRestaurant(null); setError(cause instanceof Error ? cause.message : 'تعذر تحميل بيانات المطعم.'); }
      }).finally(() => { if (active && request === generation) setIsLoading(false); });
    };
    const onUpdated = () => { invalidateRestaurantInfo(); reload(); };
    const timer = window.setTimeout(reload, 0);
    window.addEventListener('restaurant:updated', onUpdated);
    return () => { active = false; window.clearTimeout(timer); window.removeEventListener('restaurant:updated', onUpdated); };
  }, [user?.id]);
  return { restaurant, error, isLoading };
}
