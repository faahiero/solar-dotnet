import { useState, useEffect } from 'react';

interface QuestionItem {
  id: number;
  text: string;
  correct: boolean;
}

interface ExamQuestion {
  id: number;
  enunciation: string;
  type: string;
  items: QuestionItem[];
}

interface ExamStartData {
  examId: number;
  name: string;
  durationMinutes: number;
  startedAt: string;
  blockContent: boolean;
  questions: ExamQuestion[];
}

interface OnlineExamPlayerProps {
  curriculumUnitId: number;
  examId: number;
  onFinishExam: () => void;
}

export const OnlineExamPlayer = ({
  curriculumUnitId,
  examId,
  onFinishExam
}: OnlineExamPlayerProps) => {
  const [examData, setExamData] = useState<ExamStartData | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedAnswers, setSelectedAnswers] = useState<Record<number, number>>({});
  const [timeLeftSeconds, setTimeLeftSeconds] = useState(3600); // 60 min
  const [submitting, setSubmitting] = useState(false);
  const [examResult, setExamResult] = useState<{
    score: number;
    totalQuestions: number;
    correctAnswers: number;
    situation: string;
    message: string;
  } | null>(null);

  useEffect(() => {
    fetch(`/api/v1/curriculum-units/${curriculumUnitId}/exams/${examId}/start`, {
      method: 'POST'
    })
      .then((res) => res.json())
      .then((data) => {
        setExamData(data);
        setTimeLeftSeconds((data.durationMinutes || 60) * 60);
        setLoading(false);
      })
      .catch((err) => {
        console.error('Erro ao iniciar prova:', err);
        setLoading(false);
      });
  }, [curriculumUnitId, examId]);

  // Cronômetro Regressivo
  useEffect(() => {
    if (examResult) return;
    const timer = setInterval(() => {
      setTimeLeftSeconds((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          handleSubmitExam();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [examResult]);

  const handleSelectOption = (questionId: number, itemId: number) => {
    setSelectedAnswers((prev) => ({
      ...prev,
      [questionId]: itemId
    }));
  };

  const handleSubmitExam = async () => {
    setSubmitting(true);
    try {
      const response = await fetch(`/api/v1/curriculum-units/${curriculumUnitId}/exams/${examId}/submit`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ answers: selectedAnswers })
      });
      const data = await response.json();
      setExamResult(data);
    } catch (err) {
      alert('Erro ao submeter prova: ' + err);
    } finally {
      setSubmitting(false);
    }
  };

  const formatTime = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  if (loading) {
    return (
      <div className="solar-portlet-card" style={{ padding: '30px', textAlign: 'center' }}>
        <p>Iniciando ambiente seguro de avaliação online...</p>
      </div>
    );
  }

  if (examResult) {
    return (
      <div className="solar-portlet-card" style={{ padding: '28px', textAlign: 'center' }}>
        <div style={{ fontSize: '3rem', marginBottom: '12px' }}>
          {examResult.score >= 7.0 ? '🎉' : '📝'}
        </div>
        <h2 style={{ fontSize: '1.4rem', color: 'var(--solar-blue-dark)', marginBottom: '8px' }}>
          Resultado da Avaliação Online
        </h2>
        <p style={{ fontSize: '1rem', color: '#444', marginBottom: '16px' }}>
          {examResult.message}
        </p>

        <div style={{ display: 'inline-flex', gap: '24px', background: '#f8fafc', padding: '16px 28px', borderRadius: '6px', border: '1px solid var(--solar-border)' }}>
          <div>
            <span style={{ fontSize: '0.8rem', color: '#666', display: 'block' }}>Sua Nota</span>
            <strong style={{ fontSize: '1.8rem', color: examResult.score >= 7 ? 'var(--solar-success)' : 'var(--solar-error)' }}>
              {examResult.score.toFixed(1)}
            </strong>
          </div>
          <div style={{ borderLeft: '1px solid #ddd', paddingLeft: '24px' }}>
            <span style={{ fontSize: '0.8rem', color: '#666', display: 'block' }}>Acertos</span>
            <strong style={{ fontSize: '1.8rem', color: 'var(--solar-blue-main)' }}>
              {examResult.correctAnswers} / {examResult.totalQuestions}
            </strong>
          </div>
          <div style={{ borderLeft: '1px solid #ddd', paddingLeft: '24px' }}>
            <span style={{ fontSize: '0.8rem', color: '#666', display: 'block' }}>Situação</span>
            <strong style={{ fontSize: '1.2rem', color: '#222', lineHeight: '2.4' }}>
              {examResult.situation}
            </strong>
          </div>
        </div>

        <div style={{ marginTop: '24px' }}>
          <button type="button" className="btn-solar-blue" onClick={onFinishExam}>
            Retornar à Turma
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="exam-player-root solar-portlet-card" style={{ padding: '20px' }}>
      {/* Banner de Trava Anti-Fraude */}
      <div style={{ background: '#fffbeb', border: '1px solid #fde68a', borderRadius: '4px', padding: '10px 16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span style={{ fontSize: '1.2rem' }}>🔒</span>
          <div>
            <strong style={{ color: '#b45309', fontSize: '0.9rem' }}>Modo Seguro de Prova Ativo (Trava Anti-Fraude)</strong>
            <p style={{ fontSize: '0.78rem', color: '#78350f' }}>O acesso a outras aulas e módulos fica retido até a finalização desta avaliação.</p>
          </div>
        </div>

        <div style={{ textAlign: 'right' }}>
          <span style={{ fontSize: '0.75rem', color: '#78350f', display: 'block' }}>Tempo Restante:</span>
          <strong style={{ fontSize: '1.3rem', color: timeLeftSeconds < 300 ? '#dc2626' : '#b45309', fontFamily: 'monospace' }}>
            ⏱ {formatTime(timeLeftSeconds)}
          </strong>
        </div>
      </div>

      <h2 style={{ fontSize: '1.25rem', color: 'var(--solar-blue-dark)', marginBottom: '16px' }}>
        {examData?.name}
      </h2>

      {/* Questões */}
      <div className="exam-questions-list">
        {(examData?.questions || []).map((q) => (
          <div key={q.id} style={{ background: '#ffffff', border: '1px solid var(--solar-border)', borderRadius: '4px', padding: '16px', marginBottom: '16px' }}>
            <h3 style={{ fontSize: '0.96rem', color: '#222', marginBottom: '12px', fontWeight: 600 }}>
              {q.enunciation}
            </h3>

            <div className="exam-options-group" style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {q.items.map((item) => (
                <label
                  key={item.id}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '10px',
                    padding: '8px 12px',
                    borderRadius: '4px',
                    border: '1px solid',
                    borderColor: selectedAnswers[q.id] === item.id ? 'var(--solar-blue-main)' : '#e2e8f0',
                    background: selectedAnswers[q.id] === item.id ? '#eff6ff' : '#fafbfc',
                    cursor: 'pointer',
                    fontSize: '0.88rem'
                  }}
                >
                  <input
                    type="radio"
                    name={`question_${q.id}`}
                    checked={selectedAnswers[q.id] === item.id}
                    onChange={() => handleSelectOption(q.id, item.id)}
                  />
                  <span>{item.text}</span>
                </label>
              ))}
            </div>
          </div>
        ))}
      </div>

      {/* Ações Finais */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '24px', borderTop: '1px solid var(--solar-border)', paddingTop: '16px' }}>
        <span style={{ fontSize: '0.85rem', color: '#666' }}>
          Respondidas: <strong>{Object.keys(selectedAnswers).length}</strong> de <strong>{examData?.questions.length || 0}</strong>
        </span>

        <button
          type="button"
          className="solar-btn-acessar"
          style={{ width: 'auto', padding: '10px 28px', fontSize: '0.95rem' }}
          onClick={handleSubmitExam}
          disabled={submitting}
        >
          {submitting ? 'Corrigindo respostas...' : 'Finalizar e Entregar Prova ✔'}
        </button>
      </div>
    </div>
  );
};
