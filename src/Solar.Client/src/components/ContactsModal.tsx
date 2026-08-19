import { useState, useEffect } from 'react';

export interface ContactUser {
  id: number;
  name: string;
  email: string;
  username: string;
  role: string;
  typeMask: number;
  resume: string;
}

interface ContactsModalProps {
  onClose: () => void;
  onConfirmSelection: (selected: ContactUser[]) => void;
  initiallySelected?: ContactUser[];
  currentUserId?: number;
}

export const ContactsModal = ({
  onClose,
  onConfirmSelection,
  initiallySelected = [],
  currentUserId
}: ContactsModalProps) => {
  const [contactsType, setContactsType] = useState<number>(1);
  const [roleType, setRoleType] = useState<number>(0);
  const [course, setCourse] = useState<string>('all');
  const [discipline, setDiscipline] = useState<string>('all');
  const [semester, setSemester] = useState<string>('all');
  const [selectionMode, setSelectionMode] = useState<'offer' | 'group'>('offer');
  const [searchFilter, setSearchFilter] = useState<string>('');

  const [availableUsers, setAvailableUsers] = useState<ContactUser[]>([]);
  const [selectedUsers, setSelectedUsers] = useState<ContactUser[]>(initiallySelected);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchContacts = () => {
    setLoading(true);
    const query = new URLSearchParams();
    if (contactsType) query.set('contactsType', contactsType.toString());
    if (roleType > 0) query.set('roleType', roleType.toString());
    if (searchFilter) query.set('search', searchFilter);
    if (currentUserId && currentUserId > 0) query.set('userId', currentUserId.toString());

    fetch(`/api/v1/messages/contacts?${query.toString()}`)
      .then((res) => res.json())
      .then((data: ContactUser[]) => {
        setAvailableUsers(data);
      })
      .catch((err) => console.error('Erro ao buscar contatos:', err))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchContacts();
  }, [contactsType, roleType]);

  const handleAddUser = (user: ContactUser) => {
    if (!selectedUsers.some((u) => u.id === user.id)) {
      setSelectedUsers((prev) => [...prev, user]);
    }
  };

  const handleRemoveUser = (userId: number) => {
    setSelectedUsers((prev) => prev.filter((u) => u.id !== userId));
  };

  const handleAddAll = () => {
    const newOnes = availableUsers.filter((u) => !selectedUsers.some((su) => su.id === u.id));
    setSelectedUsers((prev) => [...prev, ...newOnes]);
  };

  const handleClearSelected = () => {
    setSelectedUsers([]);
  };

  const handleConfirm = () => {
    onConfirmSelection(selectedUsers);
    onClose();
  };

  return (
    <div className="solar-modal-backdrop">
      <div className="solar-contacts-modal-card">
        {/* Cabeçalho Oficial (Espelha 03_selecao_contatos.png) */}
        <div className="contacts-modal-header">
          <div className="contacts-modal-title">
            <h2>Seleção de contatos</h2>
            <span className="contacts-required-notice">* campo(s) obrigatório(s)</span>
          </div>
          <button type="button" className="contacts-modal-close" onClick={onClose} title="Fechar">
            ✕
          </button>
        </div>

        {/* Link de Atendimento */}
        <div className="contacts-help-bar">
          <a
            href="#atendimento"
            onClick={(e) => {
              e.preventDefault();
              alert('Mensagem direcionada para o suporte central: atendimento@virtual.ufc.br');
            }}
          >
            Enviar mensagem para o atendimento?
          </a>
        </div>

        {/* Filtros de Contato */}
        <div className="contacts-filters-panel">
          <div className="filter-row">
            <div className="filter-col">
              <label className="filter-label">Contatos*</label>
              <select
                className="filter-select"
                value={contactsType}
                onChange={(e) => setContactsType(Number(e.target.value))}
              >
                <option value={1}>Contatos do Sistema</option>
                <option value={2}>Meus Contatos</option>
              </select>
            </div>

            <div className="filter-col">
              <label className="filter-label">Tipo*</label>
              <select
                className="filter-select"
                value={roleType}
                onChange={(e) => setRoleType(Number(e.target.value))}
              >
                <option value={0}>Todos</option>
                <option value={4}>Docentes / Professores</option>
                <option value={2}>Tutores a Distância</option>
                <option value={32}>Tutores Presenciais</option>
                <option value={1}>Alunos</option>
                <option value={8}>Coordenação / Edição</option>
                <option value={16}>Administração</option>
              </select>
            </div>
          </div>

          {/* Filtros acadêmicos (visíveis apenas para Contatos do Sistema como no Solar Ruby) */}
          {contactsType === 1 ? (
            <div className="filter-row" style={{ marginTop: '8px' }}>
              <div className="filter-col">
                <label className="filter-label">Curso</label>
                <select
                  className="filter-select"
                  value={course}
                  onChange={(e) => setCourse(e.target.value)}
                >
                  <option value="all">Todos os Cursos</option>
                  <option value="quimica">Licenciatura em Química</option>
                  <option value="letras">Licenciatura em Letras</option>
                  <option value="adm">Administração Pública</option>
                </select>
              </div>

              <div className="filter-col">
                <label className="filter-label">Disciplina</label>
                <select
                  className="filter-select"
                  value={discipline}
                  onChange={(e) => setDiscipline(e.target.value)}
                >
                  <option value="all">Todas as Disciplinas</option>
                  <option value="qm1">Química I (QM-CAU)</option>
                  <option value="ling">Introdução à Linguística (RM404)</option>
                  <option value="lit">Teoria da Literatura I (RM405)</option>
                </select>
              </div>

              <div className="filter-col">
                <label className="filter-label">Semestre</label>
                <select
                  className="filter-select"
                  value={semester}
                  onChange={(e) => setSemester(e.target.value)}
                >
                  <option value="all">Todos os Semestres</option>
                  <option value="2026.1">2026.1</option>
                  <option value="2025.2">2025.2</option>
                </select>
              </div>
            </div>
          ) : (
            <div style={{ marginTop: '8px', fontSize: '0.8rem', color: '#0369a1', background: '#e0f2fe', padding: '6px 10px', borderRadius: '4px' }}>
              ℹ️ <strong>Meus Contatos:</strong> Exibindo professores, tutores, coordenação e colegas vinculados diretamente às suas turmas.
            </div>
          )}

          <div className="filter-action-row" style={{ marginTop: '10px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div className="filter-radios" style={{ display: 'flex', gap: '16px', fontSize: '0.85rem' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '4px', cursor: 'pointer' }}>
                <input
                  type="radio"
                  name="selectionMode"
                  checked={selectionMode === 'offer'}
                  onChange={() => setSelectionMode('offer')}
                />
                Oferta
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: '4px', cursor: 'pointer' }}>
                <input
                  type="radio"
                  name="selectionMode"
                  checked={selectionMode === 'group'}
                  onChange={() => setSelectionMode('group')}
                />
                Turma
              </label>
            </div>

            <div style={{ display: 'flex', gap: '8px' }}>
              <input
                type="text"
                placeholder="Buscar por nome/email..."
                value={searchFilter}
                onChange={(e) => setSearchFilter(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    fetchContacts();
                  }
                }}
                className="filter-input-search"
                style={{ padding: '4px 8px', fontSize: '0.82rem', borderRadius: '3px', border: '1px solid #ccc' }}
              />
              <button
                type="button"
                className="btn-filter-action"
                onClick={fetchContacts}
              >
                Filtrar
              </button>
            </div>
          </div>
        </div>

        {/* 1. Lista de Usuários Disponíveis */}
        <div className="contacts-box-container">
          <div className="contacts-box-header">
            <strong>Lista de usuários para seleção de destinatários</strong>
            <button
              type="button"
              className="btn-link-action"
              onClick={handleAddAll}
              disabled={availableUsers.length === 0}
            >
              + Adicionar Todos
            </button>
          </div>
          <div className="contacts-list-scroll">
            {loading ? (
              <div className="contacts-loading">Carregando contatos do sistema...</div>
            ) : availableUsers.length === 0 ? (
              <div className="contacts-empty">Nenhum usuário encontrado com os filtros selecionados.</div>
            ) : (
              <div className="contacts-grid-items">
                {availableUsers.map((user) => {
                  const isSelected = selectedUsers.some((u) => u.id === user.id);
                  return (
                    <div
                      key={user.id}
                      className={`contact-item-card ${isSelected ? 'already-selected' : ''}`}
                      onClick={() => !isSelected && handleAddUser(user)}
                    >
                      <div className="contact-card-info">
                        <span className="contact-name">{user.name}</span>
                        <span className="contact-email">{user.email}</span>
                        <span className="contact-role-badge">{user.role}</span>
                      </div>
                      <button
                        type="button"
                        className="btn-add-contact"
                        disabled={isSelected}
                        onClick={(e) => {
                          e.stopPropagation();
                          handleAddUser(user);
                        }}
                      >
                        {isSelected ? '✓ Adicionado' : '+ Adicionar'}
                      </button>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        {/* 2. Lista de Destinatários Selecionados */}
        <div className="contacts-box-container" style={{ marginTop: '12px' }}>
          <div className="contacts-box-header">
            <strong>Lista de destinatários selecionados ({selectedUsers.length})</strong>
            {selectedUsers.length > 0 && (
              <button
                type="button"
                className="btn-link-action btn-danger-link"
                onClick={handleClearSelected}
              >
                Limpar Selecionados
              </button>
            )}
          </div>
          <div className="contacts-list-scroll contacts-selected-box">
            {selectedUsers.length === 0 ? (
              <div className="contacts-empty" style={{ fontStyle: 'italic', color: '#888' }}>
                Nenhum destinatário selecionado. Clique em "+ Adicionar" acima para incluir contatos.
              </div>
            ) : (
              <div className="selected-chips-container">
                {selectedUsers.map((u) => (
                  <div key={u.id} className="selected-contact-chip">
                    <span className="chip-name">{u.name}</span>
                    <span className="chip-role">({u.role})</span>
                    <button
                      type="button"
                      className="chip-remove-btn"
                      onClick={() => handleRemoveUser(u.id)}
                      title="Remover destinatário"
                    >
                      ✕
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Rodapé de Ações */}
        <div className="contacts-modal-footer">
          <button type="button" className="btn-solar-secondary" onClick={onClose}>
            Cancelar
          </button>
          <button
            type="button"
            className="btn-solar-blue"
            onClick={handleConfirm}
            disabled={selectedUsers.length === 0}
          >
            Confirmar Destinatários ({selectedUsers.length})
          </button>
        </div>
      </div>
    </div>
  );
};
