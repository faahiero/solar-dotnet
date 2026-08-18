import { useState, useEffect } from 'react';
import type { CurriculumUnit } from '../types/academic';
import { AgendaPortlet } from './AgendaPortlet';

interface MySolarHomeProps {
  onOpenCurriculumUnit: (cu: CurriculumUnit) => void;
}

export const MySolarHome = ({ onOpenCurriculumUnit }: MySolarHomeProps) => {
  const [showFlash, setShowFlash] = useState(true);
  const [curriculumUnits, setCurriculumUnits] = useState<CurriculumUnit[]>([]);
  const [searchFilter, setSearchFilter] = useState('');
  const [showSearchBox, setShowSearchBox] = useState(false);

  useEffect(() => {
    fetch('/api/v1/curriculum-units')
      .then((res) => res.json())
      .then((data) => setCurriculumUnits(data))
      .catch((err) => console.error('Erro ao carregar turmas:', err));
  }, []);

  const filteredUnits = curriculumUnits.filter((cu) =>
    cu.name.toLowerCase().includes(searchFilter.toLowerCase()) ||
    cu.courseName.toLowerCase().includes(searchFilter.toLowerCase()) ||
    cu.code.toLowerCase().includes(searchFilter.toLowerCase())
  );

  return (
    <div className="mysolar-home-layout">
      {/* Flash Notice Verde de Sucesso */}
      {showFlash && (
        <div className="solar-flash-success">
          <div className="flash-content">
            <span className="flash-check-icon">✔</span>
            <span className="flash-text-box">Login efetuado com sucesso.</span>
          </div>
          <button type="button" className="flash-close-btn" onClick={() => setShowFlash(false)}>
            ✕
          </button>
        </div>
      )}

      {/* Grid Principal: Disciplinas + Agenda */}
      <div className="mysolar-grid-layout">
        {/* Coluna Esquerda: Disciplinas Ativas e Avisos */}
        <div className="mysolar-main-column">
          <div className="solar-portlet-card">
            <div className="portlet-table-header">
              <div className="portlet-title-left">
                <span className="portlet-title-icon">📑</span>
                <strong>Disciplinas ativas</strong>
              </div>

              <div className="portlet-actions-right">
                <button
                  type="button"
                  className="btn-action-text"
                  onClick={() => setShowSearchBox(!showSearchBox)}
                >
                  🔍 Buscar
                </button>
                <button
                  type="button"
                  className="btn-action-text"
                  onClick={() => alert('Todas as disciplinas encerradas já estão sincronizadas com o histórico.')}
                >
                  ⏱ (visualizar encerradas)
                </button>
              </div>
            </div>

            {showSearchBox && (
              <div className="portlet-search-bar">
                <input
                  type="text"
                  placeholder="Filtrar por nome, código ou curso..."
                  value={searchFilter}
                  onChange={(e) => setSearchFilter(e.target.value)}
                  className="search-input-inline"
                  autoFocus
                />
              </div>
            )}

            <table className="solar-curriculum-table">
              <thead>
                <tr>
                  <th style={{ width: '40px', textAlign: 'center' }}>Tipo</th>
                  <th>Unidade Curricular ⬍</th>
                  <th>Curso ⬍</th>
                  <th style={{ width: '80px', textAlign: 'center' }}>Semestre ⬍</th>
                  <th style={{ width: '60px', textAlign: 'center' }}>Acesso</th>
                </tr>
              </thead>
              <tbody>
                {filteredUnits.map((cu) => (
                  <tr
                    key={cu.id}
                    className="curriculum-row"
                    onClick={() => onOpenCurriculumUnit(cu)}
                    title={`Abrir ${cu.name}`}
                  >
                    <td style={{ textAlign: 'center' }}>
                      {cu.type === 'distance_undergrad' ? (
                        <span className="cu-type-badge type-dist" title={cu.typeLabel}>📄</span>
                      ) : (
                        <span className="cu-type-badge type-pres" title={cu.typeLabel}>📗</span>
                      )}
                    </td>
                    <td className="cu-name-cell">
                      <strong>{cu.code} - {cu.name}</strong>
                    </td>
                    <td className="cu-course-cell">
                      {cu.courseCode} - {cu.courseName}
                    </td>
                    <td style={{ textAlign: 'center' }} className="cu-sem-cell">
                      {cu.semester}
                    </td>
                    <td style={{ textAlign: 'center' }}>
                      <button
                        type="button"
                        className="btn-enter-cu"
                        title={`Acessar ${cu.name}`}
                        onClick={(e) => {
                          e.stopPropagation();
                          onOpenCurriculumUnit(cu);
                        }}
                      >
                        ➔
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Portlet de Avisos */}
          <div className="solar-portlet-card" style={{ marginTop: '20px' }}>
            <div className="portlet-simple-header">
              <span className="portlet-title-icon">⚠️</span>
              <strong>Avisos</strong>
            </div>
            <div className="portlet-simple-body">
              <div className="notice-item">
                <strong>📢 Início do Semestre Letivo 2026.1</strong>
                <p>Confira os prazos de atividades e webconferências na aba de cada disciplina.</p>
              </div>
              <div className="notice-item" style={{ marginTop: '10px' }}>
                <strong>📢 Atendimento com a Monitoria</strong>
                <p>Plantão de dúvidas online ativo via chat e fórum.</p>
              </div>
            </div>
          </div>
        </div>

        {/* Coluna Direita: Agenda */}
        <div className="mysolar-side-column">
          <AgendaPortlet />
        </div>
      </div>
    </div>
  );
};
