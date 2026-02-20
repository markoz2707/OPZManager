import React, { useState, useEffect, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { equipmentAPI, EquipmentModel } from '../../services/api';
import { useKnowledgeBase } from '../../hooks/useKnowledgeBase';
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
  const [model, setModel] = useState<EquipmentModel | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);

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
  useEffect(() => {
    const hasProcessing = documents.some(d => d.status === 'Oczekuje' || d.status === 'Przetwarzanie');
    if (!hasProcessing) return;
    const interval = setInterval(refreshDocuments, 3000);
    return () => clearInterval(interval);
  }, [documents, refreshDocuments]);

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
        <h1 className="text-2xl font-bold text-gray-900 mb-2">{model.modelName}</h1>
        <div className="flex gap-4 text-sm text-gray-500 mb-6">
          <span>Producent: <span className="font-medium text-gray-700">{model.manufacturerName}</span></span>
          <span>Typ: <span className="font-medium text-gray-700">{model.typeName}</span></span>
        </div>

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
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {doc.originalFilename}
                  </p>
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
