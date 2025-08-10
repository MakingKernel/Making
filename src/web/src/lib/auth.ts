import { UserManager, User, WebStorageStateStore } from 'oidc-client-ts';

export interface AuthConfig {
  authority: string;
  client_id: string;
  redirect_uri: string;
  post_logout_redirect_uri: string;
  response_type: string;
  scope: string;
}

const defaultAuthConfig: AuthConfig = {
  authority: 'https://localhost:5001',
  client_id: 'web-client',
  redirect_uri: `${window.location.origin}/callback`,
  post_logout_redirect_uri: window.location.origin,
  response_type: 'code',
  scope: 'openid profile email roles',
};

export class AuthService {
  private userManager: UserManager;

  constructor(config: Partial<AuthConfig> = {}) {
    const mergedConfig = { ...defaultAuthConfig, ...config };
    
    this.userManager = new UserManager({
      ...mergedConfig,
      userStore: new WebStorageStateStore({ store: window.localStorage }),
      automaticSilentRenew: true,
      silent_redirect_uri: `${window.location.origin}/silent-renew.html`,
    });

    this.userManager.events.addAccessTokenExpiring(() => {
      console.log('Token expiring...');
    });

    this.userManager.events.addAccessTokenExpired(() => {
      console.log('Token expired');
      this.signout();
    });

    this.userManager.events.addSilentRenewError((e) => {
      console.log('Silent renew error', e.message);
    });

    this.userManager.events.addUserLoaded((user) => {
      console.log('New user loaded: ', user);
    });

    this.userManager.events.addUserUnloaded(() => {
      console.log('User unloaded');
    });
  }

  public getUser(): Promise<User | null> {
    return this.userManager.getUser();
  }

  public login(): Promise<void> {
    return this.userManager.signinRedirect();
  }

  public loginWithProvider(provider: 'github' | 'gitee'): Promise<void> {
    return this.userManager.signinRedirect({
      extraQueryParams: {
        acr_values: provider,
      },
    });
  }

  public renewToken(): Promise<User | null> {
    return this.userManager.signinSilent();
  }

  public logout(): Promise<void> {
    return this.userManager.signoutRedirect();
  }

  public signout(): Promise<void> {
    return this.userManager.removeUser();
  }

  public async completeAuthentication(): Promise<User> {
    const user = await this.userManager.signinRedirectCallback();
    return user;
  }

  public async isAuthenticated(): Promise<boolean> {
    const user = await this.getUser();
    return user != null && !user.expired;
  }

  public async getAccessToken(): Promise<string | null> {
    const user = await this.getUser();
    return user?.access_token ?? null;
  }
}

export const authService = new AuthService();