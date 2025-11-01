const runtimeConfig = (window as typeof window & {
  __LEXIFLOW_CONFIG__?: { apiBaseUrl?: string };
}).__LEXIFLOW_CONFIG__;
const defaultBase = (import.meta.env.VITE_API_BASE as string | undefined) ?? 'http://localhost:8081';
const apiBase = (runtimeConfig?.apiBaseUrl ?? defaultBase).replace(/\/$/, '');

let authToken: string | null = null;

export interface LoginResponse {
  token: string;
}

export interface ReceiptDto {
  id: string;
  vendor: string;
  invoiceDate: string;
  total: number;
  vat: number;
  currency: string;
  status: 'Pending' | 'Booked' | string;
  createdAt: string;
  updatedAt: string | null;
  rawText?: string | null;
  filePath?: string | null;
  voucherId?: string | null;
}

export interface BookReceiptResponse {
  voucherId: string;
}

export function setAuthToken(token: string | null) {
  authToken = token;
}

async function apiRequest<T>(path: string, init: RequestInit = {}): Promise<T> {
  const url = `${apiBase}${path}`;
  const headers = new Headers(init.headers ?? {});

  if (authToken) {
    headers.set('Authorization', `Bearer ${authToken}`);
  }

  const bodyIsFormData = init.body instanceof FormData;
  if (!bodyIsFormData && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(url, {
    ...init,
    headers,
  });

  if (!response.ok) {
    let errorMessage = response.statusText;
    try {
      const payload = await response.json();
      errorMessage = payload?.detail || payload?.message || JSON.stringify(payload);
    } catch (error) {
      // ignore json parse errors
    }
    const apiError = new Error(errorMessage || `Request failed (${response.status})`) as Error & {
      status?: number;
    };
    apiError.status = response.status;
    throw apiError;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('Content-Type') || '';
  if (contentType.includes('application/json')) {
    return (await response.json()) as T;
  }

  return (await response.text()) as unknown as T;
}

export async function login(username: string, password: string): Promise<LoginResponse> {
  return apiRequest<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  });
}

export async function fetchReceipts(): Promise<ReceiptDto[]> {
  return apiRequest<ReceiptDto[]>('/api/receipts');
}

export async function uploadReceipt(file: File): Promise<ReceiptDto> {
  const formData = new FormData();
  formData.append('file', file);
  return apiRequest<ReceiptDto>('/api/upload', {
    method: 'POST',
    body: formData,
  });
}

export async function bookReceipt(receiptId: string): Promise<BookReceiptResponse> {
  return apiRequest<BookReceiptResponse>('/api/book', {
    method: 'POST',
    body: JSON.stringify({ receiptId }),
  });
}

export function getApiBase(): string {
  return apiBase;
}
