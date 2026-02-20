import { useState, useEffect, useCallback } from 'react';
import { opzAPI, OPZDocument, OPZDocumentDetail } from '../services/api';
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

  const analyze = async () => {
    try {
      toast.loading('Analizowanie dokumentu...', { id: 'analyze' });
      await opzAPI.analyzeOPZ(id);
      toast.success('Analiza zakończona', { id: 'analyze' });
      fetchDocument();
    } catch {
      toast.error('Błąd podczas analizy', { id: 'analyze' });
    }
  };

  return { document, loading, refresh: fetchDocument, analyze };
}
