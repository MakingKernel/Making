import { useEffect, useState } from 'react';
import { Card } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { 
  Users, 
  UserCheck, 
  UserX, 
  Shield, 
  Activity, 
  AlertTriangle,
  TrendingUp,
  Clock
} from 'lucide-react';

interface UserStats {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  adminUsers: number;
  lockedUsers: number;
  recentlyCreated: number;
}

interface SystemHealth {
  status: string;
  timestamp: string;
  checks: Array<{
    name: string;
    status: string;
    duration?: string;
    usage?: string;
  }>;
}

const AdminDashboard = () => {
  const [userStats, setUserStats] = useState<UserStats | null>(null);
  const [systemHealth, setSystemHealth] = useState<SystemHealth | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setLoading(true);
        
        // 并行获取用户统计和系统健康状态
        const [userStatsRes, systemHealthRes] = await Promise.all([
          fetch('/admin/users/stats', {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('access_token')}` }
          }),
          fetch('/admin/system/health', {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('access_token')}` }
          })
        ]);

        if (userStatsRes.ok) {
          const stats = await userStatsRes.json();
          setUserStats(stats.data || stats);
        }

        if (systemHealthRes.ok) {
          const health = await systemHealthRes.json();
          setSystemHealth(health.data || health);
        }
      } catch (err) {
        setError('加载仪表盘数据失败');
        console.error('Dashboard data fetch error:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboardData();
    
    // 每30秒刷新一次数据
    const interval = setInterval(fetchDashboardData, 30000);
    return () => clearInterval(interval);
  }, []);

  if (loading) {
    return (
      <div className=\"space-y-6\">
        <div className=\"animate-pulse\">
          <div className=\"h-8 bg-gray-200 rounded w-1/4 mb-6\"></div>
          <div className=\"grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8\">
            {[...Array(4)].map((_, i) => (
              <div key={i} className=\"h-32 bg-gray-200 rounded-lg\"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className=\"space-y-6\">
      {/* 页面标题 */}
      <div>
        <h1 className=\"text-2xl font-bold text-gray-900\">管理仪表盘</h1>
        <p className=\"text-gray-600 mt-1\">授权中心系统概览</p>
      </div>

      {error && (
        <Alert>
          <AlertTriangle className=\"h-4 w-4\" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* 用户统计卡片 */}
      {userStats && (
        <div className=\"grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6\">
          <Card className=\"p-6\">
            <div className=\"flex items-center\">
              <div className=\"p-2 bg-blue-100 rounded-lg\">
                <Users className=\"h-6 w-6 text-blue-600\" />
              </div>
              <div className=\"ml-4\">
                <h3 className=\"text-sm font-medium text-gray-500\">总用户数</h3>
                <p className=\"text-2xl font-bold text-gray-900\">{userStats.totalUsers}</p>
              </div>
            </div>
            <div className=\"mt-4 flex items-center text-sm text-green-600\">
              <TrendingUp className=\"h-4 w-4 mr-1\" />
              本周新增 {userStats.recentlyCreated} 人
            </div>
          </Card>

          <Card className=\"p-6\">
            <div className=\"flex items-center\">
              <div className=\"p-2 bg-green-100 rounded-lg\">
                <UserCheck className=\"h-6 w-6 text-green-600\" />
              </div>
              <div className=\"ml-4\">
                <h3 className=\"text-sm font-medium text-gray-500\">活跃用户</h3>
                <p className=\"text-2xl font-bold text-gray-900\">{userStats.activeUsers}</p>
              </div>
            </div>
            <div className=\"mt-4 flex items-center text-sm text-gray-600\">
              活跃率: {userStats.totalUsers > 0 ? Math.round((userStats.activeUsers / userStats.totalUsers) * 100) : 0}%
            </div>
          </Card>

          <Card className=\"p-6\">
            <div className=\"flex items-center\">
              <div className=\"p-2 bg-orange-100 rounded-lg\">
                <Shield className=\"h-6 w-6 text-orange-600\" />
              </div>
              <div className=\"ml-4\">
                <h3 className=\"text-sm font-medium text-gray-500\">管理员用户</h3>
                <p className=\"text-2xl font-bold text-gray-900\">{userStats.adminUsers}</p>
              </div>
            </div>
            <div className=\"mt-4 flex items-center text-sm text-gray-600\">
              管理员占比: {userStats.totalUsers > 0 ? Math.round((userStats.adminUsers / userStats.totalUsers) * 100) : 0}%
            </div>
          </Card>

          <Card className=\"p-6\">
            <div className=\"flex items-center\">
              <div className=\"p-2 bg-red-100 rounded-lg\">
                <UserX className=\"h-6 w-6 text-red-600\" />
              </div>
              <div className=\"ml-4\">
                <h3 className=\"text-sm font-medium text-gray-500\">锁定用户</h3>
                <p className=\"text-2xl font-bold text-gray-900\">{userStats.lockedUsers}</p>
              </div>
            </div>
            <div className=\"mt-4 flex items-center text-sm text-gray-600\">
              {userStats.lockedUsers > 0 && (
                <span className=\"text-red-600\">需要关注</span>
              )}
              {userStats.lockedUsers === 0 && (
                <span className=\"text-green-600\">状态良好</span>
              )}
            </div>
          </Card>
        </div>
      )}

      <div className=\"grid grid-cols-1 lg:grid-cols-2 gap-6\">
        {/* 系统健康状态 */}
        {systemHealth && (
          <Card className=\"p-6\">
            <div className=\"flex items-center mb-4\">
              <div className={`p-2 rounded-lg ${systemHealth.status === 'Healthy' ? 'bg-green-100' : 'bg-red-100'}`}>
                <Activity className={`h-6 w-6 ${systemHealth.status === 'Healthy' ? 'text-green-600' : 'text-red-600'}`} />
              </div>
              <div className=\"ml-4\">
                <h3 className=\"text-lg font-semibold text-gray-900\">系统健康状态</h3>
                <p className=\"text-sm text-gray-600\">
                  <Clock className=\"h-4 w-4 inline mr-1\" />
                  {new Date(systemHealth.timestamp).toLocaleString()}
                </p>
              </div>
            </div>
            
            <div className=\"space-y-3\">
              {systemHealth.checks.map((check, index) => (
                <div key={index} className=\"flex items-center justify-between p-3 bg-gray-50 rounded-lg\">
                  <div className=\"flex items-center\">
                    <div className={`w-2 h-2 rounded-full mr-3 ${check.status === 'Healthy' ? 'bg-green-500' : 'bg-red-500'}`}></div>
                    <span className=\"font-medium\">{check.name}</span>
                  </div>
                  <div className=\"text-sm text-gray-600\">
                    {check.duration && <span>{check.duration}</span>}
                    {check.usage && <span>{check.usage}</span>}
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}

        {/* 快速操作 */}
        <Card className=\"p-6\">
          <h3 className=\"text-lg font-semibold text-gray-900 mb-4\">快速操作</h3>
          <div className=\"space-y-3\">
            <button className=\"w-full text-left p-3 bg-blue-50 hover:bg-blue-100 rounded-lg transition-colors\">
              <div className=\"flex items-center\">
                <Users className=\"h-5 w-5 text-blue-600 mr-3\" />
                <div>
                  <h4 className=\"font-medium\">创建新用户</h4>
                  <p className=\"text-sm text-gray-600\">添加新的系统用户</p>
                </div>
              </div>
            </button>
            
            <button className=\"w-full text-left p-3 bg-green-50 hover:bg-green-100 rounded-lg transition-colors\">
              <div className=\"flex items-center\">
                <Shield className=\"h-5 w-5 text-green-600 mr-3\" />
                <div>
                  <h4 className=\"font-medium\">角色管理</h4>
                  <p className=\"text-sm text-gray-600\">管理系统角色和权限</p>
                </div>
              </div>
            </button>
            
            <button className=\"w-full text-left p-3 bg-orange-50 hover:bg-orange-100 rounded-lg transition-colors\">
              <div className=\"flex items-center\">
                <Activity className=\"h-5 w-5 text-orange-600 mr-3\" />
                <div>
                  <h4 className=\"font-medium\">查看审计日志</h4>
                  <p className=\"text-sm text-gray-600\">监控系统操作记录</p>
                </div>
              </div>
            </button>
          </div>
        </Card>
      </div>

      {/* 最近活动 */}
      <Card className=\"p-6\">
        <h3 className=\"text-lg font-semibold text-gray-900 mb-4\">系统概览</h3>
        <div className=\"grid grid-cols-1 md:grid-cols-3 gap-6\">
          <div className=\"text-center p-4 bg-gray-50 rounded-lg\">
            <h4 className=\"text-2xl font-bold text-gray-900\">{userStats?.totalUsers || 0}</h4>
            <p className=\"text-gray-600\">注册用户总数</p>
          </div>
          <div className=\"text-center p-4 bg-gray-50 rounded-lg\">
            <h4 className=\"text-2xl font-bold text-green-600\">99.9%</h4>
            <p className=\"text-gray-600\">系统可用性</p>
          </div>
          <div className=\"text-center p-4 bg-gray-50 rounded-lg\">
            <h4 className=\"text-2xl font-bold text-blue-600\">24/7</h4>
            <p className=\"text-gray-600\">服务运行时间</p>
          </div>
        </div>
      </Card>
    </div>
  );
};

export default AdminDashboard;