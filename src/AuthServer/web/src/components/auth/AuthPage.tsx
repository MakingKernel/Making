import React, { useEffect, useState } from 'react';
import { authStore, AuthErrorType } from '../../lib/auth-store';
import type { ExtendedAuthState, ExternalProviderInfo } from '../../lib/auth-store';

/**
 * 认证页面组件
 * 演示完整的 OIDC 认证流程
 */
export const AuthPage: React.FC = () => {
  const [authState, setAuthState] = useState<ExtendedAuthState>(authStore.getState());
  const [externalProviders, setExternalProviders] = useState<ExternalProviderInfo[]>([]);
  const [showErrorDetails, setShowErrorDetails] = useState(false);

  useEffect(() => {
    // 订阅认证状态变化
    const unsubscribe = authStore.subscribe(setAuthState);

    // 加载外部登录提供商
    loadExternalProviders();

    // 检查是否为回调页面
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('code') || urlParams.get('error')) {
      handleAuthCallback();
    }

    return unsubscribe;
  }, []);

  const loadExternalProviders = async () => {
    try {
      const providers = await authStore.getExternalProviders();
      setExternalProviders(providers);
    } catch (error) {
      console.error('加载外部提供商失败:', error);
    }
  };

  const handleAuthCallback = async () => {
    try {
      await authStore.handleCallback();
    } catch (error) {
      console.error('处理认证回调失败:', error);
    }
  };

  const handleLogin = async () => {
    try {
      if (authStore.isLocked()) {
        alert('登录尝试次数过多，请稍后再试');
        return;
      }
      
      await authStore.login();
    } catch (error) {
      console.error('登录失败:', error);
    }
  };

  const handleLogout = async () => {
    try {
      await authStore.logout();
    } catch (error) {
      console.error('注销失败:', error);
    }
  };

  const handleExtendSession = async () => {
    try {
      const success = await authStore.extendSession();
      if (success) {
        alert('会话已延长');
      } else {
        alert('会话延长失败');
      }
    } catch (error) {
      console.error('延长会话失败:', error);
      alert('延长会话失败');
    }
  };

  const handleClearErrors = () => {
    authStore.clearErrorHistory();
    setShowErrorDetails(false);
  };

  const formatTimeRemaining = (milliseconds: number): string => {
    const minutes = Math.floor(milliseconds / 60000);
    const seconds = Math.floor((milliseconds % 60000) / 1000);
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

  const getErrorTypeColor = (type: AuthErrorType): string => {
    switch (type) {
      case AuthErrorType.NETWORK_ERROR:
        return '#ff9800'; // orange
      case AuthErrorType.INVALID_CREDENTIALS:
        return '#f44336'; // red
      case AuthErrorType.TOKEN_EXPIRED:
        return '#2196f3'; // blue
      case AuthErrorType.REFRESH_FAILED:
        return '#9c27b0'; // purple
      default:
        return '#757575'; // gray
    }
  };

  if (authState.isLoading) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <div>正在加载...</div>
        <div style={{ 
          width: '40px', 
          height: '40px', 
          border: '4px solid #f3f3f3',
          borderTop: '4px solid #3498db',
          borderRadius: '50%',
          animation: 'spin 1s linear infinite',
          margin: '20px auto'
        }} />
        <style>{`
          @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
          }
        `}</style>
      </div>
    );
  }

  return (
    <div style={{ 
      maxWidth: '800px', 
      margin: '0 auto', 
      padding: '20px',
      fontFamily: 'Arial, sans-serif'
    }}>
      <h1>企业级 OIDC 认证演示</h1>
      
      {/* 认证状态卡片 */}
      <div style={{
        border: `2px solid ${authState.isAuthenticated ? '#4caf50' : '#f44336'}`,
        borderRadius: '8px',
        padding: '20px',
        marginBottom: '20px',
        backgroundColor: authState.isAuthenticated ? '#f1f8e9' : '#ffebee'
      }}>
        <h2>认证状态</h2>
        <div><strong>已认证:</strong> {authState.isAuthenticated ? '是' : '否'}</div>
        
        {authState.isAuthenticated && authState.user && (
          <div style={{ marginTop: '10px' }}>
            <div><strong>用户ID:</strong> {authState.user.sub}</div>
            <div><strong>邮箱:</strong> {authState.user.email}</div>
            <div><strong>姓名:</strong> {authState.user.name}</div>
            {authState.user.picture && (
              <img 
                src={authState.user.picture} 
                alt="头像" 
                style={{ width: '50px', height: '50px', borderRadius: '50%', marginTop: '10px' }}
              />
            )}
          </div>
        )}

        {authState.expiresAt && (
          <div style={{ marginTop: '10px' }}>
            <div><strong>会话过期:</strong> {new Date(authState.expiresAt).toLocaleString()}</div>
            <div><strong>剩余时间:</strong> {formatTimeRemaining(authStore.getSessionTimeRemaining())}</div>
          </div>
        )}

        {authState.sessionWarning && (
          <div style={{ 
            backgroundColor: '#fff3cd', 
            border: '1px solid #ffecb5',
            borderRadius: '4px',
            padding: '10px',
            marginTop: '10px',
            color: '#856404'
          }}>
            ⚠️ 会话即将过期，请延长会话或重新登录
            <button 
              onClick={handleExtendSession}
              style={{ 
                marginLeft: '10px',
                padding: '5px 10px',
                backgroundColor: '#ffc107',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer'
              }}
            >
              延长会话
            </button>
          </div>
        )}
      </div>

      {/* 操作按钮 */}
      <div style={{ marginBottom: '20px' }}>
        {!authState.isAuthenticated ? (
          <div>
            <button
              onClick={handleLogin}
              disabled={authStore.isLocked()}
              style={{
                padding: '10px 20px',
                fontSize: '16px',
                backgroundColor: authStore.isLocked() ? '#ccc' : '#2196f3',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: authStore.isLocked() ? 'not-allowed' : 'pointer',
                marginRight: '10px'
              }}
            >
              {authStore.isLocked() ? '已锁定' : '登录'}
            </button>

            {authStore.isLocked() && (
              <button
                onClick={() => authStore.resetLoginAttempts()}
                style={{
                  padding: '10px 20px',
                  fontSize: '16px',
                  backgroundColor: '#ff9800',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer'
                }}
              >
                重置锁定
              </button>
            )}
          </div>
        ) : (
          <button
            onClick={handleLogout}
            style={{
              padding: '10px 20px',
              fontSize: '16px',
              backgroundColor: '#f44336',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer'
            }}
          >
            注销
          </button>
        )}
      </div>

      {/* 外部登录提供商 */}
      {!authState.isAuthenticated && externalProviders.length > 0 && (
        <div style={{ marginBottom: '20px' }}>
          <h3>外部登录</h3>
          <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
            {externalProviders.map(provider => (
              <button
                key={provider.name}
                onClick={() => authStore.startExternalLogin(provider.provider)}
                style={{
                  padding: '8px 16px',
                  backgroundColor: '#4caf50',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer'
                }}
              >
                {provider.displayName}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* 错误信息 */}
      {(authState.error || authState.errorHistory.length > 0) && (
        <div style={{
          border: '1px solid #f44336',
          borderRadius: '8px',
          padding: '15px',
          marginBottom: '20px',
          backgroundColor: '#ffebee'
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 style={{ margin: 0, color: '#d32f2f' }}>错误信息</h3>
            <div>
              <button
                onClick={() => setShowErrorDetails(!showErrorDetails)}
                style={{
                  padding: '4px 8px',
                  fontSize: '12px',
                  backgroundColor: '#1976d2',
                  color: 'white',
                  border: 'none',
                  borderRadius: '3px',
                  cursor: 'pointer',
                  marginRight: '5px'
                }}
              >
                {showErrorDetails ? '隐藏详情' : '显示详情'}
              </button>
              <button
                onClick={handleClearErrors}
                style={{
                  padding: '4px 8px',
                  fontSize: '12px',
                  backgroundColor: '#757575',
                  color: 'white',
                  border: 'none',
                  borderRadius: '3px',
                  cursor: 'pointer'
                }}
              >
                清除
              </button>
            </div>
          </div>

          {authState.error && (
            <div style={{ marginTop: '10px', color: '#d32f2f' }}>
              <strong>当前错误:</strong> {authState.error}
            </div>
          )}

          {showErrorDetails && authState.errorHistory.length > 0 && (
            <div style={{ marginTop: '10px' }}>
              <strong>错误历史:</strong>
              <div style={{ maxHeight: '200px', overflowY: 'auto', marginTop: '5px' }}>
                {authState.errorHistory.map((error, index) => (
                  <div
                    key={index}
                    style={{
                      padding: '8px',
                      marginBottom: '5px',
                      backgroundColor: 'white',
                      borderLeft: `4px solid ${getErrorTypeColor(error.type)}`,
                      fontSize: '14px'
                    }}
                  >
                    <div style={{ fontWeight: 'bold', color: getErrorTypeColor(error.type) }}>
                      {error.type}
                    </div>
                    <div>{error.message}</div>
                    <div style={{ fontSize: '12px', color: '#666', marginTop: '4px' }}>
                      {new Date(error.timestamp).toLocaleString()}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* 调试信息 */}
      <div style={{
        border: '1px solid #ccc',
        borderRadius: '8px',
        padding: '15px',
        backgroundColor: '#f9f9f9'
      }}>
        <h3>调试信息</h3>
        <div><strong>登录尝试次数:</strong> {authState.loginAttempts}</div>
        <div><strong>最后活动时间:</strong> {new Date(authState.lastActivity).toLocaleString()}</div>
        <div><strong>会话警告:</strong> {authState.sessionWarning ? '是' : '否'}</div>
        <div><strong>错误历史数量:</strong> {authState.errorHistory.length}</div>
        
        <details style={{ marginTop: '10px' }}>
          <summary style={{ cursor: 'pointer', fontWeight: 'bold' }}>完整状态 (JSON)</summary>
          <pre style={{ 
            backgroundColor: 'white',
            padding: '10px',
            borderRadius: '4px',
            fontSize: '12px',
            overflow: 'auto',
            marginTop: '10px'
          }}>
            {JSON.stringify({
              ...authState,
              accessToken: authState.accessToken ? '[REDACTED]' : null,
              refreshToken: authState.refreshToken ? '[REDACTED]' : null
            }, null, 2)}
          </pre>
        </details>
      </div>
    </div>
  );
};