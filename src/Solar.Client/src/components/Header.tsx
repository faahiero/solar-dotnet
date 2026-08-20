import { useState, useEffect } from 'react';
import type { UserProfile } from '../types/auth';
import { isAdminUser } from '../types/auth';
import { useTranslation } from '../context/LanguageContext';

interface HeaderProps {
  user: UserProfile;
  activeTabKey: string;
  openCurriculumUnits: { id: number; name: string; semester: string }[];
  onSelectTab: (tabKey: string) => void;
  onCloseCurriculumUnitTab: (id: number) => void;
  onLogout: () => void;
}

export const Header = ({
  user,
  activeTabKey,
  openCurriculumUnits,
  onSelectTab,
  onCloseCurriculumUnitTab,
  onLogout
}: HeaderProps) => {
  const { t, formatTime } = useTranslation();
  const [clock, setClock] = useState(formatTime(new Date()));
  const [showShortcuts, setShowShortcuts] = useState(false);

  useEffect(() => {
    const timer = setInterval(() => {
      setClock(formatTime(new Date()));
    }, 1000);
    return () => clearInterval(timer);
  }, [formatTime]);

  return (
    <header className="solar-official-header">
      {/* 1. Topbar Azul Escuro */}
      <div className="solar-topbar-blue">
        <div className="solar-topbar-logo" onClick={() => onSelectTab('home')} style={{ cursor: 'pointer' }}>
          <img src="/assets/images/solar_logo_small_cursos.png" alt="Solar Cursos" style={{ height: '32px' }} />
        </div>

        <div className="solar-topbar-right">
          <div className="user-nick-box">
            <div className="user-avatar-circle">👤</div>
            <span className="user-nick-name">{user.name || user.username}</span>
          </div>

          <span className="server-clock-text">{clock}</span>

          <a href="#acessibilidade" className="topbar-nav-link" onClick={(e) => { e.preventDefault(); alert(t('topbar_accessibility_active')); }}>
            {t('topbar_accessibility')}
          </a>

          <a href="#ajuda" className="topbar-nav-link" onClick={(e) => { e.preventDefault(); alert('Central de Ajuda e Tutoriais do Solar LMS.'); }}>
            {t('topbar_help')}
          </a>

          <button type="button" className="topbar-btn-sair" onClick={onLogout}>
            {t('topbar_logout')}
          </button>
        </div>
      </div>

      {/* 2. Barra de Abas (Home | Turmas Abertas ✖ | Mensagens | Matrícula) */}
      <div className="solar-tabs-bar">
        <div className="solar-tabs-group">
          <button
            type="button"
            className={`solar-main-tab ${activeTabKey === 'home' ? 'active' : ''}`}
            onClick={() => onSelectTab('home')}
          >
            {t('nav_home').toUpperCase()}
          </button>

          {openCurriculumUnits.map((cu) => (
            <div
              key={cu.id}
              className={`solar-main-tab closable-tab ${activeTabKey === `cu_${cu.id}` ? 'active' : ''}`}
              onClick={() => onSelectTab(`cu_${cu.id}`)}
            >
              <span>{cu.name.toUpperCase()} - {cu.semester}</span>
              <button
                type="button"
                className="close-tab-btn"
                title="Fechar aba da disciplina"
                onClick={(e) => {
                  e.stopPropagation();
                  onCloseCurriculumUnitTab(cu.id);
                }}
              >
                ✕
              </button>
            </div>
          ))}

          <button
            type="button"
            className={`solar-main-tab ${activeTabKey === 'messages' ? 'active' : ''}`}
            onClick={() => onSelectTab('messages')}
          >
            {t('nav_messages').toUpperCase()}
          </button>

          <button
            type="button"
            className={`solar-main-tab ${activeTabKey === 'enrollment' ? 'active' : ''}`}
            onClick={() => onSelectTab('enrollment')}
          >
            {t('nav_enrollment').toUpperCase()}
          </button>

          {isAdminUser(user) && (
            <button
              type="button"
              className={`solar-main-tab ${activeTabKey === 'logs' ? 'active' : ''}`}
              onClick={() => onSelectTab('logs')}
              title="Dashboard de Observabilidade e Logs (Apenas Administradores)"
              style={{ backgroundColor: activeTabKey === 'logs' ? 'var(--solar-blue-mid, #005a9c)' : '#0f172a', color: '#38bdf8' }}
            >
              🔭 {t('nav_logs').toUpperCase()} (ADMIN)
            </button>
          )}
        </div>

        <div className="solar-top-actions">
          <div className="shortcuts-dropdown-container">
            <button
              type="button"
              className="btn-shortcuts"
              onClick={() => setShowShortcuts(!showShortcuts)}
            >
              Atalhos ▼
            </button>
            {showShortcuts && (
              <div className="shortcuts-menu-dropdown">
                <a href="#sigaa" onClick={(e) => { e.preventDefault(); alert('Redirecionando para o Portal SIGAA UFC.'); }}>
                  🔗 Portal SIGAA
                </a>
                <a href="#biblioteca" onClick={(e) => { e.preventDefault(); alert('Redirecionando para o Sistema de Bibliotecas UFC (Pergamum).'); }}>
                  📚 Sistema de Bibliotecas
                </a>
                <a href="#certificados" onClick={(e) => { e.preventDefault(); alert('Consulta de Certificados de Extensão.'); }}>
                  📜 Validar Certificado
                </a>
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  );
};
