import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useEquipment } from '../../hooks/useEquipment';
import { useAuth } from '../../hooks/useAuth';
import { EquipmentModel, FolderImportResult } from '../../services/api';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';
import ConfirmDialog from '../../components/common/ConfirmDialog';
import toast from 'react-hot-toast';

type Tab = 'manufacturers' | 'types' | 'models';

const statusLabels: Record<string, { label: string; color: string }> = {
  created: { label: 'Nowy model', color: 'bg-green-100 text-green-800' },
  uploaded: { label: 'Wgrany', color: 'bg-blue-100 text-blue-800' },
  skipped: { label: 'Pominięty', color: 'bg-yellow-100 text-yellow-800' },
  error: { label: 'Błąd', color: 'bg-red-100 text-red-800' },
};

const EquipmentCatalogPage: React.FC = () => {
  const { user } = useAuth();
  const {
    manufacturers, types, models, loading,
    createManufacturer, createType, createModel,
    deleteManufacturer, deleteType, deleteModel,
    updateModel, importFolder, purgeAll,
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

  // Edit modal state
  const [editModel, setEditModel] = useState<EquipmentModel | null>(null);
  const [editName, setEditName] = useState('');
  const [editMfr, setEditMfr] = useState(0);
  const [editType, setEditType] = useState(0);
  const [editSaving, setEditSaving] = useState(false);

  // Import modal state
  const [showImportModal, setShowImportModal] = useState(false);
  const [importPath, setImportPath] = useState('');
  const [importing, setImporting] = useState(false);
  const [importResult, setImportResult] = useState<FolderImportResult | null>(null);

  // Purge state
  const [showPurgeConfirm, setShowPurgeConfirm] = useState(false);
  const [purgeIncludeManufacturers, setPurgeIncludeManufacturers] = useState(false);
  const [purgeIncludeTypes, setPurgeIncludeTypes] = useState(false);
  const [purging, setPurging] = useState(false);

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

  const openEditModal = (m: EquipmentModel) => {
    setEditModel(m);
    setEditName(m.modelName);
    setEditMfr(m.manufacturerId);
    setEditType(m.typeId);
  };

  const handleSaveEdit = async () => {
    if (!editModel) return;
    setEditSaving(true);
    try {
      await updateModel(editModel.id, {
        manufacturerId: editMfr,
        typeId: editType,
        modelName: editName,
      });
      setEditModel(null);
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Błąd podczas aktualizacji');
    } finally {
      setEditSaving(false);
    }
  };

  const handlePurge = async () => {
    setPurging(true);
    try {
      await purgeAll(purgeIncludeManufacturers, purgeIncludeTypes);
      setShowPurgeConfirm(false);
      setPurgeIncludeManufacturers(false);
      setPurgeIncludeTypes(false);
    } catch {
      // Error toast is handled in the hook
    } finally {
      setPurging(false);
    }
  };

  const handleImport = async () => {
    if (!importPath.trim()) {
      toast.error('Podaj ścieżkę do folderu');
      return;
    }
    setImporting(true);
    setImportResult(null);
    try {
      const result = await importFolder(importPath.trim());
      setImportResult(result);
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Błąd podczas importu');
    } finally {
      setImporting(false);
    }
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
          <div className="flex gap-2">
            <button
              onClick={() => setShowPurgeConfirm(true)}
              className="px-4 py-2 bg-red-600 text-white text-sm font-medium rounded-lg hover:bg-red-700"
            >
              Wyczysc baze
            </button>
            <button
              onClick={() => { setImportResult(null); setShowImportModal(true); }}
              className="px-4 py-2 bg-emerald-600 text-white text-sm font-medium rounded-lg hover:bg-emerald-700"
            >
              Import folderu
            </button>
            <button
              onClick={() => { resetForm(); setShowAddModal(true); }}
              className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700"
            >
              Dodaj {activeTab === 'manufacturers' ? 'producenta' : activeTab === 'types' ? 'typ' : 'model'}
            </button>
          </div>
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
                    <Link to={`/admin/equipment/${m.id}`} className="text-indigo-600 hover:text-indigo-800 font-medium">{m.modelName}</Link>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">{m.manufacturerName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{m.typeName}</td>
                  <td className="px-6 py-4 text-right">
                    {isAdmin && (
                      <div className="flex justify-end gap-3">
                        <button onClick={() => openEditModal(m)} className="text-indigo-600 hover:text-indigo-800 text-sm">Edytuj</button>
                        <button onClick={() => setDeleteTarget({ type: 'models', id: m.id })} className="text-red-600 hover:text-red-800 text-sm">Usuń</button>
                      </div>
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

      {/* Edit Modal */}
      <Modal isOpen={editModel !== null} onClose={() => setEditModel(null)} title="Edytuj model">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Producent</label>
            <select value={editMfr} onChange={(e) => setEditMfr(Number(e.target.value))} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm">
              {manufacturers.map((m) => <option key={m.id} value={m.id}>{m.name}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Typ sprzętu</label>
            <select value={editType} onChange={(e) => setEditType(Number(e.target.value))} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm">
              {types.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Nazwa modelu</label>
            <input value={editName} onChange={(e) => setEditName(e.target.value)} className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm" />
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button onClick={() => setEditModel(null)} className="px-4 py-2 text-sm text-gray-700 border border-gray-300 rounded-md hover:bg-gray-50">Anuluj</button>
            <button onClick={handleSaveEdit} disabled={editSaving || !editName.trim()} className="px-4 py-2 text-sm text-white bg-indigo-600 rounded-md hover:bg-indigo-700 disabled:opacity-50">
              {editSaving ? 'Zapisywanie...' : 'Zapisz'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Import Modal */}
      <Modal isOpen={showImportModal} onClose={() => setShowImportModal(false)} title="Import folderu PDF">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Ścieżka folderu</label>
            <input
              value={importPath}
              onChange={(e) => setImportPath(e.target.value)}
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm font-mono"
              placeholder="I:\AI_OPZ\IMPORT"
            />
            <p className="text-xs text-gray-500 mt-1">
              Struktura: Producent/Typ/pliki.pdf (np. DELL/Servers/poweredge-r570.pdf)
            </p>
          </div>

          {!importResult && (
            <div className="flex justify-end gap-3 pt-2">
              <button onClick={() => setShowImportModal(false)} className="px-4 py-2 text-sm text-gray-700 border border-gray-300 rounded-md hover:bg-gray-50">Anuluj</button>
              <button
                onClick={handleImport}
                disabled={importing || !importPath.trim()}
                className="px-4 py-2 text-sm text-white bg-emerald-600 rounded-md hover:bg-emerald-700 disabled:opacity-50"
              >
                {importing ? 'Importowanie...' : 'Importuj'}
              </button>
            </div>
          )}

          {importing && (
            <div className="flex items-center gap-3 p-4 bg-blue-50 rounded-lg">
              <svg className="animate-spin h-5 w-5 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              <span className="text-sm text-blue-800">Trwa import plików PDF. To może potrwać kilka minut...</span>
            </div>
          )}

          {importResult && (
            <div>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-4">
                <div className="bg-gray-50 rounded-lg p-3 text-center">
                  <div className="text-2xl font-bold text-gray-900">{importResult.totalFiles}</div>
                  <div className="text-xs text-gray-500">Plików</div>
                </div>
                <div className="bg-green-50 rounded-lg p-3 text-center">
                  <div className="text-2xl font-bold text-green-700">{importResult.createdModels}</div>
                  <div className="text-xs text-gray-500">Nowych modeli</div>
                </div>
                <div className="bg-blue-50 rounded-lg p-3 text-center">
                  <div className="text-2xl font-bold text-blue-700">{importResult.uploadedDocuments}</div>
                  <div className="text-xs text-gray-500">Dokumentów</div>
                </div>
                {importResult.errors > 0 && (
                  <div className="bg-red-50 rounded-lg p-3 text-center">
                    <div className="text-2xl font-bold text-red-700">{importResult.errors}</div>
                    <div className="text-xs text-gray-500">Błędów</div>
                  </div>
                )}
                {importResult.skippedFiles > 0 && (
                  <div className="bg-yellow-50 rounded-lg p-3 text-center">
                    <div className="text-2xl font-bold text-yellow-700">{importResult.skippedFiles}</div>
                    <div className="text-xs text-gray-500">Pominiętych</div>
                  </div>
                )}
              </div>

              <div className="max-h-64 overflow-y-auto border border-gray-200 rounded-lg">
                <table className="min-w-full divide-y divide-gray-200 text-sm">
                  <thead className="bg-gray-50 sticky top-0">
                    <tr>
                      <th className="px-3 py-2 text-left text-xs font-medium text-gray-500">Plik</th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-gray-500">Model</th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-gray-500">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-200">
                    {importResult.items.map((item, idx) => {
                      const st = statusLabels[item.status] || { label: item.status, color: 'bg-gray-100 text-gray-800' };
                      return (
                        <tr key={idx}>
                          <td className="px-3 py-2 text-gray-700 truncate max-w-[200px]" title={item.filename}>{item.filename}</td>
                          <td className="px-3 py-2 text-gray-700">
                            {item.modelName ? `${item.manufacturerName} ${item.modelName}` : '-'}
                          </td>
                          <td className="px-3 py-2">
                            <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${st.color}`}>
                              {st.label}
                            </span>
                            {item.errorMessage && (
                              <span className="text-xs text-red-600 ml-1" title={item.errorMessage}>
                                {item.errorMessage.length > 40 ? item.errorMessage.substring(0, 40) + '...' : item.errorMessage}
                              </span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              <div className="flex justify-end pt-3">
                <button onClick={() => setShowImportModal(false)} className="px-4 py-2 text-sm text-white bg-indigo-600 rounded-md hover:bg-indigo-700">Zamknij</button>
              </div>
            </div>
          )}
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

      {/* Purge Confirmation Modal */}
      <Modal isOpen={showPurgeConfirm} onClose={() => setShowPurgeConfirm(false)} title="Czyszczenie bazy sprzętu">
        <div className="space-y-4">
          <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-sm text-red-800 font-medium">Ta operacja usunie wszystkie modele sprzętu wraz z dokumentami bazy wiedzy, embeddingami i dopasowaniami OPZ.</p>
          </div>
          <div className="space-y-2">
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input type="checkbox" checked={purgeIncludeManufacturers} onChange={(e) => setPurgeIncludeManufacturers(e.target.checked)} className="rounded border-gray-300 text-red-600 focus:ring-red-500" />
              Usun rowniez producentow
            </label>
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input type="checkbox" checked={purgeIncludeTypes} onChange={(e) => setPurgeIncludeTypes(e.target.checked)} className="rounded border-gray-300 text-red-600 focus:ring-red-500" />
              Usun rowniez typy sprzetu
            </label>
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button onClick={() => setShowPurgeConfirm(false)} className="px-4 py-2 text-sm text-gray-700 border border-gray-300 rounded-md hover:bg-gray-50">Anuluj</button>
            <button
              onClick={handlePurge}
              disabled={purging}
              className="px-4 py-2 text-sm text-white bg-red-600 rounded-md hover:bg-red-700 disabled:opacity-50"
            >
              {purging ? 'Czyszczenie...' : 'Wyczysc'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default EquipmentCatalogPage;
