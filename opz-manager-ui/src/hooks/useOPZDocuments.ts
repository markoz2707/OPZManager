import { useState, useEffect, useCallback, useRef } from 'react';
import { opzAPI, OPZDocument, OPZDocumentDetail, AnalysisProgress } from '../services/api';
import toast from 'react-hot-toast';

export function useOPZDocuments() {
  const [documents, setDocuments] = useState<OPZDocument[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchDocuments = useCallback(async () => {
    try {
      setLoading(true);
      const data = await opzAPI.getOPZDocuments();
      setDocuments(data);
    } catch {
      toast.error('Błąd podczas pobierania dokumentów OPZ');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchDocuments(); }, [fetchDocuments]);

  const deleteDocument = async (id: number) => {
    try {
      await opzAPI.deleteOPZ(id);
      toast.success('Dokument został usunięty');
      fetchDocuments();
    } catch {
      toast.error('Błąd podczas usuwania dokumentu');
    }
  };

  return { documents, loading, refresh: fetchDocuments, deleteDocument };
}

export function useOPZDocument(id: number) {
  const [document, setDocument] = useState<OPZDocumentDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [analysisProgress, setAnalysisProgress] = useState<AnalysisProgress | null>(null);
  const pollingRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchDocument = useCallback(async () => {
    try {
      setLoading(true);
      const data = await opzAPI.getOPZDocument(id);
      setDocument(data);
    } catch {
      toast.error('Błąd podczas pobierania dokumentu OPZ');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { fetchDocument(); }, [fetchDocument]);

  // Cleanup polling on unmount
  useEffect(() => {
    return () => {
      if (pollingRef.current) clearInterval(pollingRef.current);
    };
  }, []);

  const startPolling = useCallback(() => {
    if (pollingRef.current) clearInterval(pollingRef.current);
    pollingRef.current = setInterval(async () => {
      try {
        const progress = await opzAPI.getAnalysisProgress(id);
        setAnalysisProgress(progress);

        if (progress.status === 'completed' || progress.status === 'error' || progress.status === 'cancelled') {
          if (pollingRef.current) clearInterval(pollingRef.current);
          pollingRef.current = null;

          if (progress.status === 'completed') {
            toast.success('Analiza zakończona');
            fetchDocument();
          } else if (progress.status === 'cancelled') {
            toast.success('Analiza została anulowana');
            fetchDocument();
          } else {
            toast.error(`Błąd analizy: ${progress.errorMessage}`);
          }

          // Clear progress after a short delay so user sees the final state
          setTimeout(() => setAnalysisProgress(null), 3000);
        }
      } catch {
        // Silently ignore polling errors
      }
    }, 2000);
  }, [id, fetchDocument]);

  const analyze = async () => {
    try {
      await opzAPI.analyzeOPZ(id);
      setAnalysisProgress({
        status: 'running',
        totalEquipment: 0,
        completedEquipment: 0,
        currentEquipmentName: 'Przygotowywanie...',
        percentage: 0
      });
      startPolling();
    } catch {
      toast.error('Błąd podczas uruchamiania analizy');
    }
  };

  const reprocess = async () => {
    try {
      toast.loading('Ponowne przetwarzanie wymagań...', { id: 'reprocess' });
      const result = await opzAPI.reprocessOPZ(id);
      toast.success(result.message, { id: 'reprocess' });
      fetchDocument();
    } catch {
      toast.error('Błąd podczas ponownego przetwarzania', { id: 'reprocess' });
    }
  };

  const cancelAnalysis = async () => {
    try {
      await opzAPI.cancelAnalysis(id);
    } catch {
      toast.error('Błąd podczas anulowania analizy');
    }
  };

  return { document, loading, refresh: fetchDocument, analyze, reprocess, cancelAnalysis, analysisProgress };
}
