import { useState } from 'react';

export const LockoutTab = () => {
  const [studentId, setStudentId] = useState(999);
  const [output, setOutput] = useState('// Clique no botão para testar...');
  const [loading, setLoading] = useState(false);

  const handleTest = async () => {
    setLoading(true);
    try {
      const res = await fetch('/api/v1/lessons', {
        headers: { 'X-User-Id': studentId.toString() }
      });
      const data = await res.json();
      setOutput(`HTTP ${res.status} ${res.statusText}\n` + JSON.stringify(data, null, 2));
    } catch (err) {
      setOutput('Erro: ' + err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="solar-portlet">
      <div className="solar-portlet-header">
        <div className="solar-portlet-header-title">
          <img src="/assets/images/gear.png" alt="" className="portlet-icon" />
          <span>Simulador de Bloqueio Anti-Fraude (ExamLockoutMiddleware)</span>
        </div>
      </div>

      <div className="solar-portlet-body">
        <p style={{ fontSize: '0.88rem', color: 'var(--solar-text-secondary)', marginBottom: '16px' }}>
          Enquanto um aluno está em avaliação ativa (com <code>block_content = true</code>), o middleware interrompe acessos indevidos a materiais didáticos.
        </p>

        <div className="form-group">
          <label htmlFor="stId">Simular Aluno com ID:</label>
          <input
            id="stId"
            type="number"
            value={studentId}
            onChange={(e) => setStudentId(parseInt(e.target.value) || 0)}
            style={{ width: '140px', display: 'inline-block' }}
          />
          <button
            type="button"
            className="btn-solar-blue"
            onClick={handleTest}
            disabled={loading}
            style={{ width: 'auto', marginLeft: '10px' }}
          >
            {loading ? 'Consultando...' : 'Tentar Acessar /api/v1/lessons'}
          </button>
        </div>

        <div className="result-box">{output}</div>
      </div>
    </div>
  );
};
