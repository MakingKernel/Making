// 重新导出OIDC认证服务的类型和接口，保持向后兼容
import type { 
  OIDCConfig as OIDCConfigType, 
  User as UserType, 
  AuthState as AuthStateType, 
  TokenResponse as TokenResponseType, 
  ExternalProviderInfo as ExternalProviderInfoType 
} from './oidc-auth';

export type AuthConfig = OIDCConfigType;
export type UserInfo = UserType;
export type AuthState = AuthStateType;
export type TokenResponse = TokenResponseType;
export type ExternalProviderInfo = ExternalProviderInfoType;

// 为了兼容性，保留原有的登录和注册接口
export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface AccountResponse {
  success: boolean;
  message: string;
  user?: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
  };
  errors?: string[];
}

import { authenticatedHttpClient } from './http-interceptors';
import { authService as oidcAuthService, OIDCAuthService } from './oidc-auth';

/**
 * 增强的认证服务，包装OIDC服务并提供传统登录支持
 * 这是为了保持向后兼容性，同时支持新的OAuth2/OIDC流程
 */
class EnhancedAuthService {
  private oidcService: OIDCAuthService;

  constructor() {
    this.oidcService = oidcAuthService;
  }

  /**
   * 统一登录方法 - 支持OAuth2流程和传统密码登录
   */
  async login(credentials?: LoginRequest): Promise<void> {
    if (credentials) {
      return this.loginWithCredentials(credentials);
    }
    
    // OAuth2/OIDC 流程
    return this.oidcService.login();
  }

  /**
   * 传统密码登录（通过Account Management API）
   */
  async loginWithCredentials(credentials: LoginRequest): Promise<void> {
    try {
      const response = await authenticatedHttpClient.post<AccountResponse>('/Account/Login', {
        email: credentials.email,
        password: credentials.password,
        rememberMe: credentials.rememberMe || false
      });

      // 如果登录成功，模拟用户信息并更新OIDC服务状态
      if (response.data.success && response.data.user) {
        const user = response.data.user;
        // 模拟OAuth2用户信息格式
        const oidcUser = {
          sub: user.id,
          email: user.email,
          given_name: user.firstName,
          family_name: user.lastName,
          name: `${user.firstName} ${user.lastName}`.trim()
        };

        // 手动设置OIDC服务状态（用于传统登录）
        this.oidcService['setState']({
          user: oidcUser,
          isAuthenticated: true,
          accessToken: 'cookie-based-auth', // 标记为基于Cookie的认证
          expiresAt: Date.now() + 24 * 60 * 60 * 1000, // 24小时
          isLoading: false,
          error: null
        });
      } else {
        throw new Error(response.data.message || 'Login failed');
      }
    } catch (error: any) {
      console.error('Credential login failed:', error);
      
      if (error.status === 400 || error.status === 401) {
        throw new Error('邮箱或密码错误');
      }
      throw new Error('登录失败，请稍后重试');
    }
  }

  /**
   * 用户注册
   */
  async register(request: RegisterRequest): Promise<void> {
    try {
      const response = await authenticatedHttpClient.post<AccountResponse>('/Account/Register', request);

      if (response.data.success && response.data.user) {
        // 注册成功后自动登录
        const user = response.data.user;
        const oidcUser = {
          sub: user.id,
          email: user.email,
          given_name: user.firstName,
          family_name: user.lastName,
          name: `${user.firstName} ${user.lastName}`.trim()
        };

        this.oidcService['setState']({
          user: oidcUser,
          isAuthenticated: true,
          accessToken: 'cookie-based-auth',
          expiresAt: Date.now() + 24 * 60 * 60 * 1000,
          isLoading: false,
          error: null
        });
      } else {
        throw new Error(response.data.message || 'Registration failed');
      }
    } catch (error) {
      console.error('Registration failed:', error);
      throw error;
    }
  }

  /**
   * 获取外部登录提供商
   */
  async getExternalProviders(): Promise<ExternalProviderInfo[]> {
    return this.oidcService.getExternalProviders();
  }

  /**
   * 启动外部登录
   */
  startExternalLogin(provider: string, returnUrl?: string): void {
    this.oidcService.startExternalLogin(provider, returnUrl);
  }

  /**
   * 处理OAuth2授权回调
   */
  async handleCallback(): Promise<void> {
    return this.oidcService.handleCallback();
  }

  /**
   * 注销
   */
  async logout(): Promise<void> {
    try {
      // 尝试调用后端注销端点
      await authenticatedHttpClient.post('/Account/Logout', {});
    } catch (error) {
      console.error('Backend logout failed:', error);
    }
    
    // 无论后端调用成功与否，都进行OIDC注销
    return this.oidcService.logout();
  }

  // ====== 委托给OIDC服务的方法 ======

  async refreshAccessToken(): Promise<boolean> {
    return this.oidcService.silentRenew();
  }

  isTokenExpired(): boolean {
    return this.oidcService.isTokenExpired();
  }

  async ensureValidToken(): Promise<boolean> {
    return this.oidcService.ensureValidToken();
  }

  getAccessToken(): string | null {
    return this.oidcService.getAccessToken();
  }

  getUser(): UserInfo | null {
    return this.oidcService.getUser();
  }

  isAuthenticated(): boolean {
    return this.oidcService.isAuthenticated();
  }

  getState(): AuthState {
    return this.oidcService.getState();
  }

  onStateChange(callback: (state: AuthState) => void): () => void {
    return this.oidcService.onStateChange(callback);
  }
}

export const authService = new EnhancedAuthService();