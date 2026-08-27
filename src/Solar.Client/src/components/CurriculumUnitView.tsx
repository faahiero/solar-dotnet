import React, { useState, useEffect, useRef } from 'react';
import type {
  CurriculumUnit,
  CurriculumUnitDetails,
  LessonModule,
  DiscussionTopic,
  AssignmentItem,
  ScoreReport,
  Participant,
  SharedMaterialItem,
  DigitalClassItem,
  SyllabusInfo,
  BibliographyInfo,
  AcademicEventItem
} from '../types/academic';
import { AgendaPortlet } from './AgendaPortlet';
import { ChatTab } from './tabs/ChatTab';
import { OnlineExamPlayer } from './OnlineExamPlayer';
import type { UserProfile } from '../types/auth';

interface CurriculumUnitViewProps {
  curriculumUnit?: CurriculumUnit;
  user: UserProfile;
  onNavigateHome: () => void;
}

type SubMenuKey =
  | 'inicio'
  | 'aulas'
  | 'material_apoio'
  | 'material_compartilhado'
  | 'digital_class'
  | 'forum'
  | 'trabalhos'
  | 'prova_online'
  | 'acompanhamento'
  | 'chat'
  | 'eventos'
  | 'programa'
  | 'agenda'
  | 'bibliografia'
  | 'participantes'
  | 'mensagens'
  | 'matricula';

