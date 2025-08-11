import React, { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { authService } from '@/lib/auth';
import { useAuthStore } from '@/stores/auth';

export const AuthCallback: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { setUser } = useAuthStore();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handleCallback = async () => {
      try {
        await authService.handleCallback();
        const user = authService.getUser();
        if (user) {
          setUser(user);
        }
        
        // 获取重定向URL
        const returnUrl = sessionStorage.getItem('auth_return_url') || '/dashboard';
        sessionStorage.removeItem('auth_return_url');
        
        navigate(returnUrl, { replace: true });
      } catch (err) {
        console.error('Authentication callback failed:', err);
        setError('登录失败，请重试');
        
        setTimeout(() => {
          navigate('/login', { replace: true });
        }, 3000);
      }
    };

    handleCallback();
  }, [navigate, location, setUser]);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-red-600">登录失败</h1>
          <p className="text-gray-600 mt-2">{error}</p>
          <p className="text-sm text-gray-500 mt-2">3秒后将重定向到登录页面...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-gray-900 mx-auto"></div>
        <p className="mt-4 text-gray-600">正在处理登录...</p>
      </div>
    </div>
  );
};