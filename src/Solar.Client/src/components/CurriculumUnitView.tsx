import { useState, useEffect, useRef } from 'react';
import type {
  CurriculumUnit,
  CurriculumUnitDetails,
  LessonModule,
  DiscussionTopic,
  AssignmentItem,
  ScoreReport,
  Participant
} from '../types/academic';
import { AgendaPortlet } from './AgendaPortlet';
import { ChatTab } from './tabs/ChatTab';
import { OnlineExamPlayer } from './OnlineExamPlayer';
import type { UserProfile } from '../types/auth';

interface CurriculumUnitViewProps {
  curriculumUnit: CurriculumUnit;
  user: UserProfile;
  onNavigateHome: () => void;
}

type SubMenuKey =
  | 'inicio'
  | 'aulas'
  | 'material_apoio'
  | 'forum'
  | 'trabalhos'
  | 'prova_online'
  | 'acompanhamento'
  | 'participantes'
  | 'chat';

export const CurriculumUnitView = ({
  curriculumUnit,
  user,
  onNavigateHome
}: CurriculumUnitViewProps) => {
  const [activeSubMenu, setActiveSubMenu] = useState<SubMenuKey>('inicio');
  const [details, setDetails] = useState<CurriculumUnitDetails | null>(null);
  const [lessons, setLessons] = useState<LessonModule[]>([]);
  const [discussions, setDiscussions] = useState<DiscussionTopic[]>([]);
  const [assignments, setAssignments] = useState<AssignmentItem[]>([]);
  const [scores, setScores] = useState<ScoreReport | null>(null);
  const [participants, setParticipants] = useState<Participant[]>([]);
  const [scoreTab, setScoreTab] = useState<'avaliativa' | 'frequencia' | 'nao_avaliativa'>('avaliativa');

  // Estado para upload real de arquivo de trabalho
  const [uploadingAssignmentId, setUploadingAssignmentId] = useState<number | null>(null);
  const [uploadFeedback, setUploadFeedback] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    fetch(`/api/v1/curriculum-units/${curriculumUnit.id}`)
      .then((res) => res.json())
      .then((data) => setDetails(data));

    fetch(`/api/v1/curriculum-units/${curriculumUnit.id}/lessons`)
      .then((res) => res.json())
      .then((data) => setLessons(data));

    fetch(`/api/v1/curriculum-units/${curriculumUnit.id}/discussions`)
      .then((res) => res.json())
      .then((data) => setDiscussions(data));

    fetch(`/api/v1/curriculum-units/${curriculumUnit.id}/assignments`)
      .then((res) => res.json())
      .then((data) => setAssignments(data));

    fetch(`/api/v1/curriculum-units/${curriculumUnit.id}/scores`)
      .then((res) => res.json())
      .then((data) => setScores(data));

    fetch(`/api/v1/curriculum-units/${curriculumUnit.id}/participants`)
      .then((res) => res.json())
      .then((data) => setParticipants(data));
  }, [curriculumUnit.id]);

  const handleTriggerUpload = (assignmentId: number) => {
    setUploadingAssignmentId(assignmentId);
    setUploadFeedback(null);
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleFileSelected = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || uploadingAssignmentId === null) return;

    const formData = new FormData();
    formData.append('file', file);

    setUploadFeedback(`Enviando ${file.name}...`);

    try {
      const response = await fetch(`/api/v1/curriculum-units/${curriculumUnit.id}/assignments/${uploadingAssignmentId}/upload`, {
        method: 'POST',
        body: formData
      });

      const data = await response.json();
      if (data.success) {
        setUploadFeedback(`✔ ${data.message} (${data.FileName})`);
        // Atualiza a listagem de assignments local
        setAssignments((prev) =>
          prev.map((a) =>
            a.id === uploadingAssignmentId
              ? { ...a, status: 'Enviado', submittedFile: data.FileName }
              : a
          )
        );
      } else {
        setUploadFeedback(`❌ ${data.message}`);
      }
    } catch (err) {
      setUploadFeedback(`❌ Erro no envio: ${err}`);
    } finally {
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  return (
    <div className="curriculum-unit-container">
      {/* Input de Arquivo Oculto para Submissão */}
      <input
        type="file"
        ref={fileInputRef}
        style={{ display: 'none' }}
        onChange={handleFileSelected}
        accept=".pdf,.zip,.docx,.doc,.txt,.png,.jpg"
      />

      {/* 1. Breadcrumbs e Código da Turma */}
      <div className="cu-breadcrumb-bar">
        <div className="cu-breadcrumb-left">
          <span className="crumb-link" onClick={onNavigateHome}>Home</span>
          <span className="crumb-sep">&gt;</span>
          <span className="crumb-current">
            {curriculumUnit.courseName} {curriculumUnit.name} {curriculumUnit.semester}
          </span>
          {activeSubMenu !== 'inicio' && (
            <>
              <span className="crumb-sep">&gt;</span>
              <span className="crumb-sub">{activeSubMenu.toUpperCase().replace('_', ' ')}</span>
            </>
          )}
        </div>
        <div className="cu-class-code">
          Turma: <strong>{curriculumUnit.classCode}</strong>
        </div>
      </div>

      {/* 2. Título Principal da Turma */}
      <h1 className="cu-main-title">
        {curriculumUnit.courseName} - {curriculumUnit.name} - {curriculumUnit.semester}
      </h1>

      {/* 3. Layout: Sidebar de Navegação à Esquerda + Conteúdo à Direita */}
      <div className="cu-content-grid">
        {/* Menu Lateral da Disciplina */}
        <aside className="cu-sidebar-menu">
          <div
            className={`cu-menu-item ${activeSubMenu === 'inicio' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('inicio')}
          >
            INÍCIO
          </div>

          <div className="cu-menu-category-header">CONTEÚDO</div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'aulas' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('aulas')}
          >
            Aulas
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'material_apoio' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('material_apoio')}
          >
            Material de Apoio
          </div>
          <div className="cu-menu-subitem" onClick={() => alert('Material Compartilhado')}>
            Material Compartilhado
          </div>
          <div className="cu-menu-subitem" onClick={() => alert('Digital Class')}>
            Digital Class
          </div>

          <div className="cu-menu-category-header">ATIVIDADES</div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'forum' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('forum')}
          >
            Fórum
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'trabalhos' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('trabalhos')}
          >
            Trabalhos ✱
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'prova_online' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('prova_online')}
          >
            Prova Online 🔒
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'acompanhamento' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('acompanhamento')}
          >
            Acompanhamento
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'chat' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('chat')}
          >
            Webconferência / Chat
          </div>

          <div className="cu-menu-category-header">INFORMAÇÕES GERAIS</div>
          <div className="cu-menu-subitem" onClick={() => alert('Programa da Disciplina')}>
            Programa
          </div>
          <div className="cu-menu-subitem" onClick={() => alert('Agenda do Semestre')}>
            Agenda
          </div>
          <div className="cu-menu-subitem" onClick={() => alert('Bibliografia Recomendada')}>
            Bibliografia
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'participantes' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('participantes')}
          >
            Participantes
          </div>
        </aside>

        {/* Painel Central de Conteúdo */}
        <main className="cu-main-panel">
          {/* SUBTELA 1: INÍCIO DA TURMA (Espelha 07_turma_disciplina_interna.png) */}
          {activeSubMenu === 'inicio' && (
            <div className="cu-inicio-view">
              {/* Bloco: Descrição e Responsáveis */}
              <div className="cu-portlet-white">
                <div className="cu-two-columns">
                  <div className="cu-desc-col">
                    <h3 className="cu-block-title">Disciplina</h3>
                    <p style={{ fontSize: '0.88rem', color: '#444', marginTop: '6px' }}>
                      {details?.description || curriculumUnit.description}
                    </p>
                  </div>
                  <div className="cu-staff-col">
                    <h3 className="cu-block-title">Responsáveis</h3>
                    <div className="staff-list">
                      {(details?.staff || []).map((st, idx) => (
                        <div key={idx} className="staff-row">
                          <span className="staff-name-role">{st.name}</span>
                          <a href={`mailto:${st.email}`} className="staff-email-icon" title={`Enviar e-mail para ${st.name}`}>
                            ✉️
                          </a>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>

              {/* Bloco: Aulas Rápidas */}
              <div className="cu-portlet-white" style={{ marginTop: '16px' }}>
                <div className="cu-block-header-line">
                  <span style={{ marginRight: '6px' }}>📑</span>
                  <strong>Aulas</strong>
                </div>
                <div style={{ padding: '12px', fontSize: '0.86rem', color: '#555' }}>
                  {lessons.length > 0 ? (
                    lessons.map((mod) => (
                      <div key={mod.moduleId} style={{ marginBottom: '8px' }}>
                        <strong>{mod.moduleName}:</strong> {mod.lessons.map((l) => l.title).join(', ')}
                      </div>
                    ))
                  ) : (
                    <span>Nenhuma aula pendente.</span>
                  )}
                </div>
              </div>

              {/* Grid de 3 Colunas Inferiores: Mensagens, Fórum, Agenda */}
              <div className="cu-bottom-three-cols" style={{ marginTop: '16px' }}>
                <div className="cu-portlet-white bottom-col">
                  <div className="cu-block-header-line">
                    <span>✉️</span>
                    <strong>Mensagens</strong>
                  </div>
                  <div className="bottom-col-content">
                    <p style={{ fontSize: '0.85rem', color: '#666' }}>Sem novas mensagens na turma.</p>
                    <div style={{ marginTop: '16px', fontSize: '0.78rem' }}>
                      <a href="#inbox" onClick={() => alert('Abrindo Caixa de Entrada')}>Caixa de Entrada</a> |{' '}
                      <a href="#new" onClick={() => alert('Abrindo formulário de nova mensagem')}>Nova Mensagem</a>
                    </div>
                  </div>
                </div>

                <div className="cu-portlet-white bottom-col">
                  <div className="cu-block-header-line">
                    <span>💬</span>
                    <strong>Fórum</strong>
                  </div>
                  <div className="bottom-col-content">
                    <div className="forum-snippet-item">
                      <span className="snippet-date">19/07</span>{' '}
                      <strong className="snippet-author">User 2</strong>: As experiências acumuladas demonstram...
                    </div>
                    <div className="forum-snippet-item" style={{ marginTop: '8px' }}>
                      <span className="snippet-date">18/07</span>{' '}
                      <strong className="snippet-author">Aluno 1</strong>: A estrutura atual da organização...
                    </div>
                  </div>
                </div>

                <div className="cu-portlet-white bottom-col">
                  <AgendaPortlet />
                </div>
              </div>
            </div>
          )}

          {/* SUBTELA 2: AULAS (Espelha 08_turma_aulas.png) */}
          {activeSubMenu === 'aulas' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>📚</span>
                <strong>Módulos Didáticos e Aulas da Disciplina</strong>
              </div>
              <div style={{ padding: '16px' }}>
                {lessons.map((mod) => (
                  <div key={mod.moduleId} style={{ marginBottom: '20px' }}>
                    <h3 style={{ fontSize: '0.95rem', color: 'var(--solar-blue-dark)', marginBottom: '8px', borderBottom: '1px solid #ddd', paddingBottom: '4px' }}>
                      {mod.moduleName}
                    </h3>
                    <table className="solar-table">
                      <thead>
                        <tr>
                          <th>Título da Aula</th>
                          <th>Tipo de Mídia</th>
                          <th>Status</th>
                          <th>Anotações</th>
                        </tr>
                      </thead>
                      <tbody>
                        {mod.lessons.map((l) => (
                          <tr key={l.id}>
                            <td><strong>{l.title}</strong></td>
                            <td>{l.type}</td>
                            <td>
                              {l.viewed ? (
                                <span style={{ color: 'var(--solar-success)', fontWeight: 600 }}>✔ Visualizado</span>
                              ) : (
                                <span style={{ color: 'var(--solar-warning)', fontWeight: 600 }}>⏳ Pendente</span>
                              )}
                            </td>
                            <td>{l.notesCount > 0 ? `${l.notesCount} anotações` : 'Nenhuma'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* SUBTELA 3: MATERIAL DE APOIO (Espelha 09_turma_material_apoio.png) */}
          {activeSubMenu === 'material_apoio' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>📁</span>
                <strong>Material de Apoio e Arquivos para Download</strong>
              </div>
              <div style={{ padding: '16px' }}>
                <table className="solar-table">
                  <thead>
                    <tr>
                      <th>Arquivo</th>
                      <th>Formato</th>
                      <th>Tamanho</th>
                      <th>Ação</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td><strong>Plano_de_Ensino_2026.pdf</strong></td>
                      <td>PDF Document</td>
                      <td>1.2 MB</td>
                      <td><button type="button" className="btn-solar-blue" style={{ fontSize: '0.78rem' }}>Baixar</button></td>
                    </tr>
                    <tr>
                      <td><strong>Apostila_Modulo_1_Quimica.pdf</strong></td>
                      <td>PDF Document</td>
                      <td>4.8 MB</td>
                      <td><button type="button" className="btn-solar-blue" style={{ fontSize: '0.78rem' }}>Baixar</button></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* SUBTELA 4: FÓRUM (Espelha 10_turma_forum_discussoes.png) */}
          {activeSubMenu === 'forum' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>💬</span>
                <strong>Fóruns Disponíveis</strong>
              </div>
              <div style={{ padding: '16px' }}>
                <table className="solar-table">
                  <thead>
                    <tr>
                      <th>Fóruns ⬍</th>
                      <th>Período</th>
                      <th style={{ textAlign: 'center' }}>Postagens ⬍</th>
                      <th>Situação ⬍</th>
                      <th style={{ textAlign: 'center' }}>Avaliativa ⬍</th>
                      <th style={{ textAlign: 'center' }}>Frequência ⬍</th>
                      <th style={{ textAlign: 'center' }}>Nota / Comentários</th>
                    </tr>
                  </thead>
                  <tbody>
                    {discussions.map((disc) => (
                      <tr key={disc.id}>
                        <td>
                          <strong style={{ color: 'var(--solar-blue-main)', cursor: 'pointer' }}>
                            {disc.title}
                          </strong>
                          <p style={{ fontSize: '0.78rem', color: '#666', marginTop: '2px' }}>
                            {disc.description}
                          </p>
                        </td>
                        <td style={{ fontSize: '0.8rem', whiteSpace: 'nowrap' }}>{disc.period}</td>
                        <td style={{ textAlign: 'center' }}>{disc.postsCount}</td>
                        <td style={{ color: 'var(--solar-blue-main)', fontWeight: 600 }}>{disc.status}</td>
                        <td style={{ textAlign: 'center' }}>{disc.isEvaluative ? 'Sim' : 'Não'}</td>
                        <td style={{ textAlign: 'center' }}>{disc.isFrequency ? 'Sim' : 'Não'}</td>
                        <td style={{ textAlign: 'center' }}>
                          <button type="button" className="btn-icon-action" title="Ver comentários e avaliação">
                            💬
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* SUBTELA 5: TRABALHOS / ASSIGNMENTS (Espelha 11_turma_trabalhos_assignments.png) */}
          {activeSubMenu === 'trabalhos' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>📝</span>
                <strong>Trabalhos e Portfólio Avaliativo</strong>
              </div>
              <div style={{ padding: '16px' }}>
                {uploadFeedback && (
                  <div style={{ background: '#f0fdf4', border: '1px solid #86efac', color: '#166534', padding: '10px 14px', borderRadius: '4px', marginBottom: '16px', fontWeight: 600, fontSize: '0.88rem' }}>
                    {uploadFeedback}
                  </div>
                )}

                {assignments.map((asg) => (
                  <div
                    key={asg.id}
                    style={{
                      border: '1px solid var(--solar-border)',
                      padding: '16px',
                      borderRadius: '4px',
                      marginBottom: '16px',
                      background: '#fafbfc'
                    }}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <h3 style={{ fontSize: '1rem', color: 'var(--solar-blue-dark)' }}>
                        {asg.title} ({asg.type})
                      </h3>
                      <span
                        style={{
                          background: asg.status === 'Enviado' ? 'var(--solar-success-bg)' : 'var(--solar-warning-bg)',
                          color: asg.status === 'Enviado' ? 'var(--solar-success)' : 'var(--solar-warning)',
                          padding: '4px 10px',
                          borderRadius: '4px',
                          fontWeight: 700,
                          fontSize: '0.8rem'
                        }}
                      >
                        {asg.status}
                      </span>
                    </div>

                    <p style={{ fontSize: '0.82rem', color: '#666', marginTop: '4px' }}>
                      Prazo Limite: <strong>{asg.deadline}</strong>
                    </p>

                    {asg.submittedFile && (
                      <div style={{ marginTop: '6px', fontSize: '0.84rem', color: '#166534', background: '#dcfce7', padding: '4px 8px', borderRadius: '3px', display: 'inline-block' }}>
                        📎 Arquivo entregue: <strong>{asg.submittedFile}</strong>
                      </div>
                    )}

                    {asg.groupName && (
                      <div className="login-notice" style={{ marginTop: '8px', marginBottom: '8px' }}>
                        {asg.groupName}
                      </div>
                    )}

                    {asg.grade !== undefined && asg.grade !== null && (
                      <div style={{ marginTop: '8px', fontSize: '0.88rem' }}>
                        <strong>Nota Atribuída:</strong> <span style={{ color: 'var(--solar-blue-main)', fontWeight: 800 }}>{asg.grade.toFixed(1)}</span>
                        {asg.feedback && <p style={{ fontStyle: 'italic', color: '#555', marginTop: '2px' }}>"{asg.feedback}"</p>}
                      </div>
                    )}

                    <div style={{ marginTop: '12px' }}>
                      <button
                        type="button"
                        className="btn-solar-blue"
                        style={{ fontSize: '0.82rem' }}
                        onClick={() => handleTriggerUpload(asg.id)}
                      >
                        📤 {asg.status === 'Enviado' ? 'Substituir Arquivo' : 'Selecionar e Enviar Arquivo'}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* SUBTELA 5.1: PROVA ONLINE INTERATIVA */}
          {activeSubMenu === 'prova_online' && (
            <OnlineExamPlayer
              curriculumUnitId={curriculumUnit.id}
              examId={1}
              onFinishExam={() => setActiveSubMenu('acompanhamento')}
            />
          )}

          {/* SUBTELA 6: ACOMPANHAMENTO / DIÁRIO DE NOTAS (Espelha 12_turma_acompanhamento_notas.png) */}
          {activeSubMenu === 'acompanhamento' && scores && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>📊</span>
                <strong>Acompanhamento de Rendimento Acadêmico</strong>
              </div>
              <div style={{ padding: '20px' }}>
                {/* Card do Aluno com Foto e Resumo */}
                <div className="score-student-card">
                  <div className="student-avatar-big">👤</div>
                  <div className="student-info-meta">
                    <h2 style={{ fontSize: '1.15rem', color: 'var(--solar-blue-dark)' }}>{scores.studentName}</h2>
                    <p style={{ fontSize: '0.85rem', color: '#555' }}>
                      <strong>Carga horária da disciplina:</strong> {scores.workingHours}<br />
                      <strong>Responsáveis:</strong> {scores.staffResponsibles}
                    </p>
                  </div>
                  <div className="student-score-summary-box">
                    <div className="score-metric">
                      <span className="metric-label">A.F.</span>
                      <strong className="metric-val">{scores.finalExamGrade ?? '-'}</strong>
                    </div>
                    <div className="score-metric">
                      <span className="metric-label">Média Final</span>
                      <strong className="metric-val" style={{ color: 'var(--solar-blue-main)' }}>{scores.finalGrade.toFixed(1)}</strong>
                    </div>
                    <div className="score-metric">
                      <span className="metric-label">Frequência</span>
                      <strong className="metric-val">{scores.frequencyHours} h/a ({scores.attendancePercentage}%)</strong>
                    </div>
                    <div className="score-metric">
                      <span className="metric-label">Situação</span>
                      <strong className="metric-val" style={{ color: 'var(--solar-warning)' }}>{scores.situation}</strong>
                    </div>
                  </div>
                </div>

                {/* Abas: Avaliativa / Frequência / Não Avaliativa */}
                <div className="score-filter-tabs">
                  <button
                    type="button"
                    className={`score-tab-btn ${scoreTab === 'avaliativa' ? 'active' : ''}`}
                    onClick={() => setScoreTab('avaliativa')}
                  >
                    Avaliativa
                  </button>
                  <button
                    type="button"
                    className={`score-tab-btn ${scoreTab === 'frequencia' ? 'active' : ''}`}
                    onClick={() => setScoreTab('frequencia')}
                  >
                    Frequência
                  </button>
                  <button
                    type="button"
                    className={`score-tab-btn ${scoreTab === 'nao_avaliativa' ? 'active' : ''}`}
                    onClick={() => setScoreTab('nao_avaliativa')}
                  >
                    Não Avaliativa
                  </button>
                </div>

                <table className="solar-table" style={{ marginTop: '12px' }}>
                  <thead>
                    <tr>
                      <th>Atividade</th>
                      <th>Peso</th>
                      <th>Peso Final</th>
                      <th>Nota</th>
                      <th>Frequência</th>
                    </tr>
                  </thead>
                  <tbody>
                    {scores.evaluativeActivities.map((act, i) => (
                      <tr key={i}>
                        <td><strong>{act.name}</strong></td>
                        <td>{act.weight.toFixed(1)}</td>
                        <td>{act.finalWeight}</td>
                        <td><strong style={{ color: 'var(--solar-blue-main)' }}>{act.grade.toFixed(1)}</strong></td>
                        <td>{act.frequency}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {/* Histórico de Acessos */}
                <div style={{ marginTop: '24px' }}>
                  <h3 style={{ fontSize: '0.92rem', color: 'var(--solar-blue-dark)', marginBottom: '8px' }}>
                    Histórico de acessos (total: {scores.accessHistory.length})
                  </h3>
                  <table className="solar-table">
                    <thead>
                      <tr>
                        <th>Data</th>
                        <th>Horário</th>
                      </tr>
                    </thead>
                    <tbody>
                      {scores.accessHistory.map((acc, i) => (
                        <tr key={i}>
                          <td>{acc.date}</td>
                          <td>{acc.time}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}

          {/* SUBTELA 7: PARTICIPANTES (Espelha 13_turma_participantes.png) */}
          {activeSubMenu === 'participantes' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>👥</span>
                <strong>Docentes e Colegas da Turma</strong>
              </div>
              <div style={{ padding: '16px' }}>
                <table className="solar-table">
                  <thead>
                    <tr>
                      <th>Nome</th>
                      <th>Perfil</th>
                      <th>Polo / Localidade</th>
                      <th>Contato Direto</th>
                    </tr>
                  </thead>
                  <tbody>
                    {participants.map((p) => (
                      <tr key={p.id}>
                        <td><strong>{p.name}</strong></td>
                        <td>
                          <span
                            style={{
                              background: p.role === 'Professor' ? '#dbeafe' : p.role.includes('Tutor') ? '#fef3c7' : '#f1f5f9',
                              color: p.role === 'Professor' ? '#1e40af' : p.role.includes('Tutor') ? '#92400e' : '#334155',
                              padding: '2px 8px',
                              borderRadius: '4px',
                              fontSize: '0.78rem',
                              fontWeight: 700
                            }}
                          >
                            {p.role}
                          </span>
                        </td>
                        <td>{p.location}</td>
                        <td>
                          <a href={`mailto:${p.email}`} className="btn-solar-secondary" style={{ fontSize: '0.78rem', textDecoration: 'none', display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                            ✉️ {p.email}
                          </a>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* SUBTELA 8: CHAT SIGNALR DA TURMA */}
          {activeSubMenu === 'chat' && (
            <ChatTab user={user} />
          )}
        </main>
      </div>
    </div>
  );
};
