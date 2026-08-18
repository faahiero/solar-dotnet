export const ImportTab = () => {
  return (
    <div className="solar-portlet">
      <div className="solar-portlet-header">
        <div className="solar-portlet-header-title">
          <img src="/assets/images/icon_arrow_right.png" alt="" className="portlet-icon" />
          <span>Clonagem de Disciplinas (DisciplineImportService)</span>
        </div>
      </div>

      <div className="solar-portlet-body">
        <p style={{ fontSize: '0.88rem', color: 'var(--solar-text-secondary)', marginBottom: '16px' }}>
          Visualização do reajuste automático de cronogramas e importação de conteúdos entre períodos letivos.
        </p>

        <table className="solar-table">
          <thead>
            <tr>
              <th>Ferramenta</th>
              <th>Tipo</th>
              <th>Data Origem (2025.2)</th>
              <th>Data Reajustada (2026.1)</th>
              <th>Status de Suporte</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Prova Bimestral 1</td>
              <td>Exam</td>
              <td>15/09/2025</td>
              <td><strong style={{ color: 'var(--solar-blue-main)' }}>15/03/2026</strong></td>
              <td><span style={{ color: 'var(--solar-success)', fontWeight: 600 }}>✔ Suportado</span></td>
            </tr>
            <tr>
              <td>Trabalho em Grupo</td>
              <td>Assignment</td>
              <td>01/10/2025</td>
              <td><strong style={{ color: 'var(--solar-blue-main)' }}>01/04/2026</strong></td>
              <td><span style={{ color: 'var(--solar-success)', fontWeight: 600 }}>✔ Suportado</span></td>
            </tr>
            <tr>
              <td>Fórum de Apresentação</td>
              <td>Discussion</td>
              <td>01/08/2025</td>
              <td><strong style={{ color: 'var(--solar-blue-main)' }}>01/02/2026</strong></td>
              <td><span style={{ color: 'var(--solar-success)', fontWeight: 600 }}>✔ Suportado</span></td>
            </tr>
            <tr>
              <td>Aula Inaugural Ao Vivo</td>
              <td>Webconference</td>
              <td>05/08/2025</td>
              <td>—</td>
              <td><span style={{ color: 'var(--solar-error)', fontWeight: 600 }}>✖ Ignorado (Sessão BBB específica)</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
};
