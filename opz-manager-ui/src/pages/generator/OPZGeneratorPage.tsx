import React, { useState, useEffect } from 'react';
import { equipmentAPI, EquipmentModel, EquipmentType } from '../../services/api';
import { useOPZGenerator } from '../../hooks/useOPZGenerator';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import toast from 'react-hot-toast';

type Step = 'select-type' | 'select-models' | 'preview';

const OPZGeneratorPage: React.FC = () => {
  const [step, setStep] = useState<Step>('select-type');
  const [types, setTypes] = useState<EquipmentType[]>([]);
  const [models, setModels] = useState<EquipmentModel[]>([]);
  const [selectedType, setSelectedType] = useState('');
  const [selectedModelIds, setSelectedModelIds] = useState<number[]>([]);
  const [loading, setLoading] = useState(true);
  const [title, setTitle] = useState('');
  const { content, setContent, generating, generateContent, downloadPdf } = useOPZGenerator();

  useEffect(() => {
    const fetch = async () => {
      try {
        const [t, m] = await Promise.all([equipmentAPI.getTypes(), equipmentAPI.getModels()]);
        setTypes(t);
        setModels(m);
      } catch {
        toast.error('Błąd podczas pobierania danych');
      } finally {
        setLoading(false);
      }
    };
    fetch();
  }, []);

  const filteredModels = selectedType
    ? models.filter((m) => m.typeName === selectedType)
    : models;

  const toggleModel = (id: number) => {
    setSelectedModelIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    );
  };

  const handleGenerate = async () => {
    if (selectedModelIds.length === 0) {
      toast.error('Wybierz co najmniej jeden model');
      return;
    }
    await generateContent(selectedModelIds, selectedType);
    setTitle(`OPZ - ${selectedType} - ${new Date().toLocaleDateString('pl-PL')}`);
    setStep('preview');
  };

  if (loading) return <LoadingSpinner message="Ładowanie danych..." />;

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Generator OPZ</h1>

      {/* Stepper */}
      <div className="flex items-center gap-2 mb-8">
        {[
          { key: 'select-type', label: '1. Wybierz typ' },
          { key: 'select-models', label: '2. Wybierz modele' },
          { key: 'preview', label: '3. Podgląd i pobierz' },
        ].map((s, i) => (
          <React.Fragment key={s.key}>
            {i > 0 && <div className="flex-1 h-px bg-gray-300" />}
            <div
              className={`px-3 py-1.5 rounded-full text-sm font-medium ${
                step === s.key ? 'bg-indigo-600 text-white' : 'bg-gray-100 text-gray-500'
              }`}
            >
              {s.label}
            </div>
          </React.Fragment>
        ))}
      </div>

      {/* Step 1: Select Type */}
      {step === 'select-type' && (
        <div className="space-y-4">
          <p className="text-gray-600">Wybierz typ sprzętu dla dokumentu OPZ:</p>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {types.map((t) => (
              <button
                key={t.id}
                onClick={() => { setSelectedType(t.name); setStep('select-models'); }}
                className={`p-4 rounded-lg border-2 text-left transition-colors ${
                  selectedType === t.name
                    ? 'border-indigo-600 bg-indigo-50'
                    : 'border-gray-200 hover:border-gray-300'
                }`}
              >
                <h3 className="font-semibold text-gray-900">{t.name}</h3>
                <p className="text-sm text-gray-500 mt-1">{t.description}</p>
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Step 2: Select Models */}
      {step === 'select-models' && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <p className="text-gray-600">
              Wybierz modele sprzętu typu <strong>{selectedType}</strong>:
            </p>
            <button onClick={() => setStep('select-type')} className="text-sm text-indigo-600 hover:text-indigo-800">
              ← Zmień typ
            </button>
          </div>

          {filteredModels.length === 0 ? (
            <p className="text-gray-500 py-8 text-center">Brak modeli dla wybranego typu</p>
          ) : (
            <div className="space-y-2 mb-6">
              {filteredModels.map((m) => (
                <label
                  key={m.id}
                  className={`flex items-center p-4 rounded-lg border-2 cursor-pointer transition-colors ${
                    selectedModelIds.includes(m.id)
                      ? 'border-indigo-600 bg-indigo-50'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={selectedModelIds.includes(m.id)}
                    onChange={() => toggleModel(m.id)}
                    className="h-4 w-4 text-indigo-600 rounded border-gray-300"
                  />
                  <div className="ml-3">
                    <span className="font-medium text-gray-900">{m.manufacturerName} {m.modelName}</span>
                  </div>
                </label>
              ))}
            </div>
          )}

          <button
            onClick={handleGenerate}
            disabled={selectedModelIds.length === 0 || generating}
            className="px-6 py-3 bg-indigo-600 text-white font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50"
          >
            {generating ? 'Generowanie...' : `Generuj OPZ (${selectedModelIds.length} modeli)`}
          </button>
        </div>
      )}

      {/* Step 3: Preview */}
      {step === 'preview' && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <p className="text-gray-600">Podgląd wygenerowanego dokumentu OPZ:</p>
            <button onClick={() => setStep('select-models')} className="text-sm text-indigo-600 hover:text-indigo-800">
              ← Zmień modele
            </button>
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-1">Tytuł dokumentu</label>
            <input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm"
            />
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
            <textarea
              value={content}
              onChange={(e) => setContent(e.target.value)}
              rows={20}
              className="w-full text-sm text-gray-800 font-mono border-0 focus:outline-none resize-y"
            />
          </div>

          <div className="flex gap-4">
            <button
              onClick={() => downloadPdf(title)}
              className="px-6 py-3 bg-indigo-600 text-white font-medium rounded-lg hover:bg-indigo-700"
            >
              Pobierz PDF
            </button>
            <button
              onClick={handleGenerate}
              disabled={generating}
              className="px-6 py-3 bg-white text-indigo-600 border border-indigo-600 font-medium rounded-lg hover:bg-indigo-50 disabled:opacity-50"
            >
              Generuj ponownie
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default OPZGeneratorPage;
