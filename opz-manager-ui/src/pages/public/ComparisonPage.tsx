import React, { useState, useEffect } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import publicApi from '../../services/publicApi';

interface ComparedModel {
  id: number;
  modelName: string;
  manufacturerName: string;
  typeName: string;
  specifications: string;
}

const ComparisonPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const [models, setModels] = useState<ComparedModel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const ids = searchParams.get('ids')?.split(',').map(Number).filter(Boolean) || [];
    if (ids.length < 2) {
      setError('Wybierz co najmniej 2 modele do porównania.');
      setLoading(false);
      return;
    }

    const fetchData = async () => {
      try {
        const response = await publicApi.post('/public/equipment/models/compare', { modelIds: ids });
        setModels(response.data);
      } catch {
        setError('Nie udało się pobrać danych do porównania.');
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [searchParams]);

  const parseSpecs = (json: string | null): Record<string, string> => {
    if (!json) return {};
    try { return JSON.parse(json); } catch { return {}; }
  };

  const allKeys = Array.from(new Set(models.flatMap(m => Object.keys(parseSpecs(m.specifications)))));

  if (loading) return <div className="max-w-7xl mx-auto px-4 py-12 text-center text-gray-500">Ładowanie...</div>;
  if (error) return (
    <div className="max-w-7xl mx-auto px-4 py-12 text-center">
      <p className="text-red-600">{error}</p>
      <Link to="/catalog" className="text-indigo-600 hover:underline mt-4 inline-block">Wróć do katalogu</Link>
    </div>
  );

  return (
    <div className="max-w-7xl mx-auto px-4 py-8">
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Porównanie modeli</h1>
          <p className="text-gray-600 text-sm mt-1">Porównujesz {models.length} modeli</p>
        </div>
        <Link to="/catalog" className="text-sm text-indigo-600 hover:underline">Wróć do katalogu</Link>
      </div>

      <div className="overflow-x-auto">
        <table className="min-w-full bg-white border rounded-lg shadow-sm">
          <thead>
            <tr className="bg-gray-50">
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase w-48">Parametr</th>
              {models.map(m => (
                <th key={m.id} className="px-4 py-3 text-left text-xs font-medium text-gray-900">
                  <div className="font-bold">{m.modelName}</div>
                  <div className="text-gray-500 font-normal">{m.manufacturerName}</div>
                  <span className="inline-block mt-1 px-2 py-0.5 bg-blue-100 text-blue-800 text-xs rounded-full">
                    {m.typeName}
                  </span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {allKeys.length > 0 ? allKeys.map(key => (
              <tr key={key} className="hover:bg-gray-50">
                <td className="px-4 py-2 text-sm font-medium text-gray-700">{key}</td>
                {models.map(m => {
                  const specs = parseSpecs(m.specifications);
                  return (
                    <td key={m.id} className="px-4 py-2 text-sm text-gray-600">
                      {specs[key] || <span className="text-gray-300">-</span>}
                    </td>
                  );
                })}
              </tr>
            )) : (
              <tr>
                <td colSpan={models.length + 1} className="px-4 py-8 text-center text-gray-500">
                  Brak danych specyfikacji dla wybranych modeli.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="mt-6 text-center">
        <Link to={`/generate?models=${models.map(m => m.id).join(',')}`}
          className="inline-flex items-center px-6 py-3 bg-indigo-600 text-white rounded-lg font-medium hover:bg-indigo-700">
          Użyj w kreatorze SWZ
        </Link>
      </div>
    </div>
  );
};

export default ComparisonPage;
