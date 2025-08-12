import React from 'react';
import { useAuthStore } from '@/stores/auth';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { User, Settings, Shield, Database } from 'lucide-react';

export const Dashboard: React.FC = () => {
  const { profile } = useAuthStore();

  return (
    <div className="space-y-6">
      {/* 欢迎标题 */}
      <div className="border-b border-gray-200 pb-4">
        <h1 className="text-2xl font-bold text-gray-900">
          欢迎回来，{profile?.firstName || profile?.username}！
        </h1>
        <p className="text-gray-600">
          这是您的个人仪表板
        </p>
      </div>

      {/* 用户信息卡片 */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">用户信息</CardTitle>
            <User className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="text-sm">
                <span className="font-medium">用户名:</span> {profile?.username}
              </div>
              <div className="text-sm">
                <span className="font-medium">邮箱:</span> {profile?.email}
              </div>
              {profile?.firstName && (
                <div className="text-sm">
                  <span className="font-medium">姓名:</span> {profile.firstName} {profile.lastName}
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">角色权限</CardTitle>
            <Shield className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {profile?.roles && profile.roles.length > 0 ? (
                profile.roles.map((role, index) => (
                  <div key={index} className="text-sm bg-blue-100 text-blue-800 px-2 py-1 rounded">
                    {role}
                  </div>
                ))
              ) : (
                <div className="text-sm text-gray-500">无特定角色</div>
              )}
            </div>
          </CardContent>
        </Card>

        {profile?.tenantId && (
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">租户信息</CardTitle>
              <Database className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <div className="text-sm">
                  <span className="font-medium">租户ID:</span> {profile.tenantId}
                </div>
                {profile.tenantName && (
                  <div className="text-sm">
                    <span className="font-medium">租户名称:</span> {profile.tenantName}
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      {/* 快速操作 */}
      <Card>
        <CardHeader>
          <CardTitle>快速操作</CardTitle>
          <CardDescription>
            常用功能快速访问
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-4">
            <Button variant="outline">
              <Settings className="mr-2 h-4 w-4" />
              系统设置
            </Button>
            <Button variant="outline">
              <User className="mr-2 h-4 w-4" />
              用户管理
            </Button>
            {profile?.tenantId && (
              <Button variant="outline">
                <Database className="mr-2 h-4 w-4" />
                租户管理
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      {/* 系统状态 */}
      <Card>
        <CardHeader>
          <CardTitle>系统状态</CardTitle>
          <CardDescription>
            当前系统运行状态
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-green-600">正常</div>
              <div className="text-sm text-gray-500">认证服务</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-green-600">在线</div>
              <div className="text-sm text-gray-500">数据库</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-blue-600">
                {profile?.tenantId ? '多租户' : '单租户'}
              </div>
              <div className="text-sm text-gray-500">运行模式</div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};