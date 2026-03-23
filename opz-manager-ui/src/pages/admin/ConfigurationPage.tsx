import React, { useState, useEffect } from 'react';
import {
  configAPI,
  ConfigStatus,
  LlmSettings,
  EmbeddingSettings,
} from '../../services/api';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import toast from 'react-hot-toast';

const LLM_PROVIDERS = [
  { value: 'local', label: 'Local / OpenAI-compatible' },
  { value: 'gemini', label: 'Google Gemini' },
  { value: 'anthropic', label: 'Anthropic Claude' },
];

const EMBEDDING_PROVIDERS = [
  { value: 'openai-compatible', label: 'OpenAI-compatible' },
  { value: 'mistral', label: 'Mistral' },
  { value: 'gemini', label: 'Google Gemini' },
];

const ConfigurationPage: React.FC = () => {
  const [status, setStatus] = useState<ConfigStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [testingLlm, setTestingLlm] = useState(false);
  const [testingEmbedding, setTestingEmbedding] = useState(false);
  const [savingLlm, setSavingLlm] = useState(false);
  const [savingEmbedding, setSavingEmbedding] = useState(false);

  // LLM form state
  const [llmProvider, setLlmProvider] = useState('local');
  const [llmBaseUrl, setLlmBaseUrl] = useState('');
  const [llmApiKey, setLlmApiKey] = useState('');
  const [llmModelName, setLlmModelName] = useState('');
  const [llmApiKeyPlaceholder, setLlmApiKeyPlaceholder] = useState('');

  // Embedding form state
  const [embProvider, setEmbProvider] = useState('openai-compatible');
  const [embBaseUrl, setEmbBaseUrl] = useState('');
  const [embApiKey, setEmbApiKey] = useState('');
  const [embModelName, setEmbModelName] = useState('');
  const [embDimensions, setEmbDimensions] = useState(1536);
  const [embApiKeyPlaceholder, setEmbApiKeyPlaceholder] = useState('');

  useEffect(() => {
    const fetchAll = async () => {
      try {
        const [statusData, llmSettings, embSettings] = await Promise.all([
          configAPI.getStatus(),
          configAPI.getLlmSettings(),
          configAPI.getEmbeddingSettings(),
        ]);
        setStatus(statusData);
        applyLlmSettings(llmSettings);
        applyEmbeddingSettings(embSettings);
      } catch {
        toast.error('Błąd podczas pobierania konfiguracji');
      } finally {
        setLoading(false);
      }
    };
    fetchAll();
  }, []);

  const applyLlmSettings = (s: LlmSettings) => {
    setLlmProvider(s.provider || 'local');
    setLlmBaseUrl(s.baseUrl || '');
    setLlmModelName(s.modelName || '');
    setLlmApiKey('');
    setLlmApiKeyPlaceholder(s.apiKey || '');
  };

  const applyEmbeddingSettings = (s: EmbeddingSettings) => {
    setEmbProvider(s.provider || 'openai-compatible');
    setEmbBaseUrl(s.baseUrl || '');
    setEmbModelName(s.modelName || '');
    setEmbDimensions(s.dimensions || 1536);
    setEmbApiKey('');
    setEmbApiKeyPlaceholder(s.apiKey || '');
  };

  const handleTestLlm = async () => {
    setTestingLlm(true);
    try {
      const result = await configAPI.testLlm();
      if (result.connected) {
        toast.success(result.message);
      } else {
        toast.error(result.message);
      }
      const data = await configAPI.getStatus();
      setStatus(data);
    } catch {
      toast.error('Nie udało się przetestować połączenia z LLM');
    } finally {
      setTestingLlm(false);
    }
  };

  const handleTestEmbedding = async () => {
    setTestingEmbedding(true);
    try {
      const result = await configAPI.testEmbedding();
      if (result.connected) {
        toast.success(result.message);
      } else {
        toast.error(result.message);
      }
      const data = await configAPI.getStatus();
      setStatus(data);
    } catch {
      toast.error('Nie udało się przetestować połączenia z modelem embeddingu');
    } finally {
      setTestingEmbedding(false);
    }
  };

  const handleSaveLlm = async () => {
    setSavingLlm(true);
    try {
      const result = await configAPI.updateLlmSettings({
        provider: llmProvider,
        baseUrl: llmBaseUrl || undefined,
        apiKey: llmApiKey || undefined,
        modelName: llmModelName || undefined,
      });
      toast.success(result.message);
      // Refresh settings to get masked API key
      const [newStatus, newSettings] = await Promise.all([
        configAPI.getStatus(),
        configAPI.getLlmSettings(),
      ]);
      setStatus(newStatus);
      applyLlmSettings(newSettings);
    } catch {
      toast.error('Nie udało się zapisać ustawień LLM');
    } finally {
      setSavingLlm(false);
    }
  };

  const handleSaveEmbedding = async () => {
    setSavingEmbedding(true);
    try {
      const result = await configAPI.updateEmbeddingSettings({
        provider: embProvider,
        baseUrl: embBaseUrl || undefined,
        apiKey: embApiKey || undefined,
        modelName: embModelName || undefined,
        dimensions: embDimensions || undefined,
      });
      toast.success(result.message);
      const [newStatus, newSettings] = await Promise.all([
        configAPI.getStatus(),
        configAPI.getEmbeddingSettings(),
      ]);
      setStatus(newStatus);
      applyEmbeddingSettings(newSettings);
    } catch {
      toast.error('Nie udało się zapisać ustawień embeddingu');
    } finally {
      setSavingEmbedding(false);
    }
  };

  if (loading) return <LoadingSpinner message="Ładowanie konfiguracji..." />;
  if (!status) return <p className="text-gray-500">Nie udało się załadować konfiguracji.</p>;

  const showLlmBaseUrl = llmProvider === 'local';
  const showEmbBaseUrl = embProvider !== 'gemini';

  const stats = [
    { label: 'Producenci', value: status.manufacturersCount },
    { label: 'Typy sprzętu', value: status.equipmentTypesCount },
    { label: 'Modele sprzętu', value: status.equipmentModelsCount },
    { label: 'Dokumenty OPZ', value: status.opzDocumentsCount },
    { label: 'Dane treningowe', value: status.trainingDataCount },
  ];

  const kbStats = [
    { label: 'Dokumenty KB', value: status.knowledgeDocumentsCount },
    { label: 'Fragmenty (chunki)', value: status.knowledgeChunksCount },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Konfiguracja systemu</h1>

      {/* LLM Settings */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">
            Model LLM
          </h2>
          <div className="flex items-center gap-2">
            <div className={`w-3 h-3 rounded-full ${status.llmConnected ? 'bg-green-500' : 'bg-red-500'}`} />
            <span className="text-sm text-gray-600">
              {status.llmConnected ? 'Połączono' : 'Brak połączenia'}
            </span>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Dostawca</label>
            <select
              value={llmProvider}
              onChange={e => setLlmProvider(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            >
              {LLM_PROVIDERS.map(p => (
                <option key={p.value} value={p.value}>{p.label}</option>
              ))}
            </select>
          </div>

          {showLlmBaseUrl && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Base URL</label>
              <input
                type="text"
                value={llmBaseUrl}
                onChange={e => setLlmBaseUrl(e.target.value)}
                placeholder="http://localhost:1234/v1/"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Klucz API</label>
            <input
              type="password"
              value={llmApiKey}
              onChange={e => setLlmApiKey(e.target.value)}
              placeholder={llmApiKeyPlaceholder || 'Wprowadź klucz API...'}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Nazwa modelu</label>
            <input
              type="text"
              value={llmModelName}
              onChange={e => setLlmModelName(e.target.value)}
              placeholder="np. gemini-2.0-flash, claude-sonnet-4-20250514"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={handleSaveLlm}
            disabled={savingLlm}
            className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50"
          >
            {savingLlm ? 'Zapisywanie...' : 'Zapisz'}
          </button>
          <button
            onClick={handleTestLlm}
            disabled={testingLlm}
            className="px-4 py-2 bg-gray-100 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-200 border border-gray-300 disabled:opacity-50"
          >
            {testingLlm ? 'Testowanie...' : 'Testuj połączenie'}
          </button>
          {status.llmProvider && (
            <span className="text-sm text-gray-500">
              Aktywny: {status.llmProvider} &middot; {status.llmModelName}
            </span>
          )}
        </div>
      </div>

      {/* Embedding Settings */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">
            Model embeddingu
          </h2>
          <div className="flex items-center gap-2">
            <div className={`w-3 h-3 rounded-full ${status.embeddingConnected ? 'bg-green-500' : 'bg-red-500'}`} />
            <span className="text-sm text-gray-600">
              {status.embeddingConnected ? 'Połączono' : 'Brak połączenia'}
            </span>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Dostawca</label>
            <select
              value={embProvider}
              onChange={e => setEmbProvider(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            >
              {EMBEDDING_PROVIDERS.map(p => (
                <option key={p.value} value={p.value}>{p.label}</option>
              ))}
            </select>
          </div>

          {showEmbBaseUrl && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Base URL</label>
              <input
                type="text"
                value={embBaseUrl}
                onChange={e => setEmbBaseUrl(e.target.value)}
                placeholder="http://localhost:1234/v1/"
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              />
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Klucz API</label>
            <input
              type="password"
              value={embApiKey}
              onChange={e => setEmbApiKey(e.target.value)}
              placeholder={embApiKeyPlaceholder || 'Wprowadź klucz API...'}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Nazwa modelu</label>
            <input
              type="text"
              value={embModelName}
              onChange={e => setEmbModelName(e.target.value)}
              placeholder="np. text-embedding-3-small"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Wymiary (dimensions)</label>
            <input
              type="number"
              value={embDimensions}
              onChange={e => setEmbDimensions(parseInt(e.target.value) || 0)}
              placeholder="1536"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={handleSaveEmbedding}
            disabled={savingEmbedding}
            className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50"
          >
            {savingEmbedding ? 'Zapisywanie...' : 'Zapisz'}
          </button>
          <button
            onClick={handleTestEmbedding}
            disabled={testingEmbedding}
            className="px-4 py-2 bg-gray-100 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-200 border border-gray-300 disabled:opacity-50"
          >
            {testingEmbedding ? 'Testowanie...' : 'Testuj połączenie'}
          </button>
          {status.embeddingProvider && (
            <span className="text-sm text-gray-500">
              Aktywny: {status.embeddingProvider} &middot; {status.embeddingModelName}
            </span>
          )}
        </div>
      </div>

      {/* System Stats */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Statystyki systemu</h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4">
          {stats.map((s) => (
            <div key={s.label} className="bg-gray-50 rounded-lg p-4 text-center">
              <p className="text-2xl font-bold text-gray-900">{s.value}</p>
              <p className="text-sm text-gray-500 mt-1">{s.label}</p>
            </div>
          ))}
        </div>
      </div>

      {/* Knowledge Base Stats */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Baza wiedzy</h2>
        <div className="grid grid-cols-2 gap-4">
          {kbStats.map((s) => (
            <div key={s.label} className="bg-gray-50 rounded-lg p-4 text-center">
              <p className="text-2xl font-bold text-gray-900">{s.value}</p>
              <p className="text-sm text-gray-500 mt-1">{s.label}</p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default ConfigurationPage;
