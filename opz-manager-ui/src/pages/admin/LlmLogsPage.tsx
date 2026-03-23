import React, { useState, useEffect, useCallback } from 'react';
import { llmLogsAPI, LlmLogSummary, LlmLogDetail } from '../../services/api';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import toast from 'react-hot-toast';

const LlmLogsPage: React.FC = () => {
  const [logs, setLogs] = useState<LlmLogSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [loading, setLoading] = useState(true);
  const [methods, setMethods] = useState<string[]>([]);

  // Filters
  const [statusFilter, setStatusFilter] = useState('');
  const [methodFilter, setMethodFilter] = useState('');

  // Expanded row detail
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const [detail, setDetail] = useState<LlmLogDetail | null>(null);
  const [loadingDetail, setLoadingDetail] = useState(false);

  const fetchLogs = useCallback(async () => {
    try {
      setLoading(true);
      const params: Record<string, string | number> = { page, pageSize };
      if (statusFilter) params.status = statusFilter;
      if (methodFilter) params.method = methodFilter;
      const data = await llmLogsAPI.getLogs(params);
      setLogs(data.items);
      setTotalCount(data.totalCount);
    } catch {
      toast.error('Błąd podczas pobierania logów LLM');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, statusFilter, methodFilter]);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  useEffect(() => {
    llmLogsAPI.getMethods().then(setMethods).catch(() => {});
  }, []);

  const handleRowClick = async (id: number) => {
    if (expandedId === id) {
      setExpandedId(null);
      setDetail(null);
      return;
    }
    setExpandedId(id);
    setLoadingDetail(true);
    try {
      const d = await llmLogsAPI.getLogDetail(id);
      setDetail(d);
    } catch {
      toast.error('Błąd podczas pobierania szczegółów');
    } finally {
      setLoadingDetail(false);
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  const formatDuration = (ms: number) => {
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(1)}s`;
  };

  const formatTimestamp = (ts: string) => {
    return new Date(ts).toLocaleString('pl-PL', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit', second: '2-digit',
    });
  };

  const shortMethod = (method: string) => {
    const parts = method.split('.');
    return parts.length > 1 ? parts[parts.length - 1] : method;
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Log komunikacji LLM</h1>
        <button
          onClick={() => { setPage(1); fetchLogs(); }}
          className="px-4 py-2 bg-gray-100 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-200 border border-gray-300"
        >
          Odśwież
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4 mb-4 flex flex-wrap gap-4 items-end">
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Status</label>
          <select
            value={statusFilter}
            onChange={e => { setStatusFilter(e.target.value); setPage(1); }}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500"
          >
            <option value="">Wszystkie</option>
            <option value="success">Sukces</option>
            <option value="error">Błąd</option>
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Metoda</label>
          <select
            value={methodFilter}
            onChange={e => { setMethodFilter(e.target.value); setPage(1); }}
            className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500"
          >
            <option value="">Wszystkie</option>
            {methods.map(m => (
              <option key={m} value={m}>{shortMethod(m)}</option>
            ))}
          </select>
        </div>
        <div className="text-sm text-gray-500">
          {totalCount} {totalCount === 1 ? 'wpis' : 'wpisów'}
        </div>
      </div>

      {/* Table */}
      {loading ? (
        <LoadingSpinner message="Ładowanie logów..." />
      ) : logs.length === 0 ? (
        <p className="text-gray-500 bg-white rounded-lg border p-6 text-center">Brak wpisów w logu.</p>
      ) : (
        <div className="overflow-x-auto border border-gray-200 rounded-lg shadow-sm">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Czas</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Metoda</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Dostawca</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Model</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Czas trwania</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Tokeny (we/wy)</th>
                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Status</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {logs.map(log => (
                <React.Fragment key={log.id}>
                  <tr
                    onClick={() => handleRowClick(log.id)}
                    className={`cursor-pointer hover:bg-gray-50 ${expandedId === log.id ? 'bg-indigo-50' : ''}`}
                  >
                    <td className="px-4 py-3 text-sm text-gray-600 whitespace-nowrap">
                      {formatTimestamp(log.timestamp)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-800 font-medium" title={log.callerMethod}>
                      {shortMethod(log.callerMethod)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600">{log.providerName}</td>
                    <td className="px-4 py-3 text-sm text-gray-600 max-w-[200px] truncate" title={log.modelName}>
                      {log.modelName}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600 text-right whitespace-nowrap">
                      {formatDuration(log.durationMs)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600 text-right whitespace-nowrap">
                      {log.inputTokens ?? '—'} / {log.outputTokens ?? '—'}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <span className={`inline-block px-2 py-0.5 rounded text-xs font-medium ${
                        log.success
                          ? 'bg-green-100 text-green-700'
                          : 'bg-red-100 text-red-700'
                      }`}>
                        {log.success ? 'OK' : 'Błąd'}
                      </span>
                    </td>
                  </tr>
                  {expandedId === log.id && (
                    <tr>
                      <td colSpan={7} className="px-4 py-4 bg-gray-50 border-t border-gray-200">
                        {loadingDetail ? (
                          <div className="text-sm text-gray-500">Ładowanie szczegółów...</div>
                        ) : detail ? (
                          <div className="space-y-3">
                            <div className="flex gap-4 text-xs text-gray-500">
                              <span>Max tokens: {detail.maxTokensRequested}</span>
                              <span>Temperatura: {detail.temperature}</span>
                              <span>Czas: {formatDuration(detail.durationMs)}</span>
                            </div>
                            {detail.errorMessage && (
                              <div>
                                <h4 className="text-xs font-semibold text-red-700 mb-1">Błąd</h4>
                                <pre className="bg-red-50 border border-red-200 rounded p-3 text-xs text-red-800 whitespace-pre-wrap max-h-32 overflow-y-auto">
                                  {detail.errorMessage}
                                </pre>
                              </div>
                            )}
                            <div>
                              <h4 className="text-xs font-semibold text-gray-700 mb-1">System Prompt</h4>
                              <pre className="bg-white border border-gray-200 rounded p-3 text-xs text-gray-700 whitespace-pre-wrap max-h-48 overflow-y-auto">
                                {detail.systemPrompt || '(pusty)'}
                              </pre>
                            </div>
                            <div>
                              <h4 className="text-xs font-semibold text-gray-700 mb-1">User Prompt</h4>
                              <pre className="bg-white border border-gray-200 rounded p-3 text-xs text-gray-700 whitespace-pre-wrap max-h-64 overflow-y-auto">
                                {detail.userPrompt || '(pusty)'}
                              </pre>
                            </div>
                            <div>
                              <h4 className="text-xs font-semibold text-gray-700 mb-1">Odpowiedź</h4>
                              <pre className="bg-white border border-gray-200 rounded p-3 text-xs text-gray-700 whitespace-pre-wrap max-h-64 overflow-y-auto">
                                {detail.response || '(pusta)'}
                              </pre>
                            </div>
                          </div>
                        ) : null}
                      </td>
                    </tr>
                  )}
                </React.Fragment>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page <= 1}
            className="px-4 py-2 bg-white border border-gray-300 rounded-lg text-sm hover:bg-gray-50 disabled:opacity-50"
          >
            Poprzednia
          </button>
          <span className="text-sm text-gray-600">
            Strona {page} z {totalPages}
          </span>
          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page >= totalPages}
            className="px-4 py-2 bg-white border border-gray-300 rounded-lg text-sm hover:bg-gray-50 disabled:opacity-50"
          >
            Następna
          </button>
        </div>
      )}
    </div>
  );
};

export default LlmLogsPage;
