import React, { useState } from 'react';
import type { UserProfile } from '../types/auth';

interface RegistrationWizardProps {
  initialCpf: string;
  onCancel: () => void;
  onRegistrationSuccess: (user: UserProfile, token: string) => void;
}

export const RegistrationWizard: React.FC<RegistrationWizardProps> = ({
  initialCpf,
  onCancel,
  onRegistrationSuccess
}) => {
  const [currentStep, setCurrentStep] = useState<1 | 2 | 3 | 4>(1);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // CEP lookup state
  const [cepLoading, setCepLoading] = useState(false);
  const [cepMessage, setCepMessage] = useState<string | null>(null);

  // LGPD consent state
  const [acceptTerms, setAcceptTerms] = useState(true);

  // Form State
  const [formData, setFormData] = useState({
    name: '',
    cpf: initialCpf,
    birthdate: '',
    gender: 'true',
    hasSpecialNeeds: false,
    specialNeeds: '',
    nick: '',
    username: '',
    password: '',
    passwordConfirmation: '',
    email: '',
    emailConfirmation: '',
    alternateEmail: '',
    address: '',
    addressNumber: '',
    addressComplement: '',
    addressNeighborhood: '',
    zipcode: '',
    state: 'CE',
    city: '',
    telephone: '',
    cellPhone: '',
    institution: 'Universidade Federal do Ceará'
  });

  const handleChange = (field: string, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    setErrorMessage(null);
  };

  const handleCepChange = async (cepInput: string) => {
    handleChange('zipcode', cepInput);
    const clean = cepInput.replace(/\D/g, '');
    if (clean.length === 8) {
      setCepLoading(true);
      setCepMessage('Consultando CEP...');
      try {
        const res = await fetch(`/api/v1/cep/${clean}`);
        if (res.ok) {
          const data = await res.json();
          if (data.found) {
            setFormData((prev) => ({
              ...prev,
              address: data.logradouro || prev.address,
              addressNeighborhood: data.bairro || prev.addressNeighborhood,
              city: data.localidade || prev.city,
              state: data.uf || prev.state
            }));
            setCepMessage('✅ Endereço preenchido automaticamente.');
          } else {
            setCepMessage('⚠️ CEP não localizado na base dos Correios.');
          }
        }
      } catch {
        setCepMessage(null);
      } finally {
        setCepLoading(false);
      }
    } else {
      setCepMessage(null);
    }
  };

  const calculatePasswordStrength = (pwd: string) => {
    if (!pwd) return { score: 0, label: '', color: '#ccc' };
    let score = 0;
    if (pwd.length >= 8) score += 30;
    if (/[A-Z]/.test(pwd)) score += 20;
    if (/[a-z]/.test(pwd)) score += 20;
    if (/[0-9]/.test(pwd)) score += 15;
    if (/[^A-Za-z0-9]/.test(pwd)) score += 15;
    if (score >= 80) return { score: 100, label: 'Muito Forte', color: '#16a34a' };
    if (score >= 60) return { score: 75, label: 'Forte', color: '#2563eb' };
    if (score >= 40) return { score: 50, label: 'Média', color: '#eab308' };
    return { score: 25, label: 'Fraca (recomendado min. 8 caracteres com letras e números)', color: '#dc2626' };
  };

  const pwdStrength = calculatePasswordStrength(formData.password);

  const handleNext = () => {
    if (currentStep === 1) {
      if (!formData.name.trim()) {
        setErrorMessage('Por favor, informe seu nome completo.');
        return;
      }
      setCurrentStep(2);
    } else if (currentStep === 2) {
      if (!formData.username.trim()) {
        setErrorMessage('Por favor, informe um nome de usuário.');
        return;
      }
      if (!formData.password) {
        setErrorMessage('Por favor, defina uma senha de acesso.');
        return;
      }
      if (formData.password.length < 8) {
        setErrorMessage('A senha deve conter no mínimo 8 caracteres (recomendação NIST).');
        return;
      }
      if (formData.password !== formData.passwordConfirmation) {
        setErrorMessage('A confirmação de senha não confere.');
        return;
      }
      if (!formData.email.trim()) {
        setErrorMessage('Por favor, informe seu e-mail.');
        return;
      }
      if (formData.emailConfirmation && formData.email.trim() !== formData.emailConfirmation.trim()) {
        setErrorMessage('A confirmação de e-mail não confere.');
        return;
      }
      setCurrentStep(3);
    } else if (currentStep === 3) {
      setCurrentStep(4);
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep((prev) => (prev - 1) as 1 | 2 | 3 | 4);
      setErrorMessage(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!acceptTerms) {
      setErrorMessage('É necessário concordar com os Termos de Uso e Política de Privacidade LGPD.');
      return;
    }

    setLoading(true);
    setErrorMessage(null);

    try {
      const response = await fetch('/api/v1/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: formData.name.trim(),
          cpf: formData.cpf.trim(),
          birthdate: formData.birthdate || null,
          gender: formData.gender === 'true',
          hasSpecialNeeds: formData.hasSpecialNeeds,
          specialNeeds: formData.hasSpecialNeeds ? formData.specialNeeds : null,
          nick: formData.nick.trim() || formData.name.trim().split(' ')[0],
          username: formData.username.trim(),
          password: formData.password,
          passwordConfirmation: formData.passwordConfirmation,
          email: formData.email.trim(),
          emailConfirmation: formData.emailConfirmation.trim() || formData.email.trim(),
          alternateEmail: formData.alternateEmail.trim() || null,
          address: formData.address.trim() || null,
          addressNumber: formData.addressNumber.trim() || null,
          addressComplement: formData.addressComplement.trim() || null,
          addressNeighborhood: formData.addressNeighborhood.trim() || null,
          zipcode: formData.zipcode.trim() || null,
          state: formData.state,
          city: formData.city.trim() || null,
          telephone: formData.telephone.trim() || null,
          cellPhone: formData.cellPhone.trim() || null,
          institution: formData.institution.trim() || null,
          acceptTerms: true,
          termsVersion: 'v2.0_2026'
        })
      });

      const data = await response.json();
      if (response.ok && data.success && data.user && data.token) {
        onRegistrationSuccess(data.user, data.token);
      } else {
        setErrorMessage(data.message || 'Erro ao realizar cadastro.');
      }
    } catch (err: any) {
      setErrorMessage('Erro ao conectar ao servidor: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const states = [
    'AC', 'AL', 'AP', 'AM', 'BA', 'CE', 'DF', 'ES', 'GO', 'MA',
    'MT', 'MS', 'MG', 'PA', 'PB', 'PR', 'PE', 'PI', 'RJ', 'RN',
    'RS', 'RO', 'RR', 'SC', 'SP', 'SE', 'TO'
  ];

  return (
    <div className="solar-register-wrapper">
      {/* Aba Superior Integrada */}
      <div className="solar-register-tab-header">
        <span className="solar-register-tab-label">Cadastrar</span>
      </div>

      <div className="solar-register-card">
        {/* Barra de Etapas com Design Modernizado */}
        <div className="solar-register-stepper">
          <div
            className={`solar-step-item ${currentStep === 1 ? 'active' : currentStep > 1 ? 'completed' : ''}`}
            onClick={() => setCurrentStep(1)}
          >
            <span className="solar-step-title">Dados Pessoais</span>
            <span className="solar-step-dot" />
          </div>

          <div
            className={`solar-step-item ${currentStep === 2 ? 'active' : currentStep > 2 ? 'completed' : ''}`}
            onClick={() => currentStep > 1 && setCurrentStep(2)}
          >
            <span className="solar-step-title">Acesso</span>
            <span className="solar-step-dot" />
          </div>

          <div
            className={`solar-step-item ${currentStep === 3 ? 'active' : currentStep > 3 ? 'completed' : ''}`}
            onClick={() => currentStep > 2 && setCurrentStep(3)}
          >
            <span className="solar-step-title">Contato</span>
            <span className="solar-step-dot" />
          </div>

          <div
            className={`solar-step-item ${currentStep === 4 ? 'active' : ''}`}
            onClick={() => currentStep > 3 && setCurrentStep(4)}
          >
            <span className="solar-step-title">LGPD & Outros</span>
            <span className="solar-step-dot" />
          </div>
        </div>

        {errorMessage && (
          <div className="solar-register-error-banner">
            ⚠️ {errorMessage}
          </div>
        )}

        <form onSubmit={handleSubmit} className="solar-register-form">
          {/* ETAPA 1: DADOS PESSOAIS */}
          {currentStep === 1 && (
            <div className="solar-step-panel">
              <div className="solar-form-row">
                <label className="solar-form-label">
                  <span className="required-star">*</span> Nome
                </label>
                <div className="solar-form-control-col">
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.name}
                    onChange={(e) => handleChange('name', e.target.value)}
                    placeholder="Seu nome completo"
                    required
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">
                  <span className="required-star">*</span> CPF
                </label>
                <div className="solar-form-control-col">
                  <input
                    type="text"
                    className="solar-form-input readonly"
                    value={formData.cpf}
                    readOnly
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">Data de Nascimento</label>
                <div className="solar-form-control-col">
                  <input
                    type="date"
                    className="solar-form-input"
                    value={formData.birthdate}
                    onChange={(e) => handleChange('birthdate', e.target.value)}
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">Sexo</label>
                <div className="solar-form-control-col radio-group">
                  <label className="solar-radio-label">
                    <input
                      type="radio"
                      name="gender"
                      value="true"
                      checked={formData.gender === 'true'}
                      onChange={(e) => handleChange('gender', e.target.value)}
                    />
                    Masculino
                  </label>
                  <label className="solar-radio-label">
                    <input
                      type="radio"
                      name="gender"
                      value="false"
                      checked={formData.gender === 'false'}
                      onChange={(e) => handleChange('gender', e.target.value)}
                    />
                    Feminino
                  </label>
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">Portador de Necessidades Especiais</label>
                <div className="solar-form-control-col">
                  <label className="solar-checkbox-label">
                    <input
                      type="checkbox"
                      checked={formData.hasSpecialNeeds}
                      onChange={(e) => handleChange('hasSpecialNeeds', e.target.checked)}
                    />
                    Sim, necessito de recursos de acessibilidade
                  </label>
                </div>
              </div>

              {formData.hasSpecialNeeds && (
                <div className="solar-form-row">
                  <label className="solar-form-label">
                    <span className="required-star">*</span> Descrição da Necessidade
                  </label>
                  <div className="solar-form-control-col">
                    <input
                      type="text"
                      className="solar-form-input"
                      value={formData.specialNeeds}
                      onChange={(e) => handleChange('specialNeeds', e.target.value)}
                      placeholder="Ex: Baixa visão, Deficiência auditiva, Mobilidade reduzida"
                    />
                  </div>
                </div>
              )}
            </div>
          )}

          {/* ETAPA 2: DADOS DE ACESSO */}
          {currentStep === 2 && (
            <div className="solar-step-panel">
              <div className="solar-form-row">
                <label className="solar-form-label">
                  <span className="required-star">*</span> Apelido (Nick)
                </label>
                <div className="solar-form-control-col">
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.nick}
                    onChange={(e) => handleChange('nick', e.target.value)}
                    placeholder="Como deseja ser chamado no chat/fórum"
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">
                  <span className="required-star">*</span> Login (Username)
                </label>
                <div className="solar-form-control-col">
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.username}
                    onChange={(e) => handleChange('username', e.target.value)}
                    placeholder="nome.sobrenome (apenas letras, números e ponto)"
                    required
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">
                  <span className="required-star">*</span> Senha
                </label>
                <div className="solar-form-control-col">
                  <input
                    type="password"
                    className="solar-form-input"
                    value={formData.password}
                    onChange={(e) => handleChange('password', e.target.value)}
                    placeholder="Mínimo 8 caracteres com letras e números"
                    required
                  />
                  {formData.password && (
                    <div style={{ marginTop: '6px' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', marginBottom: '2px' }}>
                        <span>Força da Senha:</span>
                        <strong style={{ color: pwdStrength.color }}>{pwdStrength.label}</strong>
                      </div>
                      <div style={{ height: '5px', backgroundColor: '#e2e8f0', borderRadius: '4px', overflow: 'hidden' }}>
                        <div style={{ height: '100%', width: `${pwdStrength.score}%`, backgroundColor: pwdStrength.color, transition: 'all 0.3s' }} />
                      </div>
                    </div>
                  )}
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">
                  <span className="required-star">*</span> Confirmação
                </label>
                <div className="solar-form-control-col">
                  <input
                    type="password"
                    className="solar-form-input"
                    value={formData.passwordConfirmation}
                    onChange={(e) => handleChange('passwordConfirmation', e.target.value)}
                    placeholder="Repita a senha criada"
                    required
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">
                  <span className="required-star">*</span> E-mail
                </label>
                <div className="solar-form-control-col">
                  <input
                    type="email"
                    className="solar-form-input"
                    value={formData.email}
                    onChange={(e) => handleChange('email', e.target.value)}
                    placeholder="seu.email@dominio.com"
                    required
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">
                  <span className="required-star">*</span> Confirmação
                </label>
                <div className="solar-form-control-col">
                  <input
                    type="email"
                    className="solar-form-input"
                    value={formData.emailConfirmation}
                    onChange={(e) => handleChange('emailConfirmation', e.target.value)}
                    placeholder="Confirme o seu e-mail"
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">E-mail Alternativo</label>
                <div className="solar-form-control-col">
                  <input
                    type="email"
                    className="solar-form-input"
                    value={formData.alternateEmail}
                    onChange={(e) => handleChange('alternateEmail', e.target.value)}
                    placeholder="Opcional (para recuperação)"
                  />
                </div>
              </div>
            </div>
          )}

          {/* ETAPA 3: CONTATO COM PREENCHIMENTO VIA CEP */}
          {currentStep === 3 && (
            <div className="solar-step-panel">
              <div className="solar-form-row-multi">
                <div className="solar-form-row" style={{ flex: 1.2 }}>
                  <label className="solar-form-label" style={{ width: '130px' }}>CEP</label>
                  <div style={{ flex: 1 }}>
                    <input
                      type="text"
                      className="solar-form-input"
                      value={formData.zipcode}
                      onChange={(e) => handleCepChange(e.target.value)}
                      placeholder="00000-000 (preenchimento automático)"
                    />
                    {cepMessage && (
                      <div style={{ fontSize: '0.75rem', marginTop: '4px', color: cepLoading ? '#2563eb' : '#475569' }}>
                        {cepMessage}
                      </div>
                    )}
                  </div>
                </div>
                <div className="solar-form-row" style={{ flex: 1 }}>
                  <label className="solar-form-label" style={{ width: '80px' }}>Estado</label>
                  <select
                    className="solar-form-select"
                    value={formData.state}
                    onChange={(e) => handleChange('state', e.target.value)}
                  >
                    {states.map((st) => (
                      <option key={st} value={st}>{st}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">Endereço</label>
                <div className="solar-form-control-col">
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.address}
                    onChange={(e) => handleChange('address', e.target.value)}
                    placeholder="Rua, Avenida, Logradouro"
                  />
                </div>
              </div>

              <div className="solar-form-row-multi">
                <div className="solar-form-row" style={{ flex: 1 }}>
                  <label className="solar-form-label" style={{ width: '130px' }}>Número</label>
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.addressNumber}
                    onChange={(e) => handleChange('addressNumber', e.target.value)}
                    placeholder="Nº"
                  />
                </div>
                <div className="solar-form-row" style={{ flex: 1.5 }}>
                  <label className="solar-form-label" style={{ width: '100px' }}>Complemento</label>
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.addressComplement}
                    onChange={(e) => handleChange('addressComplement', e.target.value)}
                    placeholder="Apto, Bloco"
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">Bairro</label>
                <div className="solar-form-control-col">
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.addressNeighborhood}
                    onChange={(e) => handleChange('addressNeighborhood', e.target.value)}
                    placeholder="Nome do bairro"
                  />
                </div>
              </div>

              <div className="solar-form-row">
                <label className="solar-form-label">Cidade</label>
                <div className="solar-form-control-col">
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.city}
                    onChange={(e) => handleChange('city', e.target.value)}
                    placeholder="Cidade"
                  />
                </div>
              </div>

              <div className="solar-form-row-multi">
                <div className="solar-form-row" style={{ flex: 1 }}>
                  <label className="solar-form-label" style={{ width: '130px' }}>Telefone</label>
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.telephone}
                    onChange={(e) => handleChange('telephone', e.target.value)}
                    placeholder="(85) 0000-0000"
                  />
                </div>
                <div className="solar-form-row" style={{ flex: 1 }}>
                  <label className="solar-form-label" style={{ width: '80px' }}>Celular</label>
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.cellPhone}
                    onChange={(e) => handleChange('cellPhone', e.target.value)}
                    placeholder="(85) 90000-0000"
                  />
                </div>
              </div>
            </div>
          )}

          {/* ETAPA 4: OUTROS & LGPD */}
          {currentStep === 4 && (
            <div className="solar-step-panel">
              <div className="solar-form-row">
                <label className="solar-form-label">Instituição</label>
                <div className="solar-form-control-col">
                  <input
                    type="text"
                    className="solar-form-input"
                    value={formData.institution}
                    onChange={(e) => handleChange('institution', e.target.value)}
                    placeholder="Universidade Federal do Ceará (UFC)"
                  />
                </div>
              </div>

              <div className="solar-register-summary-box">
                <h4 style={{ margin: '0 0 10px 0', fontSize: '0.92rem', color: '#1e3a8a' }}>
                  📋 Confirmação dos Dados Cadastrais:
                </h4>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '6px', fontSize: '0.84rem', color: '#334155' }}>
                  <div><strong>Nome:</strong> {formData.name}</div>
                  <div><strong>CPF:</strong> {formData.cpf}</div>
                  <div><strong>Login:</strong> {formData.username}</div>
                  <div><strong>E-mail:</strong> {formData.email}</div>
                  <div><strong>UF / Cidade:</strong> {formData.city ? `${formData.city} - ${formData.state}` : formData.state}</div>
                  <div><strong>Instituição:</strong> {formData.institution}</div>
                </div>
              </div>

              {/* Termos de Uso e LGPD */}
              <div style={{ marginTop: '16px', padding: '12px', backgroundColor: '#f8fafc', border: '1px solid #e2e8f0', borderRadius: '8px' }}>
                <label style={{ display: 'flex', alignItems: 'flex-start', gap: '10px', fontSize: '0.84rem', color: '#1e293b', cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={acceptTerms}
                    onChange={(e) => setAcceptTerms(e.target.checked)}
                    style={{ marginTop: '3px' }}
                    required
                  />
                  <span>
                    <span className="required-star">*</span> Declaro que li e concordo com os <strong>Termos de Uso</strong> e a <strong>Política de Privacidade da UFC Virtual</strong>, autorizando o tratamento dos meus dados pessoais estritamente para fins acadêmicos em conformidade com a <strong>LGPD (Lei nº 13.709/2018)</strong>.
                  </span>
                </label>
              </div>
            </div>
          )}

          {/* Rodapé Interno com Contenção Perfeita dos Botões */}
          <div className="solar-register-actions-footer">
            <span className="solar-required-note">
              * campo(s) obrigatório(s)
            </span>

            <div className="solar-buttons-group">
              {currentStep > 1 && (
                <button
                  type="button"
                  className="solar-btn-wizard-back"
                  onClick={handleBack}
                  disabled={loading}
                >
                  Anterior
                </button>
              )}

              {currentStep < 4 ? (
                <button
                  type="button"
                  className="solar-btn-wizard-next"
                  onClick={handleNext}
                >
                  Próximo
                </button>
              ) : (
                <button
                  type="submit"
                  className="solar-btn-wizard-complete"
                  disabled={loading || !acceptTerms}
                >
                  {loading ? 'Cadastrando...' : 'Concluir'}
                </button>
              )}
            </div>
          </div>
        </form>
      </div>

      {/* Link de Cancelar Elegante */}
      <div className="solar-register-cancel-wrap">
        <button
          type="button"
          className="solar-register-cancel-link"
          onClick={onCancel}
        >
          Cancelar
        </button>
      </div>
    </div>
  );
};
