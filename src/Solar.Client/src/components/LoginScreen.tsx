import { useState } from 'react';
import type { FormEvent } from 'react';
import type { UserProfile, LoginResponse, VerifyCpfResponse } from '../types/auth';
import { OfficialFooter } from './OfficialFooter';
import { RegistrationWizard } from './RegistrationWizard';
import { PasswordRecoveryModal } from './PasswordRecoveryModal';
import { useTranslation } from '../context/LanguageContext';

interface LoginScreenProps {
  onLoginSuccess: (user: UserProfile, token: string) => void;
}

export const LoginScreen = ({ onLoginSuccess }: LoginScreenProps) => {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<'signin' | 'signup'>('signin');
  const [login, setLogin] = useState('');
  const [password, setPassword] = useState('');
  const [registerCpf, setRegisterCpf] = useState('');
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [cpfResult, setCpfResult] = useState<VerifyCpfResponse | null>(null);
  const [showWizard, setShowWizard] = useState(false);
  const [showPasswordRecovery, setShowPasswordRecovery] = useState(false);

  // Estado para importação rápida SIGAA
  const [sigaaPassword, setSigaaPassword] = useState('');
  const [sigaaPasswordConf, setSigaaPasswordConf] = useState('');

  const handleLogin = async (e: FormEvent) => {
    e.preventDefault();
    if (!login || !password) {
      setErrorMessage(t('login_error_required'));
      return;
    }

    setLoading(true);
    setErrorMessage(null);

    try {
      const response = await fetch('/api/v1/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ login: login.trim(), password })
      });

      const data: LoginResponse = await response.json();

      if (!response.ok) {
        setErrorMessage(data.message || t('login_error_invalid'));
        return;
      }

      if (data.success && data.user && data.token) {
        onLoginSuccess(data.user, data.token);
      } else {
        setErrorMessage(data.message || t('login_error_invalid'));
      }
    } catch {
      setErrorMessage(t('login_error_invalid'));
    } finally {
      setLoading(false);
    }
  };

  // Formatação automática de CPF (000.000.000-00) idêntica à de esqueci senha
  const handleRegisterCpfChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value;
    const numbers = raw.replace(/\D/g, '').slice(0, 11);
    let formatted = numbers;
    if (numbers.length > 9) {
      formatted = `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6, 9)}-${numbers.slice(9)}`;
    } else if (numbers.length > 6) {
      formatted = `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6)}`;
    } else if (numbers.length > 3) {
      formatted = `${numbers.slice(0, 3)}.${numbers.slice(3)}`;
    }
    setRegisterCpf(formatted);
  };

  const handleVerifyCpf = async (e: FormEvent) => {
    e.preventDefault();
    if (!registerCpf) {
      alert(t('cpf_required'));
      return;
    }

    setLoading(true);
    setErrorMessage(null);
    setCpfResult(null);

    try {
      const response = await fetch('/api/v1/auth/verify-cpf', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ cpf: registerCpf.trim() })
      });
      const data: VerifyCpfResponse = await response.json();
      
      if (data.existsInLocal || data.existsInSigaa) {
        // Exibe o card específico (Usuário já cadastrado OU Vínculo SIGAA localizado)
        setCpfResult(data);
      } else {
        // CPF não encontrado nem no Solar nem no SIGAA -> abre o formulário completo de autocadastro
        setShowWizard(true);
      }
    } catch (err: any) {
      setErrorMessage('Erro ao verificar CPF: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleImportSigaa = async (e: FormEvent) => {
    e.preventDefault();
    if (!sigaaPassword) {
      setErrorMessage('Por favor, informe uma senha para o seu acesso.');
      return;
    }
    if (sigaaPassword.length < 8) {
      setErrorMessage('A senha deve conter no mínimo 8 caracteres (recomendação NIST).');
      return;
    }
    if (sigaaPassword !== sigaaPasswordConf) {
      setErrorMessage('A confirmação de senha não confere.');
      return;
    }

    setLoading(true);
    setErrorMessage(null);

    try {
      const response = await fetch('/api/v1/auth/import-sigaa', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          cpf: registerCpf.trim(),
          password: sigaaPassword,
          passwordConfirmation: sigaaPasswordConf,
          acceptTerms: true,
          termsVersion: 'v2.0_2026'
        })
      });

      const data = await response.json();
      if (response.ok && data.success && data.user && data.token) {
        onLoginSuccess(data.user, data.token);
      } else {
        setErrorMessage(data.message || 'Erro ao importar cadastro do SIGAA.');
      }
    } catch (err: any) {
      setErrorMessage('Erro ao conectar ao servidor: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const fillDemo = (userLogin: string, userPass: string) => {
    setLogin(userLogin);
    setPassword(userPass);
  };

  return (
    <div className="login-page-root">
      {/* 1. Barra Brasil Oficial do Governo Federal */}
      <div className="bar-brasil-gov">
        <div className="bar-brasil-content">
          <a href="https://www.gov.br" target="_blank" rel="noreferrer" className="logo-brasil">
            <svg width="20" height="14" viewBox="0 0 20 14" fill="none" xmlns="http://www.w3.org/2000/svg" style={{ borderRadius: '2px', flexShrink: 0 }}>
              <rect width="20" height="14" fill="#009C3B"/>
              <polygon points="10,1.5 18.5,7 10,12.5 1.5,7" fill="#FFDF00"/>
              <circle cx="10" cy="7" r="3.5" fill="#002776"/>
              <path d="M6.8 6.2C8 5.6 11.5 5.6 13.2 7.4" stroke="white" strokeWidth="0.7" fill="none"/>
            </svg>
            <span>{t('gov_brasil')}</span>
          </a>
          <ul className="gov-links-list">
            <li><a href="https://simplifique.gov.br" target="_blank" rel="noreferrer">{t('gov_simplifique')}</a></li>
            <li><a href="https://www.gov.br/comunicabr" target="_blank" rel="noreferrer">{t('gov_comunica_br')}</a></li>
            <li><a href="https://www.gov.br/participamaisbrasil" target="_blank" rel="noreferrer">{t('gov_participe')}</a></li>
            <li><a href="https://acessoainformacao.gov.br" target="_blank" rel="noreferrer">{t('gov_acesso_informacao')}</a></li>
            <li><a href="https://www.planalto.gov.br/legislacao" target="_blank" rel="noreferrer">{t('gov_legislacao')}</a></li>
            <li><a href="https://www.gov.br/canais" target="_blank" rel="noreferrer">{t('gov_canais')}</a></li>
          </ul>
        </div>
      </div>

      {/* 2. Container Central do Login / Cadastro */}
      <div className="login-central-wrapper">
        <div className="solar-brand-container">
          <img src="/assets/images/solar_logo_small_cursos.png" alt="Solar Cursos" className="solar-cursos-logo" />
          <h2 className="solar-subtitle">{t('solar_subtitle')}</h2>
        </div>

        {showWizard ? (
          /* WIZARD COMPLETO DE AUTOCADASTRO (4 ETAPAS) */
          <RegistrationWizard
            initialCpf={registerCpf}
            initialName={cpfResult?.existsInSigaa ? cpfResult.name : undefined}
            initialEmail={cpfResult?.existsInSigaa ? cpfResult.email : undefined}
            isSigaaImport={cpfResult?.existsInSigaa ?? false}
            onCancel={() => setShowWizard(false)}
            onRegistrationSuccess={onLoginSuccess}
          />
        ) : showPasswordRecovery ? (
          /* MODAL/TELA DE RECUPERAÇÃO DE SENHA (ESQUECEU A SENHA) */
          <PasswordRecoveryModal
            onBackToLogin={() => setShowPasswordRecovery(false)}
            onPasswordResetSuccess={(usernameOrEmail) => {
              setShowPasswordRecovery(false);
              setLogin(usernameOrEmail);
              setActiveTab('signin');
            }}
            onGoToRegister={() => {
              setShowPasswordRecovery(false);
              setActiveTab('signup');
            }}
          />
        ) : (
          /* CARD PRINCIPAL (LOGIN / VERIFICAÇÃO DE CPF) */
          <div className="login-form-card">
            <div className="login-tabs-header">
              <button
                type="button"
                className={`login-tab-item ${activeTab === 'signin' ? 'active' : ''}`}
                onClick={() => { setActiveTab('signin'); setErrorMessage(null); setCpfResult(null); }}
              >
                {t('login_tab')}
              </button>
              <button
                type="button"
                className={`login-tab-item ${activeTab === 'signup' ? 'active' : ''}`}
                onClick={() => { setActiveTab('signup'); setErrorMessage(null); }}
              >
                {t('register_tab')}
              </button>
            </div>

            <div className="login-card-content">
              {errorMessage && (
                <div className="login-flash-error">
                  ⚠️ {errorMessage}
                </div>
              )}

              {activeTab === 'signin' ? (
                /* FORMULÁRIO DE LOGIN */
                <form onSubmit={handleLogin}>
                  <div className="login-field-box">
                    <input
                      type="text"
                      value={login}
                      onChange={(e) => setLogin(e.target.value)}
                      placeholder={t('user_placeholder')}
                      className="solar-input-field"
                      autoFocus
                    />
                  </div>

                  <div className="login-field-box">
                    <input
                      type="password"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      placeholder={t('password_placeholder')}
                      className="solar-input-field"
                    />
                  </div>

                  <button type="submit" className="solar-btn-acessar" disabled={loading}>
                    {loading ? t('btn_accessing') : t('btn_access')}
                  </button>

                  <div className="login-forgot-pwd">
                    <a
                      href="#recuperar-senha"
                      onClick={(e) => {
                        e.preventDefault();
                        setShowPasswordRecovery(true);
                        setErrorMessage(null);
                      }}
                    >
                      {t('forgot_password')}
                    </a>
                  </div>

                  <div className="login-quick-demo">
                    <span style={{ fontSize: '0.74rem', color: '#555', display: 'block', marginBottom: '4px' }}>
                      💡 {t('quick_test_users')}
                    </span>
                    <button type="button" className="quick-chip" onClick={() => fillDemo('alunoteste', 'senhadoteste123')}>
                      alunoteste
                    </button>
                    <button type="button" className="quick-chip" onClick={() => fillDemo('aluno1', '123456')}>
                      aluno1
                    </button>
                    <button type="button" className="quick-chip" onClick={() => fillDemo('prof', '123456')}>
                      prof (123456)
                    </button>
                    <button type="button" className="quick-chip" onClick={() => fillDemo('prof.fabricio', 'solar123')}>
                      prof.fabricio
                    </button>
                  </div>
                </form>
              ) : (
                /* ABA CADASTRAR: VERIFICAÇÃO DE CPF E OPÇÕES DE CADASTRO */
                <div>
                  <form onSubmit={handleVerifyCpf}>
                    <div className="login-field-box" style={{ textAlign: 'left', marginBottom: '14px' }}>
                      <label className="solar-pwd-label">
                        CPF (somente números ou formatado):
                      </label>
                      <input
                        type="text"
                        value={registerCpf}
                        onChange={handleRegisterCpfChange}
                        placeholder="000.000.000-00"
                        maxLength={14}
                        className="solar-input-field"
                        autoFocus
                      />
                    </div>

                    <button
                      type="submit"
                      className="solar-btn-acessar"
                      style={{ background: 'linear-gradient(to bottom, #285596, #204882)', color: '#fff', border: '1px solid #143564' }}
                      disabled={loading}
                    >
                      {loading ? t('btn_verifying') : t('btn_verify_cpf')}
                    </button>
                  </form>

                  {cpfResult && (
                    <div style={{ marginTop: '16px', fontSize: '0.85rem', textAlign: 'left' }}>
                      {cpfResult.existsInLocal ? (
                        /* CPF JÁ EXISTE NO SOLAR */
                        <div style={{ background: '#fee2e2', color: '#dc2626', padding: '12px 14px', borderRadius: '6px', border: '1px solid #fca5a5' }}>
                          <p style={{ margin: '0 0 6px 0', fontWeight: 600 }}>⚠️ {cpfResult.message}</p>
                          <button
                            type="button"
                            className="quick-chip"
                            style={{ background: '#dc2626', color: '#fff', border: 'none', padding: '6px 12px', marginTop: '4px' }}
                            onClick={() => { setActiveTab('signin'); setLogin(registerCpf); }}
                          >
                            Ir para a tela de Login
                          </button>
                        </div>
                      ) : cpfResult.existsInSigaa ? (
                        /* LOCALIZADO NO SIGAA -> OPÇÃO DE IMPORTAÇÃO DIRETA OU WIZARD PRÉ-PREENCHIDO */
                        <div style={{ background: '#dcfce7', color: '#166534', padding: '16px', borderRadius: '8px', border: '1px solid #86efac', boxShadow: '0 2px 6px rgba(22,101,52,0.08)' }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '8px' }}>
                            <span style={{ fontSize: '1.3rem' }}>🎓</span>
                            <strong style={{ fontSize: '0.95rem' }}>Vínculo Localizado no SIGAA / UFC</strong>
                          </div>
                          
                          <p style={{ margin: '0 0 6px 0', fontWeight: 600, color: '#14532d' }}>
                            Nome: {cpfResult.name}
                          </p>
                          {cpfResult.email && (
                            <p style={{ margin: '0 0 10px 0', fontSize: '0.82rem', color: '#15803d' }}>
                              E-mail institucional: {cpfResult.email}
                            </p>
                          )}

                          <p style={{ margin: '0 0 12px 0', fontSize: '0.82rem', lineHeight: 1.4 }}>
                            Escolha uma senha para sincronizar automaticamente seus dados acadêmicos e entrar imediatamente:
                          </p>

                          <form onSubmit={handleImportSigaa}>
                            <input
                              type="password"
                              className="solar-input-field"
                              placeholder="Crie sua senha (mín. 8 caracteres)"
                              value={sigaaPassword}
                              onChange={(e) => setSigaaPassword(e.target.value)}
                              style={{ marginBottom: '8px' }}
                              required
                            />
                            <input
                              type="password"
                              className="solar-input-field"
                              placeholder="Confirme sua senha"
                              value={sigaaPasswordConf}
                              onChange={(e) => setSigaaPasswordConf(e.target.value)}
                              style={{ marginBottom: '10px' }}
                              required
                            />
                            <button
                              type="submit"
                              className="solar-btn-acessar"
                              style={{ background: '#16a34a', color: '#fff', border: 'none', fontWeight: 600 }}
                              disabled={loading}
                            >
                              {loading ? 'Sincronizando...' : '✔ Importar Dados do SIGAA e Entrar'}
                            </button>
                          </form>

                          <div style={{ textAlign: 'center', marginTop: '12px', borderTop: '1px solid #bbf7d0', paddingTop: '10px' }}>
                            <button
                              type="button"
                              style={{ background: 'none', border: 'none', color: '#15803d', cursor: 'pointer', fontSize: '0.8rem', textDecoration: 'underline' }}
                              onClick={() => setShowWizard(true)}
                            >
                              Ou clique aqui para revisar e preencher o formulário completo em 4 etapas
                            </button>
                          </div>
                        </div>
                      ) : (
                        /* NÃO LOCALIZADO NO SIGAA -> AUTOCADASTRO COMPLETO */
                        <div style={{ background: '#e0f2fe', color: '#0369a1', padding: '14px', borderRadius: '6px', border: '1px solid #7dd3fc' }}>
                          <p style={{ margin: '0 0 8px 0', fontWeight: 600 }}>ℹ️ {cpfResult.message}</p>
                          <p style={{ margin: '0 0 12px 0', fontSize: '0.8rem' }}>
                            Você pode realizar seu cadastro individual em 4 etapas rápidas com preenchimento automático por CEP.
                          </p>
                          <button
                            type="button"
                            className="solar-btn-acessar"
                            style={{ background: 'var(--solar-blue-main)', color: '#fff', border: 'none' }}
                            onClick={() => setShowWizard(true)}
                          >
                            📝 Iniciar Formulário de Cadastro (4 Etapas)
                          </button>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        )}

        {/* Logos Oficiais UFC e UFC Virtual */}
        <div className="login-institutional-logos">
          <img src="/assets/images/logos_brancos6.png" alt="UFC e UFC Virtual" style={{ width: '290px', height: 'auto', display: 'block' }} />
        </div>
      </div>

      {/* 3. Rodapé Oficial da Página de Login */}
      <OfficialFooter variant="login" />
    </div>
  );
};
