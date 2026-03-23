import React, { useState, useEffect, useCallback } from 'react';
import api from '../../services/api';

interface Lead {
  id: number;
  email: string;
  source: string;
  marketingConsent: boolean;
  isRegistered: boolean;
  createdAt: string;
  ipAddress: string | null;
}

const LeadsPage: React.FC = () => {
  const [leads, setLeads] = useState<Lead[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const pageSize = 20;

  const fetchLeads = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
      if (search) params.append('search', search);
      const response = await api.get(`/analytics/leads?${params}`);
      setLeads(response.data.items);
      setTotalCount(response.data.totalCount);
    } catch { /* ignore */ } finally {
      setLoading(false);
    }
  }, [page, search]);

  useEffect(() => {
    const timer = setTimeout(fetchLeads, 300);
    return () => clearTimeout(timer);
  }, [fetchLeads]);

  const exportCsv = async () => {
    try {
      const response = await api.get('/analytics/leads/export', { responseType: 'blob' });
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.download = `leady_${new Date().toISOString().slice(0, 10)}.csv`;
      link.click();
      window.URL.revokeObjectURL(url);
    } catch { /* ignore */ }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Leady</h1>
          <p className="text-sm text-gray-500">{totalCount} rekordów</p>
        </div>
        <button onClick={exportCsv}
          className="px-4 py-2 bg-green-600 text-white rounded-md text-sm font-medium hover:bg-green-700">
          Eksportuj CSV
        </button>
      </div>

      <div className="mb-4">
        <input type="text" value={search} onChange={e => { setSearch(e.target.value); setPage(1); }}
          placeholder="Szukaj po email..."
          className="w-full max-w-sm px-3 py-2 border rounded-md text-sm"
        />
      </div>

      <div className="bg-white rounded-lg shadow-sm border overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Data</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Źródło</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Zgoda</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Zarejestrowany</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">IP</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {loading ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-500">Ładowanie...</td></tr>
            ) : leads.length === 0 ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-500">Brak leadów.</td></tr>
            ) : leads.map(lead => (
              <tr key={lead.id} className="hover:bg-gray-50">
                <td className="px-4 py-3 text-sm text-gray-900">{lead.email}</td>
                <td className="px-4 py-3 text-sm text-gray-500">{new Date(lead.createdAt).toLocaleString('pl-PL')}</td>
                <td className="px-4 py-3 text-sm text-gray-500">{lead.source}</td>
                <td className="px-4 py-3">
                  <span className={`px-2 py-0.5 text-xs rounded-full ${lead.marketingConsent ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'}`}>
                    {lead.marketingConsent ? 'Tak' : 'Nie'}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <span className={`px-2 py-0.5 text-xs rounded-full ${lead.isRegistered ? 'bg-blue-100 text-blue-800' : 'bg-gray-100 text-gray-600'}`}>
                    {lead.isRegistered ? 'Tak' : 'Nie'}
                  </span>
                </td>
                <td className="px-4 py-3 text-sm text-gray-400">{lead.ipAddress || '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="flex justify-center gap-2 mt-4">
          <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
            className="px-3 py-1 border rounded text-sm disabled:opacity-50">Poprzednia</button>
          <span className="px-3 py-1 text-sm text-gray-600">Strona {page} z {totalPages}</span>
          <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}
            className="px-3 py-1 border rounded text-sm disabled:opacity-50">Następna</button>
        </div>
      )}
    </div>
  );
};

export default LeadsPage;
