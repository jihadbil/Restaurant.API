/**
 * Formats an image URL returned from the API to be loaded correctly by the browser.
 * Ensures relative paths starting with '/images' or 'images' are properly resolved.
 */
export const getFullImageUrl = (url?: string | null): string => {
  if (!url || typeof url !== 'string') return '';
  const trimmed = url.trim();
  if (!trimmed) return '';
  if (
    trimmed.startsWith('http://') ||
    trimmed.startsWith('https://') ||
    trimmed.startsWith('data:') ||
    trimmed.startsWith('blob:')
  ) {
    return trimmed;
  }
  const relative = trimmed.startsWith('/') ? trimmed : `/${trimmed}`;
  if (!ENV.API_BASE_URL) return relative;
  return new URL(relative, new URL(ENV.API_BASE_URL, window.location.origin)).href;
};
import { ENV } from '../config/env';
