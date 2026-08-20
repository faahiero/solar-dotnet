import React, { useState } from 'react';
import { useTranslation } from '../context/LanguageContext';

interface PasswordRecoveryModalProps {
  onBackToLogin: () => void;
  onPasswordResetSuccess: (usernameOrEmail: string) => void;
}

export const PasswordRecoveryModal: React.FC<PasswordRecoveryModalProps> = ({
  onBackToLogin,
  onPasswordResetSuccess
}) => {
  const { t } = useTranslation();
  const [viewMode, setViewMode] = useState<'request' | 'reset'>('request');
  const [loading, setLoading] = useState(false);
  const [cpfOrUser, setCpfOrUser] = useState('');
  const [resetToken, setResetToken] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [passwordConfirmation, setPasswordConfirmation] = useState('');
  
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSigaaInfo, setIsSigaaInfo] = useState(false);

  const handleRequestToken = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!cpfOrUser.trim()) {
      setErrorMessage('Informe seu CPF, login ou e-mail cadastrado.');
      return;
    }

    setLoading(true);
    setErrorMessage(null);
    setSuccessMessage(null);
    setIsSigaaInfo(false);

    try {
      const response = await fetch('/api/v1/auth/forgot-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ emailOrUsernameOrCpf: cpfOrUser.trim() })
      });

      const data = await response.json();

      if (data.isIntegratedSigaa) {
        setIsSigaaInfo(true);
        setErrorMessage(data.message);
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
        onPasswordResetSuccess(cpfOrUser);
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
          /* MODO 1: SOLICITAÇÃO POR CPF / EMAIL */
          <div>
            <h2 className="solar-pwd-title">
              {t('forgot_password')}
            </h2>

            <div className="solar-pwd-instructions">
              <p>
                Informe o seu <strong>CPF</strong> ou <strong>nome de usuário</strong> cadastrado para receber as instruções e o código de recuperação por e-mail.
              </p>
              <p className="solar-pwd-sigaa-notice">
                ℹ️ <em>Caso você seja aluno ou professor integrado do SIGAA, sua senha deve ser recuperada diretamente no SIGAA.</em>
              </p>
            </div>

            {errorMessage && (
              <div className={`solar-pwd-alert ${isSigaaInfo ? 'info' : 'error'}`}>
                {isSigaaInfo ? '🏛️ ' : '⚠️ '} {errorMessage}
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
                    onClick={() => setViewMode('reset')}
                  >
                    🔑 Inserir Código e Criar Nova Senha
                  </button>
                </div>
              </div>
            ) : (
              <form onSubmit={handleRequestToken}>
                <div className="solar-pwd-field">
                  <input
                    type="text"
                    className="solar-pwd-input"
                    placeholder="Digite seu CPF, usuário ou e-mail"
                    value={cpfOrUser}
                    onChange={(e) => setCpfOrUser(e.target.value)}
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
                  onClick={() => setViewMode('request')}
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
