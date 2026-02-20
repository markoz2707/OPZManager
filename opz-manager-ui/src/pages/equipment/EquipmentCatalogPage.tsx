import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useEquipment } from '../../hooks/useEquipment';
import { useAuth } from '../../hooks/useAuth';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import toast from 'react-hot-toast';

type Tab = 'manufacturers' | 'types' | 'models';

const EquipmentCatalogPage: React.FC = () => {
  const { user } = useAuth();
  const {
    manufacturers, types, models, loading,
    createManufacturer, createType, createModel,
    deleteManufacturer, deleteType, deleteModel,
  } = useEquipment();
  const [activeTab, setActiveTab] = useState<Tab>('models');
  const [showAddModal, setShowAddModal] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<{ type: Tab; id: number } | null>(null);

  // Form state
  const [formName, setFormName] = useState('');
  const [formDesc, setFormDesc] = useState('');
  const [formMfr, setFormMfr] = useState(0);
  const [formType, setFormType] = useState(0);
  const [formModelName, setFormModelName] = useState('');
  const [formSpecs, setFormSpecs] = useState('{}');

  const isAdmin = user?.role === 'Admin';

  const resetForm = () => {
    setFormName(''); setFormDesc(''); setFormMfr(0); setFormType(0);
    setFormModelName(''); setFormSpecs('{}');
  };

  const handleAdd = async () => {
    try {
      if (activeTab === 'manufacturers') {
        await createManufacturer({ name: formName, description: formDesc });
      } else if (activeTab === 'types') {
        await createType({ name: formName, description: formDesc });
      } else {
        await createModel({ manufacturerId: formMfr, typeId: formType, modelName: formModelName, specificationsJson: formSpecs });
      }
      setShowAddModal(false);
      resetForm();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Błąd podczas dodawania');
    }
  };

  const handleDelete = () => {
    if (!deleteTarget) return;
    if (deleteTarget.type === 'manufacturers') deleteManufacturer(deleteTarget.id);
    else if (deleteTarget.type === 'types') deleteType(deleteTarget.id);
    else deleteModel(deleteTarget.id);
  };

  if (loading) return <LoadingSpinner message="Ładowanie katalogu sprzętu..." />;

  const tabs: { key: Tab; label: string; count: number }[] = [
    { key: 'models', label: 'Modele', count: models.length },
    { key: 'manufacturers', label: 'Producenci', count: manufacturers.length },
    { key: 'types', label: 'Typy sprzętu', count: types.length },
  ];

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Katalog sprzętu</h1>
        {isAdmin && (
          <button
            onClick={() => { resetForm(); setShowAddModal(true); }}
            className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700"
          >
            Dodaj {activeTab === 'manufacturers' ? 'producenta' : activeTab === 'types' ? 'typ' : 'model'}
          </button>
        )}
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <div className="flex gap-4">
          {tabs.map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              className={`pb-3 px-1 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab.key
                  ? 'border-indigo-600 text-indigo-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              {tab.label} ({tab.count})
            </button>
          ))}
        </div>
      </div>

      {/* Manufacturers Tab */}
      {activeTab === 'manufacturers' && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {manufacturers.map((m) => (
            <div key={m.id} className="bg-white rounded-lg border border-gray-200 p-4">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-gray-900">{m.name}</h3>
                  <p className="text-sm text-gray-500 mt-1">{m.description}</p>
                </div>
                {isAdmin && (
                  <button onClick={() => setDeleteTarget({ type: 'manufacturers', id: m.id })} className="text-red-500 hover:text-red-700 text-sm">Usuń</button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Types Tab */}
      {activeTab === 'types' && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {types.map((t) => (
            <div key={t.id} className="bg-white rounded-lg border border-gray-200 p-4">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-gray-900">{t.name}</h3>
                  <p className="text-sm text-gray-500 mt-1">{t.description}</p>
                </div>
                {isAdmin && (
                  <button onClick={() => setDeleteTarget({ type: 'types', id: t.id })} className="text-red-500 hover:text-red-700 text-sm">Usuń</button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Models Tab */}
      {activeTab === 'models' && (
        <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Model</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Producent</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Typ</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Akcje</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {models.map((m) => (
                <tr key={m.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4">
                    <Link to={`/equipment/${m.id}`} className="text-indigo-600 hover:text-indigo-800 font-medium">{m.modelName}</Link>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">{m.manufacturerName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{m.typeName}</td>
                  <td className="px-6 py-4 text-right">
                    {isAdmin && (
                      <button onClick={() => setDeleteTarget({ type: 'models', id: m.id })} className="text-red-600 hover:text-red-800 text-sm">Usuń</button>
                    )}
                  </td>
                </tr>
              ))}
              {models.length === 0 && (
                <tr><td colSpan={4} className="px-6 py-8 text-center text-gray-500">Brak modeli sprzętu</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Add Modal */}
      <Modal isOpen={showAddModal} onClose={() => setShowAddModal(false)} title={`Dodaj ${activeTab === 'manufacturers' ? 'producenta' : activeTab === 'types' ? 'typ sprzętu' : 'model'}`}>
        <div className="space-y-4">
          {activeTab === 'models' ? (
            <>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Producent</label>
                <select value={formMfr} onChange={(e) => setFormMfr(Number(e.target.value))} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm">
                  <option value={0}>Wybierz producenta</option>
                  {manufacturers.map((m) => <option key={m.id} value={m.id}>{m.name}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Typ sprzętu</label>
                <select value={formType} onChange={(e) => setFormType(Number(e.target.value))} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm">
                  <option value={0}>Wybierz typ</option>
                  {types.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Nazwa modelu</label>
                <input value={formModelName} onChange={(e) => setFormModelName(e.target.value)} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm" placeholder="np. ME5012" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Specyfikacja (JSON)</label>
                <textarea value={formSpecs} onChange={(e) => setFormSpecs(e.target.value)} rows={4} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm font-mono" placeholder='{"RAM": "32GB", "Storage": "10TB"}' />
              </div>
            </>
          ) : (
            <>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Nazwa</label>
                <input value={formName} onChange={(e) => setFormName(e.target.value)} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Opis</label>
                <textarea value={formDesc} onChange={(e) => setFormDesc(e.target.value)} rows={3} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm" />
              </div>
            </>
          )}
          <div className="flex justify-end gap-3 pt-2">
            <button onClick={() => setShowAddModal(false)} className="px-4 py-2 text-sm text-gray-700 border border-gray-300 rounded-md hover:bg-gray-50">Anuluj</button>
            <button onClick={handleAdd} className="px-4 py-2 text-sm text-white bg-indigo-600 rounded-md hover:bg-indigo-700">Zapisz</button>
          </div>
        </div>
      </Modal>

      <ConfirmDialog
        isOpen={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Potwierdź usunięcie"
        message="Czy na pewno chcesz usunąć ten element? Ta operacja jest nieodwracalna."
        confirmLabel="Usuń"
      />
    </div>
  );
};

export default EquipmentCatalogPage;
