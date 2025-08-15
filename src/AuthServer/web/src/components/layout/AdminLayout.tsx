import { useState } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Users,
  Shield,
  Activity,
  Settings,
  Menu,
  X,
  User,
  LogOut,
  Home,
  Database,
  FileText,
} from 'lucide-react';

const AdminLayout = () => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuthStore();

  const menuItems = [
    {
      name: '仪表盘',
      href: '/admin/dashboard',
      icon: Home,
    },
    {
      name: '用户管理',
      href: '/admin/users',
      icon: Users,
    },
    {
      name: '角色管理',
      href: '/admin/roles',
      icon: Shield,
    },
    {
      name: '审计日志',
      href: '/admin/audit',
      icon: FileText,
    },
    {
      name: '系统监控',
      href: '/admin/monitor',
      icon: Activity,
    },
    {
      name: '客户端管理',
      href: '/admin/clients',
      icon: Database,
    },
    {
      name: '系统设置',
      href: '/admin/settings',
      icon: Settings,
    },
  ];

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const handleBackToApp = () => {
    navigate('/dashboard');
  };

  return (
    <div className=\"min-h-screen bg-gray-50 flex\">
      {/* 侧边栏 */}
      <div
        className={`${
          sidebarOpen ? 'w-64' : 'w-16'
        } bg-gray-900 text-white transition-all duration-300 ease-in-out flex flex-col`}
      >
        {/* 侧边栏头部 */}
        <div className=\"p-4 border-b border-gray-700\">
          <div className=\"flex items-center justify-between\">
            {sidebarOpen && (
              <div>
                <h1 className=\"text-lg font-semibold\">管理控制台</h1>
                <p className=\"text-xs text-gray-400\">授权中心管理</p>
              </div>
            )}
            <Button
              variant=\"ghost\"
              size=\"sm\"
              onClick={() => setSidebarOpen(!sidebarOpen)}
              className=\"text-white hover:bg-gray-800\"
            >
              {sidebarOpen ? <X className=\"h-4 w-4\" /> : <Menu className=\"h-4 w-4\" />}
            </Button>
          </div>
        </div>

        {/* 导航菜单 */}
        <nav className=\"flex-1 p-4 space-y-2\">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.href;
            return (
              <button
                key={item.name}
                onClick={() => navigate(item.href)}
                className={`w-full flex items-center px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-300 hover:bg-gray-800 hover:text-white'
                }`}
              >
                <Icon className=\"h-5 w-5\" />
                {sidebarOpen && <span className=\"ml-3\">{item.name}</span>}
              </button>
            );
          })}
        </nav>

        {/* 用户信息区域 */}
        <div className=\"p-4 border-t border-gray-700\">
          <div className=\"flex items-center space-x-3\">
            <div className=\"w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center\">
              <User className=\"h-4 w-4 text-white\" />
            </div>
            {sidebarOpen && (
              <div className=\"flex-1 min-w-0\">
                <p className=\"text-sm font-medium text-white truncate\">
                  {user?.firstName} {user?.lastName}
                </p>
                <p className=\"text-xs text-gray-400 truncate\">{user?.email}</p>
              </div>
            )}
          </div>
          
          {sidebarOpen && (
            <div className=\"mt-3 flex space-x-2\">
              <Button
                variant=\"outline\"
                size=\"sm\"
                onClick={handleBackToApp}
                className=\"flex-1 text-gray-300 border-gray-600 hover:bg-gray-800\"
              >
                返回应用
              </Button>
              <Button
                variant=\"outline\"
                size=\"sm\"
                onClick={handleLogout}
                className=\"text-gray-300 border-gray-600 hover:bg-gray-800\"
              >
                <LogOut className=\"h-4 w-4\" />
              </Button>
            </div>
          )}
        </div>
      </div>

      {/* 主内容区 */}
      <div className=\"flex-1 flex flex-col overflow-hidden\">
        {/* 顶部导航栏 */}
        <header className=\"bg-white shadow-sm border-b border-gray-200 px-6 py-4\">
          <div className=\"flex items-center justify-between\">
            <div>
              <h2 className=\"text-xl font-semibold text-gray-800\">
                {menuItems.find(item => item.href === location.pathname)?.name || '管理控制台'}
              </h2>
            </div>
            
            <div className=\"flex items-center space-x-4\">
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant=\"outline\" size=\"sm\">
                    <User className=\"h-4 w-4 mr-2\" />
                    {user?.firstName} {user?.lastName}
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align=\"end\">
                  <DropdownMenuItem onClick={() => navigate('/profile')}>
                    <User className=\"h-4 w-4 mr-2\" />
                    个人资料
                  </DropdownMenuItem>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem onClick={handleBackToApp}>
                    <Home className=\"h-4 w-4 mr-2\" />
                    返回应用
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={handleLogout}>
                    <LogOut className=\"h-4 w-4 mr-2\" />
                    退出登录
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </header>

        {/* 主内容 */}
        <main className=\"flex-1 overflow-x-hidden overflow-y-auto p-6\">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AdminLayout;