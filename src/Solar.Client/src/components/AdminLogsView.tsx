import { useState, useEffect, useCallback } from 'react';

interface StructuredLogEntry {
  id: string;
  timestamp: string;
  level: string;
  message: string;
  requestMethod?: string;
  requestPath?: string;
  statusCode?: number;
  elapsedMs?: number;
  exception?: string;
  traceId?: string;
  sourceContext?: string;
  properties: Record<string, any>;
}

interface LogsApiResponse {
  total: number;
  errorCount: number;
  warningCount: number;
  informationCount: number;
  averageLatencyMs: number;
  maxLatencyMs: number;
  logs: StructuredLogEntry[];
}

export const AdminLogsView = () => {
  const [data, setData] = useState<LogsApiResponse>({
    total: 0,
    errorCount: 0,
    warningCount: 0,
    informationCount: 0,
    averageLatencyMs: 0,
    maxLatencyMs: 0,
    logs: []
  });
  const [loading, setLoading] = useState(false);
  const [levelFilter, setLevelFilter] = useState('ALL');
  const [searchTerm, setSearchTerm] = useState('');
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [expandedLogId, setExpandedLogId] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const fetchLogs = useCallback(async () => {
    try {
      const params = new URLSearchParams();
      params.append('limit', '200');
      if (levelFilter !== 'ALL') params.append('level', levelFilter);
      if (searchTerm.trim()) params.append('search', searchTerm.trim());

      const res = await fetch(`/api/v1/admin/logs?${params.toString()}`);
      if (res.ok) {
        const json = await res.json();
        setData(json);
      }
    } catch (err) {
      console.error('Erro ao consultar logs:', err);
    }
  }, [levelFilter, searchTerm]);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  useEffect(() => {
    if (!autoRefresh) return;
    const interval = setInterval(() => {
      fetchLogs();
    }, 3000);
    return () => clearInterval(interval);
  }, [autoRefresh, fetchLogs]);

  const handleClearLogs = async () => {
    if (!confirm('Deseja realmente limpar o buffer de logs em memória?')) return;
    setLoading(true);
    try {
      const res = await fetch('/api/v1/admin/logs/clear', { method: 'POST' });
      if (res.ok) {
        await fetchLogs();
      }
    } finally {
      setLoading(false);
    }
  };

  const handleCopyLog = (log: StructuredLogEntry) => {
    navigator.clipboard.writeText(JSON.stringify(log, null, 2));
    setCopiedId(log.id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const formatTime = (isoString: string) => {
    const d = new Date(isoString);
    return d.toLocaleTimeString('pt-BR', { hour12: false }) + '.' + d.getMilliseconds().toString().padStart(3, '0');
  };

  const getStatusColor = (code?: number) => {
    if (!code) return '#6b7280';
    if (code >= 500) return '#ef4444';
    if (code >= 400) return '#f59e0b';
    if (code >= 300) return '#3b82f6';
    return '#10b981';
  };

  return (
    <div className="solar-admin-logs-container" style={{ padding: '16px', maxWidth: '1400px', margin: '0 auto' }}>
      {/* 1. Header do Dashboard */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', flexWrap: 'wrap', gap: '12px' }}>
        <div>
          <h1 style={{ fontSize: '1.4rem', fontWeight: 700, color: 'var(--solar-blue-dark, #002b49)', margin: 0 }}>
            🔭 Observabilidade & Logs em Tempo Real (Serilog)
          </h1>
          <p style={{ fontSize: '0.85rem', color: '#666', margin: '4px 0 0 0' }}>
            Monitoramento de requisições HTTP, exceções e métricas de execução do Solar LMS (.NET 10).
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <button
            type="button"
            onClick={() => setAutoRefresh(!autoRefresh)}
            style={{
              padding: '6px 12px',
              borderRadius: '4px',
              border: '1px solid #cbd5e1',
              backgroundColor: autoRefresh ? '#dcfce7' : '#f1f5f9',
              color: autoRefresh ? '#166534' : '#475569',
              fontWeight: 600,
              fontSize: '0.8rem',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: '6px'
            }}
          >
            <span style={{ height: '8px', width: '8px', borderRadius: '50%', backgroundColor: autoRefresh ? '#22c55e' : '#94a3b8', display: 'inline-block' }}></span>
            {autoRefresh ? 'Live Streaming (3s)' : 'Pausado'}
          </button>

          <button
            type="button"
            onClick={fetchLogs}
            style={{
              padding: '6px 12px',
              borderRadius: '4px',
              border: '1px solid #0284c7',
              backgroundColor: '#0284c7',
              color: '#fff',
              fontWeight: 600,
              fontSize: '0.8rem',
              cursor: 'pointer'
            }}
          >
            🔄 Atualizar
          </button>

          <button
            type="button"
            onClick={handleClearLogs}
            disabled={loading}
            style={{
              padding: '6px 12px',
              borderRadius: '4px',
              border: '1px solid #e2e8f0',
              backgroundColor: '#fff',
              color: '#ef4444',
              fontWeight: 600,
              fontSize: '0.8rem',
              cursor: 'pointer'
            }}
          >
            🗑️ Limpar Buffer
          </button>
        </div>
      </div>

      {/* 2. Banner de Métricas Rápidas */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '12px', marginBottom: '16px' }}>
        <div style={{ backgroundColor: '#fff', padding: '12px 16px', borderRadius: '8px', border: '1px solid #e2e8f0', boxShadow: '0 1px 3px rgba(0,0,0,0.05)' }}>
          <div style={{ fontSize: '0.75rem', fontWeight: 600, color: '#64748b', textTransform: 'uppercase' }}>Total de Eventos</div>
          <div style={{ fontSize: '1.4rem', fontWeight: 700, color: '#0f172a', marginTop: '4px' }}>{data.total}</div>
        </div>

        <div style={{ backgroundColor: '#fff', padding: '12px 16px', borderRadius: '8px', border: '1px solid #fee2e2', borderLeft: '4px solid #ef4444', boxShadow: '0 1px 3px rgba(0,0,0,0.05)' }}>
          <div style={{ fontSize: '0.75rem', fontWeight: 600, color: '#991b1b', textTransform: 'uppercase' }}>Erros Registrados</div>
          <div style={{ fontSize: '1.4rem', fontWeight: 700, color: '#ef4444', marginTop: '4px' }}>{data.errorCount}</div>
        </div>

        <div style={{ backgroundColor: '#fff', padding: '12px 16px', borderRadius: '8px', border: '1px solid #fef3c7', borderLeft: '4px solid #f59e0b', boxShadow: '0 1px 3px rgba(0,0,0,0.05)' }}>
          <div style={{ fontSize: '0.75rem', fontWeight: 600, color: '#92400e', textTransform: 'uppercase' }}>Avisos (Warnings)</div>
          <div style={{ fontSize: '1.4rem', fontWeight: 700, color: '#f59e0b', marginTop: '4px' }}>{data.warningCount}</div>
        </div>

        <div style={{ backgroundColor: '#fff', padding: '12px 16px', borderRadius: '8px', border: '1px solid #e2e8f0', boxShadow: '0 1px 3px rgba(0,0,0,0.05)' }}>
          <div style={{ fontSize: '0.75rem', fontWeight: 600, color: '#64748b', textTransform: 'uppercase' }}>Latência Média</div>
          <div style={{ fontSize: '1.4rem', fontWeight: 700, color: '#0284c7', marginTop: '4px' }}>{data.averageLatencyMs} <span style={{ fontSize: '0.85rem' }}>ms</span></div>
        </div>

        <div style={{ backgroundColor: '#fff', padding: '12px 16px', borderRadius: '8px', border: '1px solid #e2e8f0', boxShadow: '0 1px 3px rgba(0,0,0,0.05)' }}>
          <div style={{ fontSize: '0.75rem', fontWeight: 600, color: '#64748b', textTransform: 'uppercase' }}>Pico de Latência</div>
          <div style={{ fontSize: '1.4rem', fontWeight: 700, color: '#6366f1', marginTop: '4px' }}>{data.maxLatencyMs} <span style={{ fontSize: '0.85rem' }}>ms</span></div>
        </div>
      </div>

      {/* 3. Barra de Filtros */}
      <div style={{ backgroundColor: '#fff', padding: '12px 16px', borderRadius: '8px', border: '1px solid #e2e8f0', marginBottom: '16px', display: 'flex', gap: '12px', flexWrap: 'wrap', alignItems: 'center' }}>
        <div style={{ display: 'flex', gap: '6px' }}>
          {['ALL', 'Information', 'Warning', 'Error'].map((lvl) => (
            <button
              key={lvl}
              type="button"
              onClick={() => setLevelFilter(lvl)}
              style={{
                padding: '6px 12px',
                borderRadius: '4px',
                border: '1px solid',
                borderColor: levelFilter === lvl ? '#0284c7' : '#cbd5e1',
                backgroundColor: levelFilter === lvl ? '#0284c7' : '#fff',
                color: levelFilter === lvl ? '#fff' : '#475569',
                fontSize: '0.8rem',
                fontWeight: 600,
                cursor: 'pointer'
              }}
            >
              {lvl === 'ALL' ? 'Todos os Níveis' : lvl}
            </button>
          ))}
        </div>

        <div style={{ flex: 1, minWidth: '240px' }}>
          <input
            type="text"
            placeholder="Buscar por rota, status, mensagem ou exceção (ex: /auth, 500, DB)..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            style={{
              width: '100%',
              padding: '6px 12px',
              borderRadius: '4px',
              border: '1px solid #cbd5e1',
              fontSize: '0.85rem'
            }}
          />
        </div>
      </div>

      {/* 4. Lista de Logs Estilo Terminal Interativo */}
      <div style={{ backgroundColor: '#0f172a', borderRadius: '8px', border: '1px solid #1e293b', overflow: 'hidden', boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
        <div style={{ padding: '10px 16px', backgroundColor: '#1e293b', borderBottom: '1px solid #334155', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span style={{ fontSize: '0.8rem', fontWeight: 600, color: '#94a3b8', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Fluxo de Eventos ({data.logs.length} exibidos)
          </span>
          <span style={{ fontSize: '0.75rem', color: '#64748b' }}>Clique em uma linha para inspecionar JSON e StackTrace</span>
        </div>

        {data.logs.length === 0 ? (
          <div style={{ padding: '32px', textAlign: 'center', color: '#64748b', fontSize: '0.9rem' }}>
            Nenhum evento registrado com os filtros atuais. Realize requisições no Solar LMS para visualizar os logs.
          </div>
        ) : (
          <div style={{ maxHeight: '650px', overflowY: 'auto' }}>
            {data.logs.map((log) => {
              const isExpanded = expandedLogId === log.id;
              return (
                <div
                  key={log.id}
                  style={{
                    borderBottom: '1px solid #1e293b',
                    fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
                    fontSize: '0.82rem',
                    backgroundColor: isExpanded ? '#1e293b' : 'transparent',
                    transition: 'background-color 0.15s ease'
                  }}
                >
                  <div
                    onClick={() => setExpandedLogId(isExpanded ? null : log.id)}
                    style={{
                      padding: '8px 16px',
                      display: 'flex',
                      alignItems: 'center',
                      gap: '10px',
                      cursor: 'pointer',
                      color: '#e2e8f0',
                      flexWrap: 'nowrap'
                    }}
                  >
                    {/* Timestamp */}
                    <span style={{ color: '#64748b', minWidth: '95px' }}>{formatTime(log.timestamp)}</span>

                    {/* Level */}
                    <span
                      style={{
                        padding: '1px 6px',
                        borderRadius: '3px',
                        fontSize: '0.7rem',
                        fontWeight: 700,
                        backgroundColor:
                          log.level === 'Error' || log.level === 'Fatal' ? '#7f1d1d' :
                          log.level === 'Warning' ? '#78350f' : '#1e3a8a',
                        color:
                          log.level === 'Error' || log.level === 'Fatal' ? '#fca5a5' :
                          log.level === 'Warning' ? '#fcd34d' : '#93c5fd',
                        minWidth: '40px',
                        textAlign: 'center'
                      }}
                    >
                      {log.level.substring(0, 3).toUpperCase()}
                    </span>

                    {/* Method & Status */}
                    {log.requestMethod && (
                      <span style={{ fontWeight: 700, color: '#38bdf8', minWidth: '42px' }}>
                        {log.requestMethod}
                      </span>
                    )}

                    {log.statusCode && (
                      <span style={{ fontWeight: 700, color: getStatusColor(log.statusCode), minWidth: '32px' }}>
                        {log.statusCode}
                      </span>
                    )}

                    {/* Message or Path */}
                    <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', color: log.level === 'Error' ? '#fca5a5' : '#f8fafc' }}>
                      {log.message}
                    </span>

                    {/* Elapsed Time */}
                    {log.elapsedMs !== undefined && log.elapsedMs !== null && (
                      <span style={{ color: log.elapsedMs > 500 ? '#f59e0b' : '#94a3b8', minWidth: '70px', textAlign: 'right', fontSize: '0.75rem' }}>
                        {log.elapsedMs.toFixed(1)} ms
                      </span>
                    )}

                    <span style={{ color: '#64748b', fontSize: '0.75rem' }}>{isExpanded ? '▲' : '▼'}</span>
                  </div>

                  {/* Detalhes Expandidos (JSON Properties & Stack Trace) */}
                  {isExpanded && (
                    <div style={{ padding: '12px 16px', backgroundColor: '#090d16', borderTop: '1px solid #334155' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                        <span style={{ fontSize: '0.75rem', fontWeight: 600, color: '#38bdf8' }}>
                          PROPRIEDADES DO EVENTO SERILOG & RASTREAMENTO
                        </span>
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleCopyLog(log);
                          }}
                          style={{
                            padding: '3px 8px',
                            backgroundColor: '#334155',
                            color: '#e2e8f0',
                            borderRadius: '4px',
                            border: 'none',
                            fontSize: '0.7rem',
                            cursor: 'pointer'
                          }}
                        >
                          {copiedId === log.id ? '✅ Copiado!' : '📋 Copiar JSON'}
                        </button>
                      </div>

                      {log.exception && (
                        <div style={{ marginBottom: '12px', padding: '8px', backgroundColor: '#450a0a', border: '1px solid #7f1d1d', borderRadius: '4px', color: '#fca5a5', whiteSpace: 'pre-wrap', fontSize: '0.75rem' }}>
                          <strong>Stack Trace:</strong>
                          <br />
                          {log.exception}
                        </div>
                      )}

                      <pre style={{ margin: 0, padding: '8px', backgroundColor: '#020617', borderRadius: '4px', color: '#94a3b8', fontSize: '0.75rem', overflowX: 'auto' }}>
                        {JSON.stringify(
                          {
                            traceId: log.traceId,
                            sourceContext: log.sourceContext,
                            properties: log.properties
                          },
                          null,
                          2
                        )}
                      </pre>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
