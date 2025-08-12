# Making Web 前端应用

这是一个基于 React + TypeScript + TailwindCSS + shadcn/ui 构建的现代化 Web 前端应用，支持完整的用户认证、第三方登录和多租户功能。

## 功能特性

- ✅ **完整认证系统**：支持 OAuth2 / OpenID Connect
- ✅ **第三方登录**：支持 GitHub、Gitee 等第三方登录
- ✅ **多租户支持**：支持租户切换功能
- ✅ **现代化 UI**：使用 shadcn/ui 组件库，支持深色/浅色主题
- ✅ **响应式设计**：完美适配桌面和移动设备
- ✅ **TypeScript**：完整的类型支持
- ✅ **状态管理**：使用 Zustand 进行状态管理
- ✅ **路由保护**：基于角色的权限控制

## 技术栈

- **React 19** - UI框架
- **TypeScript** - 类型安全
- **Vite** - 构建工具
- **TailwindCSS 4** - 样式框架
- **shadcn/ui** - UI组件库
- **React Router v7** - 路由管理
- **Zustand** - 状态管理
- **OIDC Client** - OpenID Connect 客户端
- **Lucide React** - 图标库

## 项目结构

```
src/
├── components/          # 通用组件
│   ├── auth/           # 认证相关组件
│   ├── layout/         # 布局组件
│   └── ui/             # shadcn/ui 组件
├── lib/                # 工具库
├── pages/              # 页面组件
├── stores/             # 状态管理
└── App.tsx             # 应用入口
```

## 快速开始

### 1. 安装依赖

```bash
npm install
```

### 2. 启动开发服务器

```bash
npm run dev
```

应用将在 http://localhost:5002 启动

### 3. 构建生产版本

```bash
npm run build
```

## 配置说明

### 认证配置

认证配置位于 `src/lib/auth.ts`：

```typescript
const defaultAuthConfig: AuthConfig = {
  authority: 'https://localhost:5001',        // OpenID Connect 提供者
  client_id: 'web-client',                    // 客户端ID
  redirect_uri: `${window.location.origin}/callback`,  // 回调地址
  post_logout_redirect_uri: window.location.origin,    // 登出回调地址
  response_type: 'code',                      // OAuth2 流程类型
  scope: 'openid profile email roles',       // 请求的权限范围
};
```

### 代理配置

开发环境下，Vite 会自动代理后端API请求到认证服务器：

```typescript
server: {
  port: 5002,
  proxy: {
    '/connect': {
      target: 'https://localhost:5001',
      changeOrigin: true,
      secure: false,
    },
    '/api': {
      target: 'https://localhost:5001',
      changeOrigin: true,
      secure: false,
    },
  },
},
```

## 主要组件说明

### 认证组件

- **`ProtectedRoute`**: 路由保护组件，支持角色权限检查
- **`AuthCallback`**: OAuth2 回调处理组件
- **`Login`**: 登录页面，支持多种登录方式

### 页面组件

- **`Dashboard`**: 用户仪表板，显示用户信息和系统状态
- **`TenantSwitch`**: 租户切换页面（支持多租户功能）

### 状态管理

使用 Zustand 管理认证状态：

```typescript
interface AuthState {
  user: User | null;              // OIDC 用户对象
  profile: UserProfile | null;    // 用户资料
  isLoading: boolean;             // 加载状态
  isAuthenticated: boolean;       // 认证状态
  
  // Actions
  login: () => Promise<void>;
  loginWithProvider: (provider: 'github' | 'gitee') => Promise<void>;
  logout: () => Promise<void>;
  checkAuth: () => Promise<void>;
  switchTenant: (tenantId: string) => Promise<void>;
}
```

## 支持的登录方式

1. **默认登录**: 使用系统内置的用户名密码登录
2. **GitHub**: 通过 GitHub OAuth 登录
3. **Gitee**: 通过 Gitee OAuth 登录

> **注意**: 第三方登录按钮会根据后端配置动态显示/隐藏

## 多租户功能

当用户属于多个租户时，可以通过以下方式切换租户：

1. 点击头部导航栏的租户显示区域
2. 或访问 `/tenant-switch` 页面
3. 选择目标租户并确认切换

## 开发指南

### 添加新页面

1. 在 `src/pages/` 下创建页面组件
2. 在 `src/App.tsx` 中添加路由配置
3. 如需权限控制，使用 `ProtectedRoute` 包装

### 添加新的UI组件

1. 使用 shadcn/ui CLI 添加组件:
   ```bash
   npx shadcn@latest add [component-name]
   ```
2. 组件会自动添加到 `src/components/ui/` 目录

### 状态管理

使用 Zustand 创建新的 store:

```typescript
interface MyState {
  data: any;
  setData: (data: any) => void;
}

const useMyStore = create<MyState>((set) => ({
  data: null,
  setData: (data) => set({ data }),
}));
```

## 环境要求

- Node.js >= 18
- npm >= 9

## 浏览器支持

- Chrome >= 90
- Firefox >= 88  
- Safari >= 14
- Edge >= 90

---

## 相关项目

- [认证服务器](../AuthServer/README.md)
- [Making 框架](../../framework/README.md)
