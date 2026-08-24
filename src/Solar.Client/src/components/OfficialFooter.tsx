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
  const [privacySearch, setPrivacySearch] = useState('');
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

            {/* Campo de Busca em Tempo Real (Espelha #search_policy do Ruby) */}
            <div style={{ padding: '12px 24px', background: '#f1f5f9', borderBottom: '1px solid #e2e8f0', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <div style={{ position: 'relative', width: '100%' }}>
                <input
                  type="text"
                  value={privacySearch}
                  onChange={(e) => setPrivacySearch(e.target.value)}
                  placeholder={t('privacy_search_placeholder')}
                  style={{
                    width: '100%',
                    padding: '8px 32px 8px 12px',
                    borderRadius: '6px',
                    border: '1px solid #cbd5e1',
                    fontSize: '0.85rem',
                    boxSizing: 'border-box'
                  }}
                  autoFocus
                />
                {privacySearch && (
                  <button
                    type="button"
                    onClick={() => setPrivacySearch('')}
                    style={{
                      position: 'absolute',
                      right: '8px',
                      top: '50%',
                      transform: 'translateY(-50%)',
                      background: 'none',
                      border: 'none',
                      color: '#64748b',
                      cursor: 'pointer',
                      fontSize: '0.85rem'
                    }}
                  >
                    ✕
                  </button>
                )}
              </div>
            </div>
            
            <div className="privacy-modal-body" style={{ maxHeight: '60vh', overflowY: 'auto' }}>
              {/* Seção 1 */}
              {(privacySearch === '' || [t('privacy_sec1_title'), t('privacy_sec1_p1'), t('privacy_sec1_p2')].some(s => s.toLowerCase().includes(privacySearch.toLowerCase()))) && (
                <section className="privacy-section">
                  <h4>{t('privacy_sec1_title')}</h4>
                  <p>{t('privacy_sec1_p1')}</p>
                  <p>{t('privacy_sec1_p2')}</p>
                  <p style={{ fontWeight: 600 }}>{t('privacy_sec1_p3')}</p>
                </section>
              )}

              {/* Seção 2 */}
              {(privacySearch === '' || [t('privacy_sec2_title'), t('privacy_sec2_intro'), t('privacy_sec2_item1'), t('privacy_sec2_item2'), t('privacy_sec2_item3'), t('privacy_sec2_item4')].some(s => s.toLowerCase().includes(privacySearch.toLowerCase()))) && (
                <section className="privacy-section">
                  <h4>{t('privacy_sec2_title')}</h4>
                  <p>{t('privacy_sec2_intro')}</p>
                  <ul style={{ paddingLeft: '20px', lineHeight: 1.6 }}>
                    <li style={{ marginBottom: '8px' }}>{t('privacy_sec2_item1')}</li>
                    <li style={{ marginBottom: '8px' }}>{t('privacy_sec2_item2')}</li>
                    <li style={{ marginBottom: '8px' }}>{t('privacy_sec2_item3')}</li>
                    <li style={{ marginBottom: '8px' }}>{t('privacy_sec2_item4')}</li>
                  </ul>
                </section>
              )}

              {/* Seção 3 */}
              {(privacySearch === '' || [t('privacy_sec3_title'), t('privacy_sec3_p1'), t('privacy_sec3_p2'), t('privacy_sec3_p3')].some(s => s.toLowerCase().includes(privacySearch.toLowerCase()))) && (
                <section className="privacy-section">
                  <h4>{t('privacy_sec3_title')}</h4>
                  <p>{t('privacy_sec3_p1')}</p>
                  <p>{t('privacy_sec3_p2')}</p>
                  <p>{t('privacy_sec3_p3')}</p>
                </section>
              )}

              {/* Seção 4 */}
              {(privacySearch === '' || [t('privacy_sec4_title'), t('privacy_sec4_p1'), t('privacy_sec4_p2'), t('privacy_sec4_p3'), t('privacy_sec4_p4'), t('privacy_sec4_p5'), t('privacy_sec4_p6'), t('privacy_sec4_p7')].some(s => s.toLowerCase().includes(privacySearch.toLowerCase()))) && (
                <section className="privacy-section">
                  <h4>{t('privacy_sec4_title')}</h4>
                  <p>{t('privacy_sec4_p1')}</p>
                  <p>{t('privacy_sec4_p2')}</p>
                  <p>{t('privacy_sec4_p3')}</p>
                  <p>{t('privacy_sec4_p4')}</p>
                  <p>{t('privacy_sec4_p5')}</p>
                  <p>{t('privacy_sec4_p6')}</p>
                  <p>{t('privacy_sec4_p7')}</p>
                </section>
              )}

              {/* Seção 5 */}
              {(privacySearch === '' || [t('privacy_sec5_title'), t('privacy_sec5_body')].some(s => s.toLowerCase().includes(privacySearch.toLowerCase()))) && (
                <section className="privacy-section">
                  <h4>{t('privacy_sec5_title')}</h4>
                  <p>{t('privacy_sec5_body')}</p>
                </section>
              )}

              {/* Seção 6 */}
              {(privacySearch === '' || [t('privacy_sec6_title'), t('privacy_sec6_p1'), t('privacy_sec6_p2'), t('privacy_sec6_p3')].some(s => s.toLowerCase().includes(privacySearch.toLowerCase()))) && (
                <section className="privacy-section">
                  <h4>{t('privacy_sec6_title')}</h4>
                  <p>{t('privacy_sec6_p1')}</p>
                  <p>{t('privacy_sec6_p2')}</p>
                  <p>{t('privacy_sec6_p3')}</p>
                </section>
              )}

              {/* Seção 7 */}
              {(privacySearch === '' || [t('privacy_sec7_title'), t('privacy_sec7_p1'), t('privacy_sec7_p2'), t('privacy_sec7_p3'), t('privacy_sec7_p4')].some(s => s.toLowerCase().includes(privacySearch.toLowerCase()))) && (
                <section className="privacy-section">
                  <h4>{t('privacy_sec7_title')}</h4>
                  <p>{t('privacy_sec7_p1')}</p>
                  <p>{t('privacy_sec7_p2')}</p>
                  <p>{t('privacy_sec7_p3')}</p>
                  <p>{t('privacy_sec7_p4')}</p>
                </section>
              )}

              {/* Seção 8 */}
              {(privacySearch === '' || [t('privacy_sec8_title'), t('privacy_sec8_item1'), t('privacy_sec8_item2'), t('privacy_sec8_item3'), t('privacy_sec8_item4'), t('privacy_sec8_item5'), t('privacy_sec8_item6'), t('privacy_sec8_item7')].some(s => s.toLowerCase().includes(privacySearch.toLowerCase()))) && (
                <section className="privacy-section" style={{ background: '#f8fafc', padding: '14px', borderRadius: '8px', border: '1px solid #e2e8f0' }}>
                  <h4 style={{ color: '#0369a1' }}>{t('privacy_sec8_title')}</h4>
                  <ul style={{ paddingLeft: '18px', fontSize: '0.82rem', margin: '6px 0 0 0' }}>
                    <li>{t('privacy_sec8_item1')}</li>
                    <li>{t('privacy_sec8_item2')}</li>
                    <li>{t('privacy_sec8_item3')}</li>
                    <li>{t('privacy_sec8_item4')}</li>
                    <li>{t('privacy_sec8_item5')}</li>
                    <li>{t('privacy_sec8_item6')}</li>
                    <li>{t('privacy_sec8_item7')}</li>
                  </ul>
                </section>
              )}

              {/* Seção 9 */}
              {(privacySearch === '' || [t('privacy_sec9_title'), t('privacy_sec9_body')].some(s => s.toLowerCase().includes(privacySearch.toLowerCase()))) && (
                <section className="privacy-section" style={{ marginTop: '14px', background: '#ecfdf5', padding: '12px 14px', borderRadius: '8px', border: '1px solid #a7f3d0' }}>
                  <h4 style={{ color: '#047857', margin: '0 0 4px 0' }}>{t('privacy_sec9_title')}</h4>
                  <p style={{ margin: 0, fontSize: '0.84rem', color: '#065f46' }}>{t('privacy_sec9_body')}</p>
                </section>
              )}
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
