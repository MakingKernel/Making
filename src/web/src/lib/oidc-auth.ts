import { HttpError } from './http';

/**
 * OpenID Connect 配置接口
 */
export interface OIDCConfig {
  authority: string;
  client_id: string;
  redirect_uri: string;
  post_logout_redirect_uri: string;
  silent_redirect_uri: string;
  response_type: 'code';
  scope: string;
  automaticSilentRenew: boolean;
  filterProtocolClaims: boolean;
  loadUserInfo: boolean;
}

/**
 * 用户信息接口
 */
export interface User {
  sub: string;
  name?: string;
  given_name?: string;
  family_name?: string;
  email?: string;
  email_verified?: boolean;
  picture?: string;
  preferred_username?: string;
  [key: string]: any;
}

/**
 * 认证状态接口
 */
export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: number | null;
  isLoading: boolean;
  error: string | null;
}

/**
 * Token 响应接口
 */
export interface TokenResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token?: string;
  id_token?: string;
  scope?: string;
}

/**
 * 外部登录提供商信息
 */
export interface ExternalProviderInfo {
  name: string;
  displayName: string;
  provider: string;
}

/**
 * PKCE 辅助类
 */
class PKCEHelper {
  static generateCodeVerifier(): string {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return btoa(String.fromCharCode(...array))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');
  }

  static async generateCodeChallenge(verifier: string): Promise<string> {
    const encoder = new TextEncoder();
    const data = encoder.encode(verifier);
    const digest = await crypto.subtle.digest('SHA-256', data);
    return btoa(String.fromCharCode(...new Uint8Array(digest)))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');
  }

  static generateState(): string {
    const array = new Uint8Array(16);
    crypto.getRandomValues(array);
    return btoa(String.fromCharCode(...array))
      .replace(/[^a-zA-Z0-9]/g, '');
  }
}

/**
 * 默认 OIDC 配置
 */
const defaultOIDCConfig: OIDCConfig = {
  authority: 'http://localhost:5274',
  client_id: 'web-client',
  redirect_uri: `${window.location.origin}/callback`,
  post_logout_redirect_uri: `${window.location.origin}/`,
  silent_redirect_uri: `${window.location.origin}/auth/silent-callback`,
  response_type: 'code',
  scope: 'openid profile email',
  automaticSilentRenew: true,
  filterProtocolClaims: true,
  loadUserInfo: true
};

/**
 * 企业级 OIDC 认证服务
 * 遵循 OAuth2/OpenID Connect 标准实现
 */
export class OIDCAuthService {
  private config: OIDCConfig;
  private state: AuthState = {
    isAuthenticated: false,
    user: null,
    accessToken: null,
    refreshToken: null,
    expiresAt: null,
    isLoading: false,
    error: null
  };
  private listeners: Array<(state: AuthState) => void> = [];
  private silentRenewTimer?: NodeJS.Timeout;

  constructor(config?: Partial<OIDCConfig>) {
    this.config = { ...defaultOIDCConfig, ...config };
    this.initialize();
  }

  /**
   * 初始化认证服务
   */
  private async initialize(): Promise<void> {
    try {
      this.setState({ isLoading: true });
      await this.loadTokensFromStorage();
      this.setupSilentRenew();
    } catch (error) {
      console.error('认证服务初始化失败:', error);
      this.setState({ error: '认证服务初始化失败', isLoading: false });
    }
  }

  /**
   * 启动登录流程
   */
  async login(): Promise<void> {
    try {
      this.setState({ isLoading: true, error: null });
      
      const codeVerifier = PKCEHelper.generateCodeVerifier();
      const codeChallenge = await PKCEHelper.generateCodeChallenge(codeVerifier);
      const state = PKCEHelper.generateState();

      // 保存 PKCE 参数
      sessionStorage.setItem('pkce_code_verifier', codeVerifier);
      sessionStorage.setItem('auth_state', state);
      sessionStorage.setItem('auth_redirect_uri', this.config.redirect_uri);

      // 构建授权 URL
      const authUrl = new URL(`${this.config.authority}/connect/authorize`);
      authUrl.searchParams.set('client_id', this.config.client_id);
      authUrl.searchParams.set('response_type', this.config.response_type);
      authUrl.searchParams.set('redirect_uri', this.config.redirect_uri);
      authUrl.searchParams.set('scope', this.config.scope);
      authUrl.searchParams.set('state', state);
      authUrl.searchParams.set('code_challenge', codeChallenge);
      authUrl.searchParams.set('code_challenge_method', 'S256');

      // 重定向到授权服务器
      window.location.href = authUrl.toString();
    } catch (error) {
      console.error('启动登录流程失败:', error);
      this.setState({ error: '登录失败', isLoading: false });
      throw error;
    }
  }

