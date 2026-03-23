import React, { useState, useMemo, useEffect, useCallback } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import StepIndicator from '../../components/public/StepIndicator';
import { useAuth } from '../../hooks/useAuth';
import publicApi from '../../services/publicApi';
import api from '../../services/api';
import LoadingSpinner from '../../components/common/LoadingSpinner';

interface EquipmentType { id: number; name: string; description: string; }
interface EquipmentModel { id: number; modelName: string; manufacturerName: string; typeId: number; typeName: string; specificationsJson: string; }
interface SWZRequirement { category: string; parameterName: string; minValue: string; unit: string; description: string; isCommon: boolean; }

const steps = [
  { label: 'Kategoria' },
  { label: 'Wybierz produkty' },
  { label: 'Parametry' },
  { label: 'Podgląd SWZ' },
  { label: 'Pobieranie' },
];

const STORAGE_KEY = 'swz_creator_state';

const SWZCreatorPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { isAuthenticated } = useAuth();

  const [currentStep, setCurrentStep] = useState(0);
  const [types, setTypes] = useState<EquipmentType[]>([]);
  const [models, setModels] = useState<EquipmentModel[]>([]);
  const [selectedTypeId, setSelectedTypeId] = useState<number | null>(null);
  const [selectedModelIds, setSelectedModelIds] = useState<number[]>([]);
  const [requirements, setRequirements] = useState<SWZRequirement[]>([]);
  const [content, setContent] = useState('');
  const [isFullContent, setIsFullContent] = useState(false);
  const [totalRequirements, setTotalRequirements] = useState(0);
  const [visibleRequirements, setVisibleRequirements] = useState(0);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [downloading, setDownloading] = useState(false);

  // Load equipment data
  useEffect(() => {
    const fetchData = async () => {
      try {
        const [typesRes, modelsRes] = await Promise.all([
          publicApi.get('/public/equipment/types'),
          publicApi.get('/public/equipment/models'),
        ]);
        setTypes(typesRes.data);
        setModels(modelsRes.data);

        // Check if models were pre-selected from catalog
        const preselected = searchParams.get('models');
        if (preselected) {
          const ids = preselected.split(',').map(Number).filter(Boolean);
          if (ids.length > 0) {
            setSelectedModelIds(ids);
            // Auto-detect type from first model
            const firstModel = modelsRes.data.find((m: EquipmentModel) => ids.includes(m.id));
            if (firstModel) {
              setSelectedTypeId(firstModel.typeId);
              setCurrentStep(1);
            }
          }
        }
      } catch { /* ignore */ }
      setLoading(false);
    };
    fetchData();
  }, [searchParams]);

  // Restore state after login redirect
  useEffect(() => {
    if (isAuthenticated && !loading) {
      const saved = sessionStorage.getItem(STORAGE_KEY);
      if (saved) {
        try {
          const state = JSON.parse(saved);
          sessionStorage.removeItem(STORAGE_KEY);
          if (state.selectedTypeId && state.selectedModelIds?.length > 0) {
            setSelectedTypeId(state.selectedTypeId);
            setSelectedModelIds(state.selectedModelIds);
            setCurrentStep(2);
          }
        } catch {
          sessionStorage.removeItem(STORAGE_KEY);
        }
      }
    }
  }, [isAuthenticated, loading]);

  const selectedType = useMemo(() => types.find(t => t.id === selectedTypeId), [types, selectedTypeId]);
  const filteredModels = useMemo(() => models.filter(m => m.typeId === selectedTypeId), [models, selectedTypeId]);

  const handleGenerate = useCallback(async () => {
    if (!selectedType || selectedModelIds.length === 0) return;
    setGenerating(true);
    setCurrentStep(2);

    try {
      // Use authenticated API if logged in, public otherwise
      const client = isAuthenticated ? api : publicApi;
      const response = await client.post('/swz/generate', {
        equipmentModelIds: selectedModelIds,
        equipmentType: selectedType.name,
      });

      setContent(response.data.content);
      setIsFullContent(response.data.isFullContent);
      setRequirements(response.data.requirements || []);
      setTotalRequirements(response.data.totalRequirements);
      setVisibleRequirements(response.data.visibleRequirements);
      setCurrentStep(3);
    } catch (err) {
      console.error('SWZ generation failed', err);
    } finally {
      setGenerating(false);
    }
  }, [selectedType, selectedModelIds, isAuthenticated]);

  const handleRegisterRedirect = () => {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ selectedTypeId, selectedModelIds }));
    navigate('/register?returnTo=/swz');
  };

  const handleDownload = async (format: 'pdf' | 'docx') => {
    if (!selectedType) return;
    setDownloading(true);
    try {
      const response = await api.post(`/swz/download/${format}`, {
        equipmentModelIds: selectedModelIds,
        equipmentType: selectedType.name,
      }, { responseType: 'blob' });

      const ext = format;
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `SWZ_${selectedType.name}_${new Date().toISOString().slice(0, 10)}.${ext}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error('Download failed', err);
    } finally {
      setDownloading(false);
    }
  };

  if (loading) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20">
        <LoadingSpinner size="lg" message="Ładowanie katalogu sprzętu..." />
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <h1 className="text-3xl font-bold text-gray-900 mb-2">Kreator SWZ</h1>
      <p className="text-gray-500 mb-8">
        Wybierz produkty, a system wygeneruje Specyfikację Warunków Zamówienia dopasowaną tylko do nich
      </p>

      <StepIndicator steps={steps} currentStep={currentStep} />

      {/* Step 0: Select Category */}
      {currentStep === 0 && (
        <div>
          <h2 className="text-lg font-semibold text-gray-800 mb-4">Wybierz kategorię sprzętu</h2>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            {types.map(type => (
              <button key={type.id}
                onClick={() => { setSelectedTypeId(type.id); setSelectedModelIds([]); setCurrentStep(1); }}
                className={`p-6 border-2 rounded-xl text-left transition-all hover:shadow-md ${
                  selectedTypeId === type.id ? 'border-blue-500 bg-blue-50' : 'border-gray-200 hover:border-blue-300'
                }`}>
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
              <h2 className="text-lg font-semibold text-gray-800">Wybierz produkty do SWZ</h2>
              <p className="text-sm text-gray-500">
                Kategoria: {selectedType?.name} | Wybrano: {selectedModelIds.length} (min. 2, max. 5)
              </p>
            </div>
            <div className="flex gap-2">
              <button onClick={() => setCurrentStep(0)}
                className="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50">Wstecz</button>
              <button onClick={handleGenerate}
                disabled={selectedModelIds.length < 2}
                className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed">
                Generuj SWZ
              </button>
            </div>
          </div>

          {filteredModels.length === 0 ? (
            <div className="bg-yellow-50 border border-yellow-200 rounded-xl p-6 text-center">
              <p className="text-yellow-700">Brak modeli sprzętu w tej kategorii.</p>
            </div>
          ) : (
            <div className="space-y-3">
              {filteredModels.map(model => {
                const isSelected = selectedModelIds.includes(model.id);
                const isDisabled = !isSelected && selectedModelIds.length >= 5;
                return (
                  <label key={model.id}
                    className={`flex items-center p-4 border-2 rounded-xl transition-all ${
                      isSelected ? 'border-blue-500 bg-blue-50' : isDisabled ? 'border-gray-100 opacity-50' : 'border-gray-200 hover:border-blue-300 cursor-pointer'
                    }`}>
                    <input type="checkbox" checked={isSelected} disabled={isDisabled}
                      onChange={() => setSelectedModelIds(prev =>
                        prev.includes(model.id) ? prev.filter(id => id !== model.id) : [...prev, model.id]
                      )}
                      className="w-5 h-5 text-blue-600 border-gray-300 rounded focus:ring-blue-500 mr-4"
                    />
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <span className="font-semibold text-gray-900">{model.manufacturerName}</span>
                        <span className="text-gray-700">{model.modelName}</span>
                      </div>
                      {model.specificationsJson && model.specificationsJson !== '{}' && (
                        <p className="text-xs text-gray-400 mt-1 truncate max-w-lg">{model.specificationsJson}</p>
                      )}
                    </div>
                  </label>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* Step 2: Generating */}
      {currentStep === 2 && generating && (
        <div className="bg-white border rounded-2xl p-12 text-center">
          <div className="animate-spin w-16 h-16 border-4 border-blue-200 border-t-blue-600 rounded-full mx-auto mb-6" />
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Generowanie SWZ...</h2>
          <p className="text-gray-500">System analizuje parametry wybranych produktów i tworzy specyfikację</p>
        </div>
      )}

      {/* Step 3: Preview */}
      {currentStep === 3 && content && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-lg font-semibold text-gray-800">
                {isFullContent ? 'Podgląd SWZ' : `Podgląd SWZ (${visibleRequirements} z ${totalRequirements} wymagań)`}
              </h2>
              {requirements.length > 0 && (
                <p className="text-sm text-gray-500">{requirements.length} wymagań technicznych wyodrębnionych</p>
              )}
            </div>
            <div className="flex gap-2">
              <button onClick={() => setCurrentStep(1)}
                className="px-4 py-2 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50">Zmień produkty</button>
              {isFullContent ? (
                <button onClick={() => setCurrentStep(4)}
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
                  Pobierz dokument
                </button>
              ) : (
                <button onClick={handleRegisterRedirect}
                  className="px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700">
                  Zarejestruj się, aby pobrać
                </button>
              )}
            </div>
          </div>

          {/* Requirements summary */}
          {requirements.length > 0 && (
            <div className="mb-4 bg-white border rounded-xl p-4">
              <h3 className="text-sm font-semibold text-gray-700 mb-3">Wyodrębnione wymagania techniczne:</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                {requirements.map((req, i) => (
                  <div key={i} className="flex items-center gap-2 text-sm">
                    <span className={`w-2 h-2 rounded-full ${req.isCommon ? 'bg-green-500' : 'bg-yellow-500'}`} />
                    <span className="text-gray-600">{req.parameterName}:</span>
                    <span className="font-medium text-gray-900">{req.description}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Content preview */}
          {isFullContent ? (
            <textarea value={content} onChange={e => setContent(e.target.value)}
              className="w-full h-96 p-4 border border-gray-300 rounded-xl font-mono text-sm focus:ring-2 focus:ring-blue-500 outline-none resize-y"
            />
          ) : (
            <>
              <div className="w-full p-4 border border-gray-300 rounded-xl font-mono text-sm bg-gray-50 overflow-y-auto whitespace-pre-wrap text-gray-700"
                style={{ maxHeight: '400px' }}>
                {content}
              </div>
              {/* Blurred teaser */}
              <div className="relative mt-2 p-4 border border-gray-200 rounded-xl overflow-hidden" style={{ height: '120px' }}>
                <div className="font-mono text-sm text-gray-400 blur-sm select-none">
                  2.{visibleRequirements + 1}.1. Procesor: nie mniej niż 32 rdzenie, architektura x86-64{'\n'}
                  2.{visibleRequirements + 1}.2. Pamięć RAM: nie mniej niż 512GB DDR5 ECC{'\n'}
                  2.{visibleRequirements + 2}.1. Interfejsy sieciowe: minimum 4x 25GbE SFP28{'\n'}
                  2.{visibleRequirements + 2}.2. Zasilanie redundantne: minimum 2x 800W
                </div>
                <div className="absolute inset-0 flex items-center justify-center bg-white/80">
                  <div className="text-center">
                    <p className="font-semibold text-gray-800 mb-2">
                      Pozostałe {totalRequirements - visibleRequirements} wymagań dostępne po rejestracji
                    </p>
                    <button onClick={handleRegisterRedirect}
                      className="px-6 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 text-sm">
                      Zarejestruj się za darmo
                    </button>
                  </div>
                </div>
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
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Specyfikacja SWZ jest gotowa</h2>
          <p className="text-gray-500 mb-6">Wybierz format pobierania dokumentu</p>
          <div className="flex justify-center gap-4">
            <button onClick={() => handleDownload('pdf')} disabled={downloading}
              className="px-8 py-3 bg-blue-600 text-white rounded-xl hover:bg-blue-700 transition-colors font-medium text-lg disabled:opacity-50">
              {downloading ? 'Pobieranie...' : 'Pobierz PDF'}
            </button>
            <button onClick={() => handleDownload('docx')} disabled={downloading}
              className="px-8 py-3 bg-green-600 text-white rounded-xl hover:bg-green-700 transition-colors font-medium text-lg disabled:opacity-50">
              {downloading ? 'Pobieranie...' : 'Pobierz DOCX'}
            </button>
          </div>
          <div className="mt-8 pt-6 border-t">
            <button onClick={() => { setCurrentStep(0); setSelectedTypeId(null); setSelectedModelIds([]); setContent(''); setRequirements([]); }}
              className="text-blue-600 hover:text-blue-800 font-medium">
              Generuj kolejną specyfikację
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default SWZCreatorPage;
