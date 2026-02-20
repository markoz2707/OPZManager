import { useState } from 'react';
import { publicOPZAPI, PublicOPZDocument, VerificationResult, EquipmentMatch } from '../services/publicApi';
import toast from 'react-hot-toast';

export function usePublicOPZ() {
  const [document, setDocument] = useState<PublicOPZDocument | null>(null);
  const [verification, setVerification] = useState<VerificationResult | null>(null);
  const [matches, setMatches] = useState<EquipmentMatch[]>([]);
  const [uploading, setUploading] = useState(false);
  const [verifying, setVerifying] = useState(false);
  const [analyzing, setAnalyzing] = useState(false);

  const uploadOPZ = async (file: File) => {
    try {
      setUploading(true);
      const doc = await publicOPZAPI.uploadOPZ(file);
      setDocument(doc);
      toast.success('Dokument został przesłany');
      return doc;
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Błąd podczas przesyłania dokumentu';
      toast.error(msg);
      return null;
    } finally {
      setUploading(false);
    }
  };

  const verifyOPZ = async (id: number) => {
    try {
      setVerifying(true);
      const result = await publicOPZAPI.verifyOPZ(id);
      setVerification(result);
      toast.success('Weryfikacja zakończona');
      return result;
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Błąd podczas weryfikacji';
      toast.error(msg);
      return null;
    } finally {
      setVerifying(false);
    }
  };

  const getVerification = async (id: number) => {
    try {
      const result = await publicOPZAPI.getVerification(id);
      setVerification(result);
      return result;
    } catch {
      return null;
    }
  };

  const analyzeOPZ = async (id: number) => {
    try {
      setAnalyzing(true);
      const result = await publicOPZAPI.analyzeOPZ(id);
      setMatches(result.matches);
      toast.success(`Analiza zakończona - ${result.matchesCount} dopasowań`);
      return result;
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Błąd podczas analizy';
      toast.error(msg);
      return null;
    } finally {
      setAnalyzing(false);
    }
  };

  const loadDocument = async (id: number) => {
    try {
      const doc = await publicOPZAPI.getOPZDocument(id);
      setDocument(doc);
      return doc;
    } catch {
      return null;
    }
  };

  return {
    document, verification, matches,
    uploading, verifying, analyzing,
    uploadOPZ, verifyOPZ, getVerification, analyzeOPZ, loadDocument,
    setDocument, setVerification,
  };
}
