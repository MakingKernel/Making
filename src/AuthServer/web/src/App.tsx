import { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';

// Components
import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import { AuthCallback } from '@/components/auth/AuthCallback';
import { AppLayout } from '@/components/layout/AppLayout';
import AdminLayout from '@/components/layout/AdminLayout';

// Pages
import { Login } from '@/pages/Login';
import { Dashboard } from '@/pages/Dashboard';
import { TenantSwitch } from '@/pages/TenantSwitch';

// Admin Pages
import AdminDashboard from '@/pages/admin/AdminDashboard';
import UserManagement from '@/pages/admin/UserManagement';
import AuditLog from '@/pages/admin/AuditLog';

function App() {
  const { checkAuth } = useAuthStore();

  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  return (
    <Router>
      <Routes>
        {/* Public routes */}
        <Route path="/login" element={<Login />} />
        <Route path="/callback" element={<AuthCallback />} />
        
        {/* Protected routes */}
        <Route path="/" element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<Dashboard />} />
          <Route path="tenant-switch" element={<TenantSwitch />} />
          <Route path="profile" element={<div>个人资料页面</div>} />
          <Route path="settings" element={<div>设置页面</div>} />
        </Route>

        {/* Admin routes */}
        <Route path="/admin" element={
          <ProtectedRoute requireAdmin={true}>
            <AdminLayout />
          </ProtectedRoute>
        }>
          <Route index element={<Navigate to="/admin/dashboard" replace />} />
          <Route path="dashboard" element={<AdminDashboard />} />
          <Route path="users" element={<UserManagement />} />
          <Route path="audit" element={<AuditLog />} />
          <Route path="roles" element={<div>角色管理页面</div>} />
          <Route path="clients" element={<div>客户端管理页面</div>} />
          <Route path="monitor" element={<div>系统监控页面</div>} />
          <Route path="settings" element={<div>系统设置页面</div>} />
        </Route>

        {/* 404 fallback */}
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </Router>
  );
}

export default App;
