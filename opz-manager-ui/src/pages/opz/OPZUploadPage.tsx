import React, { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { opzAPI } from '../../services/api';
import toast from 'react-hot-toast';

const OPZUploadPage: React.FC = () => {
  const navigate = useNavigate();
  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [dragActive, setDragActive] = useState(false);

  const handleDrag = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') setDragActive(true);
    else if (e.type === 'dragleave') setDragActive(false);
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    const droppedFile = e.dataTransfer.files?.[0];
    if (droppedFile?.type === 'application/pdf') {
      setFile(droppedFile);
    } else {
      toast.error('Tylko pliki PDF są obsługiwane');
    }
  }, []);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (selected) setFile(selected);
  };

  const handleUpload = async () => {
    if (!file) return;
    setUploading(true);
    try {
      const doc = await opzAPI.uploadOPZ(file);
      toast.success('Dokument został wgrany pomyślnie');
      navigate(`/admin/opz/${doc.id}`);
    } catch {
      toast.error('Błąd podczas wgrywania dokumentu');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Wgraj dokument OPZ</h1>

      <div className="max-w-xl mx-auto">
        <div
          className={`border-2 border-dashed rounded-lg p-12 text-center transition-colors ${
            dragActive ? 'border-indigo-500 bg-indigo-50' : 'border-gray-300 hover:border-gray-400'
          }`}
          onDragEnter={handleDrag}
          onDragLeave={handleDrag}
          onDragOver={handleDrag}
          onDrop={handleDrop}
        >
          {file ? (
            <div>
              <p className="text-lg font-medium text-gray-900">{file.name}</p>
              <p className="text-sm text-gray-500 mt-1">{(file.size / 1024 / 1024).toFixed(2)} MB</p>
              <button
                onClick={() => setFile(null)}
                className="mt-3 text-sm text-red-600 hover:text-red-800"
              >
                Zmień plik
              </button>
            </div>
          ) : (
            <div>
              <p className="text-gray-600 mb-2">Przeciągnij plik PDF tutaj lub</p>
              <label className="inline-block px-4 py-2 bg-white border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50 cursor-pointer">
                Wybierz plik
                <input type="file" accept=".pdf" className="hidden" onChange={handleFileSelect} />
              </label>
              <p className="text-xs text-gray-400 mt-3">Obsługiwane formaty: PDF (max 50 MB)</p>
            </div>
          )}
        </div>

        {file && (
          <button
            onClick={handleUpload}
            disabled={uploading}
            className="mt-6 w-full px-4 py-3 bg-indigo-600 text-white font-medium rounded-lg hover:bg-indigo-700 disabled:opacity-50 transition-colors"
          >
            {uploading ? 'Wgrywanie i przetwarzanie...' : 'Wgraj i przetwórz'}
          </button>
        )}
      </div>
    </div>
  );
};

export default OPZUploadPage;
