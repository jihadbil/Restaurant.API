import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import { ENV } from '../config/env';

/**
 * Centralized API Client with JWT Bearer Token Injection
 * Matches Restaurant.Desktop (Core/ApiClient.cs)
 */
class ApiClient {
  private instance: AxiosInstance;

  constructor() {
    this.instance = axios.create({
      baseURL: ENV.API_BASE_URL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
      },
    });

    // Request Interceptor: Inject JWT Bearer Token
    this.instance.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        const token = localStorage.getItem('token');
        if (token && config.headers) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response Interceptor: Handle Global 401 & Normalize Errors
    this.instance.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        if (error.response?.status === 401) {
          // Token expired or invalid
          localStorage.removeItem('token');
          localStorage.removeItem('currentUser');
          window.dispatchEvent(new Event('auth:unauthorized'));
          
          if (!window.location.pathname.includes('/login')) {
            window.location.href = '/login';
          }
        }
        return Promise.reject(this.normalizeError(error));
      }
    );
  }

  private normalizeError(error: AxiosError): Error {
    let message = 'حدث خطأ في الاتصال بالخادم. يرجى التحقق من اتصال الشبكة.';
    if (error.response?.data) {
      const data = error.response.data as Record<string, unknown>;
      if (typeof data === 'string') {
        message = data;
      } else if (data.message && typeof data.message === 'string') {
        message = data.message;
      } else if (data.title && typeof data.title === 'string') {
        message = data.title;
      } else if (data.errors && typeof data.errors === 'object') {
        const errorsList = Object.values(data.errors).flat();
        if (errorsList.length > 0) {
          message = String(errorsList[0]);
        }
      }
    } else if (error.message) {
      message = error.message;
    }
    return new Error(message);
  }

  public async get<T>(url: string, params?: Record<string, unknown>): Promise<T> {
    const res = await this.instance.get<T>(url, { params });
    return res.data;
  }

  public async post<T>(url: string, data?: unknown): Promise<T> {
    const res = await this.instance.post<T>(url, data);
    return res.data;
  }

  public async put<T>(url: string, data?: unknown): Promise<T> {
    const res = await this.instance.put<T>(url, data);
    return res.data;
  }

  public async delete<T>(url: string, params?: Record<string, unknown>): Promise<T> {
    const res = await this.instance.delete<T>(url, { params });
    return res.data;
  }

  public async upload<T>(url: string, formData: FormData): Promise<T> {
    const res = await this.instance.post<T>(url, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return res.data;
  }
}

export const apiClient = new ApiClient();
export default apiClient;
