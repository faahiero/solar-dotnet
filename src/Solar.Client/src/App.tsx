import { useState, useEffect } from 'react';
import type { UserProfile } from './types/auth';
import type { CurriculumUnit } from './types/academic';
import { LoginScreen } from './components/LoginScreen';
import { Header } from './components/Header';
import { MySolarHome } from './components/MySolarHome';
import { CurriculumUnitView } from './components/CurriculumUnitView';
import { MessagesView } from './components/MessagesView';
import './index.css';

interface OpenCourseTab {
  id: number;
  name: string;
  semester: string;
  raw: CurriculumUnit;
}

export function App() {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [activeTabKey, setActiveTabKey] = useState<string>('home');
  const [openCourses, setOpenCourses] = useState<OpenCourseTab[]>([]);

  useEffect(() => {
    const savedToken = localStorage.getItem('solar_session_token');
    const savedUser = localStorage.getItem('solar_user');
    if (savedToken && savedUser) {
      try {
        setUser(JSON.parse(savedUser));
      } catch {
        localStorage.removeItem('solar_session_token');
        localStorage.removeItem('solar_user');
      }
    }
  }, []);

  const handleLoginSuccess = (authenticatedUser: UserProfile, token: string) => {
    localStorage.setItem('solar_session_token', token);
    localStorage.setItem('solar_user', JSON.stringify(authenticatedUser));
    setUser(authenticatedUser);
    setActiveTabKey('home');
  };

  const handleLogout = () => {
    localStorage.removeItem('solar_session_token');
    localStorage.removeItem('solar_user');
    setUser(null);
    setOpenCourses([]);
    setActiveTabKey('home');
  };

  const handleOpenCurriculumUnit = (cu: CurriculumUnit) => {
    const exists = openCourses.find((c) => c.id === cu.id);
    if (!exists) {
      setOpenCourses((prev) => [
        ...prev,
        { id: cu.id, name: cu.name, semester: cu.semester, raw: cu }
      ]);
    }
    setActiveTabKey(`cu_${cu.id}`);
  };

  const handleCloseCurriculumUnitTab = (id: number) => {
    setOpenCourses((prev) => prev.filter((c) => c.id !== id));
    if (activeTabKey === `cu_${id}`) {
      setActiveTabKey('home');
    }
  };

  if (!user) {
    return <LoginScreen onLoginSuccess={handleLoginSuccess} />;
  }

  const activeCourse = openCourses.find((c) => `cu_${c.id}` === activeTabKey);

  return (
    <div className="solar-app-viewport">
      {/* 1. Header Oficial (TopBar + Tabs Multi-Turmas + Atalhos) */}
      <Header
        user={user}
        activeTabKey={activeTabKey}
        openCurriculumUnits={openCourses}
        onSelectTab={(tabKey) => setActiveTabKey(tabKey)}
        onCloseCurriculumUnitTab={handleCloseCurriculumUnitTab}
        onLogout={handleLogout}
      />

      {/* 2. Área de Conteúdo Principal Dinâmica */}
      <main className="solar-page-content">
        {activeTabKey === 'home' && (
          <MySolarHome onOpenCurriculumUnit={handleOpenCurriculumUnit} />
        )}

        {activeCourse && (
          <CurriculumUnitView
            curriculumUnit={activeCourse.raw}
            user={user}
            onNavigateHome={() => setActiveTabKey('home')}
          />
        )}

        {activeTabKey === 'messages' && (
          <MessagesView />
        )}

        {activeTabKey === 'enrollment' && (
          <div className="solar-portlet-card" style={{ padding: '24px', textAlign: 'center' }}>
            <h2 style={{ fontSize: '1.2rem', color: 'var(--solar-blue-dark)', marginBottom: '8px' }}>
              Módulo de Matrícula Institucional (SIGAA Integrado)
            </h2>
            <p style={{ fontSize: '0.9rem', color: '#555' }}>
              O período de solicitação de matrículas e ajuste de turmas para o semestre 2026.1 está regular.
            </p>
          </div>
        )}
      </main>

      {/* 3. Rodapé Oficial Solar LMS */}
      <footer className="solar-official-footer">
        <div className="footer-links-row">
          <a href="#portais">Portais ▲</a>
          <a href="#desenvolvimento">Desenvolvimento ▲</a>
          <a href="#privacidade">Política de privacidade</a>
          <a href="#faq">FAQ</a>
          <a href="#idioma">Idioma ▲</a>
        </div>
      </footer>

      {/* 4. Widget Flutuante VLibras */}
      <div className="vlibras-badge">
        <span style={{ fontSize: '0.75rem', fontWeight: 600 }}>Acessível com<br /><strong>VLibras</strong></span>
        <div className="vlibras-hand-icon">🤟</div>
      </div>
    </div>
  );
}

export default App;
