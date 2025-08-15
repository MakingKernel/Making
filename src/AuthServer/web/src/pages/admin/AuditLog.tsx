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
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import {
  Search,
  Filter,
  Download,
  RefreshCw,
  Calendar,
  User,
  Activity,
  Shield,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Clock,
  Eye,
  MoreHorizontal
} from 'lucide-react';

interface AuditLog {
  id: string;
  userId?: string;
  userName?: string;
  action: string;
  resourceType?: string;
  resourceId?: string;
  description?: string;
  result: 'Success' | 'Failed' | 'Warning' | 'Error';
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  ipAddress?: string;
  userAgent?: string;
  source?: string;
  sessionId?: string;
  timestamp: string;
  additionalData?: string;
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

const AuditLog = () => {
  const [auditLogs, setAuditLogs] = useState<PagedResult<AuditLog>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 50,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false
  });
  
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // 过滤器状态
  const [search, setSearch] = useState('');
  const [userIdFilter, setUserIdFilter] = useState('');
  const [actionFilter, setActionFilter] = useState('');
  const [resultFilter, setResultFilter] = useState<string>('');
  const [riskLevelFilter, setRiskLevelFilter] = useState<string>('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);

  // 对话框状态
  const [detailDialogOpen, setDetailDialogOpen] = useState(false);
  const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null);
  const [filterDialogOpen, setFilterDialogOpen] = useState(false);

  // 获取审计日志
  const fetchAuditLogs = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const params = new URLSearchParams({
        page: currentPage.toString(),
        pageSize: pageSize.toString(),
      });
      
      if (userIdFilter) params.append('userId', userIdFilter);
      if (actionFilter) params.append('action', actionFilter);
      if (startDate) params.append('startDate', startDate);
      if (endDate) params.append('endDate', endDate);

      const response = await fetch(`/admin/audit?${params.toString()}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('access_token')}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        setAuditLogs(data.data || data);
      } else {
        throw new Error('获取审计日志失败');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : '获取审计日志失败');
    } finally {
      setLoading(false);
    }
  };

  // 导出审计日志
  const exportAuditLogs = async () => {
    try {
      const params = new URLSearchParams({
        format: 'csv'
      });
      
      if (userIdFilter) params.append('userId', userIdFilter);
      if (actionFilter) params.append('action', actionFilter);
      if (startDate) params.append('startDate', startDate);
      if (endDate) params.append('endDate', endDate);

      const response = await fetch(`/admin/audit/export?${params.toString()}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('access_token')}`
        }
      });

      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.style.display = 'none';
        a.href = url;
        a.download = `audit_logs_${new Date().toISOString().split('T')[0]}.csv`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      } else {
        throw new Error('导出失败');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : '导出失败');
    }
  };

  // 清除过滤器
  const clearFilters = () => {
    setUserIdFilter('');
    setActionFilter('');
    setResultFilter('');
    setRiskLevelFilter('');
    setStartDate('');
    setEndDate('');
    setCurrentPage(1);
  };

  // 获取结果图标和颜色
  const getResultBadge = (result: string) => {
    switch (result) {
      case 'Success':
        return <Badge variant="default" className="bg-green-500"><CheckCircle className="h-3 w-3 mr-1" />成功</Badge>;
      case 'Failed':
        return <Badge variant="destructive"><XCircle className="h-3 w-3 mr-1" />失败</Badge>;
      case 'Warning':
        return <Badge variant="secondary" className="bg-yellow-500"><AlertTriangle className="h-3 w-3 mr-1" />警告</Badge>;
      case 'Error':
        return <Badge variant="destructive"><XCircle className="h-3 w-3 mr-1" />错误</Badge>;
      default:
        return <Badge variant="outline">{result}</Badge>;
    }
  };

  // 获取风险等级徽章
  const getRiskLevelBadge = (riskLevel: string) => {
    switch (riskLevel) {
      case 'Low':
        return <Badge variant="outline" className="border-green-500 text-green-700">低风险</Badge>;
      case 'Medium':
        return <Badge variant="outline" className="border-yellow-500 text-yellow-700">中风险</Badge>;
      case 'High':
        return <Badge variant="outline" className="border-orange-500 text-orange-700">高风险</Badge>;
      case 'Critical':
        return <Badge variant="destructive">严重</Badge>;
      default:
        return <Badge variant="outline">{riskLevel}</Badge>;
    }
  };

  // 获取操作类型图标
  const getActionIcon = (action: string) => {
    if (action.includes('LOGIN')) return <User className="h-4 w-4" />;
    if (action.includes('USER') || action.includes('ROLE')) return <Shield className="h-4 w-4" />;
    if (action.includes('SYSTEM')) return <Activity className="h-4 w-4" />;
    return <Activity className="h-4 w-4" />;
  };

  useEffect(() => {
    fetchAuditLogs();
  }, [currentPage, pageSize, userIdFilter, actionFilter, startDate, endDate]);

  const openDetailDialog = (log: AuditLog) => {
    setSelectedLog(log);
    setDetailDialogOpen(true);
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="animate-pulse">
          <div className="h-8 bg-gray-200 rounded w-1/4 mb-6"></div>
          <div className="h-64 bg-gray-200 rounded-lg"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* 页面标题 */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">审计日志</h1>
        <p className="text-gray-600 mt-1">查看系统操作和安全事件记录</p>
      </div>

      {error && (
        <Alert>
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* 操作栏 */}
      <Card className="p-6">
        <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-center justify-between">
          <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center flex-1">
            <div className="relative min-w-0 flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                placeholder="搜索操作或描述..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-10"
              />
            </div>
            
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => setFilterDialogOpen(true)}>
                <Filter className="h-4 w-4 mr-2" />
                高级筛选
                {(userIdFilter || actionFilter || startDate || endDate) && (
                  <Badge variant="secondary" className="ml-2">
                    已启用
                  </Badge>
                )}
              </Button>
              
              {(userIdFilter || actionFilter || startDate || endDate) && (
                <Button variant="outline" onClick={clearFilters}>
                  清除筛选
                </Button>
              )}
            </div>
          </div>

          <div className="flex gap-2">
            <Button variant="outline" onClick={fetchAuditLogs}>
              <RefreshCw className="h-4 w-4 mr-2" />
              刷新
            </Button>
            <Button variant="outline" onClick={exportAuditLogs}>
              <Download className="h-4 w-4 mr-2" />
              导出CSV
            </Button>
          </div>
        </div>
      </Card>

      {/* 审计日志列表 */}
      <Card>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  时间
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  用户
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  操作
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  资源
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  结果
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  风险等级
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  IP地址
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  操作
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {auditLogs.items.map((log) => (
                <tr key={log.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    <div className="flex items-center">
                      <Clock className="h-4 w-4 mr-2 text-gray-400" />
                      <div>
                        <div>{new Date(log.timestamp).toLocaleDateString()}</div>
                        <div className="text-xs text-gray-500">
                          {new Date(log.timestamp).toLocaleTimeString()}
                        </div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm">
                      <div className="font-medium text-gray-900">
                        {log.userName || log.userId || 'System'}
                      </div>
                      {log.userId && log.userId !== log.userName && (
                        <div className="text-xs text-gray-500">{log.userId}</div>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center text-sm text-gray-900">
                      {getActionIcon(log.action)}
                      <span className="ml-2">{log.action}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {log.resourceType && (
                      <div>
                        <div className="font-medium">{log.resourceType}</div>
                        {log.resourceId && (
                          <div className="text-xs text-gray-500 truncate max-w-32" title={log.resourceId}>
                            {log.resourceId}
                          </div>
                        )}
                      </div>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {getResultBadge(log.result)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {getRiskLevelBadge(log.riskLevel)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {log.ipAddress || '-'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="sm">
                          <MoreHorizontal className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => openDetailDialog(log)}>
                          <Eye className="h-4 w-4 mr-2" />
                          查看详情
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
        {auditLogs.totalPages > 1 && (
          <div className="px-6 py-4 border-t border-gray-200">
            <div className="flex items-center justify-between">
              <div className="text-sm text-gray-700">
                显示 {((auditLogs.page - 1) * auditLogs.pageSize) + 1} 到{' '}
                {Math.min(auditLogs.page * auditLogs.pageSize, auditLogs.totalCount)} 条，共 {auditLogs.totalCount} 条
              </div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!auditLogs.hasPreviousPage}
                  onClick={() => setCurrentPage(currentPage - 1)}
                >
                  上一页
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!auditLogs.hasNextPage}
                  onClick={() => setCurrentPage(currentPage + 1)}
                >
                  下一页
                </Button>
              </div>
            </div>
          </div>
        )}
      </Card>

      {/* 高级筛选对话框 */}
      <Dialog open={filterDialogOpen} onOpenChange={setFilterDialogOpen}>
        <DialogContent className="sm:max-w-[600px]">
          <DialogHeader>
            <DialogTitle>高级筛选</DialogTitle>
            <DialogDescription>
              设置筛选条件来查找特定的审计日志记录。
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="userId">用户ID</Label>
                <Input
                  id="userId"
                  value={userIdFilter}
                  onChange={(e) => setUserIdFilter(e.target.value)}
                  placeholder="输入用户ID"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="action">操作类型</Label>
                <Input
                  id="action"
                  value={actionFilter}
                  onChange={(e) => setActionFilter(e.target.value)}
                  placeholder="如: LOGIN, USER_CREATED"
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="startDate">开始时间</Label>
                <Input
                  id="startDate"
                  type="datetime-local"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="endDate">结束时间</Label>
                <Input
                  id="endDate"
                  type="datetime-local"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="result">结果</Label>
                <select
                  id="result"
                  value={resultFilter}
                  onChange={(e) => setResultFilter(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                >
                  <option value="">全部结果</option>
                  <option value="Success">成功</option>
                  <option value="Failed">失败</option>
                  <option value="Warning">警告</option>
                  <option value="Error">错误</option>
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="riskLevel">风险等级</Label>
                <select
                  id="riskLevel"
                  value={riskLevelFilter}
                  onChange={(e) => setRiskLevelFilter(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md"
                >
                  <option value="">全部等级</option>
                  <option value="Low">低风险</option>
                  <option value="Medium">中风险</option>
                  <option value="High">高风险</option>
                  <option value="Critical">严重</option>
                </select>
              </div>
            </div>
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setFilterDialogOpen(false)}>
              取消
            </Button>
            <Button onClick={() => {
              setFilterDialogOpen(false);
              setCurrentPage(1);
            }}>
              应用筛选
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* 详情对话框 */}
      <Dialog open={detailDialogOpen} onOpenChange={setDetailDialogOpen}>
        <DialogContent className="sm:max-w-[700px]">
          <DialogHeader>
            <DialogTitle>审计日志详情</DialogTitle>
          </DialogHeader>
          {selectedLog && (
            <div className="space-y-6">
              {/* 基本信息 */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label>时间</Label>
                  <p className="text-sm text-gray-900">
                    {new Date(selectedLog.timestamp).toLocaleString()}
                  </p>
                </div>
                <div>
                  <Label>用户</Label>
                  <p className="text-sm text-gray-900">
                    {selectedLog.userName || selectedLog.userId || 'System'}
                  </p>
                </div>
                <div>
                  <Label>操作</Label>
                  <p className="text-sm text-gray-900">{selectedLog.action}</p>
                </div>
                <div>
                  <Label>结果</Label>
                  <div className="mt-1">
                    {getResultBadge(selectedLog.result)}
                  </div>
                </div>
                <div>
                  <Label>风险等级</Label>
                  <div className="mt-1">
                    {getRiskLevelBadge(selectedLog.riskLevel)}
                  </div>
                </div>
                <div>
                  <Label>来源</Label>
                  <p className="text-sm text-gray-900">{selectedLog.source || '-'}</p>
                </div>
              </div>

              {/* 资源信息 */}
              {(selectedLog.resourceType || selectedLog.resourceId) && (
                <div>
                  <Label>资源信息</Label>
                  <div className="mt-2 grid grid-cols-2 gap-4">
                    <div>
                      <Label className="text-xs text-gray-500">资源类型</Label>
                      <p className="text-sm text-gray-900">{selectedLog.resourceType || '-'}</p>
                    </div>
                    <div>
                      <Label className="text-xs text-gray-500">资源ID</Label>
                      <p className="text-sm text-gray-900 break-all">{selectedLog.resourceId || '-'}</p>
                    </div>
                  </div>
                </div>
              )}

              {/* 网络信息 */}
              <div>
                <Label>网络信息</Label>
                <div className="mt-2 grid grid-cols-2 gap-4">
                  <div>
                    <Label className="text-xs text-gray-500">IP地址</Label>
                    <p className="text-sm text-gray-900">{selectedLog.ipAddress || '-'}</p>
                  </div>
                  <div>
                    <Label className="text-xs text-gray-500">会话ID</Label>
                    <p className="text-sm text-gray-900 break-all">{selectedLog.sessionId || '-'}</p>
                  </div>
                </div>
                {selectedLog.userAgent && (
                  <div className="mt-2">
                    <Label className="text-xs text-gray-500">用户代理</Label>
                    <p className="text-sm text-gray-900 break-all">{selectedLog.userAgent}</p>
                  </div>
                )}
              </div>

              {/* 描述 */}
              {selectedLog.description && (
                <div>
                  <Label>描述</Label>
                  <p className="text-sm text-gray-900 mt-1">{selectedLog.description}</p>
                </div>
              )}

              {/* 附加数据 */}
              {selectedLog.additionalData && (
                <div>
                  <Label>附加数据</Label>
                  <pre className="text-xs bg-gray-100 p-3 rounded-lg mt-1 overflow-x-auto">
                    {JSON.stringify(JSON.parse(selectedLog.additionalData), null, 2)}
                  </pre>
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default AuditLog;