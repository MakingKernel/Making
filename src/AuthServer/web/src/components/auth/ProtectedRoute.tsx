import React, { useEffect } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRoles?: string[];
  requireAdmin?: boolean;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requiredRoles = [],
  requireAdmin = false 
}) => {
  const { isAuthenticated, isLoading, user, checkAuth } = useAuthStore();
  const location = useLocation();

  useEffect(() => {
    if (!isAuthenticated && !isLoading) {
      checkAuth();
    }
  }, [isAuthenticated, isLoading, checkAuth]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-gray-900"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // 检查管理员权限
  if (requireAdmin && user) {
    if (!user.isAdmin) {
      return (
        <div className="min-h-screen flex items-center justify-center">
          <div className="text-center">
            <h1 className="text-2xl font-bold text-red-600">访问被拒绝</h1>
            <p className="text-gray-600 mt-2">您需要管理员权限才能访问此页面</p>
            <button 
              onClick={() => window.history.back()}
              className="mt-4 px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
            >
              返回
            </button>
          </div>
        </div>
      );
    }
  }

  // 检查角色权限
  if (requiredRoles.length > 0 && user) {
    const userRoles = user.roles || [];
    const hasRequiredRole = requiredRoles.some(role => 
      userRoles.includes(role)
    );
    
    if (!hasRequiredRole) {
      return (
        <div className="min-h-screen flex items-center justify-center">
          <div className="text-center">
            <h1 className="text-2xl font-bold text-red-600">访问被拒绝</h1>
            <p className="text-gray-600 mt-2">您没有足够的权限访问此页面</p>
            <button 
              onClick={() => window.history.back()}
              className="mt-4 px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
            >
              返回
            </button>
          </div>
        </div>
      );
    }
  }

  return <>{children}</>;
};