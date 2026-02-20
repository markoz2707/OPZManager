import { useState, useEffect, useCallback } from 'react';
import { trainingDataAPI, TrainingData } from '../services/api';
import toast from 'react-hot-toast';

export function useTrainingData() {
  const [data, setData] = useState<TrainingData[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<string>('');

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      const result = await trainingDataAPI.getAll(filter || undefined);
      setData(result);
    } catch {
      toast.error('Błąd podczas pobierania danych treningowych');
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => { fetchData(); }, [fetchData]);

  const create = async (input: { question: string; answer: string; context: string; dataType: string }) => {
    await trainingDataAPI.create(input);
    toast.success('Dane treningowe zostały dodane');
    fetchData();
  };

  const generate = async () => {
    try {
      toast.loading('Generowanie danych treningowych...', { id: 'gen' });
      const result = await trainingDataAPI.generate();
      toast.success(`Wygenerowano ${result.length} rekordów`, { id: 'gen' });
      fetchData();
    } catch {
      toast.error('Błąd podczas generowania', { id: 'gen' });
    }
  };

  const exportData = async () => {
    try {
      const result = await trainingDataAPI.export(filter || undefined);
      const blob = new Blob([result.data], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `training-data-${new Date().toISOString().slice(0, 10)}.json`;
      a.click();
      URL.revokeObjectURL(url);
      toast.success('Dane zostały wyeksportowane');
    } catch {
      toast.error('Błąd podczas eksportu');
    }
  };

  const importData = async (jsonData: string) => {
    try {
      await trainingDataAPI.import(jsonData);
      toast.success('Dane zostały zaimportowane');
      fetchData();
    } catch {
      toast.error('Błąd podczas importu danych');
    }
  };

  return { data, loading, filter, setFilter, refresh: fetchData, create, generate, exportData, importData };
}
