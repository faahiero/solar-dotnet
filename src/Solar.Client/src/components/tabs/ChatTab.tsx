import { useState } from 'react';
import * as signalR from '@microsoft/signalr';
import type { UserProfile } from '../../types/auth';

interface ChatTabProps {
  user: UserProfile;
}

interface ChatMessage {
  senderName: string;
  message: string;
}

export const ChatTab = ({ user }: ChatTabProps) => {
  const [room, setRoom] = useState('turma_calculo_1');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputMsg, setInputMsg] = useState('');
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [connected, setConnected] = useState(false);

  const handleConnect = async () => {
    if (connection) return;

    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/chat')
      .withAutomaticReconnect()
      .build();

    newConnection.on('ReceiveMessage', (data: ChatMessage) => {
      setMessages((prev) => [...prev, data]);
    });

    try {
      await newConnection.start();
      await newConnection.invoke('JoinRoom', room);
      setConnection(newConnection);
      setConnected(true);
    } catch (err) {
      alert('Erro ao conectar ao SignalR: ' + err);
    }
  };

  const handleSend = async () => {
    if (!inputMsg.trim() || !connection) return;
    try {
      await connection.invoke('SendMessage', room, user.name || user.username, inputMsg.trim());
      setInputMsg('');
    } catch (err) {
      alert('Erro ao enviar mensagem: ' + err);
    }
  };

  return (
    <div className="solar-portlet">
      <div className="solar-portlet-header">
        <div className="solar-portlet-header-title">
          <img src="/assets/images/icon_chat.png" alt="" className="portlet-icon" />
          <span>Chat da Turma em Tempo Real (SignalR)</span>
        </div>
      </div>

      <div className="solar-portlet-body">
        <div className="grid-2" style={{ marginBottom: '16px' }}>
          <div>
            <label htmlFor="roomInput">Sala / Turma</label>
            <input
              id="roomInput"
              type="text"
              value={room}
              onChange={(e) => setRoom(e.target.value)}
              disabled={connected}
            />
          </div>
          <div>
            <label htmlFor="userInput">Usuário Identificado</label>
            <input
              id="userInput"
              type="text"
              value={user.name || user.username}
              disabled
            />
          </div>
        </div>

        <button
          type="button"
          className="btn-solar-blue"
          onClick={handleConnect}
          disabled={connected}
          style={{ width: 'auto' }}
        >
          {connected ? '🟢 Conectado na Sala' : 'Conectar à Sala'}
        </button>

        <div className="chat-container" style={{ marginTop: '16px' }}>
          <div className="chat-messages">
            {messages.length === 0 ? (
              <div className="chat-msg other">
                <strong>Sistema:</strong> Conecte-se à sala para interagir com a turma.
              </div>
            ) : (
              messages.map((m, i) => (
                <div
                  key={i}
                  className={`chat-msg ${m.senderName === (user.name || user.username) ? 'self' : 'other'}`}
                >
                  <strong>{m.senderName}:</strong> {m.message}
                </div>
              ))
            )}
          </div>
          <div className="chat-input-bar">
            <input
              type="text"
              placeholder="Digite uma mensagem..."
              value={inputMsg}
              onChange={(e) => setInputMsg(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') handleSend(); }}
              disabled={!connected}
            />
            <button
              type="button"
              className="btn-solar-blue"
              onClick={handleSend}
              disabled={!connected}
              style={{ width: 'auto' }}
            >
              Enviar
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
