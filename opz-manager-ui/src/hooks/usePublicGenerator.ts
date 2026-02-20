import { useState, useEffect } from 'react';
import { publicGeneratorAPI, publicEquipmentAPI, EquipmentType, EquipmentModel } from '../services/publicApi';
import toast from 'react-hot-toast';

export function usePublicGenerator() {
  const [types, setTypes] = useState<EquipmentType[]>([]);
  const [models, setModels] = useState<EquipmentModel[]>([]);
  const [content, setContent] = useState('');
  const [generating, setGenerating] = useState(false);
  const [loadingData, setLoadingData] = useState(true);

  useEffect(() => {
    loadEquipmentData();
  }, []);

  const loadEquipmentData = async () => {
    try {
      setLoadingData(true);
      const [typesData, modelsData] = await Promise.all([
        publicEquipmentAPI.getTypes(),
        publicEquipmentAPI.getModels(),
      ]);
      setTypes(typesData);
      setModels(modelsData);
    } catch {
      toast.error('Błąd podczas ładowania danych sprzętu');
    } finally {
      setLoadingData(false);
    }
  };

  const generateContent = async (equipmentModelIds: number[], equipmentType: string) => {
    try {
      setGenerating(true);
      const result = await publicGeneratorAPI.generateContent(equipmentModelIds, equipmentType);
      setContent(result.content);
      toast.success('Treść OPZ wygenerowana pomyślnie');
      return result.content;
    } catch {
      toast.error('Błąd podczas generowania treści OPZ');
      return null;
    } finally {
      setGenerating(false);
    }
  };

  return {
    types, models, content, setContent,
    generating, loadingData,
    generateContent,
  };
}
