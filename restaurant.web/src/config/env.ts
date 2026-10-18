/**
 * Application Environment Configuration
 */
export const ENV = {
  // When running locally, using empty string allows Vite dev proxy (/api -> https://localhost:7040)
  // to prevent browser CORS preflight blocking and self-signed certificate issues.
  API_BASE_URL: import.meta.env.VITE_API_BASE_URL ?? '',
  PRINT_SERVICE_URL: import.meta.env.VITE_PRINT_SERVICE_URL ?? 'http://127.0.0.1:5055',
  PRINT_SYSTEM_ID: import.meta.env.VITE_PRINT_SYSTEM_ID ?? 'restaurant',
  BUSINESS_TIME_ZONE: import.meta.env.VITE_BUSINESS_TIME_ZONE ?? 'Africa/Tripoli',
  DEFAULT_PAGE_SIZE: 20,
  APP_NAME: 'Emerald Management Pro',
  CURRENCY_SYMBOL: 'د.ل', // Libyan Dinar
};
