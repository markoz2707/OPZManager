import React, { useState, useCallback, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import StepIndicator from '../../components/public/StepIndicator';
import VerificationScoreCard from '../../components/public/VerificationScoreCard';
import EmailGateModal from '../../components/public/EmailGateModal';
import { usePublicOPZ } from '../../hooks/usePublicOPZ';
import { useLeadCapture } from '../../hooks/useLeadCapture';

const steps = [
  { label: 'Upload' },
  { label: 'Przetwarzanie' },
  { label: 'Wyniki' },
  { label: 'Pobieranie' },
];

const VerifyOPZPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [currentStep, setCurrentStep] = useState(0);
  const [dragActive, setDragActive] = useState(false);
  const [showEmailModal, setShowEmailModal] = useState(false);

  const {
    document, verification,
    uploading, verifying,
    uploadOPZ, verifyOPZ, getVerification, loadDocument,
  } = usePublicOPZ();

  const { downloadToken, submitting, captureLead, downloadPdf } = useLeadCapture();

  // Load existing document if ID in URL
  useEffect(() => {
    if (id) {
      const docId = parseInt(id);
      if (!isNaN(docId)) {
        loadDocument(docId).then(doc => {
          if (doc) {
            setCurrentStep(1);
            getVerification(docId).then(v => {
              if (v) setCurrentStep(2);
            });
          }
        });
      }
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleFileUpload = useCallback(async (file: File) => {
    const doc = await uploadOPZ(file);
    if (doc) {
      setCurrentStep(1);
      // Auto-start verification
      const result = await verifyOPZ(doc.id);
      if (result) {
        setCurrentStep(2);
      }
    }
  }, [uploadOPZ, verifyOPZ]);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDragActive(false);
    const file = e.dataTransfer.files[0];
    if (file && file.type === 'application/pdf') {
      handleFileUpload(file);
    }
  }, [handleFileUpload]);

  const handleFileInput = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFileUpload(file);
  }, [handleFileUpload]);

  const handleEmailSubmit = async (email: string, marketingConsent: boolean) => {
    const result = await captureLead({
      email,
      marketingConsent,
      opzDocumentId: document?.id,
      source: 'verification',
    });
    if (result) {
      setShowEmailModal(false);
      setCurrentStep(3);
    }
  };

  const handleDownload = async () => {
    if (!verification) return;
    const title = `Raport weryfikacji OPZ - ${document?.filename || 'dokument'}`;
    const content = generateReportContent();
    await downloadPdf(content, title);
  };

  const generateReportContent = () => {
    if (!verification) return '';
    const lines = [
      `RAPORT WERYFIKACJI OPZ`,
      `Plik: ${document?.filename || 'N/A'}`,
      `Data: ${new Date().toLocaleDateString('pl-PL')}`,
      ``,
      `OCENA OGÓLNA: ${verification.overallScore}/100 (${verification.grade})`,
      ``,
      verification.summaryText || '',
    ];
    return lines.join('\n');
  };

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
      <h1 className="text-3xl font-bold text-gray-900 mb-2">Weryfikacja dokumentu OPZ</h1>
      <p className="text-gray-500 mb-8">Prześlij dokument PDF i otrzymaj szczegółową analizę jakości</p>

      <StepIndicator steps={steps} currentStep={currentStep} />

      {/* Step 0: Upload */}
      {currentStep === 0 && (
        <div
          className={`border-2 border-dashed rounded-2xl p-12 text-center transition-colors cursor-pointer ${
            dragActive ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-blue-400 hover:bg-gray-50'
          }`}
          onDragOver={(e) => { e.preventDefault(); setDragActive(true); }}
          onDragLeave={() => setDragActive(false)}
          onDrop={handleDrop}
          onClick={() => document === null && window.document.getElementById('file-input')?.click()}
        >
          <input
            id="file-input"
            type="file"
            accept=".pdf"
            onChange={handleFileInput}
            className="hidden"
          />
          <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
            </svg>
          </div>
          <p className="text-lg font-medium text-gray-700 mb-1">
            {dragActive ? 'Upuść plik tutaj' : 'Przeciągnij plik PDF lub kliknij, aby wybrać'}
          </p>
          <p className="text-sm text-gray-400">Maksymalny rozmiar pliku: 50 MB</p>
        </div>
      )}

      {/* Step 1: Processing */}
      {currentStep === 1 && (uploading || verifying) && (
        <div className="bg-white border rounded-2xl p-12 text-center">
          <div className="animate-spin w-16 h-16 border-4 border-blue-200 border-t-blue-600 rounded-full mx-auto mb-6" />
          <h2 className="text-xl font-semibold text-gray-800 mb-2">
            {uploading ? 'Przesyłanie dokumentu...' : 'Trwa weryfikacja dokumentu...'}
          </h2>
          <p className="text-gray-500">
            {uploading
              ? 'Plik jest przesyłany i przetwarzany'
              : 'Analizujemy kompletność, zgodność z normami i specyfikację techniczną'}
          </p>
          {document && (
            <p className="mt-4 text-sm text-gray-400">Plik: {document.filename}</p>
          )}
        </div>
      )}

      {/* Step 2: Results */}
      {currentStep === 2 && verification && (
        <div>
          <div className="mb-6 flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-500">Plik: {document?.filename}</p>
            </div>
            <button
              onClick={() => {
                if (downloadToken) {
                  setCurrentStep(3);
                } else {
                  setShowEmailModal(true);
                }
              }}
              className="px-6 py-2.5 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium"
            >
              Pobierz raport PDF
            </button>
          </div>

          <VerificationScoreCard result={verification} />
        </div>
      )}

      {/* Step 3: Download */}
      {currentStep === 3 && (
        <div className="bg-white border rounded-2xl p-12 text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Raport jest gotowy do pobrania</h2>
          <p className="text-gray-500 mb-6">Kliknij przycisk poniżej, aby pobrać raport weryfikacji w formacie PDF</p>
          <button
            onClick={handleDownload}
            className="px-8 py-3 bg-blue-600 text-white rounded-xl hover:bg-blue-700 transition-colors font-medium text-lg"
          >
            Pobierz PDF
          </button>
          <div className="mt-8 pt-6 border-t">
            <button
              onClick={() => {
                setCurrentStep(0);
              }}
              className="text-blue-600 hover:text-blue-800 font-medium"
            >
              Weryfikuj kolejny dokument
            </button>
          </div>
        </div>
      )}

      {/* Email Gate Modal */}
      <EmailGateModal
        isOpen={showEmailModal}
        onClose={() => setShowEmailModal(false)}
        onSubmit={handleEmailSubmit}
        submitting={submitting}
      />
    </div>
  );
};

export default VerifyOPZPage;
