import { ENV } from '../config/env';

/** Date input values represent the restaurant's calendar day, independently of the device zone. */
export function localDate(date = new Date()): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: ENV.BUSINESS_TIME_ZONE }).format(date);
}
