import { authService } from './oidc-auth';
import { HttpClient, HttpResponse, HttpError } from './http';
import { HTTP_CONFIG } from '../config/api';

/**
 * 认证拦截器配置
 */
export interface AuthInterceptorConfig {
  excludePaths?: string[];
  includeCredentials?: boolean;
  onUnauthorized?: () => void;
  onTokenRefreshFailed?: () => void;
}

/**
 * 认证拦截器类
 * 自动添加访问令牌到请求头，处理401错误并自动刷新令牌
 */
export class AuthInterceptor {
  private config: AuthInterceptorConfig;
  private isRefreshing = false;
  private failedQueue: Array<{
    resolve: (value: HttpResponse<any>) => void;
    reject: (reason?: any) => void;
    request: () => Promise<HttpResponse<any>>;
  }> = [];

  constructor(config: AuthInterceptorConfig = {}) {
    this.config = {
      excludePaths: ['/connect/token', '/connect/userinfo', '/connect/revoke'],
      includeCredentials: true,
      ...config
    };
  }

  /**
   * 请求拦截器
   */
  async interceptRequest(url: string, config: RequestInit = {}): Promise<RequestInit> {
    // 检查是否需要排除此路径
    if (this.shouldExcludePath(url)) {
      return config;
    }

    try {
      // 确保有有效的令牌
      const hasValidToken = await authService.ensureValidToken();
      
      if (hasValidToken) {
        const accessToken = authService.getAccessToken();
        if (accessToken) {
          const headers = new Headers(config.headers);
          headers.set('Authorization', `Bearer ${accessToken}`);
          
          config.headers = headers;
        }
      }

      // 添加凭据支持
      if (this.config.includeCredentials) {
        config.credentials = 'include';
      }

      return config;
    } catch (error) {
      console.error('请求拦截器处理失败:', error);
      return config;
    }
  }

  /**
   * 响应拦截器
   */
  async interceptResponse<T>(
    response: HttpResponse<T>,
    originalRequest: () => Promise<HttpResponse<T>>
  ): Promise<HttpResponse<T>> {
    // 如果响应正常，直接返回
    if (response.status !== 401) {
      return response;
    }

    // 检查是否应该处理401错误
    if (!this.shouldHandleUnauthorized(response)) {
      return response;
    }

    // 如果正在刷新令牌，将请求加入队列
    if (this.isRefreshing) {
      return new Promise((resolve, reject) => {
        this.failedQueue.push({
          resolve,
          reject,
          request: originalRequest
        });
      });
    }

    this.isRefreshing = true;

    try {
      // 尝试刷新令牌
      const refreshed = await authService.silentRenew();
      
      if (refreshed) {
        // 刷新成功，处理队列中的请求
        this.processQueue(null);
        
        // 重试原始请求
        return await originalRequest();
      } else {
        // 刷新失败，处理队列中的请求
        const error = new Error('令牌刷新失败');
        this.processQueue(error);
        
        // 触发未授权处理
        if (this.config.onUnauthorized) {
          this.config.onUnauthorized();
        }
        
        throw error;
      }
    } catch (error) {
      // 刷新令牌失败
      this.processQueue(error as Error);
      
      if (this.config.onTokenRefreshFailed) {
        this.config.onTokenRefreshFailed();
      }
      
      throw error;
    } finally {
      this.isRefreshing = false;
    }
  }

  /**
   * 处理队列中的请求
   */
  private processQueue(error: Error | null): void {
    this.failedQueue.forEach(({ resolve, reject, request }) => {
      if (error) {
        reject(error);
      } else {
        request()
          .then(resolve)
          .catch(reject);
      }
    });
    
    this.failedQueue.length = 0;
  }

  /**
   * 检查是否应该排除此路径
   */
  private shouldExcludePath(url: string): boolean {
    return this.config.excludePaths?.some(path => url.includes(path)) || false;
  }

  /**
   * 检查是否应该处理401错误
   */
  private shouldHandleUnauthorized<T>(response: HttpResponse<T>): boolean {
    // 如果用户未认证，不处理401错误
    if (!authService.isAuthenticated()) {
      return false;
    }

    // 检查是否有刷新令牌
    const state = authService.getState();
    return !!state.refreshToken;
  }
}

/**
 * 增强的HTTP客户端，集成认证拦截器
 */
export class AuthenticatedHttpClient extends HttpClient {
  private authInterceptor: AuthInterceptor;

  constructor(config = {}, interceptorConfig?: AuthInterceptorConfig) {
    super(config);
    this.authInterceptor = new AuthInterceptor(interceptorConfig);
  }

  protected async makeRequest<T>(
    url: string,
    config: RequestInit = {}
  ): Promise<HttpResponse<T>> {
    try {
      // 应用请求拦截器
      const interceptedConfig = await this.authInterceptor.interceptRequest(url, config);
      
      // 执行原始请求
      const originalRequest = () => super.makeRequest<T>(url, interceptedConfig);
      const response = await originalRequest();
      
      // 应用响应拦截器
      return await this.authInterceptor.interceptResponse(response, originalRequest);
    } catch (error) {
      if (error instanceof HttpError && error.status === 401) {
        // 尝试应用响应拦截器处理401错误
        const originalRequest = () => super.makeRequest<T>(url, config);
        const mockResponse: HttpResponse<T> = {
          data: {} as T,
          status: 401,
          statusText: 'Unauthorized',
          headers: new Headers()
        };
        
        try {
          return await this.authInterceptor.interceptResponse(mockResponse, originalRequest);
        } catch (interceptorError) {
          throw error; // 抛出原始错误
        }
      }
      
      throw error;
    }
  }
}

/**
 * 默认的认证HTTP客户端实例
 */
export const authenticatedHttpClient = new AuthenticatedHttpClient(
  HTTP_CONFIG.authServer,
  {
    onUnauthorized: () => {
      console.warn('用户未授权，可能需要重新登录');
      // 可以在这里触发重新登录流程
      // authService.login();
    },
    onTokenRefreshFailed: () => {
      console.error('令牌刷新失败，用户需要重新登录');
      // 可以在这里清理状态并重定向到登录页
      // authService.logout();
    }
  }
);

/**
 * 便捷方法：为现有HTTP客户端添加认证拦截器
 */
export function withAuthInterceptor(
  httpClient: HttpClient,
  config?: AuthInterceptorConfig
): AuthenticatedHttpClient {
  const clientConfig = httpClient.getConfig();
  return new AuthenticatedHttpClient(clientConfig, config);
}

/**
 * 兼容性方法：为现有项目提供向后兼容
 */
export function setupAuthInterceptors(httpClient: HttpClient): void {
  console.warn('setupAuthInterceptors 已废弃，请使用 AuthenticatedHttpClient');
  // 为向后兼容，可以保留此方法但建议迁移到新的实现
}

export function createAuthenticatedHttpClient(): HttpClient {
  return authenticatedHttpClient;
}

export const authHttpClient = authenticatedHttpClient;