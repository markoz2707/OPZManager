import React, { useState, useEffect, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { equipmentAPI, EquipmentModel, Manufacturer, EquipmentType } from '../../services/api';
import { useKnowledgeBase } from '../../hooks/useKnowledgeBase';
import { useAuth } from '../../hooks/useAuth';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import toast from 'react-hot-toast';

const statusColors: Record<string, string> = {
  'Oczekuje': 'bg-yellow-100 text-yellow-800',
  'Przetwarzanie': 'bg-blue-100 text-blue-800',
  'Zindeksowany': 'bg-green-100 text-green-800',
  'Błąd': 'bg-red-100 text-red-800',
};

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

const EquipmentModelDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const modelId = Number(id);
  const { user } = useAuth();
  const [model, setModel] = useState<EquipmentModel | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Edit state
  const [editing, setEditing] = useState(false);
  const [editName, setEditName] = useState('');
  const [editMfr, setEditMfr] = useState(0);
  const [editType, setEditType] = useState(0);
  const [manufacturers, setManufacturers] = useState<Manufacturer[]>([]);
  const [types, setTypes] = useState<EquipmentType[]>([]);
  const [saving, setSaving] = useState(false);

  const isAdmin = user?.role === 'Admin';

  const {
    documents,
    loading: kbLoading,
    uploading,
    searchResults,
    searching,
    uploadDocument,
    deleteDocument,
    reprocessDocument,
    search,
    refreshDocuments,
  } = useKnowledgeBase(modelId);

  useEffect(() => {
    const fetch = async () => {
      try {
        const data = await equipmentAPI.getModel(modelId);
        setModel(data);
      } catch {
        toast.error('Błąd podczas pobierania modelu');
      } finally {
        setLoading(false);
      }
    };
    fetch();
  }, [modelId]);

  // Auto-refresh documents that are processing
  const hasProcessing = documents.some(d => d.status === 'Oczekuje' || d.status === 'Przetwarzanie');
  useEffect(() => {
    if (!hasProcessing) return;
    const interval = setInterval(() => {
      refreshDocuments();
    }, 3000);
    return () => clearInterval(interval);
  }, [hasProcessing]); // eslint-disable-line react-hooks/exhaustive-deps

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      await uploadDocument(file);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      search(searchQuery.trim());
    }
  };

  const startEdit = async () => {
    if (!model) return;
    setEditName(model.modelName);
    setEditMfr(model.manufacturerId);
    setEditType(model.typeId);
    try {
      const [mfrs, tps] = await Promise.all([
        equipmentAPI.getManufacturers(),
        equipmentAPI.getTypes(),
      ]);
      setManufacturers(mfrs);
      setTypes(tps);
    } catch {
      toast.error('Błąd podczas pobierania danych');
      return;
    }
    setEditing(true);
  };

  const handleSaveEdit = async () => {
    if (!model) return;
    setSaving(true);
    try {
      const updated = await equipmentAPI.updateModel(model.id, {
        manufacturerId: editMfr,
        typeId: editType,
        modelName: editName,
      });
      setModel(updated);
      setEditing(false);
      toast.success('Model został zaktualizowany');
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Błąd podczas aktualizacji modelu');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingSpinner message="Ładowanie modelu..." />;
  if (!model) return <p className="text-gray-500">Model nie został znaleziony.</p>;

  let specs: Record<string, any> = {};
  try {
    specs = JSON.parse(model.specificationsJson || '{}');
  } catch {
    specs = {};
  }

  return (
    <div>
      <Link to="/admin/equipment" className="text-sm text-indigo-600 hover:text-indigo-800 mb-4 inline-block">
        &larr; Powrót do katalogu
      </Link>

      <div className="bg-white rounded-lg border border-gray-200 p-6">
        {editing ? (
          <div className="space-y-4 mb-6">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Nazwa modelu</label>
              <input
                value={editName}
                onChange={(e) => setEditName(e.target.value)}
                className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Producent</label>
                <select
                  value={editMfr}
                  onChange={(e) => setEditMfr(Number(e.target.value))}
                  className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  {manufacturers.map((m) => (
                    <option key={m.id} value={m.id}>{m.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Typ sprzętu</label>
                <select
                  value={editType}
                  onChange={(e) => setEditType(Number(e.target.value))}
                  className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                >
                  {types.map((t) => (
                    <option key={t.id} value={t.id}>{t.name}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="flex gap-2">
              <button
                onClick={handleSaveEdit}
                disabled={saving || !editName.trim()}
                className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50"
              >
                {saving ? 'Zapisywanie...' : 'Zapisz'}
              </button>
              <button
                onClick={() => setEditing(false)}
                className="px-4 py-2 text-sm text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                Anuluj
              </button>
            </div>
          </div>
        ) : (
          <>
            <div className="flex items-start justify-between mb-2">
              <h1 className="text-2xl font-bold text-gray-900">{model.modelName}</h1>
              {isAdmin && (
                <button
                  onClick={startEdit}
                  className="px-3 py-1.5 text-sm text-indigo-600 border border-indigo-300 rounded-lg hover:bg-indigo-50"
                >
                  Edytuj
                </button>
              )}
            </div>
            <div className="flex gap-4 text-sm text-gray-500 mb-6">
              <span>Producent: <span className="font-medium text-gray-700">{model.manufacturerName}</span></span>
              <span>Typ: <span className="font-medium text-gray-700">{model.typeName}</span></span>
            </div>
          </>
        )}

        <h2 className="text-lg font-semibold text-gray-900 mb-3">Specyfikacja techniczna</h2>
        {Object.keys(specs).length === 0 ? (
          <p className="text-gray-500">Brak specyfikacji</p>
        ) : (
          <div className="bg-gray-50 rounded-lg p-4">
            <dl className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-3">
              {Object.entries(specs).map(([key, value]) => (
                <div key={key}>
                  <dt className="text-sm font-medium text-gray-500">{key}</dt>
                  <dd className="text-sm text-gray-900">{String(value)}</dd>
                </div>
              ))}
            </dl>
          </div>
        )}

        <div className="mt-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-3">Surowe dane JSON</h2>
          <pre className="bg-gray-50 rounded-lg p-4 text-sm text-gray-700 overflow-x-auto">
            {JSON.stringify(specs, null, 2)}
          </pre>
        </div>
      </div>

      {/* Knowledge Base Section */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 mt-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Baza wiedzy</h2>
          <div>
            <input
              ref={fileInputRef}
              type="file"
              accept=".pdf"
              onChange={handleFileUpload}
              className="hidden"
              id="kb-upload"
            />
            <label
              htmlFor="kb-upload"
              className={`px-4 py-2 text-sm font-medium rounded-lg cursor-pointer ${
                uploading
                  ? 'bg-gray-300 text-gray-500 cursor-not-allowed'
                  : 'bg-indigo-600 text-white hover:bg-indigo-700'
              }`}
            >
              {uploading ? 'Przesyłanie...' : 'Dodaj PDF'}
            </label>
          </div>
        </div>

        {kbLoading ? (
          <p className="text-sm text-gray-500">Ładowanie dokumentów...</p>
        ) : documents.length === 0 ? (
          <p className="text-sm text-gray-500">
            Brak dokumentów w bazie wiedzy. Dodaj pliki PDF z kartami katalogowymi lub dokumentacją techniczną.
          </p>
        ) : (
          <div className="space-y-3">
            {documents.map((doc) => (
              <div
                key={doc.id}
                className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
              >
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    {(doc.status === 'Oczekuje' || doc.status === 'Przetwarzanie') && (
                      <svg className="animate-spin h-4 w-4 text-indigo-600 flex-shrink-0" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                      </svg>
                    )}
                    <p className="text-sm font-medium text-gray-900 truncate">
                      {doc.originalFilename}
                    </p>
                  </div>
                  <div className="flex items-center gap-3 mt-1">
                    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${statusColors[doc.status] || 'bg-gray-100 text-gray-800'}`}>
                      {doc.status}
                    </span>
                    <span className="text-xs text-gray-500">
                      {formatFileSize(doc.fileSizeBytes)}
                    </span>
                    {doc.chunkCount > 0 && (
                      <span className="text-xs text-gray-500">
                        {doc.chunkCount} fragmentów
                      </span>
                    )}
                    <span className="text-xs text-gray-500">
                      {new Date(doc.uploadedAt).toLocaleDateString('pl-PL')}
                    </span>
                  </div>
                  {(doc.status === 'Oczekuje' || doc.status === 'Przetwarzanie') && (
                    <div className="mt-2">
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-xs text-gray-600">
                          {doc.processingStep || 'Oczekiwanie na przetwarzanie...'}
                        </span>
                        <span className="text-xs font-medium text-indigo-600">
                          {doc.processingProgress}%
                        </span>
                      </div>
                      <div className="w-full bg-gray-200 rounded-full h-2">
                        <div
                          className="bg-indigo-500 h-2 rounded-full transition-all duration-500 ease-out"
                          style={{ width: `${Math.max(doc.processingProgress, 2)}%` }}
                        />
                      </div>
                    </div>
                  )}
                  {doc.errorMessage && (
                    <p className="text-xs text-red-600 mt-1 truncate" title={doc.errorMessage}>
                      {doc.errorMessage}
                    </p>
                  )}
                </div>
                <div className="flex items-center gap-2 ml-4">
                  {(doc.status === 'Błąd' || doc.status === 'Zindeksowany') && (
                    <button
                      onClick={() => reprocessDocument(doc.id)}
                      className="text-xs text-indigo-600 hover:text-indigo-800 font-medium"
                    >
                      Przetwórz ponownie
                    </button>
                  )}
                  <button
                    onClick={() => {
                      if (window.confirm('Czy na pewno chcesz usunąć ten dokument?')) {
                        deleteDocument(doc.id);
                      }
                    }}
                    className="text-xs text-red-600 hover:text-red-800 font-medium"
                  >
                    Usuń
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Search Section */}
        {documents.some(d => d.status === 'Zindeksowany') && (
          <div className="mt-6 pt-6 border-t border-gray-200">
            <h3 className="text-sm font-semibold text-gray-900 mb-3">Wyszukiwanie w bazie wiedzy</h3>
            <form onSubmit={handleSearch} className="flex gap-2">
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Wpisz zapytanie..."
                className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              />
              <button
                type="submit"
                disabled={searching || !searchQuery.trim()}
                className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50"
              >
                {searching ? 'Szukam...' : 'Szukaj'}
              </button>
            </form>

            {searchResults.length > 0 && (
              <div className="mt-4 space-y-3">
                {searchResults.map((result) => (
                  <div key={result.chunkId} className="p-3 bg-gray-50 rounded-lg">
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-xs font-medium text-gray-700">
                        {result.documentFilename} — fragment #{result.chunkIndex + 1}
                      </span>
                      <span className="text-xs text-gray-500">
                        Trafność: {(result.score * 100).toFixed(1)}%
                      </span>
                    </div>
                    <p className="text-sm text-gray-600 whitespace-pre-wrap">{result.content}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default EquipmentModelDetailPage;
