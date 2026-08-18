export const AssignmentsTab = () => {
  return (
    <div className="solar-portlet">
      <div className="solar-portlet-header">
        <div className="solar-portlet-header-title">
          <img src="/assets/images/icon_suitcase_portfolio.png" alt="" className="portlet-icon" />
          <span>Trabalhos Individuais e em Grupo (Assignments)</span>
        </div>
      </div>

      <div className="solar-portlet-body">
        <div className="grid-2">
          <div style={{ border: '1px solid var(--solar-border)', padding: '16px', borderRadius: '4px', background: 'var(--solar-blue-card)' }}>
            <h3 style={{ fontSize: '1rem', color: 'var(--solar-blue-dark)', marginBottom: '8px' }}>Trabalho Prático 1 (Em Grupo)</h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--solar-text-secondary)', marginBottom: '12px' }}>
              Prazo final: 30/11/2026 às 23:59 • Máx: 4 alunos por grupo
            </p>
            <div className="login-notice" style={{ marginTop: 0, marginBottom: '12px' }}>
              <strong>Grupo 01:</strong> Ana Silva (Líder), Carlos Eduardo, Fabrício Lima
            </div>
            <button type="button" className="btn-solar-blue" style={{ fontSize: '0.85rem' }}>
              Enviar Arquivo do Grupo
            </button>
          </div>

          <div style={{ border: '1px solid var(--solar-border)', padding: '16px', borderRadius: '4px', background: 'var(--solar-blue-card)' }}>
            <h3 style={{ fontSize: '1rem', color: 'var(--solar-blue-dark)', marginBottom: '8px' }}>Trabalho Individual 2</h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--solar-text-secondary)', marginBottom: '12px' }}>
              Prazo final: 15/12/2026 às 23:59
            </p>
            <div style={{ background: 'var(--solar-warning-bg)', color: 'var(--solar-warning)', padding: '8px 12px', borderRadius: '4px', fontSize: '0.85rem', marginBottom: '12px', borderLeft: '4px solid var(--solar-warning)' }}>
              <strong>Status:</strong> Aguardando envio de arquivo pelo aluno
            </div>
            <button type="button" className="btn-solar-secondary" style={{ fontSize: '0.85rem' }}>
              Selecionar Arquivo PDF
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
