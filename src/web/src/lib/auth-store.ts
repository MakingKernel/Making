import { authService, AuthState, User } from './oidc-auth';

/**
 * 认证错误类型
 */
export enum AuthErrorType {
  NETWORK_ERROR = 'NETWORK_ERROR',
  INVALID_CREDENTIALS = 'INVALID_CREDENTIALS',
  TOKEN_EXPIRED = 'TOKEN_EXPIRED',
  REFRESH_FAILED = 'REFRESH_FAILED',
  AUTHORIZATION_FAILED = 'AUTHORIZATION_FAILED',
  USER_INFO_FAILED = 'USER_INFO_FAILED',
  LOGOUT_FAILED = 'LOGOUT_FAILED',
  UNKNOWN_ERROR = 'UNKNOWN_ERROR'
}

/**
 * 认证错误接口
 */
export interface AuthError {
  type: AuthErrorType;
  message: string;
  details?: any;
  timestamp: number;
}

/**
 * 扩展的认证状态
 */
export interface ExtendedAuthState extends AuthState {
  // 错误历史
  errorHistory: AuthError[];
  // 最后一次活动时间
  lastActivity: number;
  // 登录尝试次数
  loginAttempts: number;
  // 会话超时警告
  sessionWarning: boolean;
}

/**
 * 认证状态管理器
 * 提供统一的状态管理和错误处理
 */
export class AuthStateManager {
  private state: ExtendedAuthState = {
    isAuthenticated: false,
    user: null,
    accessToken: null,
    refreshToken: null,
    expiresAt: null,
    isLoading: false,
    error: null,
    errorHistory: [],
    lastActivity: Date.now(),
    loginAttempts: 0,
    sessionWarning: false
  };

  private listeners: Array<(state: ExtendedAuthState) => void> = [];
  private activityTimer?: NodeJS.Timeout;
  private warningTimer?: NodeJS.Timeout;
  
  // 配置常量
  private readonly SESSION_WARNING_TIME = 5 * 60 * 1000; // 5分钟警告
  private readonly MAX_LOGIN_ATTEMPTS = 5;
  private readonly ERROR_HISTORY_LIMIT = 10;

  constructor() {
    this.initialize();
  }

  /**
   * 初始化状态管理器
   */
  private initialize(): void {
    // 监听认证服务状态变化
    authService.onStateChange((authState) => {
      this.updateFromAuthService(authState);
    });

    // 设置活动监听
    this.setupActivityTracking();
    
    // 设置会话警告
    this.setupSessionWarning();
  }

  /**
   * 从认证服务更新状态
   */
  private updateFromAuthService(authState: AuthState): void {
    const previousAuth = this.state.isAuthenticated;
    
    this.state = {
      ...this.state,
      ...authState,
      lastActivity: authState.isAuthenticated ? Date.now() : this.state.lastActivity
    };

    // 检测登录状态变化
    if (!previousAuth && authState.isAuthenticated) {
      // 刚刚登录成功
      this.state.loginAttempts = 0;
      this.state.sessionWarning = false;
      this.addError(null); // 清除错误
    } else if (previousAuth && !authState.isAuthenticated) {
      // 刚刚注销
      this.clearSessionTimers();
    }

    // 处理错误
    if (authState.error) {
      this.handleAuthError(authState.error);
    }

    this.notifyListeners();
  }

  /**
   * 处理认证错误
   */
  private handleAuthError(error: string): void {
    let errorType = AuthErrorType.UNKNOWN_ERROR;
    
    // 根据错误消息确定错误类型
    if (error.includes('网络') || error.includes('Network') || error.includes('fetch')) {
      errorType = AuthErrorType.NETWORK_ERROR;
    } else if (error.includes('邮箱') || error.includes('密码') || error.includes('credentials')) {
      errorType = AuthErrorType.INVALID_CREDENTIALS;
      this.state.loginAttempts += 1;
    } else if (error.includes('过期') || error.includes('expired')) {
      errorType = AuthErrorType.TOKEN_EXPIRED;
    } else if (error.includes('刷新') || error.includes('refresh')) {
      errorType = AuthErrorType.REFRESH_FAILED;
    } else if (error.includes('授权') || error.includes('authorization')) {
      errorType = AuthErrorType.AUTHORIZATION_FAILED;
    } else if (error.includes('用户信息') || error.includes('userinfo')) {
      errorType = AuthErrorType.USER_INFO_FAILED;
    } else if (error.includes('注销') || error.includes('logout')) {
      errorType = AuthErrorType.LOGOUT_FAILED;
    }

    this.addError({
      type: errorType,
      message: error,
      timestamp: Date.now()
    });
  }

  /**
   * 添加错误到历史记录
   */
  private addError(error: AuthError | null): void {
    if (error) {
      this.state.errorHistory.unshift(error);
      
      // 限制错误历史长度
      if (this.state.errorHistory.length > this.ERROR_HISTORY_LIMIT) {
        this.state.errorHistory = this.state.errorHistory.slice(0, this.ERROR_HISTORY_LIMIT);
      }
    } else {
      // 清除当前错误但保留历史
      this.state.error = null;
    }
  }

