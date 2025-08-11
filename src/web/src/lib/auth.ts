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

// 保留外部提供商接口，用于OAuth2扩展

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
   * 统一登录方法 - 纯OAuth2流程
   */
  async login(): Promise<void> {
    // 直接使用OAuth2/OIDC 流程
    return this.oidcService.login();
  }

  /**
   * OAuth2认证方法 - 用于OAuth2流程中的用户身份验证
   */
  async authenticateForOAuth2(email: string, password: string, rememberMe: boolean = false): Promise<void> {
    try {
      const response = await authenticatedHttpClient.post('/connect/authenticate', {
        email,
        password,
        rememberMe
      });

      if (response.data.success) {
        // 认证成功，现在可以重新发起OAuth2授权请求
        return;
      } else {
        throw new Error('认证失败');
      }
    } catch (error: any) {
      console.error('OAuth2 authentication failed:', error);
      
      if (error.status === 400) {
        const errorData = error.data || {};
        switch (errorData.error) {
          case 'invalid_credentials':
            throw new Error('邮箱或密码错误');
          case 'account_locked':
            throw new Error('账户已被锁定，请稍后再试');
          case 'two_factor_required':
            throw new Error('需要两步验证');
          default:
            throw new Error(errorData.error_description || '认证失败');
        }
      }
      
      throw new Error('认证失败，请稍后重试');
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