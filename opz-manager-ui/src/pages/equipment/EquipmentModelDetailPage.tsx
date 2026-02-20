import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { equipmentAPI, EquipmentModel } from '../../services/api';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import toast from 'react-hot-toast';

const EquipmentModelDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [model, setModel] = useState<EquipmentModel | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetch = async () => {
      try {
        const data = await equipmentAPI.getModel(Number(id));
        setModel(data);
      } catch {
        toast.error('Błąd podczas pobierania modelu');
      } finally {
        setLoading(false);
      }
    };
    fetch();
  }, [id]);

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
    </div>
  );
};

export default EquipmentModelDetailPage;
