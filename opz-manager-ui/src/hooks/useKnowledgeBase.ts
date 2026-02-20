import { useState, useEffect, useCallback } from 'react';
import { knowledgeBaseAPI, KnowledgeDocument, KnowledgeSearchResult } from '../services/api';
import toast from 'react-hot-toast';

export function useKnowledgeBase(modelId: number) {
  const [documents, setDocuments] = useState<KnowledgeDocument[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [searchResults, setSearchResults] = useState<KnowledgeSearchResult[]>([]);
  const [searching, setSearching] = useState(false);

  const fetchDocuments = useCallback(async () => {
    try {
      const data = await knowledgeBaseAPI.getDocuments(modelId);
      setDocuments(data);
    } catch {
      toast.error('Błąd podczas pobierania dokumentów bazy wiedzy');
    } finally {
      setLoading(false);
    }
  }, [modelId]);

  useEffect(() => {
    fetchDocuments();
  }, [fetchDocuments]);

  const uploadDocument = async (file: File) => {
    setUploading(true);
    try {
      await knowledgeBaseAPI.uploadDocument(modelId, file);
      toast.success('Dokument został przesłany i jest przetwarzany');
      await fetchDocuments();
    } catch {
      toast.error('Błąd podczas przesyłania dokumentu');
    } finally {
      setUploading(false);
    }
  };

  const deleteDocument = async (docId: number) => {
    try {
      await knowledgeBaseAPI.deleteDocument(modelId, docId);
      toast.success('Dokument został usunięty');
      await fetchDocuments();
    } catch {
      toast.error('Błąd podczas usuwania dokumentu');
    }
  };

  const reprocessDocument = async (docId: number) => {
    try {
      await knowledgeBaseAPI.reprocessDocument(modelId, docId);
      toast.success('Ponowne przetwarzanie zostało uruchomione');
      await fetchDocuments();
    } catch {
      toast.error('Błąd podczas ponownego przetwarzania');
    }
  };

  const search = async (query: string, topK: number = 5) => {
    setSearching(true);
    try {
      const results = await knowledgeBaseAPI.search(modelId, query, topK);
      setSearchResults(results);
    } catch {
      toast.error('Błąd podczas wyszukiwania');
    } finally {
      setSearching(false);
    }
  };

  return {
    documents,
    loading,
    uploading,
    searchResults,
    searching,
    uploadDocument,
    deleteDocument,
    reprocessDocument,
    search,
    refreshDocuments: fetchDocuments,
  };
}