  /**
   * 设置活动跟踪
   */
  private setupActivityTracking(): void {
    const events = ['mousedown', 'mousemove', 'keypress', 'scroll', 'touchstart'];
    
    const updateActivity = () => {
      if (this.state.isAuthenticated) {
        this.state.lastActivity = Date.now();
        this.state.sessionWarning = false;
        this.setupSessionWarning(); // 重置警告定时器
      }
    };

    events.forEach(event => {
      document.addEventListener(event, updateActivity, { passive: true });
    });
  }

  /**
   * 设置会话警告
   */
  private setupSessionWarning(): void {
    if (this.warningTimer) {
      clearTimeout(this.warningTimer);
    }

    if (!this.state.isAuthenticated || !this.state.expiresAt) {
      return;
    }

    const timeUntilExpiry = this.state.expiresAt - Date.now();
    const warningTime = Math.max(0, timeUntilExpiry - this.SESSION_WARNING_TIME);

    if (warningTime > 0) {
      this.warningTimer = setTimeout(() => {
        if (this.state.isAuthenticated) {
          this.state.sessionWarning = true;
          this.notifyListeners();
        }
      }, warningTime);
    }
  }

  /**
   * 清理会话定时器
   */
  private clearSessionTimers(): void {
    if (this.activityTimer) {
      clearTimeout(this.activityTimer);
    }
    if (this.warningTimer) {
      clearTimeout(this.warningTimer);
    }
  }

  // ====== 公共方法 ======

  /**
   * 获取当前状态
   */
  getState(): ExtendedAuthState {
    return { ...this.state };
  }

  /**
   * 检查是否已认证
   */
  isAuthenticated(): boolean {
    return this.state.isAuthenticated;
  }

  /**
   * 获取当前用户
   */
  getUser(): User | null {
    return this.state.user;
  }

  /**
   * 获取访问令牌
   */
  getAccessToken(): string | null {
    return this.state.accessToken;
  }

  /**
   * 检查是否被锁定（登录尝试过多）
   */
  isLocked(): boolean {
    return this.state.loginAttempts >= this.MAX_LOGIN_ATTEMPTS;
  }

  /**
   * 重置登录尝试次数
   */
  resetLoginAttempts(): void {
    this.state.loginAttempts = 0;
    this.notifyListeners();
  }

  /**
   * 获取最近的错误
   */
  getRecentErrors(limit: number = 5): AuthError[] {
    return this.state.errorHistory.slice(0, limit);
  }

  /**
   * 清除错误历史
   */
  clearErrorHistory(): void {
    this.state.errorHistory = [];
    this.state.error = null;
    this.notifyListeners();
  }

  /**
   * 检查会话是否即将过期
   */
  isSessionExpiringSoon(): boolean {
    if (!this.state.expiresAt) return false;
    return Date.now() > this.state.expiresAt - this.SESSION_WARNING_TIME;
  }

  /**
   * 获取会话剩余时间（毫秒）
   */
  getSessionTimeRemaining(): number {
    if (!this.state.expiresAt) return 0;
    return Math.max(0, this.state.expiresAt - Date.now());
  }

  /**
   * 扩展会话（通过静默刷新）
   */
  async extendSession(): Promise<boolean> {
    try {
      const success = await authService.silentRenew();
      if (success) {
        this.state.sessionWarning = false;
        this.notifyListeners();
      }
      return success;
    } catch (error) {
      console.error('扩展会话失败:', error);
      return false;
    }
  }

  // ====== 代理方法 ======

  /**
   * 登录
   */
  async login(): Promise<void> {
    if (this.isLocked()) {
      throw new Error('登录尝试次数过多，请稍后再试');
    }
    
    return authService.login();
  }

  /**
   * 注销
   */
  async logout(): Promise<void> {
    this.clearSessionTimers();
    return authService.logout();
  }

  /**
   * 处理回调
   */
  async handleCallback(): Promise<void> {
    return authService.handleCallback();
  }

  /**
   * 获取外部提供商
   */
  async getExternalProviders() {
    return authService.getExternalProviders();
  }

  /**
   * 启动外部登录
   */
  startExternalLogin(provider: string, returnUrl?: string): void {
    return authService.startExternalLogin(provider, returnUrl);
  }

  // ====== 状态订阅 ======

  /**
   * 订阅状态变化
   */
  subscribe(callback: (state: ExtendedAuthState) => void): () => void {
    this.listeners.push(callback);
    
    // 立即调用回调提供当前状态
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

  // ====== 清理 ======

  /**
   * 清理资源
   */
  dispose(): void {
    this.clearSessionTimers();
    this.listeners.length = 0;
  }
}

/**
 * 默认的认证状态管理器实例
 */
export const authStore = new AuthStateManager();

/**
 * React Hook 风格的状态订阅（如果在 React 环境中）
 */
export function useAuthState(): ExtendedAuthState {
  if (typeof window !== 'undefined' && (window as any).React) {
    const [state, setState] = (window as any).React.useState(authStore.getState());
    
    (window as any).React.useEffect(() => {
      return authStore.subscribe(setState);
    }, []);
    
    return state;
  }
  
  // 非 React 环境下返回当前状态
  return authStore.getState();
}

/**
 * 创建自定义状态管理器
 */
export function createAuthStateManager(): AuthStateManager {
  return new AuthStateManager();
}