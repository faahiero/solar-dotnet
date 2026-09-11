import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { LanguageProvider } from './context/LanguageContext'
import { GlobalErrorBoundary } from './components/GlobalErrorBoundary'

// Interceptor global para garantir envio de token Bearer e credentials em chamadas à API
const originalFetch = window.fetch;
window.fetch = async (input: RequestInfo | URL, init?: RequestInit) => {
  const url = typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;
  if (url.startsWith('/api/')) {
    const token = typeof localStorage !== 'undefined' ? localStorage.getItem('solar_session_token') : null;
    const headers = new Headers(init?.headers);
    if (token && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${token}`);
    }
    const modifiedInit: RequestInit = {
      ...init,
      headers,
      credentials: init?.credentials || 'include',
    };
    return originalFetch(input, modifiedInit);
  }
  return originalFetch(input, init);
};

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <GlobalErrorBoundary>
      <LanguageProvider>
        <App />
      </LanguageProvider>
    </GlobalErrorBoundary>
  </StrictMode>,
)