  /**
   * 处理授权回调
   */
  async handleCallback(): Promise<void> {
    try {
      this.setState({ isLoading: true, error: null });
      
      const urlParams = new URLSearchParams(window.location.search);
      const code = urlParams.get('code');
      const state = urlParams.get('state');
      const error = urlParams.get('error');

      if (error) {
        throw new Error(`授权失败: ${error}`);
      }

      if (!code || !state) {
        throw new Error('缺少授权码或状态参数');
      }

      // 验证状态参数
      const storedState = sessionStorage.getItem('auth_state');
      if (state !== storedState) {
        throw new Error('无效的状态参数，可能存在CSRF攻击');
      }

      const codeVerifier = sessionStorage.getItem('pkce_code_verifier');
      if (!codeVerifier) {
        throw new Error('缺少代码验证器');
      }

      // 清理会话存储
      sessionStorage.removeItem('auth_state');
      sessionStorage.removeItem('pkce_code_verifier');
      sessionStorage.removeItem('auth_redirect_uri');

      // 交换授权码获取令牌
      await this.exchangeCodeForTokens(code, codeVerifier);
      
      // 清理URL参数
      window.history.replaceState({}, document.title, window.location.pathname);
    } catch (error) {
      console.error('处理授权回调失败:', error);
      this.setState({ error: error instanceof Error ? error.message : '授权回调处理失败', isLoading: false });
      throw error;
    }
  }

