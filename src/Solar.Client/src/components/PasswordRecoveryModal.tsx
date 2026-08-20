import React, { useState } from 'react';
import { useTranslation } from '../context/LanguageContext';

interface PasswordRecoveryModalProps {
  onBackToLogin: () => void;
  onPasswordResetSuccess: (usernameOrEmail: string) => void;
  onGoToRegister?: () => void;
}

export const PasswordRecoveryModal: React.FC<PasswordRecoveryModalProps> = ({
  onBackToLogin,
  onPasswordResetSuccess,
  onGoToRegister
}) => {
  const { t } = useTranslation();
  const [viewMode, setViewMode] = useState<'request' | 'reset'>('request');
  const [loading, setLoading] = useState(false);
  const [cpf, setCpf] = useState('');
  const [resetToken, setResetToken] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [passwordConfirmation, setPasswordConfirmation] = useState('');
  
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSigaaInfo, setIsSigaaInfo] = useState(false);
  const [sigaaUrl, setSigaaUrl] = useState<string | null>(null);
  const [needsRegistrationFirst, setNeedsRegistrationFirst] = useState(false);

  // Formatação automática de CPF (000.000.000-00)
  const handleCpfChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const raw = e.target.value;
    // Se for e-mail ou nome de usuário (contém @ ou letras), permite livre
    if (/[a-zA-Z@]/.test(raw)) {
      setCpf(raw);
      return;
    }

    const numbers = raw.replace(/\D/g, '').slice(0, 11);
    let formatted = numbers;
    if (numbers.length > 9) {
      formatted = `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6, 9)}-${numbers.slice(9)}`;
    } else if (numbers.length > 6) {
      formatted = `${numbers.slice(0, 3)}.${numbers.slice(3, 6)}.${numbers.slice(6)}`;
    } else if (numbers.length > 3) {
      formatted = `${numbers.slice(0, 3)}.${numbers.slice(3)}`;
    }
    setCpf(formatted);
  };

  const handleRequestToken = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!cpf.trim()) {
      setErrorMessage(t('cpf_required') || 'Informe o seu CPF cadastrado.');
      return;
    }

    setLoading(true);
    setErrorMessage(null);
    setSuccessMessage(null);
    setIsSigaaInfo(false);
    setSigaaUrl(null);
    setNeedsRegistrationFirst(false);

    try {
      const response = await fetch('/api/v1/auth/forgot-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ emailOrUsernameOrCpf: cpf.trim() })
      });

      const data = await response.json();

      if (data.isIntegratedSigaa) {
        setIsSigaaInfo(true);
        setErrorMessage(data.message);
        setSigaaUrl(data.sigaaUrl || 'https://si3.ufc.br/sigaa/verTelaLogin.do');
        setNeedsRegistrationFirst(Boolean(data.needsRegistrationFirst));
      } else if (response.ok && data.success) {
        setSuccessMessage(data.message);
        if (data.generatedToken) {
          setResetToken(data.generatedToken);
        }
      } else {
        setErrorMessage(data.message || 'Não foi possível solicitar a recuperação de senha.');
      }
    } catch (err: any) {
      setErrorMessage('Erro ao conectar ao servidor: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resetToken.trim()) {
      setErrorMessage('Informe o código / token de recuperação recebido.');
      return;
    }
    if (!newPassword || newPassword.length < 6) {
      setErrorMessage('A nova senha deve possuir no mínimo 6 caracteres.');
      return;
    }
    if (newPassword !== passwordConfirmation) {
      setErrorMessage('A confirmação de senha não confere.');
      return;
    }

    setLoading(true);
    setErrorMessage(null);

    try {
      const response = await fetch('/api/v1/auth/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          token: resetToken.trim(),
          newPassword: newPassword,
          passwordConfirmation: passwordConfirmation
        })
      });

      const data = await response.json();

      if (response.ok && data.success) {
        alert('Senha redefinida com sucesso! Você já pode efetuar o login.');
        onPasswordResetSuccess(cpf);
      } else {
        setErrorMessage(data.message || 'Erro ao redefinir a senha.');
      }
    } catch (err: any) {
      setErrorMessage('Erro de conexão ao servidor: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="solar-password-recovery-wrapper">
      <div className="solar-password-recovery-card">
        {viewMode === 'request' ? (
          /* MODO 1: SOLICITAÇÃO POR CPF */
          <div>
            <h2 className="solar-pwd-title">
              {t('forgot_password')}
            </h2>

            <div className="solar-pwd-instructions">
              <p>
                Informe o seu <strong>CPF</strong> cadastrado para receber instruções de recuperação de senha por e-mail.
              </p>
              <p className="solar-pwd-sigaa-notice">
                ℹ️ <em>Caso você seja aluno ou professor integrado do SIGAA, sua senha deve ser recuperada diretamente no SIGAA.</em>
              </p>
            </div>

            {errorMessage && (
              <div className={`solar-pwd-alert ${isSigaaInfo ? 'info' : 'error'}`}>
                <div style={{ marginBottom: isSigaaInfo ? '8px' : '0' }}>
                  {isSigaaInfo ? '🏛️ ' : '⚠️ '} {errorMessage}
                </div>

                {isSigaaInfo && sigaaUrl && (
                  <div style={{ marginTop: '10px', paddingTop: '8px', borderTop: '1px solid rgba(0,0,0,0.1)' }}>
                    <a
                      href={sigaaUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="solar-pwd-sigaa-link"
                    >
                      🔗 Acessar Portal do SIGAA / SI3 (Abrir em nova aba)
                    </a>
                  </div>
                )}

                {needsRegistrationFirst && onGoToRegister && (
                  <div style={{ marginTop: '10px', textAlign: 'center' }}>
                    <button
                      type="button"
                      className="solar-btn-pwd-action"
                      onClick={onGoToRegister}
                      style={{ fontSize: '0.82rem', padding: '6px 14px' }}
                    >
                      Ir para a aba "Cadastrar"
                    </button>
                  </div>
                )}
              </div>
            )}

            {successMessage ? (
              <div className="solar-pwd-success-box">
                <div className="solar-pwd-alert success">
                  ✔ {successMessage}
                </div>
                <div style={{ marginTop: '16px', textAlign: 'center' }}>
                  <button
                    type="button"
                    className="solar-btn-pwd-action"
                    onClick={() => {
                      setErrorMessage(null);
                      setViewMode('reset');
                    }}
                  >
                    🔑 Inserir Código e Criar Nova Senha
                  </button>
                </div>
              </div>
            ) : (
              <form onSubmit={handleRequestToken}>
                <div className="solar-pwd-field">
                  <label className="solar-pwd-label">CPF (somente números ou formatado):</label>
                  <input
                    type="text"
                    className="solar-pwd-input"
                    placeholder="000.000.000-00"
                    value={cpf}
                    onChange={handleCpfChange}
                    autoFocus
                    required
                  />
                </div>

                <div className="solar-pwd-actions">
                  <button
                    type="button"
                    className="solar-btn-pwd-back"
                    onClick={onBackToLogin}
                    disabled={loading}
                  >
                    Voltar
                  </button>

                  <button
                    type="submit"
                    className="solar-btn-pwd-send"
                    disabled={loading}
                  >
                    {loading ? 'Enviando...' : 'Enviar'}
                  </button>
                </div>
              </form>
            )}

            {!successMessage && (
              <div className="solar-pwd-bottom-token-link">
                <a
                  href="#inserir-token"
                  onClick={(e) => {
                    e.preventDefault();
                    setErrorMessage(null);
                    setViewMode('reset');
                  }}
                >
                  Já possui um código de recuperação? Clique aqui
                </a>
              </div>
            )}
          </div>
        ) : (
          /* MODO 2: REDEFINIÇÃO DE SENHA */
          <div>
            <h2 className="solar-pwd-title">
              Redefinição de Senha
            </h2>

            <div className="solar-pwd-instructions">
              <p>
                Insira o código de verificação recebido e defina sua nova senha de acesso ao Solar LMS.
              </p>
            </div>

            {errorMessage && (
              <div className="solar-pwd-alert error">
                ⚠️ {errorMessage}
              </div>
            )}

            <form onSubmit={handleResetPassword}>
              <div className="solar-pwd-field">
                <label className="solar-pwd-label">Código / Token de Recuperação:</label>
                <input
                  type="text"
                  className="solar-pwd-input"
                  placeholder="Cole o código recebido no e-mail"
                  value={resetToken}
                  onChange={(e) => setResetToken(e.target.value)}
                  autoFocus
                  required
                />
              </div>

              <div className="solar-pwd-field">
                <label className="solar-pwd-label">Nova Senha (mínimo 6 caracteres):</label>
                <input
                  type="password"
                  className="solar-pwd-input"
                  placeholder="Digite sua nova senha"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  required
                />
              </div>

              <div className="solar-pwd-field">
                <label className="solar-pwd-label">Confirme a Nova Senha:</label>
                <input
                  type="password"
                  className="solar-pwd-input"
                  placeholder="Repita a nova senha"
                  value={passwordConfirmation}
                  onChange={(e) => setPasswordConfirmation(e.target.value)}
                  required
                />
              </div>

              <div className="solar-pwd-actions">
                <button
                  type="button"
                  className="solar-btn-pwd-back"
                  onClick={() => {
                    setErrorMessage(null);
                    setViewMode('request');
                  }}
                  disabled={loading}
                >
                  Voltar
                </button>

                <button
                  type="submit"
                  className="solar-btn-pwd-send"
                  disabled={loading}
                >
                  {loading ? 'Redefinindo...' : '✔ Alterar Senha'}
                </button>
              </div>
            </form>
          </div>
        )}
      </div>

      <div className="solar-register-cancel-wrap">
        <button
          type="button"
          className="solar-register-cancel-link"
          onClick={onBackToLogin}
        >
          Retornar para a tela de login
        </button>
      </div>
    </div>
  );
};
