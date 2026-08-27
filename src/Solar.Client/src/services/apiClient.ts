import type { ProblemDetails } from '../types/api';

export class ApiError extends Error {
  public problem: ProblemDetails;
  public status: number;

  constructor(problem: ProblemDetails, status: number) {
    super(problem.detail || problem.title || `Erro HTTP ${status}`);
    this.name = 'ApiError';
    this.problem = problem;
    this.status = status;
  }
}

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = '') {
    this.baseUrl = baseUrl;
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;
    const correlationId = typeof crypto !== 'undefined' && crypto.randomUUID 
      ? crypto.randomUUID() 
      : `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;

    const headers: Record<string, string> = {
      'Accept': 'application/json, application/problem+json',
      'X-Correlation-ID': correlationId,
      ...(options.headers as Record<string, string>),
    };

    if (!(options.body instanceof FormData) && !headers['Content-Type']) {
      headers['Content-Type'] = 'application/json';
    }

    const response = await fetch(url, {
      ...options,
      headers,
      credentials: 'include', // Envia cookies HttpOnly (solar_access_token) automaticamente
    });

    if (response.status === 401) {
      console.warn(`[ApiClient] Sessão expirada ou não autorizada (401) [Trace: ${correlationId}].`);
    }

    if (!response.ok) {
      let problem: ProblemDetails;
      try {
        problem = await response.json();
      } catch {
        problem = {
          title: response.statusText,
          status: response.status,
          detail: 'Ocorreu um erro na comunicação com o servidor.',
        };
      }
      throw new ApiError(problem, response.status);
    }

    // Se for 204 No Content
    if (response.status === 204) {
      return {} as T;
    }

    return await response.json();
  }

  public get<T>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: 'GET' });
  }

  public post<T>(endpoint: string, data?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'POST',
      body: data instanceof FormData ? data : JSON.stringify(data),
    });
  }

  public put<T>(endpoint: string, data?: unknown, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, {
      ...options,
      method: 'PUT',
      body: data instanceof FormData ? data : JSON.stringify(data),
    });
  }

  public delete<T>(endpoint: string, options?: RequestInit): Promise<T> {
    return this.request<T>(endpoint, { ...options, method: 'DELETE' });
  }
}

export const apiClient = new ApiClient();
