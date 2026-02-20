import { useState, useEffect, useCallback } from 'react';
import { equipmentAPI, Manufacturer, EquipmentType, EquipmentModel } from '../services/api';
import toast from 'react-hot-toast';

export function useEquipment() {
  const [manufacturers, setManufacturers] = useState<Manufacturer[]>([]);
  const [types, setTypes] = useState<EquipmentType[]>([]);
  const [models, setModels] = useState<EquipmentModel[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchAll = useCallback(async () => {
    try {
      setLoading(true);
      const [mfrs, tps, mdls] = await Promise.all([
        equipmentAPI.getManufacturers(),
        equipmentAPI.getTypes(),
        equipmentAPI.getModels(),
      ]);
      setManufacturers(mfrs);
      setTypes(tps);
      setModels(mdls);
    } catch {
      toast.error('Błąd podczas pobierania danych sprzętu');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchAll(); }, [fetchAll]);

  const createManufacturer = async (data: { name: string; description: string }) => {
    await equipmentAPI.createManufacturer(data);
    toast.success('Producent został dodany');
    fetchAll();
  };

  const createType = async (data: { name: string; description: string }) => {
    await equipmentAPI.createType(data);
    toast.success('Typ sprzętu został dodany');
    fetchAll();
  };

  const createModel = async (data: { manufacturerId: number; typeId: number; modelName: string; specificationsJson: string }) => {
    await equipmentAPI.createModel(data);
    toast.success('Model został dodany');
    fetchAll();
  };

  const deleteManufacturer = async (id: number) => {
    try {
      await equipmentAPI.deleteManufacturer(id);
      toast.success('Producent został usunięty');
      fetchAll();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Nie można usunąć producenta');
    }
  };

  const deleteType = async (id: number) => {
    try {
      await equipmentAPI.deleteType(id);
      toast.success('Typ sprzętu został usunięty');
      fetchAll();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Nie można usunąć typu');
    }
  };

  const deleteModel = async (id: number) => {
    try {
      await equipmentAPI.deleteModel(id);
      toast.success('Model został usunięty');
      fetchAll();
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'Nie można usunąć modelu');
    }
  };

  return {
    manufacturers, types, models, loading,
    refresh: fetchAll,
    createManufacturer, createType, createModel,
    deleteManufacturer, deleteType, deleteModel,
  };
}
