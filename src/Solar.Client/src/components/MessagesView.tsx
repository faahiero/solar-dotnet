import { useState, useEffect } from 'react';
import type { InternalMailMessage } from '../types/academic';

export const MessagesView = () => {
  const [activeFolder, setActiveFolder] = useState<'inbox' | 'outbox' | 'trash'>('inbox');
  const [messages, setMessages] = useState<InternalMailMessage[]>([]);
  const [searchSubject, setSearchSubject] = useState('');
  const [searchPerson, setSearchPerson] = useState('');
  const [selectedMessage, setSelectedMessage] = useState<InternalMailMessage | null>(null);
  const [showComposeModal, setShowComposeModal] = useState(false);
  const [recipient, setRecipient] = useState('');
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');
  const [sending, setSending] = useState(false);

  useEffect(() => {
    fetch(`/api/v1/messages?folder=${activeFolder}`)
      .then((res) => res.json())
      .then((data) => {
        setMessages(data);
        setSelectedMessage(null);
      })
      .catch((err) => console.error('Erro ao carregar mensagens:', err));
  }, [activeFolder]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!recipient || !subject || !body) {
      alert('Preencha todos os campos da mensagem.');
      return;
    }

    setSending(true);
    try {
      const res = await fetch('/api/v1/messages', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ recipient, subject, body })
      });
      const data = await res.json();
      if (data.success) {
        alert('Mensagem transmitida com sucesso!');
        setShowComposeModal(false);
        setRecipient('');
        setSubject('');
        setBody('');
      }
    } catch (err) {
      alert('Erro ao enviar mensagem: ' + err);
    } finally {
      setSending(false);
    }
  };

  const filtered = messages.filter((m) => {
    const matchSub = m.subject.toLowerCase().includes(searchSubject.toLowerCase());
    const matchPerson =
      (m.sender?.toLowerCase().includes(searchPerson.toLowerCase()) ?? false) ||
      (m.recipient?.toLowerCase().includes(searchPerson.toLowerCase()) ?? false);
    return matchSub && (searchPerson ? matchPerson : true);
  });

  return (
    <div className="solar-portlet-card messages-view-card">
      <div className="portlet-table-header">
        <div className="portlet-title-left">
          <span className="portlet-title-icon">✉️</span>
          <strong>Correio Eletrônico Interno</strong>
        </div>
      </div>

      <div style={{ padding: '16px' }}>
        {/* Abas das Pastas de Mensagens */}
        <div className="messages-folder-tabs">
          <button
            type="button"
            className={`msg-folder-btn ${activeFolder === 'inbox' ? 'active' : ''}`}
            onClick={() => setActiveFolder('inbox')}
          >
            📥 Entrada ({messages.filter((m) => !m.read && activeFolder === 'inbox').length})
          </button>
          <button
            type="button"
            className={`msg-folder-btn ${activeFolder === 'outbox' ? 'active' : ''}`}
            onClick={() => setActiveFolder('outbox')}
          >
            📤 Saída
          </button>
          <button
            type="button"
            className={`msg-folder-btn ${activeFolder === 'trash' ? 'active' : ''}`}
            onClick={() => setActiveFolder('trash')}
          >
            🗑️ Lixeira
          </button>
        </div>

        {/* Barra de Busca e Ações Rápidas (Espelha 03_mensagens_correio.png) */}
        <div className="messages-filter-action-bar">
          <div className="msg-search-group">
            <select className="msg-select-filter">
              <option>Visualizar: Todas</option>
              <option>Não lidas</option>
              <option>Importantes</option>
            </select>
            <input
              type="text"
              placeholder="Assunto a pesquisar"
              value={searchSubject}
              onChange={(e) => setSearchSubject(e.target.value)}
              className="msg-input-search"
            />
            <input
              type="text"
              placeholder="Remetente ou destinatário a pesquisar"
              value={searchPerson}
              onChange={(e) => setSearchPerson(e.target.value)}
              className="msg-input-search"
            />
            <button type="button" className="msg-btn-search" title="Buscar">
              🔍
            </button>
          </div>

          <div className="msg-actions-group">
            <button
              type="button"
              className="btn-msg-icon-action btn-add-msg"
              title="Nova Mensagem"
              onClick={() => setShowComposeModal(true)}
            >
              ➕ Nova Mensagem
            </button>
            <button
              type="button"
              className="btn-msg-icon-action"
              title="Marcar como lida"
              onClick={() => alert('Mensagens marcadas como lidas')}
            >
              ✉️
            </button>
            <button
              type="button"
              className="btn-msg-icon-action"
              title="Excluir selecionadas"
              onClick={() => alert('Mensagem movida para lixeira')}
            >
              🗑️
            </button>
          </div>
        </div>

        {/* Listagem de Mensagens */}
        {filtered.length === 0 ? (
          <div className="empty-messages-notice">
            Nenhuma mensagem encontrada nesta pasta.
          </div>
        ) : (
          <table className="solar-table" style={{ marginTop: '12px' }}>
            <thead>
              <tr>
                <th style={{ width: '30px' }}></th>
                <th>{activeFolder === 'outbox' ? 'Destinatário' : 'Remetente'}</th>
                <th>Assunto</th>
                <th style={{ width: '140px', textAlign: 'center' }}>Data</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((m) => (
                <tr
                  key={m.id}
                  style={{
                    fontWeight: m.read ? 400 : 700,
                    background: m.read ? '#fff' : '#f0fdf4',
                    cursor: 'pointer'
                  }}
                  onClick={() => setSelectedMessage(m)}
                >
                  <td style={{ textAlign: 'center' }}>{m.read ? '✉' : '📩'}</td>
                  <td>{m.sender || m.recipient}</td>
                  <td>{m.subject}</td>
                  <td style={{ textAlign: 'center', fontSize: '0.8rem', color: '#666' }}>{m.date}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        {/* Leitor de Mensagem Selecionada */}
        {selectedMessage && (
          <div className="message-detail-card" style={{ marginTop: '20px', padding: '16px', border: '1px solid var(--solar-border)', borderRadius: '4px', background: '#fafbfc' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid #ddd', paddingBottom: '8px' }}>
              <div>
                <h3 style={{ fontSize: '1.05rem', color: 'var(--solar-blue-dark)' }}>{selectedMessage.subject}</h3>
                <span style={{ fontSize: '0.82rem', color: '#555' }}>
                  De: <strong>{selectedMessage.sender || 'Você'}</strong> • Para: <strong>{selectedMessage.recipient || 'Você'}</strong>
                </span>
              </div>
              <span style={{ fontSize: '0.8rem', color: '#888' }}>{selectedMessage.date}</span>
            </div>
            <div style={{ marginTop: '12px', fontSize: '0.9rem', lineHeight: 1.6, color: '#333' }}>
              {selectedMessage.body}
            </div>
          </div>
        )}
      </div>

      {/* Modal de Nova Mensagem */}
      {showComposeModal && (
        <div className="solar-modal-backdrop">
          <div className="solar-modal-card">
            <div className="solar-modal-header">
              <strong>Nova Mensagem Interna</strong>
              <span onClick={() => setShowComposeModal(false)} style={{ cursor: 'pointer' }}>✕</span>
            </div>
            <form onSubmit={handleSendMessage} style={{ padding: '16px' }}>
              <div className="form-group">
                <label>Destinatário (Nome ou E-mail)</label>
                <input
                  type="text"
                  placeholder="Ex: Prof. Titular UAB ou Aluno 2"
                  value={recipient}
                  onChange={(e) => setRecipient(e.target.value)}
                  required
                />
              </div>
              <div className="form-group">
                <label>Assunto</label>
                <input
                  type="text"
                  placeholder="Assunto da mensagem"
                  value={subject}
                  onChange={(e) => setSubject(e.target.value)}
                  required
                />
              </div>
              <div className="form-group">
                <label>Mensagem</label>
                <textarea
                  rows={4}
                  placeholder="Escreva sua mensagem aqui..."
                  value={body}
                  onChange={(e) => setBody(e.target.value)}
                  style={{ width: '100%', padding: '10px', borderRadius: '4px', border: '1px solid var(--solar-border)', fontFamily: 'inherit' }}
                  required
                />
              </div>
              <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end', marginTop: '14px' }}>
                <button type="button" className="btn-solar-secondary" onClick={() => setShowComposeModal(false)}>
                  Cancelar
                </button>
                <button type="submit" className="btn-solar-blue" disabled={sending}>
                  {sending ? 'Enviando...' : 'Transmitir Mensagem'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
