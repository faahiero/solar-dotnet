export const LessonsTab = () => {
  return (
    <div className="solar-portlet">
      <div className="solar-portlet-header">
        <div className="solar-portlet-header-title">
          <img src="/assets/images/icon_lesson.png" alt="" className="portlet-icon" />
          <span>Módulos Didáticos e Aulas Interativas</span>
        </div>
      </div>

      <div className="solar-portlet-body">
        <table className="solar-table">
          <thead>
            <tr>
              <th>Módulo</th>
              <th>Título da Aula</th>
              <th>Tipo</th>
              <th>Status</th>
              <th>Anotações</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td><strong>Módulo 1: Introdução ao Cálculo</strong></td>
              <td>Limites e Continuidade</td>
              <td>Pacote Interativo (ZIP)</td>
              <td><span style={{ color: 'var(--solar-success)', fontWeight: 600 }}>✔ Visualizado</span></td>
              <td><span style={{ color: 'var(--solar-text-secondary)' }}>2 anotações</span></td>
            </tr>
            <tr>
              <td><strong>Módulo 1: Introdução ao Cálculo</strong></td>
              <td>Regra da Cadeia e Derivadas</td>
              <td>Vídeo Aula (Link)</td>
              <td><span style={{ color: 'var(--solar-warning)', fontWeight: 600 }}>⏳ Pendente</span></td>
              <td><span style={{ color: 'var(--solar-text-muted)' }}>Nenhuma</span></td>
            </tr>
            <tr>
              <td><strong>Módulo 2: Integrais Definidas</strong></td>
              <td>Teorema Fundamental do Cálculo</td>
              <td>Pacote Interativo (ZIP)</td>
              <td><span style={{ color: 'var(--solar-warning)', fontWeight: 600 }}>⏳ Pendente</span></td>
              <td><span style={{ color: 'var(--solar-text-muted)' }}>Nenhuma</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
};
