import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useOPZDocuments } from '../../hooks/useOPZDocuments';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import ConfirmDialog from '../../components/common/ConfirmDialog';

const statusColors: Record<string, string> = {
  'Uploaded': 'bg-gray-100 text-gray-700',
  'Przetworzony': 'bg-blue-100 text-blue-700',
  'Analizowanie': 'bg-yellow-100 text-yellow-700',
  'Zakończono analizę': 'bg-green-100 text-green-700',
  'Błąd analizy': 'bg-red-100 text-red-700',
};

const OPZListPage: React.FC = () => {
  const { documents, loading, deleteDocument } = useOPZDocuments();
  const [deleteId, setDeleteId] = useState<number | null>(null);

  if (loading) return <LoadingSpinner message="Ładowanie dokumentów..." />;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Dokumenty OPZ</h1>
        <Link
          to="/admin/opz/upload"
          className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700"
        >
          Wgraj nowy dokument
        </Link>
      </div>

      {documents.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
          <p className="text-gray-500 text-lg">Brak dokumentów OPZ</p>
          <p className="text-gray-400 text-sm mt-2">Wgraj pierwszy dokument, aby rozpocząć</p>
        </div>
      ) : (
        <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Nazwa pliku</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Data wgrania</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Wymagania</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Dopasowania</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Akcje</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {documents.map((doc) => (
                <tr key={doc.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4">
                    <Link to={`/admin/opz/${doc.id}`} className="text-indigo-600 hover:text-indigo-800 font-medium">
                      {doc.filename}
                    </Link>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    {new Date(doc.uploadDate).toLocaleDateString('pl-PL')}
                  </td>
                  <td className="px-6 py-4">
                    <span className={`px-2.5 py-0.5 rounded-full text-xs font-medium ${statusColors[doc.analysisStatus] || 'bg-gray-100 text-gray-700'}`}>
                      {doc.analysisStatus}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">{doc.requirementsCount}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{doc.matchesCount}</td>
                  <td className="px-6 py-4 text-right">
                    <button
                      onClick={() => setDeleteId(doc.id)}
                      className="text-red-600 hover:text-red-800 text-sm font-medium"
                    >
                      Usuń
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <ConfirmDialog
        isOpen={deleteId !== null}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && deleteDocument(deleteId)}
        title="Usuń dokument OPZ"
        message="Czy na pewno chcesz usunąć ten dokument? Ta operacja jest nieodwracalna."
        confirmLabel="Usuń"
      />
    </div>
  );
};

export default OPZListPage;
