import React, { useState, useRef } from 'react';
import { useTrainingData } from '../../hooks/useTrainingData';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';

const dataTypes = ['', 'QA', 'RequirementMatch', 'OPZGeneration', 'SpecExtraction', 'RequirementGeneration', 'DocumentAnalysis', 'SpecComparison'];

const TrainingDataPage: React.FC = () => {
  const { data, loading, filter, setFilter, generate, exportData, importData, create } = useTrainingData();
  const [showAddModal, setShowAddModal] = useState(false);
  const [formQuestion, setFormQuestion] = useState('');
  const [formAnswer, setFormAnswer] = useState('');
  const [formContext, setFormContext] = useState('');
  const [formType, setFormType] = useState('QA');
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleImportFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const text = await file.text();
    await importData(text);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleAdd = async () => {
    await create({ question: formQuestion, answer: formAnswer, context: formContext, dataType: formType });
    setShowAddModal(false);
    setFormQuestion(''); setFormAnswer(''); setFormContext(''); setFormType('QA');
  };

  if (loading) return <LoadingSpinner message="Ładowanie danych treningowych..." />;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Dane treningowe</h1>
        <div className="flex gap-2">
          <button onClick={generate} className="px-3 py-2 bg-green-600 text-white text-sm font-medium rounded-lg hover:bg-green-700">
            Generuj
          </button>
          <button onClick={exportData} className="px-3 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700">
            Eksportuj
          </button>
          <label className="px-3 py-2 bg-orange-600 text-white text-sm font-medium rounded-lg hover:bg-orange-700 cursor-pointer">
            Importuj
            <input ref={fileInputRef} type="file" accept=".json" className="hidden" onChange={handleImportFile} />
          </label>
          <button onClick={() => setShowAddModal(true)} className="px-3 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700">
            Dodaj
          </button>
        </div>
      </div>

      {/* Filter */}
      <div className="mb-4">
        <select
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          className="border border-gray-300 rounded-md px-3 py-2 text-sm"
        >
          <option value="">Wszystkie typy</option>
          {dataTypes.filter(Boolean).map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>
        <span className="ml-3 text-sm text-gray-500">{data.length} rekordów</span>
      </div>

      {/* Table */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Typ</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Pytanie</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Odpowiedź</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Data</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {data.map((item) => (
              <tr key={item.id} className="hover:bg-gray-50">
                <td className="px-4 py-3">
                  <span className="px-2 py-0.5 bg-gray-100 text-gray-600 rounded text-xs font-medium">{item.dataType}</span>
                </td>
                <td className="px-4 py-3 text-sm text-gray-800 max-w-xs truncate">{item.question}</td>
                <td className="px-4 py-3 text-sm text-gray-600 max-w-xs truncate">{item.answer}</td>
                <td className="px-4 py-3 text-sm text-gray-500">{new Date(item.createdAt).toLocaleDateString('pl-PL')}</td>
              </tr>
            ))}
            {data.length === 0 && (
              <tr><td colSpan={4} className="px-4 py-8 text-center text-gray-500">Brak danych treningowych</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Add Modal */}
      <Modal isOpen={showAddModal} onClose={() => setShowAddModal(false)} title="Dodaj dane treningowe">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Typ</label>
            <select value={formType} onChange={(e) => setFormType(e.target.value)} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm">
              {dataTypes.filter(Boolean).map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Pytanie</label>
            <textarea value={formQuestion} onChange={(e) => setFormQuestion(e.target.value)} rows={3} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Odpowiedź</label>
            <textarea value={formAnswer} onChange={(e) => setFormAnswer(e.target.value)} rows={3} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Kontekst</label>
            <textarea value={formContext} onChange={(e) => setFormContext(e.target.value)} rows={2} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm" />
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button onClick={() => setShowAddModal(false)} className="px-4 py-2 text-sm text-gray-700 border border-gray-300 rounded-md hover:bg-gray-50">Anuluj</button>
            <button onClick={handleAdd} className="px-4 py-2 text-sm text-white bg-indigo-600 rounded-md hover:bg-indigo-700">Zapisz</button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default TrainingDataPage;
