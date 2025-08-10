import React, { useEffect, useState } from 'react';
import { useNavigate, useLocation, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Github, GitBranch, Eye, EyeOff, Mail, Lock } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';

interface LoginFormData {
  email: string;
  password: string;
  rememberMe: boolean;
}

export const Login: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const { login, loginWithProvider, isAuthenticated, isLoading } = useAuthStore();
  const [error, setError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [formData, setFormData] = useState<LoginFormData>({
    email: '',
    password: '',
    rememberMe: false
  });

  const from = location.state?.from?.pathname || '/dashboard';
  const tenantId = searchParams.get('tenant');

  useEffect(() => {
    if (isAuthenticated) {
      navigate(from, { replace: true });
    }
  }, [isAuthenticated, navigate, from]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
    // 清除错误消息当用户开始输入时
    if (error) {
      setError(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      setError(null);
      await login();
    } catch (err: any) {
      setError(err?.message || '登录失败，请稍后重试');
      console.error('Login failed:', err);
    }
  };

  const handleProviderLogin = async (provider: 'github' | 'gitee') => {
    try {
      setError(null);
      await loginWithProvider(provider);
    } catch (err: any) {
      setError(`${provider === 'github' ? 'GitHub' : 'Gitee'} 登录失败，请重试`);
      console.error(`${provider} login failed:`, err);
    }
  };

  // 检查配置的第三方登录提供商
  const [availableProviders, setAvailableProviders] = useState<string[]>([]);

  useEffect(() => {
    // 这里应该从后端API获取可用的登录提供商
    // 暂时硬编码，实际应该调用 /api/auth/providers 接口
    const fetchProviders = async () => {
      try {
        // const response = await fetch('/api/auth/providers');
        // const providers = await response.json();
        // setAvailableProviders(providers);
        
        // 模拟配置的提供商
        setAvailableProviders(['github', 'gitee']);
      } catch (err) {
        console.error('Failed to fetch providers:', err);
      }
    };

    fetchProviders();
  }, []);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        {/* Header */}
        <div className="text-center">
          <h2 className="mt-6 text-3xl font-extrabold text-gray-900">
            登录到 Making
          </h2>
          <p className="mt-2 text-sm text-gray-600">
            使用您的账户登录系统
          </p>
          {tenantId && (
            <p className="mt-2 text-sm text-blue-600">
              租户: {tenantId}
            </p>
          )}
        </div>

        <Card>
          <CardHeader>
            <CardTitle>登录您的账户</CardTitle>
            <CardDescription>
              输入您的邮箱和密码来登录
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Tabs defaultValue="email" className="w-full">
              <TabsList className="grid w-full grid-cols-2">
                <TabsTrigger value="email">邮箱登录</TabsTrigger>
                <TabsTrigger value="social">第三方登录</TabsTrigger>
              </TabsList>
              
              <TabsContent value="email" className="space-y-4">
                {/* 错误提示 */}
                {error && (
                  <Alert variant="destructive">
                    <AlertDescription>{error}</AlertDescription>
                  </Alert>
                )}

                <form onSubmit={handleSubmit} className="space-y-4">
                  {/* 邮箱输入 */}
                  <div className="space-y-2">
                    <Label htmlFor="email">邮箱地址</Label>
                    <div className="relative">
                      <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                      <Input
                        id="email"
                        name="email"
                        type="email"
                        autoComplete="email"
                        required
                        placeholder="请输入邮箱地址"
                        value={formData.email}
                        onChange={handleInputChange}
                        className="pl-10"
                      />
                    </div>
                  </div>

                  {/* 密码输入 */}
                  <div className="space-y-2">
                    <Label htmlFor="password">密码</Label>
                    <div className="relative">
                      <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                      <Input
                        id="password"
                        name="password"
                        type={showPassword ? "text" : "password"}
                        autoComplete="current-password"
                        required
                        placeholder="请输入密码"
                        value={formData.password}
                        onChange={handleInputChange}
                        className="pl-10 pr-10"
                      />
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                        onClick={() => setShowPassword(!showPassword)}
                      >
                        {showPassword ? (
                          <EyeOff className="h-4 w-4 text-gray-400" />
                        ) : (
                          <Eye className="h-4 w-4 text-gray-400" />
                        )}
                      </Button>
                    </div>
                  </div>

                  {/* 记住我和忘记密码 */}
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-2">
                      <input
                        id="rememberMe"
                        name="rememberMe"
                        type="checkbox"
                        checked={formData.rememberMe}
                        onChange={handleInputChange}
                        className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                      />
                      <Label htmlFor="rememberMe" className="text-sm text-gray-600">
                        记住我
                      </Label>
                    </div>
                    <Button variant="link" className="px-0 text-sm">
                      忘记密码？
                    </Button>
                  </div>

                  {/* 登录按钮 */}
                  <Button
                    type="submit"
                    disabled={isLoading}
                    className="w-full"
                    size="lg"
                  >
                    {isLoading ? '正在登录...' : '登录'}
                  </Button>
                </form>
              </TabsContent>

              <TabsContent value="social" className="space-y-4">
                {/* 错误提示 */}
                {error && (
                  <Alert variant="destructive">
                    <AlertDescription>{error}</AlertDescription>
                  </Alert>
                )}

                {/* GitHub 登录 */}
                {availableProviders.includes('github') && (
                  <Button
                    variant="outline"
                    onClick={() => handleProviderLogin('github')}
                    disabled={isLoading}
                    className="w-full"
                    size="lg"
                  >
                    <Github className="mr-2 h-4 w-4" />
                    使用 GitHub 登录
                  </Button>
                )}

                {/* Gitee 登录 */}
                {availableProviders.includes('gitee') && (
                  <Button
                    variant="outline"
                    onClick={() => handleProviderLogin('gitee')}
                    disabled={isLoading}
                    className="w-full"
                    size="lg"
                  >
                    <GitBranch className="mr-2 h-4 w-4" />
                    使用 Gitee 登录
                  </Button>
                )}

                {/* 如果没有配置任何第三方登录 */}
                {availableProviders.length === 0 && (
                  <div className="text-center py-8">
                    <p className="text-sm text-gray-500">
                      管理员未配置第三方登录提供商
                    </p>
                  </div>
                )}
              </TabsContent>
            </Tabs>
          </CardContent>
        </Card>

        {/* 底部信息 */}
        <div className="text-center">
          <p className="mt-2 text-sm text-gray-600">
            还没有账户？{' '}
            <Button variant="link" className="px-1 font-medium text-blue-600 hover:text-blue-500">
              注册账户
            </Button>
          </p>
          <p className="mt-1 text-sm text-gray-600">
            需要帮助？{' '}
            <Button variant="link" className="px-1 font-medium text-blue-600 hover:text-blue-500">
              联系管理员
            </Button>
          </p>
        </div>
      </div>
    </div>
  );
};