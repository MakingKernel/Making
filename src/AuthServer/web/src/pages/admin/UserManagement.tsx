import { useState, useEffect } from 'react';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Search,
  Plus,
  MoreHorizontal,
  Edit,
  Lock,
  Unlock,
  UserCheck,
  UserX,
  Trash2,
  RefreshCw,
  Eye,
  Shield,
  AlertTriangle,
  Filter,
  Download
} from 'lucide-react';

interface User {
  id: string;
  userName: string;
  email: string;
  firstName?: string;
  lastName?: string;
  displayName: string;
  avatar?: string;
  department?: string;
  position?: string;
  isActive: boolean;
  isAdmin: boolean;
  emailConfirmed: boolean;
  phoneNumber?: string;
  twoFactorEnabled: boolean;
  lockoutEnd?: string;
  accessFailedCount: number;
  lastLoginAt?: string;
  lastLoginIp?: string;
  loginCount: number;
  createdAt: string;
  updatedAt: string;
  createdBy?: string;
  notes?: string;
  roles?: string[];
}

interface UserStats {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  adminUsers: number;
  lockedUsers: number;
  recentlyCreated: number;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

const UserManagement = () => {
  const [users, setUsers] = useState<PagedResult<User>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false
  });
  const [userStats, setUserStats] = useState<UserStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // 搜索和过滤状态
  const [search, setSearch] = useState('');
  const [activeFilter, setActiveFilter] = useState<boolean | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  // 对话框状态
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [viewDialogOpen, setViewDialogOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);

  // 表单状态
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    department: '',
    position: '',
    isActive: true,
    isAdmin: false,
    emailConfirmed: true,
    notes: '',
    roles: [] as string[]
  });

  // 加载数据
  const fetchUsers = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: currentPage.toString(),
        pageSize: pageSize.toString(),
      });
      
      if (search) params.append('search', search);
      if (activeFilter !== null) params.append('isActive', activeFilter.toString());

      const response = await fetch(`/admin/users?${params.toString()}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('access_token')}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        setUsers(data.data || data);
      } else {
        throw new Error('获取用户列表失败');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : '获取用户列表失败');
    } finally {
      setLoading(false);
    }
  };

  // 加载用户统计
  const fetchUserStats = async () => {
    try {
      const response = await fetch('/admin/users/stats', {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('access_token')}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        setUserStats(data.data || data);
      }
    } catch (err) {
      console.error('获取用户统计失败:', err);
    }
  };

  useEffect(() => {
    fetchUsers();
    fetchUserStats();
  }, [currentPage, pageSize, search, activeFilter]);

  // 用户操作函数
  const handleCreateUser = async () => {
    try {
      const response = await fetch('/admin/users', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('access_token')}`
        },
        body: JSON.stringify(formData)
      });

      if (response.ok) {
        setCreateDialogOpen(false);
        resetForm();
        await fetchUsers();
        await fetchUserStats();
      } else {
        const error = await response.json();
        setError(error.message || '创建用户失败');
      }
    } catch (err) {
      setError('创建用户失败');
    }
  };

  const handleUpdateUser = async () => {
    if (!selectedUser) return;

    try {
      const response = await fetch(`/admin/users/${selectedUser.id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('access_token')}`
        },
        body: JSON.stringify({
          firstName: formData.firstName,
          lastName: formData.lastName,
          department: formData.department,
          position: formData.position,
          isActive: formData.isActive,
          isAdmin: formData.isAdmin,
          notes: formData.notes
        })
      });

      if (response.ok) {
        setEditDialogOpen(false);
        resetForm();
        await fetchUsers();
        await fetchUserStats();
      } else {
        const error = await response.json();
        setError(error.message || '更新用户失败');
      }
    } catch (err) {
      setError('更新用户失败');
    }
  };

  const handleUserAction = async (userId: string, action: string) => {
    try {
      const response = await fetch(`/admin/users/${userId}/${action}`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('access_token')}`
        }
      });

      if (response.ok) {
        await fetchUsers();
        await fetchUserStats();
      } else {
        const error = await response.json();
        setError(error.message || `${action} 操作失败`);
      }
    } catch (err) {
      setError(`${action} 操作失败`);
    }
  };

  const handleDeleteUser = async (userId: string) => {
    if (!confirm('确定要删除这个用户吗？此操作不可撤销。')) return;

    try {
      const response = await fetch(`/admin/users/${userId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('access_token')}`
        }
      });

      if (response.ok) {
        await fetchUsers();
        await fetchUserStats();
      } else {
        const error = await response.json();
        setError(error.message || '删除用户失败');
      }
    } catch (err) {
      setError('删除用户失败');
    }
  };

  const resetForm = () => {
    setFormData({
      email: '',
      password: '',
      firstName: '',
      lastName: '',
      department: '',
      position: '',
      isActive: true,
      isAdmin: false,
      emailConfirmed: true,
      notes: '',
      roles: []
    });
    setSelectedUser(null);
  };

  const openEditDialog = (user: User) => {
    setSelectedUser(user);
    setFormData({
      email: user.email,
      password: '',
      firstName: user.firstName || '',
      lastName: user.lastName || '',
      department: user.department || '',
      position: user.position || '',
      isActive: user.isActive,
      isAdmin: user.isAdmin,
      emailConfirmed: user.emailConfirmed,
      notes: user.notes || '',
      roles: user.roles || []
    });
    setEditDialogOpen(true);
  };

  const openViewDialog = (user: User) => {
    setSelectedUser(user);
    setViewDialogOpen(true);
  };

  if (loading) {
    return (
      <div className=\"space-y-6\">
        <div className=\"animate-pulse\">
          <div className=\"h-8 bg-gray-200 rounded w-1/4 mb-6\"></div>
          <div className=\"grid grid-cols-1 md:grid-cols-4 gap-6 mb-8\">
            {[...Array(4)].map((_, i) => (
              <div key={i} className=\"h-24 bg-gray-200 rounded-lg\"></div>
            ))}
          </div>
          <div className=\"h-64 bg-gray-200 rounded-lg\"></div>
        </div>
      </div>
    );
  }

  return (
    <div className=\"space-y-6\">
      {/* 页面标题和统计卡片 */}
      <div>
        <h1 className=\"text-2xl font-bold text-gray-900\">用户管理</h1>
        <p className=\"text-gray-600 mt-1\">管理系统用户账户</p>
      </div>

      {error && (
        <Alert>
          <AlertTriangle className=\"h-4 w-4\" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* 统计卡片 */}
      {userStats && (
        <div className=\"grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6\">
          <Card className=\"p-6\">
            <div className=\"flex items-center\">
              <div className=\"p-2 bg-blue-100 rounded-lg\">
                <Shield className=\"h-6 w-6 text-blue-600\" />
              </div>
              <div className=\"ml-4\">
                <h3 className=\"text-sm font-medium text-gray-500\">总用户数</h3>
                <p className=\"text-2xl font-bold text-gray-900\">{userStats.totalUsers}</p>
              </div>
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
          </Card>

          <Card className=\"p-6\">
            <div className=\"flex items-center\">
              <div className=\"p-2 bg-orange-100 rounded-lg\">
                <Shield className=\"h-6 w-6 text-orange-600\" />
              </div>
              <div className=\"ml-4\">
                <h3 className=\"text-sm font-medium text-gray-500\">管理员</h3>
                <p className=\"text-2xl font-bold text-gray-900\">{userStats.adminUsers}</p>
              </div>
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
          </Card>
        </div>
      )}

      {/* 操作栏 */}
      <Card className=\"p-6\">
        <div className=\"flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between\">
          <div className=\"flex flex-col sm:flex-row gap-4 items-start sm:items-center\">
            <div className=\"relative\">
              <Search className=\"absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400\" />
              <Input
                placeholder=\"搜索用户...\"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className=\"pl-10 w-64\"
              />
            </div>
            
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant=\"outline\">
                  <Filter className=\"h-4 w-4 mr-2\" />
                  筛选
                  {activeFilter !== null && (
                    <Badge variant=\"secondary\" className=\"ml-2\">
                      {activeFilter ? '活跃' : '停用'}
                    </Badge>
                  )}
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent>
                <DropdownMenuItem onClick={() => setActiveFilter(null)}>
                  全部用户
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => setActiveFilter(true)}>
                  活跃用户
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => setActiveFilter(false)}>
                  停用用户
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className=\"flex gap-2\">
            <Button variant=\"outline\" onClick={fetchUsers}>
              <RefreshCw className=\"h-4 w-4 mr-2\" />
              刷新
            </Button>
            <Button onClick={() => setCreateDialogOpen(true)}>
              <Plus className=\"h-4 w-4 mr-2\" />
              创建用户
            </Button>
          </div>
        </div>
      </Card>

      {/* 用户列表 */}
      <Card>
        <div className=\"overflow-x-auto\">
          <table className=\"w-full\">
            <thead className=\"bg-gray-50\">
              <tr>
                <th className=\"px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider\">
                  用户
                </th>
                <th className=\"px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider\">
                  部门/职位
                </th>
                <th className=\"px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider\">
                  状态
                </th>
                <th className=\"px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider\">
                  最后登录
                </th>
                <th className=\"px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider\">
                  创建时间
                </th>
                <th className=\"px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider\">
                  操作
                </th>
              </tr>
            </thead>
            <tbody className=\"bg-white divide-y divide-gray-200\">
              {users.items.map((user) => (
                <tr key={user.id} className=\"hover:bg-gray-50\">
                  <td className=\"px-6 py-4 whitespace-nowrap\">
                    <div className=\"flex items-center\">
                      <div className=\"w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center\">
                        <span className=\"text-sm font-medium text-blue-600\">
                          {user.firstName?.[0] || user.email[0].toUpperCase()}
                        </span>
                      </div>
                      <div className=\"ml-4\">
                        <div className=\"text-sm font-medium text-gray-900\">
                          {user.displayName || user.email}
                        </div>
                        <div className=\"text-sm text-gray-500\">{user.email}</div>
                        {user.isAdmin && (
                          <Badge variant=\"secondary\" className=\"mt-1\">
                            管理员
                          </Badge>
                        )}
                      </div>
                    </div>
                  </td>
                  <td className=\"px-6 py-4 whitespace-nowrap\">
                    <div className=\"text-sm text-gray-900\">{user.department || '-'}</div>
                    <div className=\"text-sm text-gray-500\">{user.position || '-'}</div>
                  </td>
                  <td className=\"px-6 py-4 whitespace-nowrap\">
                    <div className=\"flex flex-col gap-1\">
                      <Badge variant={user.isActive ? 'default' : 'secondary'}>
                        {user.isActive ? '活跃' : '停用'}
                      </Badge>
                      {user.lockoutEnd && new Date(user.lockoutEnd) > new Date() && (
                        <Badge variant=\"destructive\">锁定</Badge>
                      )}
                      {user.emailConfirmed && (
                        <Badge variant=\"outline\">邮箱已验证</Badge>
                      )}
                    </div>
                  </td>
                  <td className=\"px-6 py-4 whitespace-nowrap text-sm text-gray-500\">
                    {user.lastLoginAt ? (
                      <div>
                        <div>{new Date(user.lastLoginAt).toLocaleDateString()}</div>
                        <div className=\"text-xs\">{user.lastLoginIp}</div>
                      </div>
                    ) : (
                      '从未登录'
                    )}
                  </td>
                  <td className=\"px-6 py-4 whitespace-nowrap text-sm text-gray-500\">
                    {new Date(user.createdAt).toLocaleDateString()}
                  </td>
                  <td className=\"px-6 py-4 whitespace-nowrap text-right text-sm font-medium\">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant=\"ghost\" size=\"sm\">
                          <MoreHorizontal className=\"h-4 w-4\" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align=\"end\">
                        <DropdownMenuItem onClick={() => openViewDialog(user)}>
                          <Eye className=\"h-4 w-4 mr-2\" />
                          查看详情
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={() => openEditDialog(user)}>
                          <Edit className=\"h-4 w-4 mr-2\" />
                          编辑
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                        {user.lockoutEnd && new Date(user.lockoutEnd) > new Date() ? (
                          <DropdownMenuItem onClick={() => handleUserAction(user.id, 'unlock')}>
                            <Unlock className=\"h-4 w-4 mr-2\" />
                            解锁
                          </DropdownMenuItem>
                        ) : (
                          <DropdownMenuItem onClick={() => handleUserAction(user.id, 'lock')}>
                            <Lock className=\"h-4 w-4 mr-2\" />
                            锁定
                          </DropdownMenuItem>
                        )}
                        {user.isActive ? (
                          <DropdownMenuItem onClick={() => handleUserAction(user.id, 'deactivate')}>
                            <UserX className=\"h-4 w-4 mr-2\" />
                            停用
                          </DropdownMenuItem>
                        ) : (
                          <DropdownMenuItem onClick={() => handleUserAction(user.id, 'activate')}>
                            <UserCheck className=\"h-4 w-4 mr-2\" />
                            激活
                          </DropdownMenuItem>
                        )}
                        <DropdownMenuItem onClick={() => handleUserAction(user.id, 'reset-password')}>
                          <RefreshCw className=\"h-4 w-4 mr-2\" />
                          重置密码
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem 
                          onClick={() => handleDeleteUser(user.id)}
                          className=\"text-red-600\"
                        >
                          <Trash2 className=\"h-4 w-4 mr-2\" />
                          删除
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* 分页 */}
        {users.totalPages > 1 && (
          <div className=\"px-6 py-4 border-t border-gray-200\">
            <div className=\"flex items-center justify-between\">
              <div className=\"text-sm text-gray-700\">
                显示 {((users.page - 1) * users.pageSize) + 1} 到{' '}
                {Math.min(users.page * users.pageSize, users.totalCount)} 条，共 {users.totalCount} 条
              </div>
              <div className=\"flex gap-2\">
                <Button
                  variant=\"outline\"
                  size=\"sm\"
                  disabled={!users.hasPreviousPage}
                  onClick={() => setCurrentPage(currentPage - 1)}
                >
                  上一页
                </Button>
                <Button
                  variant=\"outline\"
                  size=\"sm\"
                  disabled={!users.hasNextPage}
                  onClick={() => setCurrentPage(currentPage + 1)}
                >
                  下一页
                </Button>
              </div>
            </div>
          </div>
        )}
      </Card>

      {/* 创建用户对话框 */}
      <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
        <DialogContent className=\"sm:max-w-[600px]\">
          <DialogHeader>
            <DialogTitle>创建新用户</DialogTitle>
            <DialogDescription>
              填写用户信息以创建新的系统用户账户。
            </DialogDescription>
          </DialogHeader>
          <div className=\"grid gap-4 py-4\">
            <div className=\"grid grid-cols-2 gap-4\">
              <div className=\"space-y-2\">
                <Label htmlFor=\"email\">邮箱 *</Label>
                <Input
                  id=\"email\"
                  type=\"email\"
                  value={formData.email}
                  onChange={(e) => setFormData({...formData, email: e.target.value})}
                  placeholder=\"user@example.com\"
                />
              </div>
              <div className=\"space-y-2\">
                <Label htmlFor=\"password\">密码 *</Label>
                <Input
                  id=\"password\"
                  type=\"password\"
                  value={formData.password}
                  onChange={(e) => setFormData({...formData, password: e.target.value})}
                  placeholder=\"至少12位字符\"
                />
              </div>
            </div>
            <div className=\"grid grid-cols-2 gap-4\">
              <div className=\"space-y-2\">
                <Label htmlFor=\"firstName\">名字</Label>
                <Input
                  id=\"firstName\"
                  value={formData.firstName}
                  onChange={(e) => setFormData({...formData, firstName: e.target.value})}
                />
              </div>
              <div className=\"space-y-2\">
                <Label htmlFor=\"lastName\">姓氏</Label>
                <Input
                  id=\"lastName\"
                  value={formData.lastName}
                  onChange={(e) => setFormData({...formData, lastName: e.target.value})}
                />
              </div>
            </div>
            <div className=\"grid grid-cols-2 gap-4\">
              <div className=\"space-y-2\">
                <Label htmlFor=\"department\">部门</Label>
                <Input
                  id=\"department\"
                  value={formData.department}
                  onChange={(e) => setFormData({...formData, department: e.target.value})}
                />
              </div>
              <div className=\"space-y-2\">
                <Label htmlFor=\"position\">职位</Label>
                <Input
                  id=\"position\"
                  value={formData.position}
                  onChange={(e) => setFormData({...formData, position: e.target.value})}
                />
              </div>
            </div>
            <div className=\"space-y-2\">
              <Label htmlFor=\"notes\">备注</Label>
              <Input
                id=\"notes\"
                value={formData.notes}
                onChange={(e) => setFormData({...formData, notes: e.target.value})}
                placeholder=\"可选的用户备注信息\"
              />
            </div>
            <div className=\"flex gap-4\">
              <label className=\"flex items-center space-x-2\">
                <input
                  type=\"checkbox\"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({...formData, isActive: e.target.checked})}
                  className=\"rounded\"
                />
                <span className=\"text-sm\">激活用户</span>
              </label>
              <label className=\"flex items-center space-x-2\">
                <input
                  type=\"checkbox\"
                  checked={formData.isAdmin}
                  onChange={(e) => setFormData({...formData, isAdmin: e.target.checked})}
                  className=\"rounded\"
                />
                <span className=\"text-sm\">管理员权限</span>
              </label>
              <label className=\"flex items-center space-x-2\">
                <input
                  type=\"checkbox\"
                  checked={formData.emailConfirmed}
                  onChange={(e) => setFormData({...formData, emailConfirmed: e.target.checked})}
                  className=\"rounded\"
                />
                <span className=\"text-sm\">邮箱已验证</span>
              </label>
            </div>
          </div>
          <DialogFooter>
            <Button variant=\"outline\" onClick={() => setCreateDialogOpen(false)}>
              取消
            </Button>
            <Button onClick={handleCreateUser}>创建用户</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* 编辑用户对话框 */}
      <Dialog open={editDialogOpen} onOpenChange={setEditDialogOpen}>
        <DialogContent className=\"sm:max-w-[600px]\">
          <DialogHeader>
            <DialogTitle>编辑用户</DialogTitle>
            <DialogDescription>
              修改用户信息。
            </DialogDescription>
          </DialogHeader>
          <div className=\"grid gap-4 py-4\">
            <div className=\"grid grid-cols-2 gap-4\">
              <div className=\"space-y-2\">
                <Label htmlFor=\"edit-firstName\">名字</Label>
                <Input
                  id=\"edit-firstName\"
                  value={formData.firstName}
                  onChange={(e) => setFormData({...formData, firstName: e.target.value})}
                />
              </div>
              <div className=\"space-y-2\">
                <Label htmlFor=\"edit-lastName\">姓氏</Label>
                <Input
                  id=\"edit-lastName\"
                  value={formData.lastName}
                  onChange={(e) => setFormData({...formData, lastName: e.target.value})}
                />
              </div>
            </div>
            <div className=\"grid grid-cols-2 gap-4\">
              <div className=\"space-y-2\">
                <Label htmlFor=\"edit-department\">部门</Label>
                <Input
                  id=\"edit-department\"
                  value={formData.department}
                  onChange={(e) => setFormData({...formData, department: e.target.value})}
                />
              </div>
              <div className=\"space-y-2\">
                <Label htmlFor=\"edit-position\">职位</Label>
                <Input
                  id=\"edit-position\"
                  value={formData.position}
                  onChange={(e) => setFormData({...formData, position: e.target.value})}
                />
              </div>
            </div>
            <div className=\"space-y-2\">
              <Label htmlFor=\"edit-notes\">备注</Label>
              <Input
                id=\"edit-notes\"
                value={formData.notes}
                onChange={(e) => setFormData({...formData, notes: e.target.value})}
              />
            </div>
            <div className=\"flex gap-4\">
              <label className=\"flex items-center space-x-2\">
                <input
                  type=\"checkbox\"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({...formData, isActive: e.target.checked})}
                  className=\"rounded\"
                />
                <span className=\"text-sm\">激活用户</span>
              </label>
              <label className=\"flex items-center space-x-2\">
                <input
                  type=\"checkbox\"
                  checked={formData.isAdmin}
                  onChange={(e) => setFormData({...formData, isAdmin: e.target.checked})}
                  className=\"rounded\"
                />
                <span className=\"text-sm\">管理员权限</span>
              </label>
            </div>
          </div>
          <DialogFooter>
            <Button variant=\"outline\" onClick={() => setEditDialogOpen(false)}>
              取消
            </Button>
            <Button onClick={handleUpdateUser}>保存更改</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* 查看用户详情对话框 */}
      <Dialog open={viewDialogOpen} onOpenChange={setViewDialogOpen}>
        <DialogContent className=\"sm:max-w-[700px]\">
          <DialogHeader>
            <DialogTitle>用户详情</DialogTitle>
          </DialogHeader>
          {selectedUser && (
            <Tabs defaultValue=\"basic\" className=\"w-full\">
              <TabsList className=\"grid w-full grid-cols-3\">
                <TabsTrigger value=\"basic\">基本信息</TabsTrigger>
                <TabsTrigger value=\"security\">安全信息</TabsTrigger>
                <TabsTrigger value=\"activity\">活动记录</TabsTrigger>
              </TabsList>
              <TabsContent value=\"basic\" className=\"space-y-4\">
                <div className=\"grid grid-cols-2 gap-4\">
                  <div>
                    <Label>邮箱</Label>
                    <p className=\"text-sm text-gray-900\">{selectedUser.email}</p>
                  </div>
                  <div>
                    <Label>用户名</Label>
                    <p className=\"text-sm text-gray-900\">{selectedUser.userName}</p>
                  </div>
                  <div>
                    <Label>姓名</Label>
                    <p className=\"text-sm text-gray-900\">{selectedUser.displayName || '-'}</p>
                  </div>
                  <div>
                    <Label>部门</Label>
                    <p className=\"text-sm text-gray-900\">{selectedUser.department || '-'}</p>
                  </div>
                  <div>
                    <Label>职位</Label>
                    <p className=\"text-sm text-gray-900\">{selectedUser.position || '-'}</p>
                  </div>
                  <div>
                    <Label>状态</Label>
                    <div className=\"flex gap-2 mt-1\">
                      <Badge variant={selectedUser.isActive ? 'default' : 'secondary'}>
                        {selectedUser.isActive ? '活跃' : '停用'}
                      </Badge>
                      {selectedUser.isAdmin && <Badge variant=\"secondary\">管理员</Badge>}
                    </div>
                  </div>
                </div>
                {selectedUser.notes && (
                  <div>
                    <Label>备注</Label>
                    <p className=\"text-sm text-gray-900\">{selectedUser.notes}</p>
                  </div>
                )}
              </TabsContent>
              <TabsContent value=\"security\" className=\"space-y-4\">
                <div className=\"grid grid-cols-2 gap-4\">
                  <div>
                    <Label>邮箱验证状态</Label>
                    <Badge variant={selectedUser.emailConfirmed ? 'default' : 'secondary'}>
                      {selectedUser.emailConfirmed ? '已验证' : '未验证'}
                    </Badge>
                  </div>
                  <div>
                    <Label>双因素认证</Label>
                    <Badge variant={selectedUser.twoFactorEnabled ? 'default' : 'secondary'}>
                      {selectedUser.twoFactorEnabled ? '已启用' : '未启用'}
                    </Badge>
                  </div>
                  <div>
                    <Label>失败登录次数</Label>
                    <p className=\"text-sm text-gray-900\">{selectedUser.accessFailedCount}</p>
                  </div>
                  <div>
                    <Label>账户锁定状态</Label>
                    {selectedUser.lockoutEnd && new Date(selectedUser.lockoutEnd) > new Date() ? (
                      <Badge variant=\"destructive\">已锁定</Badge>
                    ) : (
                      <Badge variant=\"default\">正常</Badge>
                    )}
                  </div>
                </div>
              </TabsContent>
              <TabsContent value=\"activity\" className=\"space-y-4\">
                <div className=\"grid grid-cols-2 gap-4\">
                  <div>
                    <Label>创建时间</Label>
                    <p className=\"text-sm text-gray-900\">
                      {new Date(selectedUser.createdAt).toLocaleString()}
                    </p>
                  </div>
                  <div>
                    <Label>更新时间</Label>
                    <p className=\"text-sm text-gray-900\">
                      {new Date(selectedUser.updatedAt).toLocaleString()}
                    </p>
                  </div>
                  <div>
                    <Label>登录次数</Label>
                    <p className=\"text-sm text-gray-900\">{selectedUser.loginCount}</p>
                  </div>
                  <div>
                    <Label>最后登录</Label>
                    <p className=\"text-sm text-gray-900\">
                      {selectedUser.lastLoginAt ? 
                        new Date(selectedUser.lastLoginAt).toLocaleString() : 
                        '从未登录'
                      }
                    </p>
                  </div>
                  {selectedUser.lastLoginIp && (
                    <div>
                      <Label>最后登录IP</Label>
                      <p className=\"text-sm text-gray-900\">{selectedUser.lastLoginIp}</p>
                    </div>
                  )}
                  {selectedUser.createdBy && (
                    <div>
                      <Label>创建者</Label>
                      <p className=\"text-sm text-gray-900\">{selectedUser.createdBy}</p>
                    </div>
                  )}
                </div>
              </TabsContent>
            </Tabs>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default UserManagement;