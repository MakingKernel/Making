import { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';

// Components
import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import { AuthCallback } from '@/components/auth/AuthCallback';
import { AppLayout } from '@/components/layout/AppLayout';

// Pages
import { Login } from '@/pages/Login';
import { Dashboard } from '@/pages/Dashboard';
import { TenantSwitch } from '@/pages/TenantSwitch';

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

        {/* 404 fallback */}
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </Router>
  );
}

export default App;
