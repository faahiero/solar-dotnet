import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { LanguageProvider } from './context/LanguageContext'
import { GlobalErrorBoundary } from './components/GlobalErrorBoundary'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <GlobalErrorBoundary>
      <LanguageProvider>
        <App />
      </LanguageProvider>
    </GlobalErrorBoundary>
  </StrictMode>,
)
