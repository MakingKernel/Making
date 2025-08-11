import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { authService, UserInfo, LoginRequest } from '@/lib/auth';

export interface UserProfile {
  id: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roles: string[];
  tenantId?: string;
  tenantName?: string;
}

interface AuthState {
  user: UserInfo | null;
  profile: UserProfile | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  
  // Actions
  setUser: (user: UserInfo | null) => void;
  setProfile: (profile: UserProfile | null) => void;
  setLoading: (loading: boolean) => void;
  login: (credentials?: LoginRequest) => Promise<void>;
  loginWithProvider: (provider: 'github' | 'gitee') => Promise<void>;
  logout: () => Promise<void>;
  checkAuth: () => Promise<void>;
  switchTenant: (tenantId: string) => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      profile: null,
      isLoading: false,
      isAuthenticated: false,

      setUser: (user) => {
        set({ 
          user, 
          isAuthenticated: user != null,
          profile: user ? parseUserProfile(user) : null 
        });
      },

      setProfile: (profile) => {
        set({ profile });
      },

      setLoading: (isLoading) => {
        set({ isLoading });
      },

      login: async (credentials) => {
        try {
          set({ isLoading: true });
          await authService.login(credentials);
          
          // After successful login, get user info and update state
          const user = authService.getUser();
          if (user) {
            set({ 
              user, 
              isAuthenticated: true,
              profile: parseUserProfile(user) 
            });
          }
        } catch (error) {
          console.error('Login failed:', error);
          throw error;
        } finally {
          set({ isLoading: false });
        }
      },

      loginWithProvider: async (provider) => {
        try {
          set({ isLoading: true });
          // 使用新的外部登录方法
          authService.startExternalLogin(provider, window.location.origin + '/callback');
        } catch (error) {
          console.error(`Login with ${provider} failed:`, error);
          throw error;
        } finally {
          set({ isLoading: false });
        }
      },

      logout: async () => {
        try {
          set({ isLoading: true });
          await authService.logout();
          set({ user: null, profile: null, isAuthenticated: false });
        } catch (error) {
          console.error('Logout failed:', error);
        } finally {
          set({ isLoading: false });
        }
      },

      checkAuth: async () => {
        try {
          set({ isLoading: true });
          const isAuthenticated = authService.isAuthenticated();
          const user = authService.getUser();
          
          if (isAuthenticated && user) {
            set({ 
              user, 
              isAuthenticated: true,
              profile: parseUserProfile(user) 
            });
          } else {
            set({ 
              user: null, 
              isAuthenticated: false,
              profile: null 
            });
          }
        } catch (error) {
          console.error('Auth check failed:', error);
          set({ 
            user: null, 
            isAuthenticated: false,
            profile: null 
          });
        } finally {
          set({ isLoading: false });
        }
      },

      switchTenant: async (tenantId: string) => {
        try {
          set({ isLoading: true });
          // 租户切换逻辑 - 重新登录以获取新的租户令牌
          const currentProfile = get().profile;
          if (currentProfile) {
            // 首先登出，然后使用新的租户ID重新登录
            await authService.logout();
            // 这里可以添加租户切换的特定逻辑
            window.location.href = `/login?tenant=${tenantId}`;
          }
        } catch (error) {
          console.error('Tenant switch failed:', error);
          throw error;
        } finally {
          set({ isLoading: false });
        }
      },
    }),
    {
      name: 'auth-storage',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ 
        profile: state.profile,
        isAuthenticated: state.isAuthenticated 
      }),
    }
  )
);

function parseUserProfile(user: UserInfo): UserProfile {
  return {
    id: user.sub || '',
    username: user.name || user.email || '',
    email: user.email || '',
    firstName: user.given_name || '',
    lastName: user.family_name || '',
    roles: (user as any).role ? (Array.isArray((user as any).role) ? (user as any).role : [(user as any).role]) : [],
    tenantId: (user as any).tenant_id || '',
    tenantName: (user as any).tenant_name || '',
  };
}