import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import publicApi from '../../services/publicApi';

interface CatalogModel {
  id: number;
  modelName: string;
  manufacturerName: string;
  typeName: string;
  specificationsJson: string;
}

interface EquipmentType {
  id: number;
  name: string;
}

interface Manufacturer {
  id: number;
  name: string;
}

const CatalogPage: React.FC = () => {
  const navigate = useNavigate();
  const [models, setModels] = useState<CatalogModel[]>([]);
  const [types, setTypes] = useState<EquipmentType[]>([]);
  const [manufacturers, setManufacturers] = useState<Manufacturer[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<number | null>(null);
  const [mfgFilter, setMfgFilter] = useState<number | null>(null);
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 12;

  const fetchModels = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (search) params.append('q', search);
      if (typeFilter) params.append('typeId', String(typeFilter));
      if (mfgFilter) params.append('manufacturerId', String(mfgFilter));
      params.append('page', String(page));
      params.append('pageSize', String(pageSize));
      const response = await publicApi.get(`/public/equipment/models/search?${params}`);
      setModels(response.data.items);
      setTotalCount(response.data.totalCount);
    } catch {
      setModels([]);
    } finally {
      setLoading(false);
    }
  }, [search, typeFilter, mfgFilter, page]);

  useEffect(() => {
    const loadFilters = async () => {
      try {
        const [typesRes, mfgRes] = await Promise.all([
          publicApi.get('/public/equipment/types'),
          publicApi.get('/public/equipment/manufacturers'),
        ]);
        setTypes(typesRes.data);
        setManufacturers(mfgRes.data);
      } catch { /* ignore */ }
    };
    loadFilters();
  }, []);

  useEffect(() => {
    const timer = setTimeout(fetchModels, 300);
    return () => clearTimeout(timer);
  }, [fetchModels]);

  const toggleSelect = (id: number) => {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">Katalog sprzętu</h1>
        <p className="mt-2 text-gray-600">Przeglądaj i porównuj dostępne modele sprzętu IT</p>
      </div>

      <div className="flex flex-col lg:flex-row gap-6">
        {/* Filters */}
        <div className="lg:w-64 flex-shrink-0">
          <div className="bg-white rounded-lg shadow-sm border p-4 space-y-4 sticky top-20">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Szukaj</label>
              <input type="text" value={search} onChange={e => { setSearch(e.target.value); setPage(1); }}
                placeholder="Nazwa modelu..."
                className="w-full px-3 py-2 border rounded-md text-sm focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Kategoria</label>
              <select value={typeFilter ?? ''} onChange={e => { setTypeFilter(e.target.value ? Number(e.target.value) : null); setPage(1); }}
                className="w-full px-3 py-2 border rounded-md text-sm">
                <option value="">Wszystkie</option>
                {types.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Producent</label>
              <select value={mfgFilter ?? ''} onChange={e => { setMfgFilter(e.target.value ? Number(e.target.value) : null); setPage(1); }}
                className="w-full px-3 py-2 border rounded-md text-sm">
                <option value="">Wszyscy</option>
                {manufacturers.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
              </select>
            </div>

            {selectedIds.length >= 2 && (
              <button onClick={() => navigate(`/catalog/compare?ids=${selectedIds.join(',')}`)}
                className="w-full py-2 px-4 bg-indigo-600 text-white rounded-md text-sm font-medium hover:bg-indigo-700">
                Porównaj ({selectedIds.length})
              </button>
            )}
          </div>
        </div>

        {/* Grid */}
        <div className="flex-1">
          {loading ? (
            <div className="text-center py-12 text-gray-500">Ładowanie...</div>
          ) : models.length === 0 ? (
            <div className="text-center py-12 text-gray-500">Nie znaleziono modeli spełniających kryteria.</div>
          ) : (
            <>
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {models.map(model => (
                  <div key={model.id}
                    className={`bg-white rounded-lg shadow-sm border p-4 hover:shadow-md transition-shadow cursor-pointer ${
                      selectedIds.includes(model.id) ? 'ring-2 ring-indigo-500' : ''
                    }`}>
                    <div className="flex items-start justify-between">
                      <div className="flex-1" onClick={() => navigate(`/catalog/${model.id}`)}>
                        <span className="inline-block px-2 py-0.5 bg-blue-100 text-blue-800 text-xs rounded-full mb-2">
                          {model.typeName}
                        </span>
                        <h3 className="font-semibold text-gray-900 text-sm">{model.modelName}</h3>
                        <p className="text-xs text-gray-500 mt-1">{model.manufacturerName}</p>
                      </div>
                      <input type="checkbox" checked={selectedIds.includes(model.id)}
                        onChange={() => toggleSelect(model.id)}
                        className="h-4 w-4 text-indigo-600 border-gray-300 rounded"
                        title="Zaznacz do porównania"
                      />
                    </div>
                  </div>
                ))}
              </div>

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex justify-center gap-2 mt-6">
                  <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
                    className="px-3 py-1 border rounded text-sm disabled:opacity-50">Poprzednia</button>
                  <span className="px-3 py-1 text-sm text-gray-600">
                    Strona {page} z {totalPages}
                  </span>
                  <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}
                    className="px-3 py-1 border rounded text-sm disabled:opacity-50">Następna</button>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default CatalogPage;
