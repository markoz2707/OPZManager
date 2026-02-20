import React, { useState, useEffect } from 'react';
import { configAPI, ConfigStatus } from '../../services/api';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import toast from 'react-hot-toast';

const ConfigurationPage: React.FC = () => {
  const [status, setStatus] = useState<ConfigStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [testingLlm, setTestingLlm] = useState(false);
  const [testingEmbedding, setTestingEmbedding] = useState(false);

  useEffect(() => {
    const fetch = async () => {
      try {
        const data = await configAPI.getStatus();
        setStatus(data);
      } catch {
        toast.error('Błąd podczas pobierania statusu systemu');
      } finally {
        setLoading(false);
      }
    };
    fetch();
  }, []);

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

  if (loading) return <LoadingSpinner message="Ładowanie konfiguracji..." />;
  if (!status) return <p className="text-gray-500">Nie udało się załadować konfiguracji.</p>;

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

      {/* LLM Status */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">
          Model LLM ({status.llmProvider || 'Pllum'})
        </h2>
        <div className="flex items-center justify-between">
          <div>
            <div className="flex items-center gap-2 mb-2">
              <div className={`w-3 h-3 rounded-full ${status.llmConnected ? 'bg-green-500' : 'bg-red-500'}`} />
              <span className="text-sm font-medium text-gray-700">
                {status.llmConnected ? 'Połączono' : 'Brak połączenia'}
              </span>
            </div>
            <p className="text-sm text-gray-500">Model: {status.llmModelName}</p>
            <p className="text-sm text-gray-500">URL: {status.llmBaseUrl}</p>
          </div>
          <button
            onClick={handleTestLlm}
            disabled={testingLlm}
            className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50"
          >
            {testingLlm ? 'Testowanie...' : 'Testuj połączenie'}
          </button>
        </div>
      </div>

      {/* Embedding Status */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">
          Model embeddingu ({status.embeddingProvider || 'OpenAI-Compatible'})
        </h2>
        <div className="flex items-center justify-between">
          <div>
            <div className="flex items-center gap-2 mb-2">
              <div className={`w-3 h-3 rounded-full ${status.embeddingConnected ? 'bg-green-500' : 'bg-red-500'}`} />
              <span className="text-sm font-medium text-gray-700">
                {status.embeddingConnected ? 'Połączono' : 'Brak połączenia'}
              </span>
            </div>
            <p className="text-sm text-gray-500">Model: {status.embeddingModelName}</p>
          </div>
          <button
            onClick={handleTestEmbedding}
            disabled={testingEmbedding}
            className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50"
          >
            {testingEmbedding ? 'Testowanie...' : 'Testuj połączenie'}
          </button>
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
