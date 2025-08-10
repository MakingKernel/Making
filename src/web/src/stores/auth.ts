import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { User } from 'oidc-client-ts';
import { authService } from '@/lib/auth';

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
  user: User | null;
  profile: UserProfile | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  
  // Actions
  setUser: (user: User | null) => void;
  setProfile: (profile: UserProfile | null) => void;
  setLoading: (loading: boolean) => void;
  login: () => Promise<void>;
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
          isAuthenticated: user != null && !user.expired,
          profile: user ? parseUserProfile(user) : null 
        });
      },

      setProfile: (profile) => {
        set({ profile });
      },

      setLoading: (isLoading) => {
        set({ isLoading });
      },

      login: async () => {
        try {
          set({ isLoading: true });
          await authService.login();
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
          await authService.loginWithProvider(provider);
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
          const user = await authService.getUser();
          
          if (user && !user.expired) {
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
            await authService.signout();
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

function parseUserProfile(user: User): UserProfile {
  const profile = user.profile as Record<string, unknown>;
  
  return {
    id: (profile.sub as string) || '',
    username: (profile.preferred_username as string) || (profile.name as string) || '',
    email: (profile.email as string) || '',
    firstName: (profile.given_name as string) || '',
    lastName: (profile.family_name as string) || '',
    roles: (profile as any).role ? (Array.isArray((profile as any).role) ? (profile as any).role : [ (profile as any).role ]) : [],
    tenantId: (profile.tenant_id as string) || '',
    tenantName: (profile.tenant_name as string) || '',
  };
}