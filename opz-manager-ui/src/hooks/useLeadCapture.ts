import { useState } from 'react';
import { leadCaptureAPI, LeadCaptureRequest } from '../services/publicApi';
import toast from 'react-hot-toast';

export function useLeadCapture() {
  const [downloadToken, setDownloadToken] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const captureLead = async (data: LeadCaptureRequest) => {
    try {
      setSubmitting(true);
      const result = await leadCaptureAPI.captureLead(data);
      setDownloadToken(result.downloadToken);
      toast.success(result.message);
      return result;
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Błąd podczas zapisywania danych';
      toast.error(msg);
      return null;
    } finally {
      setSubmitting(false);
    }
  };

  const downloadPdf = async (content: string, title: string) => {
    if (!downloadToken) {
      toast.error('Najpierw podaj adres email');
      return;
    }
    try {
      const blob = await leadCaptureAPI.downloadPdf({ downloadToken, content, title });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `OPZ_${new Date().toISOString().slice(0, 10)}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
      toast.success('PDF został pobrany');
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Błąd podczas pobierania PDF';
      toast.error(msg);
    }
  };

  return { downloadToken, submitting, captureLead, downloadPdf };
}
