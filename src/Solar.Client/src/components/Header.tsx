import { useState, useEffect } from 'react';
import type { UserProfile } from '../types/auth';

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
  const [clock, setClock] = useState(new Date().toLocaleTimeString('pt-BR'));
  const [showShortcuts, setShowShortcuts] = useState(false);

  useEffect(() => {
    const timer = setInterval(() => {
      setClock(new Date().toLocaleTimeString('pt-BR'));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

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

          <a href="#acessibilidade" className="topbar-nav-link" onClick={(e) => { e.preventDefault(); alert('Modo de Acessibilidade Ativo (WCAG 2.1 AA)'); }}>
            Acessibilidade
          </a>

          <a href="#ajuda" className="topbar-nav-link" onClick={(e) => { e.preventDefault(); alert('Central de Ajuda e Tutoriais do Solar LMS.'); }}>
            Ajuda
          </a>

          <button type="button" className="topbar-btn-sair" onClick={onLogout}>
            Sair
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
            HOME
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
            MENSAGENS
          </button>

          <button
            type="button"
            className={`solar-main-tab ${activeTabKey === 'enrollment' ? 'active' : ''}`}
            onClick={() => onSelectTab('enrollment')}
          >
            MATRÍCULA
          </button>

          <button
            type="button"
            className={`solar-main-tab ${activeTabKey === 'logs' ? 'active' : ''}`}
            onClick={() => onSelectTab('logs')}
            title="Dashboard de Observabilidade e Logs (Serilog)"
          >
            🔭 LOGS & TELEMETRIA
          </button>
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
              <div className="shortcuts-popup-modal">
                <div className="shortcuts-popup-header">
                  <strong>Atalhos</strong>
                  <span onClick={() => setShowShortcuts(false)} style={{ cursor: 'pointer' }}>✕</span>
                </div>
                <div className="shortcuts-popup-body">
                  <p style={{ fontSize: '0.8rem', color: '#666' }}>Sem atalhos cadastrados.</p>
                </div>
              </div>
            )}
          </div>

          <button type="button" className="btn-icon-action" title="Avaliar o Solar">
            👍
          </button>
          <button type="button" className="btn-icon-action" title="Dúvidas Frequentes">
            ❓
          </button>
        </div>
      </div>
    </header>
  );
};
