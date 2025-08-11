/**
 * API配置文件
 * 集中管理所有API端点和配置
 */

// 环境配置
export const ENV = {
  // 是否为开发环境
  isDev: import.meta.env.DEV,
  // 前端应用地址
  clientUrl: window.location.origin,
  // AuthServer地址
  authServerUrl: 'http://localhost:5274',
} as const;

// API端点配置
export const API_ENDPOINTS = {
  // AuthServer端点
  AUTH: {
    // OAuth2端点 - 纯OAuth2流程
    AUTHORIZE: '/connect/authorize',
    TOKEN: '/connect/token',
    USERINFO: '/connect/userinfo',
    REVOKE: '/connect/revoke',
    LOGOUT: '/connect/logout',
    AUTHENTICATE: '/connect/authenticate', // OAuth2专用认证端点
    
    // 外部登录端点 (用于社交登录扩展)
    EXTERNAL_PROVIDERS: '/Account/ExternalProviders',
    EXTERNAL_LOGIN: '/Account/ExternalLogin',
  },
  
  // 其他API端点可以在这里添加
  // API: {
  //   USERS: '/api/users',
  //   ORDERS: '/api/orders',
  // }
} as const;

// OIDC客户端配置
export const OIDC_CONFIG = {
  authority: ENV.authServerUrl,
  client_id: 'web-client',
  redirect_uri: `${ENV.clientUrl}/callback`,
  post_logout_redirect_uri: `${ENV.clientUrl}/`,
  silent_redirect_uri: `${ENV.clientUrl}/auth/silent-callback`,
  response_type: 'code',
  scope: 'openid profile email',
  automaticSilentRenew: true,
  filterProtocolClaims: true,
  loadUserInfo: true,
} as const;

// HTTP客户端配置
export const HTTP_CONFIG = {
  // AuthServer HTTP客户端配置
  authServer: {
    baseURL: ENV.authServerUrl,
    timeout: 10000,
    headers: {
      'Content-Type': 'application/json',
    },
  },
  
  // 主API HTTP客户端配置（如果有其他API服务）
  // mainApi: {
  //   baseURL: 'http://localhost:8080',
  //   timeout: 15000,
  //   headers: {
  //     'Content-Type': 'application/json',
  //   },
  // }
} as const;

// 导出便捷方法
export const getAuthServerUrl = (path: string = '') => 
  `${ENV.authServerUrl}${path}`;

export const getClientUrl = (path: string = '') => 
  `${ENV.clientUrl}${path}`;