  /**
   * 交换授权码获取令牌
   */
  private async exchangeCodeForTokens(code: string, codeVerifier: string): Promise<void> {
    const tokenEndpoint = `${this.config.authority}/connect/token`;
    
    const body = new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: this.config.client_id,
      code: code,
      redirect_uri: this.config.redirect_uri,
      code_verifier: codeVerifier
    });

    try {
      const response = await fetch(tokenEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'Accept': 'application/json'
        },
        body: body.toString()
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error_description || `HTTP ${response.status}: ${response.statusText}`);
      }

      const tokenResponse: TokenResponse = await response.json();
      await this.processTokenResponse(tokenResponse);
    } catch (error) {
      console.error('Token exchange failed:', error);
      throw error;
    }
  }

  /**
   * 处理令牌响应
   */
  private async processTokenResponse(tokenResponse: TokenResponse): Promise<void> {
    const expiresAt = Date.now() + tokenResponse.expires_in * 1000;
    
    this.state.accessToken = tokenResponse.access_token;
    this.state.refreshToken = tokenResponse.refresh_token || null;
    this.state.expiresAt = expiresAt;

    // 保存到存储
    this.saveTokensToStorage();

    try {
      // 加载用户信息
      if (this.config.loadUserInfo) {
        const userInfo = await this.fetchUserInfo();
        this.setState({
          user: userInfo,
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
      } else {
        this.setState({
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
      }

      this.setupSilentRenew();
    } catch (error) {
      console.error('获取用户信息失败:', error);
      await this.logout();
      throw error;
    }
  }

  /**
   * 获取用户信息
   */
  private async fetchUserInfo(): Promise<User> {
    if (!this.state.accessToken) {
      throw new Error('没有访问令牌');
    }

    const userInfoEndpoint = `${this.config.authority}/connect/userinfo`;
    
    const response = await fetch(userInfoEndpoint, {
      headers: {
        'Authorization': `Bearer ${this.state.accessToken}`,
        'Accept': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error(`获取用户信息失败: HTTP ${response.status}`);
    }

    return await response.json();
  }

  /**
   * 静默刷新令牌
   */
  async silentRenew(): Promise<boolean> {
    try {
      if (!this.state.refreshToken) {
        console.warn('没有刷新令牌，无法静默刷新');
        return false;
      }

      const tokenEndpoint = `${this.config.authority}/connect/token`;
      const body = new URLSearchParams({
        grant_type: 'refresh_token',
        client_id: this.config.client_id,
        refresh_token: this.state.refreshToken
      });

      const response = await fetch(tokenEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'Accept': 'application/json'
        },
        body: body.toString()
      });

      if (!response.ok) {
        console.warn('刷新令牌失败:', response.status, response.statusText);
        await this.logout();
        return false;
      }

      const tokenResponse: TokenResponse = await response.json();
      await this.processTokenResponse(tokenResponse);
      return true;
    } catch (error) {
      console.error('静默刷新失败:', error);
      await this.logout();
      return false;
    }
  }

  /**
   * 设置静默刷新
   */
  private setupSilentRenew(): void {
    if (this.silentRenewTimer) {
      clearTimeout(this.silentRenewTimer);
    }

    if (!this.config.automaticSilentRenew || !this.state.expiresAt) {
      return;
    }

    // 在过期前5分钟刷新令牌
    const renewTime = this.state.expiresAt - Date.now() - 300000; // 5 minutes
    
    if (renewTime > 0) {
      this.silentRenewTimer = setTimeout(async () => {
        if (this.state.isAuthenticated) {
          await this.silentRenew();
        }
      }, renewTime);
    }
  }

  /**
   * 注销
   */
  async logout(): Promise<void> {
    try {
      if (this.silentRenewTimer) {
        clearTimeout(this.silentRenewTimer);
      }

      // 构建注销 URL
      const logoutUrl = new URL(`${this.config.authority}/connect/logout`);
      logoutUrl.searchParams.set('post_logout_redirect_uri', this.config.post_logout_redirect_uri);
      
      // 如果有 ID Token，添加到注销请求中
      const idToken = this.getIdToken();
      if (idToken) {
        logoutUrl.searchParams.set('id_token_hint', idToken);
      }

      // 清理本地状态
      this.clearTokensFromStorage();
      this.setState({
        isAuthenticated: false,
        user: null,
        accessToken: null,
        refreshToken: null,
        expiresAt: null,
        isLoading: false,
        error: null
      });

      // 重定向到注销端点
      window.location.href = logoutUrl.toString();
    } catch (error) {
      console.error('注销失败:', error);
      // 即使出错也要清理本地状态
      this.clearTokensFromStorage();
      this.setState({
        isAuthenticated: false,
        user: null,
        accessToken: null,
        refreshToken: null,
        expiresAt: null,
        isLoading: false,
        error: null
      });
    }
  }

  /**
   * 外部登录提供商
   */
  async getExternalProviders(): Promise<ExternalProviderInfo[]> {
    try {
      const response = await fetch('/Account/ExternalProviders', {
        headers: {
          'Accept': 'application/json'
        }
      });
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      
      const result = await response.json();
      // 处理API结果格式，假设返回的是包装格式
      return Array.isArray(result) ? result : (result.data || []);
    } catch (error) {
      console.error('获取外部登录提供商失败:', error);
      return [];
    }
  }

  /**
   * 启动外部登录
   */
  startExternalLogin(provider: string, returnUrl?: string): void {
    const url = new URL(`/Account/ExternalLogin`, window.location.origin);
    url.searchParams.set('provider', provider);
    
    if (returnUrl) {
      url.searchParams.set('returnUrl', returnUrl);
    } else {
      // 默认返回到 OAuth2 授权端点以继续正常流程
      url.searchParams.set('returnUrl', `/connect/authorize?${window.location.search}`);
    }
    
    window.location.href = url.toString();
  }

  // ====== 工具方法 ======

  /**
   * 检查令牌是否过期
   */
  isTokenExpired(): boolean {
    if (!this.state.expiresAt) return true;
    return Date.now() >= this.state.expiresAt - 60000; // 1分钟缓冲
  }

  /**
   * 确保有效的令牌
   */
  async ensureValidToken(): Promise<boolean> {
    if (!this.state.isAuthenticated) {
      return false;
    }

    if (!this.isTokenExpired()) {
      return true;
    }

    return await this.silentRenew();
  }

  /**
   * 获取访问令牌
   */
  getAccessToken(): string | null {
    return this.state.accessToken;
  }

  /**
   * 获取ID令牌
   */
  private getIdToken(): string | null {
    return localStorage.getItem('id_token');
  }

  /**
   * 获取用户信息
   */
  getUser(): User | null {
    return this.state.user;
  }

  /**
   * 检查是否已认证
   */
  isAuthenticated(): boolean {
    return this.state.isAuthenticated && !this.isTokenExpired();
  }

  /**
   * 获取认证状态
   */
  getState(): AuthState {
    return { ...this.state };
  }

  // ====== 状态管理 ======

  /**
   * 更新状态
   */
  private setState(updates: Partial<AuthState>): void {
    this.state = { ...this.state, ...updates };
    this.notifyListeners();
  }

  /**
   * 监听状态变化
   */
  onStateChange(callback: (state: AuthState) => void): () => void {
    this.listeners.push(callback);
    // 立即调用回调以提供当前状态
    callback(this.state);
    
    return () => {
      const index = this.listeners.indexOf(callback);
      if (index > -1) {
        this.listeners.splice(index, 1);
      }
    };
  }

  /**
   * 通知监听器
   */
  private notifyListeners(): void {
    this.listeners.forEach(listener => {
      try {
        listener(this.state);
      } catch (error) {
        console.error('状态监听器回调失败:', error);
      }
    });
  }

  // ====== 存储管理 ======

  /**
   * 保存令牌到存储
   */
  private saveTokensToStorage(): void {
    try {
      if (this.state.accessToken) {
        localStorage.setItem('access_token', this.state.accessToken);
      }
      if (this.state.refreshToken) {
        localStorage.setItem('refresh_token', this.state.refreshToken);
      }
      if (this.state.expiresAt) {
        localStorage.setItem('expires_at', this.state.expiresAt.toString());
      }
      if (this.state.user) {
        localStorage.setItem('user_info', JSON.stringify(this.state.user));
      }
    } catch (error) {
      console.error('保存令牌到存储失败:', error);
    }
  }

  /**
   * 从存储加载令牌
   */
  private async loadTokensFromStorage(): Promise<void> {
    try {
      const accessToken = localStorage.getItem('access_token');
      const refreshToken = localStorage.getItem('refresh_token');
      const expiresAt = localStorage.getItem('expires_at');
      const userInfo = localStorage.getItem('user_info');

      if (accessToken && expiresAt) {
        const expirationTime = parseInt(expiresAt, 10);
        
        this.state.accessToken = accessToken;
        this.state.refreshToken = refreshToken;
        this.state.expiresAt = expirationTime;
        
        if (userInfo) {
          this.state.user = JSON.parse(userInfo);
        }

        // 检查令牌是否过期
        if (!this.isTokenExpired()) {
          this.setState({ isAuthenticated: true });
          
          // 如果没有用户信息，尝试获取
          if (!this.state.user && this.config.loadUserInfo) {
            try {
              const user = await this.fetchUserInfo();
              this.setState({ user });
            } catch (error) {
              console.warn('获取用户信息失败:', error);
            }
          }
        } else {
          // 尝试刷新令牌
          if (refreshToken) {
            await this.silentRenew();
          } else {
            this.clearTokensFromStorage();
          }
        }
      }
    } catch (error) {
      console.error('从存储加载令牌失败:', error);
      this.clearTokensFromStorage();
    }
  }

  /**
   * 清理存储的令牌
   */
  private clearTokensFromStorage(): void {
    try {
      localStorage.removeItem('access_token');
      localStorage.removeItem('refresh_token');
      localStorage.removeItem('expires_at');
      localStorage.removeItem('id_token');
      localStorage.removeItem('user_info');
    } catch (error) {
      console.error('清理存储失败:', error);
    }
  }
}

/**
 * 默认认证服务实例
 */
export const authService = new OIDCAuthService();

/**
 * 认证服务工厂
 */
export function createAuthService(config?: Partial<OIDCConfig>): OIDCAuthService {
  return new OIDCAuthService(config);
}