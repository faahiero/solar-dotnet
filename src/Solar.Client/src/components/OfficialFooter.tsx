import { useState, useEffect, useRef } from 'react';
import { useTranslation } from '../context/LanguageContext';
import type { SupportedLanguage } from '../context/LanguageContext';

interface OfficialFooterProps {
  variant?: 'login' | 'app';
}

export const OfficialFooter = ({ variant = 'login' }: OfficialFooterProps) => {
  const { language, setLanguage, t } = useTranslation();
  const [activeMenu, setActiveMenu] = useState<'portais' | 'desenvolvimento' | 'ajuda' | 'idioma' | null>(null);
  const [showPrivacyModal, setShowPrivacyModal] = useState(false);
  const footerRef = useRef<HTMLElement>(null);

  // Fecha menus ao clicar fora
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (footerRef.current && !footerRef.current.contains(event.target as Node)) {
        setActiveMenu(null);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const toggleMenu = (menu: 'portais' | 'desenvolvimento' | 'ajuda' | 'idioma') => {
    setActiveMenu((prev) => (prev === menu ? null : menu));
  };

  const handleSelectLanguage = (lang: SupportedLanguage) => {
    setLanguage(lang);
    setActiveMenu(null);
  };

  return (
    <>
      <footer
        ref={footerRef}
        className={variant === 'login' ? 'login-official-footer' : 'solar-official-footer'}
      >
        <div className="footer-links-row">
          
          {/* 1. Portais ▲ */}
          <div className="footer-dropup-container">
            <button
              type="button"
              className={`footer-dropup-trigger ${activeMenu === 'portais' ? 'active' : ''}`}
              onClick={() => toggleMenu('portais')}
              aria-label={t('footer_portals')}
            >
              {t('footer_portals')}
            </button>
            {activeMenu === 'portais' && (
              <div className="footer-dropup-menu">
                <a
                  href="https://www.virtual.ufc.br"
                  target="_blank"
                  rel="noreferrer"
                  className="footer-dropup-item"
                  onClick={() => setActiveMenu(null)}
                >
                  <span>🌐 {t('footer_portal_virtual')}</span>
                  <span className="external-link-arrow">↗</span>
                </a>
                <a
                  href="https://www.ufc.br"
                  target="_blank"
                  rel="noreferrer"
                  className="footer-dropup-item"
                  onClick={() => setActiveMenu(null)}
                >
                  <span>🏛️ {t('footer_portal_ufc')}</span>
                  <span className="external-link-arrow">↗</span>
                </a>
              </div>
            )}
          </div>

          {/* 2. Desenvolvimento ▲ */}
          <div className="footer-dropup-container">
            <button
              type="button"
              className={`footer-dropup-trigger ${activeMenu === 'desenvolvimento' ? 'active' : ''}`}
              onClick={() => toggleMenu('desenvolvimento')}
              aria-label={t('footer_development')}
            >
              {t('footer_development')}
            </button>
            {activeMenu === 'desenvolvimento' && (
              <div className="footer-dropup-menu">
                <a
                  href="https://github.com/ufcvirtual/solar"
                  target="_blank"
                  rel="noreferrer"
                  className="footer-dropup-item"
                  onClick={() => setActiveMenu(null)}
                >
                  <span>💻 {t('footer_dev_code')}</span>
                  <span className="external-link-arrow">↗</span>
                </a>
                <a
                  href="https://github.com/ufcvirtual/solar/blob/master/README.md"
                  target="_blank"
                  rel="noreferrer"
                  className="footer-dropup-item"
                  onClick={() => setActiveMenu(null)}
                >
                  <span>👥 {t('footer_dev_team')}</span>
                  <span className="external-link-arrow">↗</span>
                </a>
                <a
                  href="https://github.com/ufcvirtual/solar/blob/master/GPLv3"
                  target="_blank"
                  rel="noreferrer"
                  className="footer-dropup-item"
                  onClick={() => setActiveMenu(null)}
                >
                  <span>📜 {t('footer_dev_license')}</span>
                  <span className="external-link-arrow">↗</span>
                </a>
              </div>
            )}
          </div>

          {/* 3. Política de privacidade */}
          <button
            type="button"
            className="footer-dropup-trigger"
            onClick={() => { setShowPrivacyModal(true); setActiveMenu(null); }}
            aria-label={t('footer_privacy_policy')}
          >
            {t('footer_privacy_policy')}
          </button>

          {/* 4. Ajuda ▲ */}
          <div className="footer-dropup-container">
            <button
              type="button"
              className={`footer-dropup-trigger ${activeMenu === 'ajuda' ? 'active' : ''}`}
              onClick={() => toggleMenu('ajuda')}
              aria-label={t('footer_help')}
            >
              {t('footer_help')}
            </button>
            {activeMenu === 'ajuda' && (
              <div className="footer-dropup-menu">
                <a
                  href="https://solar.virtual.ufc.br/faq"
                  target="_blank"
                  rel="noreferrer"
                  className="footer-dropup-item"
                  onClick={() => setActiveMenu(null)}
                >
                  <span>❓ {t('footer_help_faq')}</span>
                  <span className="external-link-arrow">↗</span>
                </a>
                <a
                  href="https://www.youtube.com/@ufcvirtual"
                  target="_blank"
                  rel="noreferrer"
                  className="footer-dropup-item"
                  onClick={() => setActiveMenu(null)}
                >
                  <span>🎬 {t('footer_help_videos')}</span>
                  <span className="external-link-arrow">↗</span>
                </a>
                <a
                  href="https://virtual.ufc.br/tutoriais"
                  target="_blank"
                  rel="noreferrer"
                  className="footer-dropup-item"
                  onClick={() => setActiveMenu(null)}
                >
                  <span>📖 {t('footer_help_manuals')}</span>
                  <span className="external-link-arrow">↗</span>
                </a>
              </div>
            )}
          </div>

          {/* 5. Idioma ▲ */}
          <div className="footer-dropup-container">
            <button
              type="button"
              className={`footer-dropup-trigger ${activeMenu === 'idioma' ? 'active' : ''}`}
              onClick={() => toggleMenu('idioma')}
              aria-label={t('footer_language')}
            >
              {t('footer_language')}
            </button>
            {activeMenu === 'idioma' && (
              <div className="footer-dropup-menu">
                <button
                  type="button"
                  className={`footer-dropup-item ${language === 'pt_BR' ? 'selected' : ''}`}
                  onClick={() => handleSelectLanguage('pt_BR')}
                >
                  <span>🇧🇷 {t('footer_lang_pt')}</span>
                  {language === 'pt_BR' && <span style={{ color: '#16a34a', fontWeight: 700 }}>✔</span>}
                </button>
                <button
                  type="button"
                  className={`footer-dropup-item ${language === 'en_US' ? 'selected' : ''}`}
                  onClick={() => handleSelectLanguage('en_US')}
                >
                  <span>🇺🇸 {t('footer_lang_en')}</span>
                  {language === 'en_US' && <span style={{ color: '#16a34a', fontWeight: 700 }}>✔</span>}
                </button>
              </div>
            )}
          </div>

        </div>
      </footer>

      {/* Modal de Política de Privacidade */}
      {showPrivacyModal && (
        <div className="modal-backdrop-custom" onClick={() => setShowPrivacyModal(false)}>
          <div className="privacy-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="privacy-modal-header">
              <h3 style={{ margin: 0, fontSize: '1.15rem', color: '#002b49', display: 'flex', alignItems: 'center', gap: '8px' }}>
                📜 {t('privacy_title')}
              </h3>
              <button
                type="button"
                className="btn-close-modal"
                onClick={() => setShowPrivacyModal(false)}
                aria-label="Fechar modal"
              >
                ✕
              </button>
            </div>
            
            <div className="privacy-modal-body">
              <section className="privacy-section">
                <h4>{t('privacy_sec1_title')}</h4>
                <p>{t('privacy_sec1_body')}</p>
              </section>

              <section className="privacy-section">
                <h4>{t('privacy_sec2_title')}</h4>
                <p>{t('privacy_sec2_item1')}</p>
                <ul>
                  <li>{t('privacy_sec2_item2')}</li>
                  <li>{t('privacy_sec2_item3')}</li>
                  <li>{t('privacy_sec2_item4')}</li>
                </ul>
              </section>

              <section className="privacy-section">
                <h4>{t('privacy_sec3_title')}</h4>
                <p>{t('privacy_sec3_body')}</p>
              </section>

              <section className="privacy-section">
                <h4>{t('privacy_sec4_title')}</h4>
                <p>{t('privacy_sec4_body')}</p>
              </section>
            </div>

            <div className="privacy-modal-footer">
              <button
                type="button"
                className="btn-privacy-confirm"
                onClick={() => setShowPrivacyModal(false)}
              >
                {t('privacy_btn_close')}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};
