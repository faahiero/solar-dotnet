import { useState } from 'react';
import type { FormEvent } from 'react';
import type { UserProfile, LoginResponse, VerifyCpfResponse } from '../types/auth';

interface LoginScreenProps {
  onLoginSuccess: (user: UserProfile, token: string) => void;
}

export const LoginScreen = ({ onLoginSuccess }: LoginScreenProps) => {
  const [activeTab, setActiveTab] = useState<'signin' | 'signup'>('signin');
  const [login, setLogin] = useState('');
  const [password, setPassword] = useState('');
  const [registerCpf, setRegisterCpf] = useState('');
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [cpfResult, setCpfResult] = useState<VerifyCpfResponse | null>(null);

  const handleLogin = async (e: FormEvent) => {
    e.preventDefault();
    if (!login || !password) {
      setErrorMessage('Por favor, informe seu login e sua senha.');
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

      if (!response.ok) {
        setErrorMessage('Usuário ou senha inválidos. Verifique suas credenciais.');
        return;
      }

      const data: LoginResponse = await response.json();
      if (data.success && data.user) {
        onLoginSuccess(data.user, data.token || 'valid_token');
      } else {
        setErrorMessage(data.message || 'Falha na autenticação.');
      }
    } catch (err) {
      setErrorMessage('Erro de comunicação com o servidor: ' + err);
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyCpf = async (e: FormEvent) => {
    e.preventDefault();
    if (!registerCpf) {
      alert('Informe um CPF válido.');
      return;
    }

    setLoading(true);
    try {
      const response = await fetch('/api/v1/auth/verify-cpf', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ cpf: registerCpf.trim() })
      });

      const data: VerifyCpfResponse = await response.json();
      setCpfResult(data);
    } catch (err) {
      alert('Erro ao verificar CPF: ' + err);
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
            <img src="/assets/images/brazil.png" alt="Brasil" style={{ height: '14px', marginRight: '6px' }} />
            <strong>BRASIL</strong>
          </a>
          <ul className="gov-links-list">
            <li><a href="#simplifique">Simplifique!</a></li>
            <li><a href="#comunica">Comunica BR</a></li>
            <li><a href="#participe">Participe</a></li>
            <li><a href="#acesso">Acesso à informação</a></li>
            <li><a href="#legislacao">Legislação</a></li>
            <li><a href="#canais">Canais</a></li>
          </ul>
        </div>
      </div>

      {/* 2. Container Central do Login */}
      <div className="login-central-wrapper">
        <div className="solar-brand-container">
          <img src="/assets/images/solar_logo_small_cursos.png" alt="Solar Cursos" className="solar-cursos-logo" />
          <h2 className="solar-subtitle">Ambiente Virtual de Aprendizagem da Universidade Federal do Ceará</h2>
        </div>

        <div className="login-form-card">
          <div className="login-tabs-header">
            <button
              type="button"
              className={`login-tab-item ${activeTab === 'signin' ? 'active' : ''}`}
              onClick={() => { setActiveTab('signin'); setErrorMessage(null); }}
            >
              Login
            </button>
            <button
              type="button"
              className={`login-tab-item ${activeTab === 'signup' ? 'active' : ''}`}
              onClick={() => { setActiveTab('signup'); setErrorMessage(null); }}
            >
              Cadastrar
            </button>
          </div>

          <div className="login-card-content">
            {errorMessage && (
              <div className="login-flash-error">
                ⚠️ {errorMessage}
              </div>
            )}

            {activeTab === 'signin' ? (
              <form onSubmit={handleLogin}>
                <div className="login-field-box">
                  <input
                    type="text"
                    value={login}
                    onChange={(e) => setLogin(e.target.value)}
                    placeholder="Digite seu login"
                    className="solar-input-field"
                    autoFocus
                  />
                </div>

                <div className="login-field-box">
                  <input
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Digite sua senha"
                    className="solar-input-field"
                  />
                </div>

                <button type="submit" className="solar-btn-acessar" disabled={loading}>
                  {loading ? 'Acessando...' : 'Acessar'}
                </button>

                <div className="login-forgot-pwd">
                  <a href="#recuperar-senha" onClick={(e) => { e.preventDefault(); alert('Em ambiente de teste institucional, utilize o usuário alunoteste ou seu CPF.'); }}>
                    Esqueceu a sua senha?
                  </a>
                </div>

                <div className="login-quick-demo">
                  <span style={{ fontSize: '0.74rem', color: '#555', display: 'block', marginBottom: '4px' }}>
                    💡 Usuários de teste rápido:
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
              <form onSubmit={handleVerifyCpf}>
                <div className="login-field-box">
                  <input
                    type="text"
                    value={registerCpf}
                    onChange={(e) => setRegisterCpf(e.target.value)}
                    placeholder="Digite seu CPF (000.000.000-00)"
                    className="solar-input-field"
                    autoFocus
                  />
                </div>

                <button type="submit" className="solar-btn-acessar" style={{ background: 'linear-gradient(to bottom, #285596, #204882)', color: '#fff', border: '1px solid #143564' }} disabled={loading}>
                  {loading ? 'Verificando...' : 'Verificar Cadastro no SIGAA'}
                </button>

                {cpfResult && (
                  <div style={{ marginTop: '14px', fontSize: '0.85rem', textAlign: 'left' }}>
                    {cpfResult.existsInLocal ? (
                      <div style={{ background: '#fee2e2', color: '#dc2626', padding: '8px 10px', borderRadius: '4px' }}>
                        ⚠️ {cpfResult.message}
                      </div>
                    ) : cpfResult.existsInSigaa ? (
                      <div style={{ background: '#dcfce7', color: '#16a34a', padding: '8px 10px', borderRadius: '4px' }}>
                        ✔ <strong>{cpfResult.name}</strong> ({cpfResult.email})<br />{cpfResult.message}
                      </div>
                    ) : (
                      <div style={{ background: '#e0f2fe', color: '#0369a1', padding: '8px 10px', borderRadius: '4px' }}>
                        ℹ️ {cpfResult.message}
                      </div>
                    )}
                  </div>
                )}
              </form>
            )}
          </div>
        </div>

        {/* Logos Oficiais UFC e UFC Virtual */}
        <div className="login-institutional-logos">
          <img src="/assets/images/logo_ufc.png" alt="UFC" style={{ height: '52px', filter: 'brightness(0) invert(1)' }} />
          <img src="/assets/images/ufcVirtual.png" alt="UFC Virtual" style={{ height: '48px', filter: 'brightness(0) invert(1)' }} />
        </div>

        {/* Rodapé de Links */}
        <div className="login-bottom-nav">
          <a href="#portais">Portais ▲</a>
          <a href="#desenvolvimento">Desenvolvimento ▲</a>
          <a href="#privacidade">Política de privacidade</a>
          <a href="#ajuda">Ajuda ▲</a>
          <a href="#idioma">Idioma ▲</a>
        </div>
      </div>

      {/* Widget VLibras Flutuante */}
      <div className="vlibras-badge">
        <span style={{ fontSize: '0.75rem', fontWeight: 600 }}>Acessível com<br /><strong>VLibras</strong></span>
        <div className="vlibras-hand-icon">🤟</div>
      </div>
    </div>
  );
};
