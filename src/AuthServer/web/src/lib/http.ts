export interface HttpClientConfig {
  baseURL?: string;
  timeout?: number;
  headers?: Record<string, string>;
}

export interface RequestConfig extends RequestInit {
  timeout?: number;
  params?: Record<string, string | number | boolean>;
}

export interface HttpResponse<T = any> {
  data: T;
  status: number;
  statusText: string;
  headers: Headers;
}

export class HttpError extends Error {
  public status: number;
  public statusText: string;
  public response: Response;

  constructor(
    status: number,
    statusText: string,
    response: Response,
    message?: string
  ) {
    super(message || `HTTP ${status}: ${statusText}`);
    this.name = 'HttpError';
    this.status = status;
    this.statusText = statusText;
    this.response = response;
  }
}

export class HttpClient {
  private config: HttpClientConfig;

  constructor(config: HttpClientConfig = {}) {
    this.config = {
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
      ...config,
    };
  }

  private buildUrl(url: string, params?: Record<string, string | number | boolean>): string {
    const baseUrl = this.config.baseURL ? `${this.config.baseURL}${url}` : url;
    
    if (!params || Object.keys(params).length === 0) {
      return baseUrl;
    }

    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      searchParams.append(key, String(value));
    });

    const separator = baseUrl.includes('?') ? '&' : '?';
    return `${baseUrl}${separator}${searchParams.toString()}`;
  }

  protected async makeRequest<T>(
    url: string,
    config: RequestConfig = {}
  ): Promise<HttpResponse<T>> {
    const { timeout, params, ...fetchConfig } = config;
    const finalUrl = this.buildUrl(url, params);
    
    const mergedHeaders = {
      ...this.config.headers,
      ...fetchConfig.headers,
    };

    const controller = new AbortController();
    const timeoutId = setTimeout(() => {
      controller.abort();
    }, timeout || this.config.timeout);

    try {
      const response = await fetch(finalUrl, {
        ...fetchConfig,
        headers: mergedHeaders,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      if (!response.ok) {
        throw new HttpError(
          response.status,
          response.statusText,
          response.clone()
        );
      }

      let data: T;
      const contentType = response.headers.get('content-type');
      
      if (contentType && contentType.includes('application/json')) {
        data = await response.json();
      } else {
        data = (await response.text()) as unknown as T;
      }

      return {
        data,
        status: response.status,
        statusText: response.statusText,
        headers: response.headers,
      };
    } catch (error) {
      clearTimeout(timeoutId);
      
      if (error instanceof HttpError) {
        throw error;
      }
      
      if (error instanceof DOMException && error.name === 'AbortError') {
        throw new Error('Request timeout');
      }
      
      throw error;
    }
  }

  async get<T>(url: string, config?: RequestConfig): Promise<HttpResponse<T>> {
    return this.makeRequest<T>(url, { ...config, method: 'GET' });
  }

  async post<T>(url: string, data?: any, config?: RequestConfig): Promise<HttpResponse<T>> {
    return this.makeRequest<T>(url, {
      ...config,
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async put<T>(url: string, data?: any, config?: RequestConfig): Promise<HttpResponse<T>> {
    return this.makeRequest<T>(url, {
      ...config,
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async patch<T>(url: string, data?: any, config?: RequestConfig): Promise<HttpResponse<T>> {
    return this.makeRequest<T>(url, {
      ...config,
      method: 'PATCH',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  async delete<T>(url: string, config?: RequestConfig): Promise<HttpResponse<T>> {
    return this.makeRequest<T>(url, { ...config, method: 'DELETE' });
  }

  setHeader(name: string, value: string): void {
    this.config.headers = { ...this.config.headers, [name]: value };
  }

  removeHeader(name: string): void {
    if (this.config.headers) {
      const { [name]: _, ...rest } = this.config.headers;
      this.config.headers = rest;
    }
  }

  setBaseURL(baseURL: string): void {
    this.config.baseURL = baseURL;
  }

  getConfig(): HttpClientConfig {
    return { ...this.config };
  }
}

// 创建默认实例，不设置 baseURL，让 Vite proxy 处理路由
export const httpClient = new HttpClient();

// 为了向后兼容，保留原有的导出
export default httpClient;