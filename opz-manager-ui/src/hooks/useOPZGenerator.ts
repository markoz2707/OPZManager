import { useState } from 'react';
import { generatorAPI } from '../services/api';
import toast from 'react-hot-toast';

export function useOPZGenerator() {
  const [content, setContent] = useState('');
  const [generating, setGenerating] = useState(false);

  const generateContent = async (equipmentModelIds: number[], equipmentType: string) => {
    try {
      setGenerating(true);
      const result = await generatorAPI.generateContent(equipmentModelIds, equipmentType);
      setContent(result.content);
      toast.success('Treść OPZ wygenerowana pomyślnie');
    } catch {
      toast.error('Błąd podczas generowania treści OPZ');
    } finally {
      setGenerating(false);
    }
  };

  const downloadPdf = async (title: string) => {
    if (!content) {
      toast.error('Najpierw wygeneruj treść OPZ');
      return;
    }
    try {
      const blob = await generatorAPI.generatePdf(content, title);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `OPZ_${new Date().toISOString().slice(0, 10)}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
      toast.success('PDF został pobrany');
    } catch {
      toast.error('Błąd podczas generowania PDF');
    }
  };

  return { content, setContent, generating, generateContent, downloadPdf };
}
