import { useState, useEffect } from 'react';
import type { InternalMailMessage } from '../types/academic';
import type { UserProfile } from '../types/auth';
import { ContactsModal, type ContactUser } from './ContactsModal';

interface MessagesViewProps {
  user?: UserProfile | null;
}

export const MessagesView = ({ user }: MessagesViewProps) => {
  const currentUserId = user?.id || (localStorage.getItem('solar_user') ? JSON.parse(localStorage.getItem('solar_user')!).id : 7);
  const [viewMode, setViewMode] = useState<'list' | 'compose' | 'detail'>('list');
  const [activeFolder, setActiveFolder] = useState<'inbox' | 'outbox' | 'trash'>('inbox');
  const [filterRead, setFilterRead] = useState<'all' | 'unread' | 'read'>('all');
  const [searchSubject, setSearchSubject] = useState('');
  const [searchPerson, setSearchPerson] = useState('');

  const [messages, setMessages] = useState<InternalMailMessage[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [selectedMessage, setSelectedMessage] = useState<InternalMailMessage | null>(null);

  // Compose State
  const [showContactsModal, setShowContactsModal] = useState(false);
  const [selectedContacts, setSelectedContacts] = useState<ContactUser[]>([]);
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');
  const [attachments, setAttachments] = useState<{ id: number; name: string; size: string }[]>([]);
  const [sending, setSending] = useState(false);

  const fetchMessages = () => {
    const params = new URLSearchParams();
    params.set('folder', activeFolder);
    if (currentUserId) params.set('userId', currentUserId.toString());
    if (filterRead !== 'all') params.set('filter', filterRead);
    if (searchSubject) params.set('subject', searchSubject);
    if (searchPerson) params.set('user', searchPerson);

    fetch(`/api/v1/messages?${params.toString()}`)
      .then((res) => res.json())
      .then((data) => {
        if (Array.isArray(data)) {
          setMessages(data);
          setUnreadCount(data.filter((m: InternalMailMessage) => !m.read).length);
        } else if (data && data.messages) {
          setMessages(data.messages);
          setUnreadCount(data.unreadCount || 0);
        }
        setSelectedIds([]);
      })
      .catch((err) => console.error('Erro ao carregar mensagens:', err));
  };

  useEffect(() => {
    fetchMessages();
  }, [activeFolder, filterRead]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchMessages();
  };

  const handleSelectAll = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.checked) {
      setSelectedIds(messages.map((m) => m.id));
    } else {
      setSelectedIds([]);
    }
  };

  const handleToggleSelect = (id: number) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    );
  };

  const handleUpdateStatus = async (status: 'read' | 'unread' | 'trash' | 'restore') => {
    if (selectedIds.length === 0 && !selectedMessage) {
      alert('Selecione pelo menos uma mensagem.');
      return;
    }

    const ids = selectedIds.length > 0 ? selectedIds : selectedMessage ? [selectedMessage.id] : [];

    if (status === 'trash' && !confirm('Deseja realmente mover a(s) mensagem(ns) selecionada(s) para a lixeira?')) {
      return;
    }

    try {
      const res = await fetch('/api/v1/messages/status', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messageIds: ids, newStatus: status })
      });
      const data = await res.json();
      if (data.success) {
        fetchMessages();
        if (selectedMessage) {
          setViewMode('list');
          setSelectedMessage(null);
        }
      }
    } catch (err) {
      alert('Erro ao atualizar status: ' + err);
    }
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedContacts.length === 0) {
      alert('Selecione pelo menos um destinatário clicando em "Adicionar destinatários".');
      return;
    }
    if (!subject.trim()) {
      alert('Informe o assunto da mensagem.');
      return;
    }
    if (!body.trim()) {
      alert('Escreva o conteúdo da mensagem.');
      return;
    }

    setSending(true);
    try {
      const res = await fetch('/api/v1/messages', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          recipientIds: selectedContacts.map((c) => c.id),
          subject,
          body,
          attachments: attachments.map((a) => a.name)
        })
      });
      const data = await res.json();
      if (data.success) {
        alert('Mensagem transmitida com sucesso para os destinatários!');
        setViewMode('list');
        setActiveFolder('outbox');
        setSelectedContacts([]);
        setSubject('');
        setBody('');
        setAttachments([]);
        fetchMessages();
      }
    } catch (err) {
      alert('Erro ao enviar mensagem: ' + err);
    } finally {
      setSending(false);
    }
  };

  const handleOpenMessage = (msg: InternalMailMessage) => {
    setSelectedMessage(msg);
    setViewMode('detail');
    // Marcar como lida automaticamente
    if (!msg.read) {
      fetch('/api/v1/messages/status', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messageIds: [msg.id], newStatus: 'read' })
      }).then(() => {
        setMessages((prev) =>
          prev.map((m) => (m.id === msg.id ? { ...m, read: true } : m))
        );
        setUnreadCount((c) => Math.max(0, c - 1));
      });
    }
  };

  const handleReply = (_isAll: boolean = false) => {
    if (!selectedMessage) return;
    setSubject(`Re: ${selectedMessage.subject}`);
    setBody(
      `\n\n----------------------------------------\nDe: ${selectedMessage.sender}\nData: ${selectedMessage.date}\nAssunto: ${selectedMessage.subject}\n\n${selectedMessage.body}`
    );
    setSelectedContacts([
      {
        id: 6,
        name: selectedMessage.sender || 'Professor',
        email: 'prof@solar.ufc.br',
        username: 'prof',
        role: 'Docente / Professor',
        typeMask: 4,
        resume: `${selectedMessage.sender} <prof@solar.ufc.br> (Professor)`
      }
    ]);
    setViewMode('compose');
  };

  const handleAddAttachment = () => {
    const fileName = prompt('Informe o nome do arquivo a anexar (ex: relatorio_laboratorio.pdf):');
    if (fileName) {
      setAttachments((prev) => [
        ...prev,
        { id: Date.now(), name: fileName, size: '1.2 MB' }
      ]);
    }
  };

  const handleRemoveAttachment = (id: number) => {
    setAttachments((prev) => prev.filter((a) => a.id !== id));
  };

  return (
    <div className="solar-messages-container">
      {/* Breadcrumb Oficial (Espelha 01_mensagens_lista.png) */}
      <div className="solar-breadcrumb-bar">
        <span className="breadcrumb-link" onClick={() => setViewMode('list')}>Home</span>
        <span className="breadcrumb-separator">&gt;</span>
        <span className="breadcrumb-current">Mensagens</span>
      </div>

      <div className="solar-messages-card">
        {/* Título Principal */}
        <h1 className="messages-main-title">Mensagens</h1>

        {/* 1. Cabeçalho de Abas das Pastas (Entrada, Saída, Lixeira) */}
        <div className="messages-folder-navigation">
          <button
            type="button"
            className={`folder-tab-btn ${activeFolder === 'inbox' ? 'active' : ''}`}
            onClick={() => {
              setActiveFolder('inbox');
              setViewMode('list');
            }}
          >
            <span className="folder-icon">📥</span>
            <span className="folder-name">Entrada</span>
            <span className="folder-badge">({unreadCount})</span>
          </button>

          <button
            type="button"
            className={`folder-tab-btn ${activeFolder === 'outbox' ? 'active' : ''}`}
            onClick={() => {
              setActiveFolder('outbox');
              setViewMode('list');
            }}
          >
            <span className="folder-icon">📤</span>
            <span className="folder-name">Saída</span>
          </button>

          <button
            type="button"
            className={`folder-tab-btn ${activeFolder === 'trash' ? 'active' : ''}`}
            onClick={() => {
              setActiveFolder('trash');
              setViewMode('list');
            }}
          >
            <span className="folder-icon">🗑️</span>
            <span className="folder-name">Lixeira</span>
          </button>
        </div>

        {/* 2. Barra de Filtros, Pesquisa e Ações em Lote (Quando em modo Lista) */}
        {viewMode === 'list' && (
          <div className="messages-toolbar-bar">
            {/* Esquerda: Filtro de Visualização */}
            <div className="toolbar-view-dropdown">
              <select
                className="select-view-mode"
                value={filterRead}
                onChange={(e) => setFilterRead(e.target.value as 'all' | 'unread' | 'read')}
              >
                <option value="all">Visualizar: Todas ▼</option>
                <option value="unread">Não lidas</option>
                <option value="read">Lidas</option>
              </select>
            </div>

            {/* Centro: Formulário de Pesquisa */}
            <form onSubmit={handleSearch} className="toolbar-search-form">
              <input
                type="text"
                placeholder="Assunto a pesquisar"
                value={searchSubject}
                onChange={(e) => setSearchSubject(e.target.value)}
                className="input-search-field"
              />
              <input
                type="text"
                placeholder="Remetente ou destinatário a pesquisar"
                value={searchPerson}
                onChange={(e) => setSearchPerson(e.target.value)}
                className="input-search-field"
              />
              <button type="submit" className="btn-search-icon" title="Pesquisar">
                🔍
              </button>
            </form>

            {/* Direita: Botões de Ação do Solar */}
            <div className="toolbar-action-buttons">
              <button
                type="button"
                className="btn-action-solar btn-new-msg"
                title="Nova Mensagem"
                onClick={() => {
                  setViewMode('compose');
                  setSelectedContacts([]);
                  setSubject('');
                  setBody('');
                  setAttachments([]);
                }}
              >
                ➕
              </button>

              {activeFolder === 'inbox' && (
                <>
                  <button
                    type="button"
                    className="btn-action-solar"
                    title="Marcar como lida"
                    onClick={() => handleUpdateStatus('read')}
                    disabled={selectedIds.length === 0}
                  >
                    ✉️
                  </button>
                  <button
                    type="button"
                    className="btn-action-solar"
                    title="Marcar como não lida"
                    onClick={() => handleUpdateStatus('unread')}
                    disabled={selectedIds.length === 0}
                  >
                    📭
                  </button>
                </>
              )}

              {activeFolder !== 'trash' ? (
                <button
                  type="button"
                  className="btn-action-solar btn-trash-msg"
                  title="Mover selecionadas para a lixeira"
                  onClick={() => handleUpdateStatus('trash')}
                  disabled={selectedIds.length === 0}
                >
                  🗑️
                </button>
              ) : (
                <button
                  type="button"
                  className="btn-action-solar btn-restore-msg"
                  title="Restaurar mensagens da lixeira"
                  onClick={() => handleUpdateStatus('restore')}
                  disabled={selectedIds.length === 0}
                >
                  ♻️
                </button>
              )}
            </div>
          </div>
        )}

        {/* 3. CONTEÚDO PRINCIPAL: MODO LISTAGEM */}
        {viewMode === 'list' && (
          <div className="messages-list-wrapper">
            {messages.length === 0 ? (
              <div className="messages-empty-box">
                Nenhuma mensagem encontrada
              </div>
            ) : (
              <table className="solar-messages-table">
                <thead>
                  <tr>
                    <th style={{ width: '36px', textAlign: 'center' }}>
                      <input
                        type="checkbox"
                        checked={selectedIds.length === messages.length && messages.length > 0}
                        onChange={handleSelectAll}
                        title="Selecionar todas"
                      />
                    </th>
                    <th style={{ width: '36px', textAlign: 'center' }}></th>
                    <th style={{ width: '28%' }}>
                      {activeFolder === 'outbox' ? 'Destinatário' : 'Remetente'}
                    </th>
                    <th>Assunto</th>
                    <th style={{ width: '130px', textAlign: 'center' }}>Data</th>
                  </tr>
                </thead>
                <tbody>
                  {messages.map((m) => {
                    const isSelected = selectedIds.includes(m.id);
                    return (
                      <tr
                        key={m.id}
                        className={`message-table-row ${!m.read ? 'row-unread' : ''} ${isSelected ? 'row-selected' : ''}`}
                        onClick={() => handleOpenMessage(m)}
                      >
                        <td
                          style={{ textAlign: 'center' }}
                          onClick={(e) => e.stopPropagation()}
                        >
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => handleToggleSelect(m.id)}
                          />
                        </td>
                        <td style={{ textAlign: 'center', fontSize: '1.1rem' }}>
                          {m.read ? '✉️' : '📩'}
                        </td>
                        <td className="msg-sender-cell">
                          <strong>{activeFolder === 'outbox' ? m.recipient : m.sender}</strong>
                        </td>
                        <td className="msg-subject-cell">
                          <span className={`subject-text ${!m.read ? 'bold-unread' : ''}`}>
                            {m.subject}
                          </span>
                        </td>
                        <td style={{ textAlign: 'center', fontSize: '0.8rem', color: '#666' }}>
                          {m.date}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
          </div>
        )}

        {/* 4. CONTEÚDO PRINCIPAL: MODO COMPOR NOVA MENSAGEM (Espelha 02_mensagens_compor.png) */}
        {viewMode === 'compose' && (
          <div className="messages-compose-wrapper">
            <form onSubmit={handleSendMessage} className="messages-compose-form">
              {/* Linha 1: Destinatários */}
              <div className="compose-field-row">
                <label className="compose-field-label">Destinatários</label>
                <div className="compose-field-input-group">
                  <div className="compose-recipients-box">
                    {selectedContacts.length === 0 ? (
                      <span className="recipients-placeholder">Nenhum destinatário adicionado</span>
                    ) : (
                      selectedContacts.map((c) => (
                        <span key={c.id} className="recipient-pill">
                          {c.name}
                          <button
                            type="button"
                            onClick={() => setSelectedContacts((prev) => prev.filter((p) => p.id !== c.id))}
                          >
                            ✕
                          </button>
                        </span>
                      ))
                    )}
                  </div>
                  <button
                    type="button"
                    className="btn-add-recipients-link"
                    onClick={() => setShowContactsModal(true)}
                  >
                    Adicionar destinatários
                  </button>
                </div>
              </div>

              {/* Linha 2: Assunto */}
              <div className="compose-field-row">
                <label className="compose-field-label">Assunto</label>
                <div className="compose-field-input-group">
                  <input
                    type="text"
                    placeholder="Informe o assunto da mensagem..."
                    value={subject}
                    onChange={(e) => setSubject(e.target.value)}
                    className="compose-input-text"
                    required
                  />
                  <button
                    type="button"
                    className="btn-mic-icon"
                    title="Gravação de Áudio / Acessibilidade"
                    onClick={() => alert('Recurso de Ditado e Acessibilidade por Voz ativo.')}
                  >
                    🎙️
                  </button>
                </div>
              </div>

              {/* Linha 3: Anexar */}
              <div className="compose-field-row">
                <label className="compose-field-label">Anexar</label>
                <div className="compose-field-input-group" style={{ alignItems: 'flex-start', flexDirection: 'column' }}>
                  <button
                    type="button"
                    className="btn-clip-attach"
                    onClick={handleAddAttachment}
                    title="Anexar arquivo"
                  >
                    📎 Anexar arquivo
                  </button>

                  {attachments.length > 0 && (
                    <div className="attachments-list-chips">
                      {attachments.map((att) => (
                        <span key={att.id} className="attachment-chip">
                          📄 {att.name} ({att.size})
                          <button
                            type="button"
                            onClick={() => handleRemoveAttachment(att.id)}
                            title="Remover anexo"
                          >
                            ✕
                          </button>
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              {/* Linha 4: Barra de Ferramentas Rica (Réplica CKEditor do Solar) */}
              <div className="ckeditor-toolbar-mock">
                <div className="ck-toolbar-group">
                  <button type="button" className="ck-btn" title="Cortar">✂️</button>
                  <button type="button" className="ck-btn" title="Copiar">📋</button>
                  <button type="button" className="ck-btn" title="Colar">📄</button>
                  <span className="ck-divider">|</span>
                  <button type="button" className="ck-btn" title="Desfazer">↩️</button>
                  <button type="button" className="ck-btn" title="Refazer">↪️</button>
                </div>

                <div className="ck-toolbar-group">
                  <select className="ck-select" defaultValue="estilo">
                    <option value="estilo">Estilo ▼</option>
                    <option value="titulo">Título</option>
                    <option value="subtitulo">Subtítulo</option>
                  </select>
                  <select className="ck-select" defaultValue="format">
                    <option value="format">Formatação ▼</option>
                    <option value="p">Parágrafo</option>
                    <option value="h2">Cabeçalho 2</option>
                  </select>
                  <select className="ck-select" defaultValue="font">
                    <option value="font">Fonte ▼</option>
                    <option value="arial">Arial</option>
                    <option value="verdana">Verdana</option>
                  </select>
                  <button type="button" className="ck-btn" title="Cor do Texto">🎨 A▼</button>
                </div>

                <div className="ck-toolbar-group">
                  <button type="button" className="ck-btn font-bold" title="Negrito"><strong>B</strong></button>
                  <button type="button" className="ck-btn font-italic" title="Itálico"><em>I</em></button>
                  <button type="button" className="ck-btn font-underline" title="Sublinhado"><u>U</u></button>
                  <button type="button" className="ck-btn font-strike" title="Tachado"><s>S</s></button>
                  <button type="button" className="ck-btn" title="Limpar Formatação">Tx</button>
                </div>

                <div className="ck-toolbar-group">
                  <button type="button" className="ck-btn" title="Numeração">1≡</button>
                  <button type="button" className="ck-btn" title="Marcadores">•≡</button>
                  <button type="button" className="ck-btn" title="Alinhamento Esquerda">⇤</button>
                  <button type="button" className="ck-btn" title="Alinhamento Centro">⇥</button>
                  <button type="button" className="ck-btn" title="Inserir Link">🔗</button>
                  <button type="button" className="ck-btn" title="Emoticons">😊</button>
                  <button type="button" className="ck-btn ck-fx" title="Fórmula Matemática"><em>fx</em></button>
                </div>
              </div>

              {/* Textarea do Editor */}
              <div className="compose-editor-body">
                <textarea
                  rows={10}
                  placeholder="Escreva sua mensagem aqui..."
                  value={body}
                  onChange={(e) => setBody(e.target.value)}
                  className="compose-textarea"
                  required
                />
              </div>

              {/* Rodapé de Ações: Enviar e Descartar */}
              <div className="compose-footer-actions">
                <button
                  type="submit"
                  className="btn-solar-blue"
                  disabled={sending}
                >
                  {sending ? 'Enviando...' : 'Enviar'}
                </button>
                <button
                  type="button"
                  className="btn-solar-secondary"
                  onClick={() => setViewMode('list')}
                >
                  Descartar
                </button>
              </div>
            </form>
          </div>
        )}

        {/* 5. CONTEÚDO PRINCIPAL: MODO DETALHES / LEITURA */}
        {viewMode === 'detail' && selectedMessage && (
          <div className="message-reading-pane">
            <div className="message-reading-header">
              <div className="reading-header-info">
                <h2 className="reading-subject">{selectedMessage.subject}</h2>
                <div className="reading-meta">
                  <span>De: <strong>{selectedMessage.sender}</strong></span>
                  <span>Para: <strong>{selectedMessage.recipient}</strong></span>
                  <span className="reading-date">{selectedMessage.date}</span>
                </div>
              </div>

              <div className="reading-actions-bar">
                <button
                  type="button"
                  className="btn-solar-blue"
                  onClick={() => handleReply(false)}
                >
                  ↩️ Responder
                </button>
                <button
                  type="button"
                  className="btn-solar-secondary"
                  onClick={() => handleReply(true)}
                >
                  👥 Responder a Todos
                </button>
                <button
                  type="button"
                  className="btn-solar-secondary"
                  onClick={() => handleUpdateStatus('trash')}
                >
                  🗑️ Excluir
                </button>
                <button
                  type="button"
                  className="btn-solar-secondary"
                  onClick={() => setViewMode('list')}
                >
                  Voltar à Lista
                </button>
              </div>
            </div>

            <div className="message-reading-body">
              {selectedMessage.body.split('\n').map((paragraph, index) => (
                <p key={index}>{paragraph || <br />}</p>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Modal de Seleção de Contatos */}
      {showContactsModal && (
        <ContactsModal
          onClose={() => setShowContactsModal(false)}
          onConfirmSelection={(contacts) => setSelectedContacts(contacts)}
          initiallySelected={selectedContacts}
          currentUserId={currentUserId}
        />
      )}
    </div>
  );
};
