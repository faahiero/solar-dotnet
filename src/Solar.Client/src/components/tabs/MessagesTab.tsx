export const MessagesTab = () => {
  return (
    <div className="grid-2">
      <div className="solar-portlet">
        <div className="solar-portlet-header">
          <div className="solar-portlet-header-title">
            <img src="/assets/images/icon_comments.png" alt="" className="portlet-icon" />
            <span>Mural de Avisos da Turma</span>
          </div>
        </div>
        <div className="solar-portlet-body">
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <div style={{ border: '1px solid var(--solar-border)', padding: '12px', borderRadius: '4px', background: '#fafbfc', borderLeft: '4px solid var(--solar-blue-main)' }}>
              <strong style={{ color: 'var(--solar-blue-main)' }}>📢 Horário de Atendimento Remoto</strong>
              <p style={{ fontSize: '0.85rem', marginTop: '4px' }}>Terças e quintas às 14:00 via BigBlueButton.</p>
              <span style={{ fontSize: '0.75rem', color: 'var(--solar-text-muted)' }}>Publicado em 18/08/2026 pelo Professor</span>
            </div>
            <div style={{ border: '1px solid var(--solar-border)', padding: '12px', borderRadius: '4px', background: '#fafbfc', borderLeft: '4px solid var(--solar-blue-main)' }}>
              <strong style={{ color: 'var(--solar-blue-main)' }}>📢 Liberação das Notas da Prova 1</strong>
              <p style={{ fontSize: '0.85rem', marginTop: '4px' }}>As notas da P1 já estão disponíveis no Diário de Notas.</p>
              <span style={{ fontSize: '0.75rem', color: 'var(--solar-text-muted)' }}>Publicado em 17/08/2026 pelo Professor</span>
            </div>
          </div>
        </div>
      </div>

      <div className="solar-portlet">
        <div className="solar-portlet-header">
          <div className="solar-portlet-header-title">
            <img src="/assets/images/icon_message.png" alt="" className="portlet-icon" />
            <span>Correio Eletrônico Interno</span>
          </div>
        </div>
        <div className="solar-portlet-body">
          <div style={{ fontSize: '0.88rem', display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <div style={{ padding: '8px 10px', background: '#f5f8fb', border: '1px solid #dce4ee', borderRadius: '4px' }}>
              • <strong>Dúvida no Exercício 4</strong> - Aluno João (Hoje às 09:30)
            </div>
            <div style={{ padding: '8px 10px', background: '#f5f8fb', border: '1px solid #dce4ee', borderRadius: '4px' }}>
              • <strong>Revisão de Menção</strong> - Aluna Maria (Ontem às 18:45)
            </div>
            <div style={{ padding: '8px 10px', background: '#f5f8fb', border: '1px solid #dce4ee', borderRadius: '4px' }}>
              • <strong>Agendamento de Webconferência</strong> - Coordenação (16/08)
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
