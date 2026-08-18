import { useState } from 'react';
import type { CalculateStudentGradesCommand, GradingCalculationResult } from '../../types/grading';

export const GradingTab = () => {
  const [gradeP1, setGradeP1] = useState(8.0);
  const [gradeT1, setGradeT1] = useState(6.5);
  const [gradeAF, setGradeAF] = useState<number | ''>('');
  const [passingGrade, setPassingGrade] = useState(7.0);
  const [minGradeAF, setMinGradeAF] = useState(3.0);
  const [result, setResult] = useState<GradingCalculationResult | null>(null);
  const [loading, setLoading] = useState(false);

  const handleCalculate = async () => {
    setLoading(true);
    const activities = [
      {
        activityId: 1,
        name: 'Prova 1',
        isEvaluative: true,
        isFrequency: true,
        weight: 1.0,
        finalWeight: 40.0,
        studentGrade: gradeP1,
        studentWorkingHours: 30.0
      },
      {
        activityId: 2,
        name: 'Trabalho 1',
        isEvaluative: true,
        isFrequency: true,
        weight: 1.0,
        finalWeight: 60.0,
        studentGrade: gradeT1,
        studentWorkingHours: 32.0
      }
    ];

    if (gradeAF !== '') {
      activities.push({
        activityId: 3,
        name: 'Prova Final (AF)',
        isEvaluative: true,
        isFrequency: false,
        weight: 1.0,
        finalWeight: 100.0,
        studentGrade: Number(gradeAF),
        studentWorkingHours: 0
      });
    }

    const payload: CalculateStudentGradesCommand = {
      userId: 1,
      allocationId: 10,
      criteria: {
        passingGrade: passingGrade,
        minGradeToFinalExam: minGradeAF,
        finalExamPassingGrade: 5.0,
        totalWorkingHours: 64,
        minHoursPercentage: 75.0,
        hasFinalExamInOffering: true
      },
      activities
    };

    try {
      const res = await fetch('/api/v1/grades/calculate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      const data: GradingCalculationResult = await res.json();
      setResult(data);
    } catch (err) {
      alert('Erro ao calcular médias: ' + err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="grid-2">
      <div className="solar-portlet">
        <div className="solar-portlet-header">
          <div className="solar-portlet-header-title">
            <img src="/assets/images/icon_curriculum_unit.png" alt="" className="portlet-icon" />
            <span>Simulador do Diário de Notas</span>
          </div>
        </div>

        <div className="solar-portlet-body">
          <div className="form-group">
            <label>Critérios do Curso / Oferta</label>
            <div className="grid-2">
              <div>
                <label style={{ fontSize: '0.75rem' }}>Média de Aprovação</label>
                <input
                  type="number"
                  step="0.1"
                  value={passingGrade}
                  onChange={(e) => setPassingGrade(parseFloat(e.target.value))}
                />
              </div>
              <div>
                <label style={{ fontSize: '0.75rem' }}>Mínimo p/ Exame Final</label>
                <input
                  type="number"
                  step="0.1"
                  value={minGradeAF}
                  onChange={(e) => setMinGradeAF(parseFloat(e.target.value))}
                />
              </div>
            </div>
          </div>

          <div className="form-group">
            <label>Atividades Avaliativas</label>
            <div style={{ fontSize: '0.85rem', color: 'var(--solar-text-secondary)', marginBottom: '12px' }}>
              • <strong>Prova 1 (Bloco 40%):</strong> Peso 1.0, Nota:{' '}
              <input
                type="number"
                step="0.1"
                value={gradeP1}
                onChange={(e) => setGradeP1(parseFloat(e.target.value) || 0)}
                style={{ width: '70px', display: 'inline-block' }}
              />{' '}
              (Frequência: 30h)<br />
              • <strong>Trabalho Prático (Bloco 60%):</strong> Peso 1.0, Nota:{' '}
              <input
                type="number"
                step="0.1"
                value={gradeT1}
                onChange={(e) => setGradeT1(parseFloat(e.target.value) || 0)}
                style={{ width: '70px', display: 'inline-block' }}
              />{' '}
              (Frequência: 32h)<br />
              • <strong>Avaliação Final (AF - Se aplicável):</strong> Nota:{' '}
              <input
                type="number"
                step="0.1"
                placeholder="Opcional"
                value={gradeAF}
                onChange={(e) => setGradeAF(e.target.value === '' ? '' : parseFloat(e.target.value))}
                style={{ width: '90px', display: 'inline-block' }}
              />
            </div>
          </div>

          <button type="button" className="btn-solar-blue" onClick={handleCalculate} disabled={loading} style={{ width: '100%' }}>
            {loading ? 'Calculando...' : 'Calcular Médias e Situação'}
          </button>
        </div>
      </div>

      <div className="solar-portlet">
        <div className="solar-portlet-header">
          <div className="solar-portlet-header-title">
            <img src="/assets/images/icon_clock.png" alt="" className="portlet-icon" />
            <span>Resultado da Avaliação</span>
          </div>
        </div>
        <div className="solar-portlet-body">
          <div className="result-box">
            {result ? JSON.stringify(result, null, 2) : '// Clique em "Calcular Médias" para processar...'}
          </div>
        </div>
      </div>
    </div>
  );
};
