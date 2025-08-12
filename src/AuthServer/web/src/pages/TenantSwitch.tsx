import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Database, ArrowRight, Search } from 'lucide-react';

interface Tenant {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export const TenantSwitch: React.FC = () => {
  const navigate = useNavigate();
  const { profile, switchTenant, isLoading } = useAuthStore();
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [error, setError] = useState<string | null>(null);

  // 获取可用租户列表
  useEffect(() => {
    const fetchTenants = async () => {
      try {
        // 这里应该从后端API获取用户可访问的租户列表
        // const response = await fetch('/api/tenants/available');
        // const data = await response.json();
        // setTenants(data);

        // 模拟数据
        const mockTenants: Tenant[] = [
          {
            id: 'tenant1',
            name: '默认租户',
            description: '系统默认租户',
            isActive: profile?.tenantId === 'tenant1',
          },
          {
            id: 'tenant2',
            name: '开发租户',
            description: '开发环境租户',
            isActive: profile?.tenantId === 'tenant2',
          },
          {
            id: 'tenant3',
            name: '生产租户',
            description: '生产环境租户',
            isActive: profile?.tenantId === 'tenant3',
          },
        ];
        setTenants(mockTenants);
      } catch (err) {
        setError('获取租户列表失败');
        console.error('Failed to fetch tenants:', err);
      }
    };

    fetchTenants();
  }, [profile?.tenantId]);

  const handleTenantSwitch = async (tenantId: string) => {
    try {
      setError(null);
      await switchTenant(tenantId);
      // switchTenant 会重定向到登录页面
    } catch (err) {
      setError('切换租户失败，请重试');
      console.error('Tenant switch failed:', err);
    }
  };

  const filteredTenants = tenants.filter(tenant =>
    tenant.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    tenant.description?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* 标题 */}
      <div className="border-b border-gray-200 pb-4">
        <h1 className="text-2xl font-bold text-gray-900">租户切换</h1>
        <p className="text-gray-600">
          选择您要访问的租户
        </p>
        {profile?.tenantName && (
          <p className="text-sm text-blue-600 mt-2">
            当前租户: {profile.tenantName}
          </p>
        )}
      </div>

      {/* 搜索框 */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Search className="h-5 w-5" />
            搜索租户
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Input
            placeholder="输入租户名称或描述进行搜索..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="max-w-md"
          />
        </CardContent>
      </Card>

      {/* 错误提示 */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md">
          {error}
        </div>
      )}

      {/* 租户列表 */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {filteredTenants.map((tenant) => (
          <Card
            key={tenant.id}
            className={`cursor-pointer transition-all hover:shadow-md ${
              tenant.isActive
                ? 'ring-2 ring-blue-500 bg-blue-50'
                : 'hover:shadow-lg'
            }`}
          >
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Database className="h-5 w-5" />
                  {tenant.name}
                </div>
                {tenant.isActive && (
                  <span className="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full">
                    当前
                  </span>
                )}
              </CardTitle>
              {tenant.description && (
                <CardDescription>{tenant.description}</CardDescription>
              )}
            </CardHeader>
            <CardContent>
              <div className="flex items-center justify-between">
                <div className="text-sm text-gray-500">
                  租户ID: {tenant.id}
                </div>
                {!tenant.isActive && (
                  <Button
                    onClick={() => handleTenantSwitch(tenant.id)}
                    disabled={isLoading}
                    size="sm"
                  >
                    <ArrowRight className="h-4 w-4 mr-1" />
                    {isLoading ? '切换中...' : '切换'}
                  </Button>
                )}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* 无结果提示 */}
      {filteredTenants.length === 0 && (
        <Card>
          <CardContent className="text-center py-8">
            <Database className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">
              没有找到匹配的租户
            </h3>
            <p className="text-gray-500">
              请尝试其他搜索条件或联系管理员
            </p>
          </CardContent>
        </Card>
      )}

      {/* 返回按钮 */}
      <div className="flex justify-center">
        <Button
          variant="outline"
          onClick={() => navigate('/dashboard')}
        >
          返回仪表板
        </Button>
      </div>
    </div>
  );
};