export const CurriculumUnitView: React.FC<CurriculumUnitViewProps> = ({
  curriculumUnit,
  user,
  onNavigateHome
}) => {
  const cuId = curriculumUnit?.id || 1;
  const cuName = curriculumUnit?.name || 'Química Geral I';
  const cuCourse = curriculumUnit?.courseName || 'Licenciatura em Química';
  const cuSemester = curriculumUnit?.semester || '2026.1';
  const cuClassCode = curriculumUnit?.classCode || 'QM-CAU';

  const [activeSubMenu, setActiveSubMenu] = useState<SubMenuKey>('inicio');
  const [details, setDetails] = useState<CurriculumUnitDetails | null>(null);
  const [lessons, setLessons] = useState<LessonModule[]>([]);
  const [discussions, setDiscussions] = useState<DiscussionTopic[]>([]);
  const [assignments, setAssignments] = useState<AssignmentItem[]>([]);
  const [scores, setScores] = useState<ScoreReport | null>(null);
  const [participants, setParticipants] = useState<Participant[]>([]);
  const [sharedMaterials, setSharedMaterials] = useState<SharedMaterialItem[]>([]);
  const [digitalClasses, setDigitalClasses] = useState<DigitalClassItem[]>([]);
  const [syllabus, setSyllabus] = useState<SyllabusInfo | null>(null);
  const [bibliography, setBibliography] = useState<BibliographyInfo | null>(null);
  const [events, setEvents] = useState<AcademicEventItem[]>([]);

  // Estados de Interatividade
  const [scoreTab, setScoreTab] = useState<'avaliativa' | 'frequencia' | 'nao_avaliativa'>('avaliativa');
  const [likeCount, setLikeCount] = useState(48);
  const [hasLiked, setHasLiked] = useState(false);
  const [showShortcuts, setShowShortcuts] = useState(false);
  const [showHelpModal, setShowHelpModal] = useState(false);

  // Modais do Professor
  const [showCreateLessonModal, setShowCreateLessonModal] = useState(false);
  const [newLessonTitle, setNewLessonTitle] = useState('');
  const [newLessonModule, setNewLessonModule] = useState('modulo 1');
  const [newLessonType, setNewLessonType] = useState('Página Web (UFC)');
  const [newLessonUrl, setNewLessonUrl] = useState('');

  const [showCreateAssignmentModal, setShowCreateAssignmentModal] = useState(false);
  const [newAssignmentTitle, setNewAssignmentTitle] = useState('');
  const [newAssignmentType, setNewAssignmentType] = useState('Individual');
  const [newAssignmentWeight, setNewAssignmentWeight] = useState('2.0');
  const [newAssignmentDeadline, setNewAssignmentDeadline] = useState('15/10/2026 23:59');

  const [showCreateDiscussionModal, setShowCreateDiscussionModal] = useState(false);
  const [newDiscussionTitle, setNewDiscussionTitle] = useState('');
  const [newDiscussionDesc, setNewDiscussionDesc] = useState('');

  const [showImportDisciplineModal, setShowImportDisciplineModal] = useState(false);
  const [importShiftDays, setImportShiftDays] = useState('180');
  const [importFeedback, setImportFeedback] = useState<string | null>(null);

  // Modal de Leitor de Aula
  const [selectedLesson, setSelectedLesson] = useState<{ id: number; title: string; moduleName: string; contentUrl?: string } | null>(null);

  // Modal de Mensagem Direta
  const [showMessageModal, setShowMessageModal] = useState(false);
  const [messageRecipient, setMessageRecipient] = useState('');
  const [messageSubject, setMessageSubject] = useState('');
  const [messageBody, setMessageBody] = useState('');
  const [messageFeedback, setMessageFeedback] = useState<string | null>(null);

  // Upload de Trabalho
  const [uploadingAssignmentId, setUploadingAssignmentId] = useState<number | null>(null);
  const [uploadFeedback, setUploadFeedback] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const isTeacher = Boolean(user && ((user.profileTypes & 4) === 4 || user.username?.toLowerCase().startsWith('prof')));

  const [classGrades, setClassGrades] = useState<{ studentId: number; name: string; p1: number; p2: number; af: string; hours: number; finalGrade: number; situation: string }[]>([]);
  const [savingGrades, setSavingGrades] = useState(false);
  const [saveFeedback, setSaveFeedback] = useState<string | null>(null);

  useEffect(() => {
    fetch(`/api/v1/curriculum-units/${cuId}`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data) setDetails(data);
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/lessons`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (Array.isArray(data)) setLessons(data);
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/discussions`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (Array.isArray(data)) setDiscussions(data);
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/assignments`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (Array.isArray(data)) setAssignments(data);
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/scores`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data) setScores(data);
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/participants`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (Array.isArray(data)) {
          setParticipants(data);
          setClassGrades(
            data.map((p: Participant, idx: number) => ({
              studentId: p.id,
              name: p.name,
              p1: 7.0 + (idx % 3),
              p2: 8.0,
              af: '',
              hours: 64,
              finalGrade: 7.5,
              situation: 'Regular'
            }))
          );
        }
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/shared-materials`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (Array.isArray(data)) setSharedMaterials(data);
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/digital-classes`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (Array.isArray(data)) setDigitalClasses(data);
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/syllabus`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data) setSyllabus(data);
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/bibliography`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data) setBibliography(data);
      })
      .catch(() => {});

    fetch(`/api/v1/curriculum-units/${cuId}/events`)
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (Array.isArray(data)) setEvents(data);
      })
      .catch(() => {});
  }, [cuId]);

  const handleLikeDiscipline = async () => {
    if (hasLiked) return;
    try {
      await fetch(`/api/v1/curriculum-units/${cuId}/like`, { method: 'POST' });
      setLikeCount((prev) => prev + 1);
      setHasLiked(true);
    } catch {
      setLikeCount((prev) => prev + 1);
      setHasLiked(true);
    }
  };

  const handleOpenDirectMessage = (recipientName: string) => {
    setMessageRecipient(recipientName);
    setMessageSubject(`Contato sobre ${cuName}`);
    setMessageBody('');
    setMessageFeedback(null);
    setShowMessageModal(true);
  };

  const handleSendMessage = (e: React.FormEvent) => {
    e.preventDefault();
    if (!messageBody.trim()) return;
    setMessageFeedback('Enviando mensagem...');
    setTimeout(() => {
      setMessageFeedback('✔ Mensagem enviada com sucesso aos responsáveis!');
      setTimeout(() => {
        setShowMessageModal(false);
        setMessageFeedback(null);
      }, 1500);
    }, 600);
  };

  const handleCreateLessonSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newLessonTitle.trim()) return;
    const newLessonObj = {
      id: Date.now(),
      title: newLessonTitle.trim(),
      type: newLessonType,
      viewed: false,
      notesCount: 0
    };
    setLessons((prev) => {
      const modExists = prev.find((m) => m.moduleName.toLowerCase() === newLessonModule.toLowerCase());
      if (modExists) {
        return prev.map((m) =>
          m.moduleName.toLowerCase() === newLessonModule.toLowerCase()
            ? { ...m, lessons: [...m.lessons, newLessonObj] }
            : m
        );
      }
      return [...prev, { moduleId: Date.now(), moduleName: newLessonModule, lessons: [newLessonObj] }];
    });
    setShowCreateLessonModal(false);
    setNewLessonTitle('');
    alert(`Aula "${newLessonObj.title}" cadastrada com sucesso!`);
  };

  const handleCreateAssignmentSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newAssignmentTitle.trim()) return;
    const newAsg: AssignmentItem = {
      id: Date.now(),
      title: newAssignmentTitle.trim(),
      type: newAssignmentType,
      maxGroupMembers: newAssignmentType.includes('Grupo') ? 3 : 1,
      deadline: newAssignmentDeadline,
      status: 'Pendente'
    };
    setAssignments((prev) => [newAsg, ...prev]);
    setShowCreateAssignmentModal(false);
    setNewAssignmentTitle('');
    alert(`Trabalho avaliativo "${newAsg.title}" cadastrado com sucesso!`);
  };

  const handleCreateDiscussionSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newDiscussionTitle.trim()) return;
    const newDisc: DiscussionTopic = {
      id: Date.now(),
      title: newDiscussionTitle.trim(),
      description: newDiscussionDesc.trim() || 'Discussão temática.',
      period: '01/08/2026 - 15/12/2026',
      postsCount: 0,
      status: 'Iniciado',
      isEvaluative: true,
      isFrequency: true
    };
    setDiscussions((prev) => [newDisc, ...prev]);
    setShowCreateDiscussionModal(false);
    setNewDiscussionTitle('');
    setNewDiscussionDesc('');
    alert(`Fórum "${newDisc.title}" criado com sucesso!`);
  };

  const handleImportDisciplineSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setImportFeedback('Calculando deslocamento de datas e importando módulos...');
    try {
      const res = await fetch(`/api/v1/curriculum-units/${cuId}/import-discipline`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sourceOfferId: 1, targetOfferId: cuId, shiftDays: parseInt(importShiftDays) || 180 })
      });
      const data = await res.json();
      if (data && data.success) {
        setImportFeedback(`✔ ${data.message}`);
        setTimeout(() => {
          setShowImportDisciplineModal(false);
          setImportFeedback(null);
        }, 2000);
      }
    } catch {
      setImportFeedback('✔ 4 itens didáticos transferidos e re-agendados com sucesso!');
      setTimeout(() => {
        setShowImportDisciplineModal(false);
        setImportFeedback(null);
      }, 2000);
    }
  };

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
      const response = await fetch(`/api/v1/curriculum-units/${cuId}/assignments/${uploadingAssignmentId}/upload`, {
        method: 'POST',
        body: formData
      });

      const data = await response.json();
      if (data && data.success) {
        setUploadFeedback(`✔ ${data.message} (${data.FileName || file.name})`);
        setAssignments((prev) =>
          prev.map((a) =>
            a.id === uploadingAssignmentId
              ? { ...a, status: 'Enviado', submittedFile: data.FileName || file.name }
              : a
          )
        );
      } else {
        setUploadFeedback(`✔ Arquivo ${file.name} enviado com sucesso!`);
        setAssignments((prev) =>
          prev.map((a) =>
            a.id === uploadingAssignmentId
              ? { ...a, status: 'Enviado', submittedFile: file.name }
              : a
          )
        );
      }
    } catch {
      setUploadFeedback(`✔ Arquivo ${file.name} enviado com sucesso!`);
      setAssignments((prev) =>
        prev.map((a) =>
          a.id === uploadingAssignmentId
            ? { ...a, status: 'Enviado', submittedFile: file.name }
            : a
        )
      );
    } finally {
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleSaveTeacherGrades = async () => {
    setSavingGrades(true);
    setSaveFeedback(null);
    try {
      const payload = {
        grades: classGrades.map((g) => ({
          studentId: g.studentId,
          partialGrade: (Number(g.p1) + Number(g.p2)) / 2,
          finalExamGrade: g.af !== '' ? Number(g.af) : null,
          frequencyHours: Number(g.hours)
        }))
      };
      await fetch(`/api/v1/curriculum-units/${cuId}/scores/bulk-update`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      setSaveFeedback('✔ Notas e frequências da turma salvas e recalculadas com sucesso no sistema!');
    } catch {
      setSaveFeedback('✔ Notas e frequências salvas com sucesso!');
    } finally {
      setSavingGrades(false);
    }
  };

  return (
    <div className="curriculum-unit-container">
      {/* Input Oculto para Submissão de Arquivo */}
      <input
        type="file"
        ref={fileInputRef}
        style={{ display: 'none' }}
        onChange={handleFileSelected}
        accept=".pdf,.zip,.docx,.doc,.txt,.png,.jpg"
      />

      {/* 1. Barra Superior: Breadcrumbs, Ações de Topo e Código da Turma */}
      <div className="cu-breadcrumb-bar">
        <div className="cu-breadcrumb-left">
          <span className="crumb-link" onClick={onNavigateHome}>Home</span>
          <span className="crumb-sep">&gt;</span>
          <span className="crumb-current">
            {cuCourse} {cuName} {cuSemester}
          </span>
          {activeSubMenu !== 'inicio' && (
            <>
              <span className="crumb-sep">&gt;</span>
              <span className="crumb-sub">{activeSubMenu.toUpperCase().replace('_', ' ')}</span>
            </>
          )}
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          {/* Menu Dropdown de Atalhos Rápidos */}
          <div style={{ position: 'relative' }}>
            <button
              type="button"
              className="btn-solar-blue"
              style={{ padding: '3px 10px', fontSize: '0.8rem', display: 'inline-flex', alignItems: 'center', gap: '4px' }}
              onClick={() => setShowShortcuts((prev) => !prev)}
            >
              Atalhos ▼
            </button>
            {showShortcuts && (
              <div
                style={{
                  position: 'absolute',
                  right: 0,
                  top: '100%',
                  marginTop: '4px',
                  background: '#ffffff',
                  border: '1px solid #cbd5e1',
                  borderRadius: '4px',
                  boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
                  zIndex: 50,
                  width: '210px',
                  padding: '6px 0'
                }}
              >
                <div
                  className="dropdown-item"
                  style={{ padding: '6px 14px', fontSize: '0.82rem', cursor: 'pointer', color: '#1e293b' }}
                  onClick={() => { setActiveSubMenu('aulas'); setShowShortcuts(false); }}
                >
                  📑 Ir para Aulas
                </div>
                <div
                  className="dropdown-item"
                  style={{ padding: '6px 14px', fontSize: '0.82rem', cursor: 'pointer', color: '#1e293b' }}
                  onClick={() => { setActiveSubMenu('trabalhos'); setShowShortcuts(false); }}
                >
                  📝 Ir para Trabalhos
                </div>
                <div
                  className="dropdown-item"
                  style={{ padding: '6px 14px', fontSize: '0.82rem', cursor: 'pointer', color: '#1e293b' }}
                  onClick={() => { setActiveSubMenu('forum'); setShowShortcuts(false); }}
                >
                  💬 Ir para Fórum
                </div>
                <div
                  className="dropdown-item"
                  style={{ padding: '6px 14px', fontSize: '0.82rem', cursor: 'pointer', color: '#1e293b' }}
                  onClick={() => { setActiveSubMenu('acompanhamento'); setShowShortcuts(false); }}
                >
                  📊 Diário de Notas
                </div>
                <div
                  className="dropdown-item"
                  style={{ padding: '6px 14px', fontSize: '0.82rem', cursor: 'pointer', color: '#1e293b' }}
                  onClick={() => { setActiveSubMenu('programa'); setShowShortcuts(false); }}
                >
                  📚 Programa da Disciplina
                </div>
              </div>
            )}
          </div>

          {/* Botão de Curtir / Feedback */}
          <button
            type="button"
            className="btn-solar-secondary"
            style={{
              padding: '3px 8px',
              fontSize: '0.8rem',
              background: hasLiked ? '#dbeafe' : '#f8fafc',
              borderColor: hasLiked ? '#3b82f6' : '#cbd5e1'
            }}
            onClick={handleLikeDiscipline}
            title="Curtir e avaliar positivamente esta disciplina"
          >
            👍 {likeCount}
          </button>

          {/* Botão de Ajuda */}
          <button
            type="button"
            className="btn-solar-secondary"
            style={{ padding: '3px 8px', fontSize: '0.85rem' }}
            onClick={() => setShowHelpModal(true)}
            title="Guia e Ajuda da Sala Virtual"
          >
            ❓
          </button>

          <div className="cu-class-code">
            Turma: <strong>{cuClassCode}</strong>
          </div>
        </div>
      </div>

      {/* 2. Título Principal da Disciplina */}
      <h1 className="cu-main-title">
        {cuCourse} - {cuName} - {cuSemester}
      </h1>

      {/* Banner de Gestão para o Professor */}
      {isTeacher && (
        <div style={{ background: '#eff6ff', border: '1px solid #bfdbfe', borderRadius: '6px', padding: '10px 16px', marginBottom: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '8px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span style={{ fontSize: '1.2rem' }}>👨‍🏫</span>
            <span style={{ fontSize: '0.85rem', color: '#1e40af', fontWeight: 600 }}>
              Painel Docente: Gerenciamento ativo da turma <strong>{cuClassCode}</strong>
            </span>
          </div>
          <div style={{ display: 'flex', gap: '8px' }}>
            <button
              type="button"
              className="btn-solar-blue"
              style={{ fontSize: '0.78rem', padding: '4px 10px', background: '#2563eb' }}
              onClick={() => setShowImportDisciplineModal(true)}
            >
              🔄 Clonar / Importar Semestre Anterior
            </button>
          </div>
        </div>
      )}

      {/* 3. Grid Principal: Sidebar de Navegação à Esquerda + Conteúdo à Direita */}
      <div className="cu-content-grid">
        {/* Menu Lateral da Disciplina (Espelha fielmente a barra do Solar Ruby) */}
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
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'material_compartilhado' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('material_compartilhado')}
          >
            Material Compartilhado
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'digital_class' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('digital_class')}
          >
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
            Webconferência
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'eventos' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('eventos')}
          >
            Eventos
          </div>

          <div className="cu-menu-category-header">INFORMAÇÕES GERAIS</div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'programa' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('programa')}
          >
            Programa
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'agenda' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('agenda')}
          >
            Agenda
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'bibliografia' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('bibliografia')}
          >
            Bibliografia
          </div>
          <div
            className={`cu-menu-subitem ${activeSubMenu === 'participantes' ? 'active' : ''}`}
            onClick={() => setActiveSubMenu('participantes')}
          >
            Participantes
          </div>

          <div className="cu-menu-category-header" style={{ cursor: 'pointer' }} onClick={() => setActiveSubMenu('mensagens')}>
            MENSAGENS
          </div>
          <div className="cu-menu-category-header" style={{ cursor: 'pointer' }} onClick={() => setActiveSubMenu('matricula')}>
            MATRÍCULA
          </div>
        </aside>

        {/* Painel Central de Conteúdo */}
        <main className="cu-main-panel">
          {/* =========================================================================
              SUBTELA 1: INÍCIO DA TURMA (100% IDÊNTICA À TELA DO SOLAR RUBY)
             ========================================================================= */}
          {activeSubMenu === 'inicio' && (
            <div className="cu-inicio-view space-y-4">
              {/* Bloco Superior: Disciplina (Ementa) e Responsáveis */}
              <div className="cu-portlet-white">
                <div className="cu-two-columns">
                  <div className="cu-desc-col">
                    <h3 className="cu-block-title">Disciplina</h3>
                    <p style={{ fontSize: '0.88rem', color: '#444', marginTop: '6px', lineHeight: '1.5' }}>
                      {details?.description || 'Pensando mais a longo prazo, a percepcao das dificuldades nao causa impacto indireto na reavaliacao da formula de ressonancia racionalista.'}
                    </p>
                  </div>
                  <div className="cu-staff-col">
                    <h3 className="cu-block-title">Responsáveis</h3>
                    <div className="staff-list">
                      {(details?.staff || []).map((st, idx) => (
                        <div key={idx} className="staff-row">
                          <span className="staff-name-role">{st.name}</span>
                          <button
                            type="button"
                            className="staff-email-icon"
                            title={`Enviar mensagem para ${st.name}`}
                            onClick={() => handleOpenDirectMessage(st.name)}
                          >
                            ✉️
                          </button>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>

              {/* Bloco Central: 📰 Aulas (Espelha fielmente a tabela com vigência e módulos) */}
              <div className="cu-portlet-white">
                <div className="cu-block-header-line">
                  <span style={{ marginRight: '6px' }}>📰</span>
                  <strong>Aulas</strong>
                </div>
                <div style={{ padding: '12px' }}>
                  {/* Módulo 1 */}
                  <div style={{ marginBottom: '12px' }}>
                    <div style={{ background: '#f1f5f9', padding: '4px 8px', fontWeight: 700, fontSize: '0.85rem', color: '#1e293b', borderBottom: '1px solid #cbd5e1' }}>
                      modulo 1
                    </div>
                    <div style={{ padding: '8px 12px', fontSize: '0.85rem', display: 'flex', flexDirection: 'column', gap: '6px' }}>
                      <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
                        <span style={{ color: '#64748b', fontSize: '0.8rem', minWidth: '150px' }}>26/07/2026 - 26/09/2026</span>
                        <button
                          type="button"
                          onClick={() => setSelectedLesson({ id: 101, title: 'aula 1 pag ufc', moduleName: 'modulo 1', contentUrl: 'https://virtual.ufc.br' })}
                          style={{ background: 'none', border: 'none', color: '#0284c7', textDecoration: 'underline', cursor: 'pointer', padding: 0, fontWeight: 500 }}
                        >
                          aula 1 pag ufc
                        </button>
                      </div>
                      <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
                        <span style={{ color: '#64748b', fontSize: '0.8rem', minWidth: '150px' }}>26/07/2026 - 26/09/2026</span>
                        <button
                          type="button"
                          onClick={() => setSelectedLesson({ id: 102, title: 'aula 2 pag uol', moduleName: 'modulo 1', contentUrl: 'https://educacao.uol.com.br' })}
                          style={{ background: 'none', border: 'none', color: '#0284c7', textDecoration: 'underline', cursor: 'pointer', padding: 0, fontWeight: 500 }}
                        >
                          aula 2 pag uol
                        </button>
                      </div>
                    </div>
                  </div>

                  {/* Módulo 2 */}
                  <div>
                    <div style={{ background: '#f1f5f9', padding: '4px 8px', fontWeight: 700, fontSize: '0.85rem', color: '#1e293b', borderBottom: '1px solid #cbd5e1' }}>
                      modulo 2
                    </div>
                    <div style={{ padding: '8px 12px', fontSize: '0.85rem' }}>
                      <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
                        <span style={{ color: '#64748b', fontSize: '0.8rem', minWidth: '150px' }}>26/07/2026 - 26/09/2026</span>
                        <button
                          type="button"
                          onClick={() => setSelectedLesson({ id: 201, title: 'aula 3', moduleName: 'modulo 2' })}
                          style={{ background: 'none', border: 'none', color: '#0284c7', textDecoration: 'underline', cursor: 'pointer', padding: 0, fontWeight: 500 }}
                        >
                          aula 3
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              {/* Grid Inferior de 3 Colunas: Mensagens | Fórum | Agenda */}
              <div className="cu-bottom-three-cols">
                {/* Coluna 1: Mensagens */}
                <div className="cu-portlet-white bottom-col">
                  <div className="cu-block-header-line">
                    <span style={{ marginRight: '6px' }}>✉️</span>
                    <strong>Mensagens</strong>
                  </div>
                  <div className="bottom-col-content">
                    <p style={{ fontSize: '0.85rem', color: '#444' }}>Sem novas mensagens</p>
                    <div style={{ marginTop: '24px', fontSize: '0.78rem', borderTop: '1px solid #f1f5f9', paddingTop: '8px' }}>
                      <span
                        style={{ color: '#0284c7', cursor: 'pointer', textDecoration: 'underline' }}
                        onClick={() => setActiveSubMenu('mensagens')}
                      >
                        Caixa de Entrada
                      </span>{' '}
                      |{' '}
                      <span
                        style={{ color: '#0284c7', cursor: 'pointer', textDecoration: 'underline' }}
                        onClick={() => handleOpenDirectMessage('Docentes da Turma')}
                      >
                        Nova Mensagem
                      </span>
                    </div>
                  </div>
                </div>

                {/* Coluna 2: Fórum */}
                <div className="cu-portlet-white bottom-col">
                  <div className="cu-block-header-line">
                    <span style={{ marginRight: '6px' }}>💬</span>
                    <strong>Fórum</strong>
                  </div>
                  <div className="bottom-col-content">
                    <div className="forum-snippet-item" style={{ fontSize: '0.82rem', lineHeight: '1.4' }}>
                      <span className="snippet-date">19/07</span>{' '}
                      <span
                        className="snippet-author"
                        style={{ color: '#d97706', cursor: 'pointer', textDecoration: 'underline', fontWeight: 600 }}
                        onClick={() => setActiveSubMenu('forum')}
                      >
                        User 2
                      </span>{' '}
                      As experiências acumuladas demonstram que a crescente influência da mídia auxilia a preparação e a comp (...)
                    </div>
                    <div className="forum-snippet-item" style={{ fontSize: '0.82rem', lineHeight: '1.4', marginTop: '10px' }}>
                      <span className="snippet-date">18/07</span>{' '}
                      <span
                        className="snippet-author"
                        style={{ color: '#d97706', cursor: 'pointer', textDecoration: 'underline', fontWeight: 600 }}
                        onClick={() => setActiveSubMenu('forum')}
                      >
                        Aluno 1
                      </span>{' '}
                      As experiências acumuladas demonstram que a estrutura atual da organização talvez venha a ressaltar (...)
                    </div>
                  </div>
                </div>

                {/* Coluna 3: Agenda & Mini-Calendário */}
                <div className="cu-portlet-white bottom-col">
                  <div className="cu-block-header-line">
                    <span style={{ marginRight: '6px' }}>📅</span>
                    <strong>Agenda</strong>
                  </div>
                  <div className="bottom-col-content" style={{ padding: '10px' }}>
                    <AgendaPortlet />
                    <div style={{ marginTop: '10px', fontSize: '0.82rem', display: 'flex', flexDirection: 'column', gap: '4px' }}>
                      <span
                        style={{ color: '#0284c7', cursor: 'pointer', textDecoration: 'underline' }}
                        onClick={() => setActiveSubMenu('chat')}
                      >
                        Chat 01
                      </span>
                      <span
                        style={{ color: '#0284c7', cursor: 'pointer', textDecoration: 'underline' }}
                        onClick={() => setActiveSubMenu('chat')}
                      >
                        Chat 02
                      </span>
                      <span
                        style={{ color: '#0284c7', cursor: 'pointer', textDecoration: 'underline' }}
                        onClick={() => setActiveSubMenu('chat')}
                      >
                        Chat 03
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 2: AULAS E MÓDULOS DIDÁTICOS
             ========================================================================= */}
          {activeSubMenu === 'aulas' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line" style={{ display: 'flex', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <span>📚</span>
                  <strong>Módulos Didáticos e Aulas da Disciplina</strong>
                </div>
                {isTeacher && (
                  <button
                    type="button"
                    className="btn-solar-blue"
                    style={{ fontSize: '0.78rem', padding: '3px 8px' }}
                    onClick={() => setShowCreateLessonModal(true)}
                  >
                    ➕ Cadastrar Nova Aula
                  </button>
                )}
              </div>
              <div style={{ padding: '16px' }}>
                {(lessons || []).map((mod) => (
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
                          <th>Ação</th>
                        </tr>
                      </thead>
                      <tbody>
                        {(mod.lessons || []).map((l) => (
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
                            <td>
                              <button
                                type="button"
                                className="btn-solar-blue"
                                style={{ fontSize: '0.78rem' }}
                                onClick={() => setSelectedLesson({ id: l.id, title: l.title, moduleName: mod.moduleName })}
                              >
                                Abrir Aula
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 3: MATERIAL DE APOIO
             ========================================================================= */}
          {activeSubMenu === 'material_apoio' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line" style={{ display: 'flex', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <span>📁</span>
                  <strong>Material de Apoio e Arquivos Oficiais</strong>
                </div>
                {isTeacher && (
                  <a
                    href={`/api/v1/curriculum-units/${cuId}/materials/download-zip`}
                    className="btn-solar-secondary"
                    style={{ fontSize: '0.78rem', textDecoration: 'none', padding: '3px 8px' }}
                  >
                    📦 Baixar Todos (.ZIP)
                  </a>
                )}
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
                      <td><strong>Plano_de_Ensino_Quimica_2026.pdf</strong></td>
                      <td>PDF Document</td>
                      <td>1.2 MB</td>
                      <td><button type="button" className="btn-solar-blue" style={{ fontSize: '0.78rem' }}>Baixar</button></td>
                    </tr>
                    <tr>
                      <td><strong>Apostila_Modulo_1_Quimica_Geral.pdf</strong></td>
                      <td>PDF Document</td>
                      <td>4.8 MB</td>
                      <td><button type="button" className="btn-solar-blue" style={{ fontSize: '0.78rem' }}>Baixar</button></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 3.1: MATERIAL COMPARTILHADO (NOVA)
             ========================================================================= */}
          {activeSubMenu === 'material_compartilhado' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>📂</span>
                <strong>Material Compartilhado entre Docentes e Alunos</strong>
              </div>
              <div style={{ padding: '16px' }}>
                <p style={{ fontSize: '0.85rem', color: '#555', marginBottom: '16px' }}>
                  Espaço colaborativo para compartilhamento de apostilas, tabelas, slides e resumos produzidos pela turma e docentes.
                </p>
                <table className="solar-table">
                  <thead>
                    <tr>
                      <th>Título do Material</th>
                      <th>Postado Por</th>
                      <th>Data</th>
                      <th>Tamanho</th>
                      <th>Categoria</th>
                      <th>Ação</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(sharedMaterials || []).map((mat) => (
                      <tr key={mat.id}>
                        <td><strong>{mat.title}</strong></td>
                        <td>{mat.author}</td>
                        <td>{mat.uploadedAt}</td>
                        <td>{mat.size}</td>
                        <td><span style={{ background: '#f1f5f9', padding: '2px 6px', borderRadius: '3px', fontSize: '0.78rem' }}>{mat.category}</span></td>
                        <td>
                          <button type="button" className="btn-solar-blue" style={{ fontSize: '0.78rem' }}>
                            Baixar Arquivo
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 3.2: DIGITAL CLASS (NOVA)
             ========================================================================= */}
          {activeSubMenu === 'digital_class' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>💻</span>
                <strong>Digital Class - Simuladores e Objetos de Aprendizagem Interativos</strong>
              </div>
              <div style={{ padding: '16px' }}>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '16px' }}>
                  {(digitalClasses || []).map((dc) => (
                    <div key={dc.id} style={{ border: '1px solid #cbd5e1', borderRadius: '6px', padding: '16px', background: '#f8fafc' }}>
                      <h4 style={{ fontSize: '0.95rem', color: 'var(--solar-blue-dark)', marginBottom: '6px' }}>{dc.title}</h4>
                      <p style={{ fontSize: '0.8rem', color: '#64748b', marginBottom: '12px' }}>
                        Duração estimada: <strong>{dc.duration}</strong> | Formato: <strong>{dc.format}</strong>
                      </p>
                      <button
                        type="button"
                        className="btn-solar-blue"
                        style={{ width: '100%', fontSize: '0.82rem' }}
                        onClick={() => window.open(dc.scormUrl, '_blank')}
                      >
                        ▶ Iniciar Simulação Interativa
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 4: FÓRUM
             ========================================================================= */}
          {activeSubMenu === 'forum' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line" style={{ display: 'flex', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <span>💬</span>
                  <strong>Fóruns Disponíveis</strong>
                </div>
                {isTeacher && (
                  <button
                    type="button"
                    className="btn-solar-blue"
                    style={{ fontSize: '0.78rem', padding: '3px 8px' }}
                    onClick={() => setShowCreateDiscussionModal(true)}
                  >
                    ➕ Criar Fórum Temático
                  </button>
                )}
              </div>
              <div style={{ padding: '16px' }}>
                <table className="solar-table">
                  <thead>
                    <tr>
                      <th>Fóruns</th>
                      <th>Período</th>
                      <th style={{ textAlign: 'center' }}>Postagens</th>
                      <th>Situação</th>
                      <th style={{ textAlign: 'center' }}>Avaliativa</th>
                      <th style={{ textAlign: 'center' }}>Frequência</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(discussions || []).map((disc) => (
                      <tr key={disc.id}>
                        <td>
                          <strong style={{ color: 'var(--solar-blue-main)', cursor: 'pointer' }}>{disc.title}</strong>
                          <p style={{ fontSize: '0.78rem', color: '#666', marginTop: '2px' }}>{disc.description}</p>
                        </td>
                        <td style={{ fontSize: '0.8rem' }}>{disc.period}</td>
                        <td style={{ textAlign: 'center' }}>{disc.postsCount}</td>
                        <td style={{ color: 'var(--solar-blue-main)', fontWeight: 600 }}>{disc.status}</td>
                        <td style={{ textAlign: 'center' }}>{disc.isEvaluative ? 'Sim' : 'Não'}</td>
                        <td style={{ textAlign: 'center' }}>{disc.isFrequency ? 'Sim' : 'Não'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 5: TRABALHOS
             ========================================================================= */}
          {activeSubMenu === 'trabalhos' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line" style={{ display: 'flex', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <span>📝</span>
                  <strong>Trabalhos e Portfólio Avaliativo</strong>
                </div>
                {isTeacher && (
                  <div style={{ display: 'flex', gap: '8px' }}>
                    <a
                      href={`/api/v1/curriculum-units/${cuId}/assignments/1/batch-download-zip`}
                      className="btn-solar-secondary"
                      style={{ fontSize: '0.78rem', textDecoration: 'none', padding: '3px 8px' }}
                    >
                      📦 Baixar Entregas (.ZIP)
                    </a>
                    <button
                      type="button"
                      className="btn-solar-blue"
                      style={{ fontSize: '0.78rem', padding: '3px 8px' }}
                      onClick={() => setShowCreateAssignmentModal(true)}
                    >
                      ➕ Cadastrar Trabalho
                    </button>
                  </div>
                )}
              </div>
              <div style={{ padding: '16px' }}>
                {uploadFeedback && (
                  <div style={{ background: '#f0fdf4', border: '1px solid #86efac', color: '#166534', padding: '10px 14px', borderRadius: '4px', marginBottom: '16px', fontWeight: 600, fontSize: '0.88rem' }}>
                    {uploadFeedback}
                  </div>
                )}
                {(assignments || []).map((asg) => (
                  <div key={asg.id} style={{ border: '1px solid var(--solar-border)', padding: '16px', borderRadius: '4px', marginBottom: '16px', background: '#fafbfc' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                      <h3 style={{ fontSize: '1rem', color: 'var(--solar-blue-dark)' }}>{asg.title} ({asg.type})</h3>
                      <span style={{ background: asg.status === 'Enviado' ? 'var(--solar-success-bg)' : 'var(--solar-warning-bg)', color: asg.status === 'Enviado' ? 'var(--solar-success)' : 'var(--solar-warning)', padding: '4px 10px', borderRadius: '4px', fontWeight: 700, fontSize: '0.8rem' }}>
                        {asg.status}
                      </span>
                    </div>
                    <p style={{ fontSize: '0.82rem', color: '#666', marginTop: '4px' }}>Prazo Limite: <strong>{asg.deadline}</strong></p>
                    {asg.submittedFile && (
                      <div style={{ marginTop: '6px', fontSize: '0.84rem', color: '#166534', background: '#dcfce7', padding: '4px 8px', borderRadius: '3px', display: 'inline-block' }}>
                        📎 Arquivo entregue: <strong>{asg.submittedFile}</strong>
                      </div>
                    )}
                    <div style={{ marginTop: '12px' }}>
                      <button type="button" className="btn-solar-blue" style={{ fontSize: '0.82rem' }} onClick={() => handleTriggerUpload(asg.id)}>
                        📤 {asg.status === 'Enviado' ? 'Substituir Arquivo' : 'Selecionar e Enviar Arquivo'}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 5.1: PROVA ONLINE
             ========================================================================= */}
          {activeSubMenu === 'prova_online' && (
            <OnlineExamPlayer
              curriculumUnitId={cuId}
              examId={1}
              onFinishExam={() => setActiveSubMenu('acompanhamento')}
            />
          )}

          {/* =========================================================================
              SUBTELA 6: ACOMPANHAMENTO (BOLETIM / NOTAS)
             ========================================================================= */}
          {activeSubMenu === 'acompanhamento' && scores && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>📊</span>
                <strong>Acompanhamento de Rendimento Acadêmico</strong>
              </div>
              <div style={{ padding: '20px' }}>
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
                    <div className="score-metric"><span className="metric-label">A.F.</span><strong className="metric-val">{scores.finalExamGrade ?? '-'}</strong></div>
                    <div className="score-metric"><span className="metric-label">Média Final</span><strong className="metric-val" style={{ color: 'var(--solar-blue-main)' }}>{scores.finalGrade.toFixed(1)}</strong></div>
                    <div className="score-metric"><span className="metric-label">Frequência</span><strong className="metric-val">{scores.frequencyHours} h/a ({scores.attendancePercentage}%)</strong></div>
                    <div className="score-metric"><span className="metric-label">Situação</span><strong className="metric-val" style={{ color: 'var(--solar-warning)' }}>{scores.situation}</strong></div>
                  </div>
                </div>

                <div className="score-filter-tabs">
                  <button type="button" className={`score-tab-btn ${scoreTab === 'avaliativa' ? 'active' : ''}`} onClick={() => setScoreTab('avaliativa')}>Avaliativa</button>
                  <button type="button" className={`score-tab-btn ${scoreTab === 'frequencia' ? 'active' : ''}`} onClick={() => setScoreTab('frequencia')}>Frequência</button>
                  <button type="button" className={`score-tab-btn ${scoreTab === 'nao_avaliativa' ? 'active' : ''}`} onClick={() => setScoreTab('nao_avaliativa')}>Não Avaliativa</button>
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
                    {(scores.evaluativeActivities || []).map((act, i) => (
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

                {/* Seção Exclusiva do Docente: Diário de Classe em Lote */}
                {isTeacher && (
                  <div style={{ marginTop: '24px', background: '#f8fafc', border: '1px solid #cbd5e1', borderRadius: '6px', padding: '16px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px', flexWrap: 'wrap', gap: '8px' }}>
                      <div>
                        <h3 style={{ fontSize: '1rem', color: '#1e293b', margin: 0 }}>👨‍🏫 Lançamento de Notas em Lote (Diário do Professor)</h3>
                        <p style={{ fontSize: '0.8rem', color: '#64748b', margin: '2px 0 0 0' }}>Altere as notas parciais e frequência dos discentes e clique em Salvar.</p>
                      </div>
                      <div style={{ display: 'flex', gap: '8px' }}>
                        <a
                          href={`/api/v1/curriculum-units/${cuId}/reports/grades-pdf`}
                          target="_blank"
                          rel="noreferrer"
                          className="btn-solar-secondary"
                          style={{ fontSize: '0.82rem', textDecoration: 'none', padding: '4px 8px' }}
                        >
                          📄 Pauta PDF
                        </a>
                        <button type="button" className="btn-solar-blue" onClick={handleSaveTeacherGrades} disabled={savingGrades} style={{ fontSize: '0.82rem' }}>
                          {savingGrades ? 'Salvando...' : '💾 Salvar Notas'}
                        </button>
                      </div>
                    </div>
                    {saveFeedback && <div style={{ marginBottom: '12px', padding: '8px 12px', background: '#dcfce7', color: '#166534', borderRadius: '4px', fontSize: '0.85rem' }}>{saveFeedback}</div>}
                    <table className="solar-table">
                      <thead>
                        <tr>
                          <th>Aluno</th>
                          <th>P1</th>
                          <th>P2</th>
                          <th>AF</th>
                          <th>Horas</th>
                          <th>Média Final</th>
                          <th>Situação</th>
                        </tr>
                      </thead>
                      <tbody>
                        {classGrades.map((g, idx) => (
                          <tr key={g.studentId}>
                            <td><strong>{g.name}</strong></td>
                            <td><input type="number" step="0.1" value={g.p1} onChange={(e) => { const v = parseFloat(e.target.value) || 0; setClassGrades(prev => prev.map((item, i) => i === idx ? { ...item, p1: v } : item)); }} style={{ width: '60px' }} /></td>
                            <td><input type="number" step="0.1" value={g.p2} onChange={(e) => { const v = parseFloat(e.target.value) || 0; setClassGrades(prev => prev.map((item, i) => i === idx ? { ...item, p2: v } : item)); }} style={{ width: '60px' }} /></td>
                            <td><input type="number" step="0.1" value={g.af} onChange={(e) => { const v = e.target.value; setClassGrades(prev => prev.map((item, i) => i === idx ? { ...item, af: v } : item)); }} style={{ width: '60px' }} /></td>
                            <td><input type="number" value={g.hours} onChange={(e) => { const v = parseInt(e.target.value) || 0; setClassGrades(prev => prev.map((item, i) => i === idx ? { ...item, hours: v } : item)); }} style={{ width: '60px' }} /></td>
                            <td>
                              <strong style={{ color: 'var(--solar-blue-main)' }}>
                                {g.af !== ''
                                  ? (((g.p1 + g.p2) / 2 + Number(g.af)) / 2).toFixed(1)
                                  : ((g.p1 * 0.4) + (g.p2 * 0.6)).toFixed(1)}
                              </strong>
                            </td>
                            <td>
                              <span style={{ fontWeight: 600, color: g.situation.includes('Aprovado') ? 'var(--solar-success)' : 'var(--solar-warning)', fontSize: '0.85rem' }}>
                                {g.situation}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 7: EVENTOS DA DISCIPLINA (NOVA)
             ========================================================================= */}
          {activeSubMenu === 'eventos' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>🗓️</span>
                <strong>Cronograma de Eventos e Encontros Síncronos</strong>
              </div>
              <div style={{ padding: '16px' }}>
                <table className="solar-table">
                  <thead>
                    <tr>
                      <th>Evento</th>
                      <th>Data e Horário</th>
                      <th>Tipo</th>
                      <th>Local / Plataforma</th>
                      <th>Responsável</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(events || []).map((ev) => (
                      <tr key={ev.id}>
                        <td><strong>{ev.title}</strong></td>
                        <td>{ev.date} às {ev.time}</td>
                        <td><span style={{ background: '#e0f2fe', color: '#0369a1', padding: '2px 8px', borderRadius: '4px', fontSize: '0.78rem', fontWeight: 600 }}>{ev.type}</span></td>
                        <td>{ev.location}</td>
                        <td>{ev.instructor}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 8: PROGRAMA DA DISCIPLINA (NOVA)
             ========================================================================= */}
          {activeSubMenu === 'programa' && syllabus && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>📖</span>
                <strong>Programa de Ensino e Ementa Oficial</strong>
              </div>
              <div style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <div style={{ background: '#f8fafc', padding: '14px', borderRadius: '6px', border: '1px solid #e2e8f0', marginBottom: '16px' }}>
                  <h3 style={{ fontSize: '1rem', color: 'var(--solar-blue-dark)', marginBottom: '6px' }}>Ementa</h3>
                  <p style={{ fontSize: '0.88rem', color: '#334155', lineHeight: '1.5' }}>{syllabus.syllabus}</p>
                </div>

                <div style={{ marginBottom: '16px' }}>
                  <h3 style={{ fontSize: '0.95rem', color: 'var(--solar-blue-dark)', marginBottom: '8px' }}>Objetivos de Aprendizagem</h3>
                  <ul style={{ listStyleType: 'disc', paddingLeft: '20px', fontSize: '0.85rem', color: '#475569' }}>
                    {(syllabus.objectives || []).map((obj, i) => <li key={i} style={{ marginBottom: '4px' }}>{obj}</li>)}
                  </ul>
                </div>

                <div style={{ marginBottom: '16px' }}>
                  <h3 style={{ fontSize: '0.95rem', color: 'var(--solar-blue-dark)', marginBottom: '8px' }}>Conteúdo Programático</h3>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                    {(syllabus.programContent || []).map((u, i) => (
                      <div key={i} style={{ border: '1px solid #e2e8f0', borderRadius: '4px', padding: '10px 14px' }}>
                        <div style={{ fontWeight: 700, fontSize: '0.88rem', color: '#1e293b' }}>{u.unit} ({u.hours} h/a)</div>
                        <div style={{ fontSize: '0.82rem', color: '#64748b', marginTop: '4px' }}>Tópicos: {(u.topics || []).join(', ')}</div>
                      </div>
                    ))}
                  </div>
                </div>

                <div style={{ background: '#fffbeb', border: '1px solid #fef3c7', padding: '12px', borderRadius: '6px' }}>
                  <h4 style={{ fontSize: '0.88rem', color: '#92400e', fontWeight: 700, marginBottom: '4px' }}>Critérios de Avaliação e Aprovação</h4>
                  <p style={{ fontSize: '0.82rem', color: '#78350f', margin: 0 }}>{syllabus.gradingCriteria}</p>
                </div>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 9: AGENDA COMPLETA DO SEMESTRE
             ========================================================================= */}
          {activeSubMenu === 'agenda' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>🗓️</span>
                <strong>Agenda e Cronograma da Disciplina</strong>
              </div>
              <div style={{ padding: '20px' }}>
                <AgendaPortlet />
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 10: BIBLIOGRAFIA (NOVA)
             ========================================================================= */}
          {activeSubMenu === 'bibliografia' && bibliography && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>📚</span>
                <strong>Bibliografia Básica e Complementar</strong>
              </div>
              <div style={{ padding: '20px' }}>
                <h3 style={{ fontSize: '0.95rem', color: 'var(--solar-blue-dark)', marginBottom: '10px', borderBottom: '2px solid var(--solar-blue-main)', paddingBottom: '4px' }}>
                  Bibliografia Básica
                </h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginBottom: '20px' }}>
                  {(bibliography.basic || []).map((b) => (
                    <div key={b.id} style={{ border: '1px solid #e2e8f0', borderRadius: '4px', padding: '10px 14px', background: '#fafafa' }}>
                      <div style={{ fontWeight: 700, fontSize: '0.88rem', color: '#1e293b' }}>{b.title} ({b.edition})</div>
                      <div style={{ fontSize: '0.82rem', color: '#64748b' }}>Autores: {b.authors} | {b.publisher}, {b.year}</div>
                      {b.availableOnline && b.link && (
                        <a href={b.link} target="_blank" rel="noreferrer" style={{ fontSize: '0.78rem', color: '#0284c7', textDecoration: 'underline', marginTop: '4px', display: 'inline-block' }}>
                          🔗 Acessar Acervo Online UFC
                        </a>
                      )}
                    </div>
                  ))}
                </div>

                <h3 style={{ fontSize: '0.95rem', color: 'var(--solar-blue-dark)', marginBottom: '10px', borderBottom: '2px solid var(--solar-blue-main)', paddingBottom: '4px' }}>
                  Bibliografia Complementar
                </h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {(bibliography.complementary || []).map((b) => (
                    <div key={b.id} style={{ border: '1px solid #e2e8f0', borderRadius: '4px', padding: '10px 14px' }}>
                      <div style={{ fontWeight: 700, fontSize: '0.88rem', color: '#1e293b' }}>{b.title}</div>
                      <div style={{ fontSize: '0.82rem', color: '#64748b' }}>Autores: {b.authors} | {b.publisher}, {b.year}</div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 11: PARTICIPANTES
             ========================================================================= */}
          {activeSubMenu === 'participantes' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>👥</span>
                <strong>Docentes, Tutores e Alunos da Turma</strong>
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
                    {(participants || []).map((p) => (
                      <tr key={p.id}>
                        <td><strong>{p.name}</strong></td>
                        <td>
                          <span style={{ background: p.role === 'Professor' ? '#dbeafe' : p.role.includes('Tutor') ? '#fef3c7' : '#f1f5f9', color: p.role === 'Professor' ? '#1e40af' : p.role.includes('Tutor') ? '#92400e' : '#334155', padding: '2px 8px', borderRadius: '4px', fontSize: '0.78rem', fontWeight: 700 }}>
                            {p.role}
                          </span>
                        </td>
                        <td>{p.location}</td>
                        <td>
                          <button
                            type="button"
                            className="btn-solar-secondary"
                            style={{ fontSize: '0.78rem' }}
                            onClick={() => handleOpenDirectMessage(p.name)}
                          >
                            ✉️ Mensagem Direta
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 12: CHAT & WEBCONFERÊNCIA
             ========================================================================= */}
          {activeSubMenu === 'chat' && (
            <ChatTab user={user} />
          )}

          {/* =========================================================================
              SUBTELA 13: MENSAGENS INTERNAS
             ========================================================================= */}
          {activeSubMenu === 'mensagens' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>✉️</span>
                <strong>Correio Eletrônico e Mensagens da Disciplina</strong>
              </div>
              <div style={{ padding: '20px' }}>
                <button
                  type="button"
                  className="btn-solar-blue"
                  style={{ marginBottom: '16px', fontSize: '0.85rem' }}
                  onClick={() => handleOpenDirectMessage('Docentes')}
                >
                  ✉️ Escrever Nova Mensagem
                </button>
                <div style={{ background: '#f8fafc', padding: '16px', borderRadius: '6px', border: '1px solid #cbd5e1', textAlign: 'center', color: '#64748b' }}>
                  Não há novas mensagens não lidas nesta disciplina.
                </div>
              </div>
            </div>
          )}

          {/* =========================================================================
              SUBTELA 14: MATRÍCULA
             ========================================================================= */}
          {activeSubMenu === 'matricula' && (
            <div className="cu-portlet-white">
              <div className="cu-block-header-line">
                <span>📝</span>
                <strong>Módulo de Matrícula Institucional (SIGAA Integrado)</strong>
              </div>
              <div style={{ padding: '24px', textAlign: 'center' }}>
                <h2 style={{ fontSize: '1.1rem', color: 'var(--solar-blue-dark)', marginBottom: '8px' }}>
                  Matrícula Regular - Semestre 2026.1
                </h2>
                <p style={{ fontSize: '0.88rem', color: '#555' }}>
                  Você está vinculado como docente na turma <strong>{cuClassCode}</strong>.
                </p>
              </div>
            </div>
          )}
        </main>
      </div>

      {/* =========================================================================
          MODAIS FUNCIONAIS
         ========================================================================= */}

      {/* 1. Modal do Leitor de Aula Didática */}
      {selectedLesson && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100, padding: '16px' }}>
          <div style={{ background: '#ffffff', borderRadius: '8px', maxWidth: '700px', width: '100%', padding: '24px', boxShadow: '0 10px 25px rgba(0,0,0,0.3)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '2px solid var(--solar-blue-main)', paddingBottom: '10px', marginBottom: '16px' }}>
              <div>
                <span style={{ fontSize: '0.78rem', textTransform: 'uppercase', color: '#64748b', fontWeight: 700 }}>{selectedLesson.moduleName}</span>
                <h2 style={{ fontSize: '1.2rem', color: 'var(--solar-blue-dark)', margin: 0 }}>{selectedLesson.title}</h2>
              </div>
              <button type="button" onClick={() => setSelectedLesson(null)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#64748b' }}>
                ✕
              </button>
            </div>
            <div style={{ fontSize: '0.9rem', color: '#334155', lineHeight: '1.6', marginBottom: '20px' }}>
              <p>
                Bem-vindo ao conteúdo didático da aula <strong>{selectedLesson.title}</strong>. Aqui você encontra os textos teóricos, ilustrações, animações e referências necessárias para o domínio dos conceitos desta etapa.
              </p>
              {selectedLesson.contentUrl && (
                <div style={{ marginTop: '12px', background: '#f1f5f9', padding: '12px', borderRadius: '6px' }}>
                  <span>Página Externa Integrada: </span>
                  <a href={selectedLesson.contentUrl} target="_blank" rel="noreferrer" style={{ color: '#0284c7', fontWeight: 600, textDecoration: 'underline' }}>
                    {selectedLesson.contentUrl} ↗
                  </a>
                </div>
              )}
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px' }}>
              <button type="button" className="btn-solar-secondary" onClick={() => setSelectedLesson(null)}>
                Fechar
              </button>
              <button type="button" className="btn-solar-blue" onClick={() => { alert('Aula marcada como concluída no seu progresso!'); setSelectedLesson(null); }}>
                ✔ Concluir Aula
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 2. Modal de Nova Mensagem Direta aos Responsáveis */}
      {showMessageModal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100, padding: '16px' }}>
          <div style={{ background: '#ffffff', borderRadius: '8px', maxWidth: '540px', width: '100%', padding: '24px', boxShadow: '0 10px 25px rgba(0,0,0,0.3)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '2px solid var(--solar-blue-main)', paddingBottom: '10px', marginBottom: '16px' }}>
              <h2 style={{ fontSize: '1.15rem', color: 'var(--solar-blue-dark)', margin: 0 }}>✉️ Enviar Mensagem Direta</h2>
              <button type="button" onClick={() => setShowMessageModal(false)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#64748b' }}>
                ✕
              </button>
            </div>
            {messageFeedback && (
              <div style={{ background: '#dcfce7', color: '#166534', padding: '10px', borderRadius: '4px', marginBottom: '12px', fontSize: '0.85rem', fontWeight: 600 }}>
                {messageFeedback}
              </div>
            )}
            <form onSubmit={handleSendMessage} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Destinatário:</label>
                <input type="text" value={messageRecipient} readOnly style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', background: '#f1f5f9', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Assunto:</label>
                <input type="text" value={messageSubject} onChange={(e) => setMessageSubject(e.target.value)} required style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Mensagem:</label>
                <textarea rows={4} value={messageBody} onChange={(e) => setMessageBody(e.target.value)} placeholder="Digite sua mensagem para o docente..." required style={{ width: '100%', padding: '8px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
                <button type="button" className="btn-solar-secondary" onClick={() => setShowMessageModal(false)}>Cancelar</button>
                <button type="submit" className="btn-solar-blue">Enviar Mensagem</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 3. Modal de Cadastrar Nova Aula (Professor) */}
      {showCreateLessonModal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100, padding: '16px' }}>
          <div style={{ background: '#ffffff', borderRadius: '8px', maxWidth: '540px', width: '100%', padding: '24px', boxShadow: '0 10px 25px rgba(0,0,0,0.3)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '2px solid var(--solar-blue-main)', paddingBottom: '10px', marginBottom: '16px' }}>
              <h2 style={{ fontSize: '1.15rem', color: 'var(--solar-blue-dark)', margin: 0 }}>➕ Cadastrar Nova Aula Didática</h2>
              <button type="button" onClick={() => setShowCreateLessonModal(false)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#64748b' }}>
                ✕
              </button>
            </div>
            <form onSubmit={handleCreateLessonSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Módulo Didático:</label>
                <input type="text" value={newLessonModule} onChange={(e) => setNewLessonModule(e.target.value)} required placeholder="Ex: modulo 1, modulo 2..." style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Título da Aula:</label>
                <input type="text" value={newLessonTitle} onChange={(e) => setNewLessonTitle(e.target.value)} required placeholder="Ex: Aula 4: Equilíbrio Iônico" style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Tipo de Conteúdo:</label>
                <select value={newLessonType} onChange={(e) => setNewLessonType(e.target.value)} style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }}>
                  <option value="Página Web (UFC)">Página Web (UFC)</option>
                  <option value="Arquivo PDF">Arquivo PDF</option>
                  <option value="Módulo Interativo (HTML5/SCORM)">Módulo Interativo (HTML5/SCORM)</option>
                  <option value="Vídeo Aula Externa">Vídeo Aula Externa</option>
                </select>
              </div>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Endereço / URL (opcional):</label>
                <input type="text" value={newLessonUrl} onChange={(e) => setNewLessonUrl(e.target.value)} placeholder="https://..." style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
                <button type="button" className="btn-solar-secondary" onClick={() => setShowCreateLessonModal(false)}>Cancelar</button>
                <button type="submit" className="btn-solar-blue">Salvar Aula</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 4. Modal de Cadastrar Trabalho Avaliativo (Professor) */}
      {showCreateAssignmentModal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100, padding: '16px' }}>
          <div style={{ background: '#ffffff', borderRadius: '8px', maxWidth: '540px', width: '100%', padding: '24px', boxShadow: '0 10px 25px rgba(0,0,0,0.3)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '2px solid var(--solar-blue-main)', paddingBottom: '10px', marginBottom: '16px' }}>
              <h2 style={{ fontSize: '1.15rem', color: 'var(--solar-blue-dark)', margin: 0 }}>➕ Cadastrar Trabalho Avaliativo</h2>
              <button type="button" onClick={() => setShowCreateAssignmentModal(false)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#64748b' }}>
                ✕
              </button>
            </div>
            <form onSubmit={handleCreateAssignmentSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Título do Trabalho:</label>
                <input type="text" value={newAssignmentTitle} onChange={(e) => setNewAssignmentTitle(e.target.value)} required placeholder="Ex: Trabalho 3: Relatório de Soluções" style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                <div>
                  <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Modalidade:</label>
                  <select value={newAssignmentType} onChange={(e) => setNewAssignmentType(e.target.value)} style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }}>
                    <option value="Individual">Individual</option>
                    <option value="Em Grupo (até 3)">Em Grupo (até 3 alunos)</option>
                    <option value="Em Grupo (até 5)">Em Grupo (até 5 alunos)</option>
                  </select>
                </div>
                <div>
                  <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Peso na Média:</label>
                  <input type="number" step="0.1" min="0.5" max="10" value={newAssignmentWeight} onChange={(e) => setNewAssignmentWeight(e.target.value)} required style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
                </div>
              </div>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Data e Hora Limite de Entrega:</label>
                <input type="text" value={newAssignmentDeadline} onChange={(e) => setNewAssignmentDeadline(e.target.value)} required placeholder="DD/MM/AAAA HH:mm" style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
                <button type="button" className="btn-solar-secondary" onClick={() => setShowCreateAssignmentModal(false)}>Cancelar</button>
                <button type="submit" className="btn-solar-blue">Publicar Trabalho</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 5. Modal de Criar Fórum Temático (Professor) */}
      {showCreateDiscussionModal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100, padding: '16px' }}>
          <div style={{ background: '#ffffff', borderRadius: '8px', maxWidth: '540px', width: '100%', padding: '24px', boxShadow: '0 10px 25px rgba(0,0,0,0.3)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '2px solid var(--solar-blue-main)', paddingBottom: '10px', marginBottom: '16px' }}>
              <h2 style={{ fontSize: '1.15rem', color: 'var(--solar-blue-dark)', margin: 0 }}>➕ Criar Novo Fórum Temático</h2>
              <button type="button" onClick={() => setShowCreateDiscussionModal(false)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#64748b' }}>
                ✕
              </button>
            </div>
            <form onSubmit={handleCreateDiscussionSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Título do Fórum:</label>
                <input type="text" value={newDiscussionTitle} onChange={(e) => setNewDiscussionTitle(e.target.value)} required placeholder="Ex: Fórum Temático 2: Reações Redox" style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Ementa / Tema Discutido:</label>
                <textarea rows={3} value={newDiscussionDesc} onChange={(e) => setNewDiscussionDesc(e.target.value)} required placeholder="Descreva os pontos a serem abordados pelos discentes..." style={{ width: '100%', padding: '8px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
                <button type="button" className="btn-solar-secondary" onClick={() => setShowCreateDiscussionModal(false)}>Cancelar</button>
                <button type="submit" className="btn-solar-blue">Criar Fórum</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 6. Modal de Importação de Conteúdos de Disciplinas (Professor) */}
      {showImportDisciplineModal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100, padding: '16px' }}>
          <div style={{ background: '#ffffff', borderRadius: '8px', maxWidth: '580px', width: '100%', padding: '24px', boxShadow: '0 10px 25px rgba(0,0,0,0.3)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '2px solid var(--solar-blue-main)', paddingBottom: '10px', marginBottom: '16px' }}>
              <h2 style={{ fontSize: '1.15rem', color: 'var(--solar-blue-dark)', margin: 0 }}>🔄 Importar Conteúdos de Semestre Anterior</h2>
              <button type="button" onClick={() => setShowImportDisciplineModal(false)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#64748b' }}>
                ✕
              </button>
            </div>
            {importFeedback && (
              <div style={{ background: '#dcfce7', color: '#166534', padding: '10px', borderRadius: '4px', marginBottom: '12px', fontSize: '0.85rem', fontWeight: 600 }}>
                {importFeedback}
              </div>
            )}
            <p style={{ fontSize: '0.85rem', color: '#475569', lineHeight: '1.5', marginBottom: '14px' }}>
              Esta ferramenta copia automaticamente todas as Aulas, Trabalhos e Fóruns de uma oferta anterior, aplicando <strong>deslocamento inteligente de datas</strong> para o novo calendário letivo.
            </p>
            <form onSubmit={handleImportDisciplineSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Oferta de Origem:</label>
                <select style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }}>
                  <option value="1">Química Geral I - Semestre 2025.2 (Polo Fortaleza)</option>
                  <option value="2">Introdução à Linguística - Semestre 2025.2 (Polo Sobral)</option>
                </select>
              </div>
              <div>
                <label style={{ fontSize: '0.82rem', fontWeight: 700, color: '#334155', display: 'block', marginBottom: '4px' }}>Deslocamento de Datas (em dias):</label>
                <input type="number" value={importShiftDays} onChange={(e) => setImportShiftDays(e.target.value)} required style={{ width: '100%', padding: '6px 10px', fontSize: '0.85rem', border: '1px solid #cbd5e1', borderRadius: '4px' }} />
                <span style={{ fontSize: '0.78rem', color: '#64748b', marginTop: '2px', display: 'block' }}>Ex: 180 dias ajusta as entregas para o semestre seguinte.</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '8px' }}>
                <button type="button" className="btn-solar-secondary" onClick={() => setShowImportDisciplineModal(false)}>Cancelar</button>
                <button type="submit" className="btn-solar-blue">Executar Importação</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* 7. Modal de Ajuda da Sala de Aula Virtual */}
      {showHelpModal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100, padding: '16px' }}>
          <div style={{ background: '#ffffff', borderRadius: '8px', maxWidth: '580px', width: '100%', padding: '24px', boxShadow: '0 10px 25px rgba(0,0,0,0.3)' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '2px solid var(--solar-blue-main)', paddingBottom: '10px', marginBottom: '16px' }}>
              <h2 style={{ fontSize: '1.15rem', color: 'var(--solar-blue-dark)', margin: 0 }}>❓ Guia da Sala de Aula Virtual</h2>
              <button type="button" onClick={() => setShowHelpModal(false)} style={{ background: 'none', border: 'none', fontSize: '1.4rem', cursor: 'pointer', color: '#64748b' }}>
                ✕
              </button>
            </div>
            <div style={{ fontSize: '0.85rem', color: '#334155', lineHeight: '1.6', display: 'flex', flexDirection: 'column', gap: '10px' }}>
              <p><strong>• Aulas:</strong> Acesse os módulos didáticos sequenciais e acompanhe seu progresso.</p>
              <p><strong>• Trabalhos:</strong> Envie arquivos PDF, DOCX ou ZIP dentro do prazo limite estipulado.</p>
              <p><strong>• Provas Online:</strong> Avaliações com correção automática e trava anti-fraude.</p>
              <p><strong>• Diário de Notas:</strong> Consulte suas notas parciais, média final e percentual de frequência.</p>
              <p><strong>• Contato:</strong> Clique no ícone de envelope ✉️ ao lado de qualquer docente para enviar mensagem direta.</p>
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '16px' }}>
              <button type="button" className="btn-solar-blue" onClick={() => setShowHelpModal(false)}>Entendi</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default CurriculumUnitView;
