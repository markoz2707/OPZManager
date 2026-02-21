import React, { useState, useMemo, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import StepIndicator from '../../components/public/StepIndicator';
import EmailGateModal from '../../components/public/EmailGateModal';
import { usePublicGenerator } from '../../hooks/usePublicGenerator';
import { useLeadCapture } from '../../hooks/useLeadCapture';
import { useAuth } from '../../hooks/useAuth';
import { publicGeneratorAuthAPI } from '../../services/api';
import LoadingSpinner from '../../components/common/LoadingSpinner';

const steps = [
  { label: 'Typ sprzętu' },
  { label: 'Modele' },
  { label: 'Sekcje' },
  { label: 'Podgląd' },
  { label: 'Pobieranie' },
];

const STORAGE_KEY = 'opz_generator_state';

const GenerateOPZPage: React.FC = () => {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [currentStep, setCurrentStep] = useState(0);
  const [selectedTypeId, setSelectedTypeId] = useState<number | null>(null);
  const [selectedModelIds, setSelectedModelIds] = useState<number[]>([]);
  const [showEmailModal, setShowEmailModal] = useState(false);

  const { types, models, content, setContent, isFullContent, generating, loadingData, generateContent, fetchFullContent } = usePublicGenerator();
  const { downloadToken, submitting, captureLead, downloadPdf } = useLeadCapture();

  // Restore state after login redirect
  useEffect(() => {
    if (isAuthenticated && !loadingData) {
      const saved = sessionStorage.getItem(STORAGE_KEY);
      if (saved) {
        try {
          const state = JSON.parse(saved);
          sessionStorage.removeItem(STORAGE_KEY);
          if (state.selectedTypeId && state.selectedModelIds?.length > 0) {
            setSelectedTypeId(state.selectedTypeId);
            setSelectedModelIds(state.selectedModelIds);
            // Auto-fetch full content
            const typeName = types.find(t => t.id === state.selectedTypeId)?.name;
            if (typeName) {
              setCurrentStep(2);
              fetchFullContent(state.selectedModelIds, typeName).then(result => {
                if (result) setCurrentStep(3);
              });
            }
          }
        } catch {
          sessionStorage.removeItem(STORAGE_KEY);
        }
      }
    }
  }, [isAuthenticated, loadingData, types]); // eslint-disable-line react-hooks/exhaustive-deps

  const selectedType = useMemo(
    () => types.find(t => t.id === selectedTypeId),
    [types, selectedTypeId]
  );

  const filteredModels = useMemo(
    () => models.filter(m => m.typeId === selectedTypeId),
    [models, selectedTypeId]
  );

  const handleTypeSelect = (typeId: number) => {
    setSelectedTypeId(typeId);
    setSelectedModelIds([]);
    setCurrentStep(1);
  };

  const toggleModel = (modelId: number) => {
    setSelectedModelIds(prev =>
      prev.includes(modelId) ? prev.filter(id => id !== modelId) : [...prev, modelId]
    );
  };

  const handleGenerate = async () => {
    if (!selectedType || selectedModelIds.length === 0) return;
    setCurrentStep(2);
    const result = await generateContent(selectedModelIds, selectedType.name);
    if (result) {
      setCurrentStep(3);
    }
  };

  const handleLoginRedirect = () => {
    // Save state before redirect
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({
      selectedTypeId,
      selectedModelIds,
    }));
    navigate('/login?returnTo=/generate');
  };

  const handleEmailSubmit = async (email: string, marketingConsent: boolean) => {
    const result = await captureLead({
      email,
      marketingConsent,
      source: 'generation',
    });
    if (result) {
      setShowEmailModal(false);
      setCurrentStep(4);
    }
  };

  const handleDownload = async () => {
    if (isAuthenticated && selectedType) {
      // Authenticated: download directly via authorized endpoint
      try {
        const blob = await publicGeneratorAuthAPI.downloadPdf(selectedModelIds, selectedType.name);
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `OPZ_${selectedType.name}_${new Date().toISOString().slice(0, 10)}.pdf`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      } catch {
        // Fallback to email gate
        const title = `OPZ - ${selectedType?.name || 'Dokument'}`;
        await downloadPdf(content, title);
      }
    } else {
      const title = `OPZ - ${selectedType?.name || 'Dokument'}`;
      await downloadPdf(content, title);
    }
  };

  if (loadingData) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20">
        <LoadingSpinner size="lg" message="Ładowanie danych sprzętu..." />
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <h1 className="text-3xl font-bold text-gray-900 mb-2">Generuj dokument OPZ</h1>
      <p className="text-gray-500 mb-8">Wybierz sprzęt i wygeneruj profesjonalny dokument OPZ</p>

      <StepIndicator steps={steps} currentStep={currentStep} />

      {/* Step 0: Select Type */}
      {currentStep === 0 && (
        <div>
          <h2 className="text-lg font-semibold text-gray-800 mb-4">Wybierz typ sprzętu</h2>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            {types.map(type => (
              <button
                key={type.id}
                onClick={() => handleTypeSelect(type.id)}
                className={`p-6 border-2 rounded-xl text-left transition-all hover:shadow-md ${
                  selectedTypeId === type.id
                    ? 'border-blue-500 bg-blue-50'
                    : 'border-gray-200 hover:border-blue-300'
                }`}
              >
                <h3 className="font-bold text-gray-900 mb-1">{type.name}</h3>
                <p className="text-sm text-gray-500">{type.description}</p>
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Step 1: Select Models */}
      {currentStep === 1 && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-lg font-semibold text-gray-800">Wybierz modele sprzętu</h2>
              <p className="text-sm text-gray-500">Typ: {selectedType?.name} | Wybrano: {selectedModelIds.length}</p>
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => setCurrentStep(0)}
                className="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50"
              >
                Wstecz
              </button>
              <button
                onClick={handleGenerate}
                disabled={selectedModelIds.length === 0}
                className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                Generuj OPZ
              </button>
            </div>
          </div>

          {filteredModels.length === 0 ? (
            <div className="bg-yellow-50 border border-yellow-200 rounded-xl p-6 text-center">
              <p className="text-yellow-700">Brak modeli sprzętu dla wybranego typu. Dodaj modele w panelu administracyjnym.</p>
            </div>
          ) : (
            <div className="space-y-3">
              {filteredModels.map(model => (
                <label
                  key={model.id}
                  className={`flex items-center p-4 border-2 rounded-xl cursor-pointer transition-all ${
                    selectedModelIds.includes(model.id)
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:border-blue-300'
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={selectedModelIds.includes(model.id)}
                    onChange={() => toggleModel(model.id)}
                    className="w-5 h-5 text-blue-600 border-gray-300 rounded focus:ring-blue-500 mr-4"
                  />
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold text-gray-900">{model.manufacturerName}</span>
                      <span className="text-gray-700">{model.modelName}</span>
                    </div>
                    {model.specificationsJson && model.specificationsJson !== '{}' && (
                      <p className="text-xs text-gray-400 mt-1 truncate max-w-lg">
                        {model.specificationsJson}
                      </p>
                    )}
                  </div>
                </label>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Step 2: Generating */}
      {currentStep === 2 && generating && (
        <div className="bg-white border rounded-2xl p-12 text-center">
          <div className="animate-spin w-16 h-16 border-4 border-blue-200 border-t-blue-600 rounded-full mx-auto mb-6" />
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Generowanie treści OPZ...</h2>
          <p className="text-gray-500">
            System tworzy profesjonalny dokument na podstawie wybranych modeli sprzętu
          </p>
        </div>
      )}

      {/* Step 3: Preview */}
      {currentStep === 3 && content && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-800">
              {isFullContent ? 'Podgląd treści OPZ' : 'Podgląd treści OPZ (fragment)'}
            </h2>
            <div className="flex gap-2">
              <button
                onClick={() => setCurrentStep(1)}
                className="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50"
              >
                Zmień modele
              </button>
              {isFullContent && (
                <button
                  onClick={async () => {
                    if (!selectedType) return;
                    await generateContent(selectedModelIds, selectedType.name);
                  }}
                  disabled={generating}
                  className="px-4 py-2 border border-blue-300 text-blue-700 rounded-lg hover:bg-blue-50 disabled:opacity-50"
                >
                  Regeneruj
                </button>
              )}
              {isFullContent ? (
                <button
                  onClick={() => {
                    if (downloadToken) {
                      setCurrentStep(4);
                    } else if (isAuthenticated) {
                      setCurrentStep(4);
                    } else {
                      setShowEmailModal(true);
                    }
                  }}
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Pobierz PDF
                </button>
              ) : (
                <button
                  onClick={handleLoginRedirect}
                  className="px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
                >
                  Zaloguj się, aby zobaczyć pełny dokument
                </button>
              )}
            </div>
          </div>

          {isFullContent ? (
            <textarea
              value={content}
              onChange={(e) => setContent(e.target.value)}
              className="w-full h-96 p-4 border border-gray-300 rounded-xl font-mono text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none resize-y"
            />
          ) : (
            <>
              <div
                className="w-full h-96 p-4 border border-gray-300 rounded-xl font-mono text-sm bg-gray-50 overflow-y-auto whitespace-pre-wrap text-gray-700"
              >
                {content}
              </div>
              <div className="mt-4 p-4 bg-indigo-50 border border-indigo-200 rounded-xl text-center">
                <p className="text-indigo-700 font-medium mb-2">
                  Zaloguj się, aby zobaczyć i edytować pełny dokument OPZ
                </p>
                <button
                  onClick={handleLoginRedirect}
                  className="px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
                >
                  Zaloguj się
                </button>
              </div>
            </>
          )}
        </div>
      )}

      {/* Step 4: Download */}
      {currentStep === 4 && (
        <div className="bg-white border rounded-2xl p-12 text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Dokument OPZ jest gotowy</h2>
          <p className="text-gray-500 mb-6">Kliknij przycisk poniżej, aby pobrać wygenerowany dokument w formacie PDF</p>
          <button
            onClick={handleDownload}
            className="px-8 py-3 bg-blue-600 text-white rounded-xl hover:bg-blue-700 transition-colors font-medium text-lg"
          >
            Pobierz PDF
          </button>
          <div className="mt-8 pt-6 border-t">
            <button
              onClick={() => {
                setCurrentStep(0);
                setSelectedTypeId(null);
                setSelectedModelIds([]);
                setContent('');
              }}
              className="text-blue-600 hover:text-blue-800 font-medium"
            >
              Generuj kolejny dokument
            </button>
          </div>
        </div>
      )}

      {/* Email Gate Modal */}
      <EmailGateModal
        isOpen={showEmailModal}
        onClose={() => setShowEmailModal(false)}
        onSubmit={handleEmailSubmit}
        submitting={submitting}
      />
    </div>
  );
};

export default GenerateOPZPage;
