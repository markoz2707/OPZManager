import React, { useState } from 'react';
import { z } from 'zod';

const emailSchema = z.object({
  email: z.string().min(1, 'Adres email jest wymagany').email('Podaj prawidłowy adres email'),
  marketingConsent: z.boolean(),
});

interface EmailGateModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (email: string, marketingConsent: boolean) => void;
  submitting?: boolean;
}

const EmailGateModal: React.FC<EmailGateModalProps> = ({ isOpen, onClose, onSubmit, submitting }) => {
  const [email, setEmail] = useState('');
  const [marketingConsent, setMarketingConsent] = useState(false);
  const [error, setError] = useState('');

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    const result = emailSchema.safeParse({ email, marketingConsent });
    if (!result.success) {
      setError(result.error.errors[0].message);
      return;
    }

    onSubmit(email, marketingConsent);
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl max-w-md w-full p-8">
        <div className="text-center mb-6">
          <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-gray-900">Pobierz dokument PDF</h2>
          <p className="text-sm text-gray-500 mt-2">
            Podaj adres email, aby otrzymać wygenerowany dokument OPZ
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
              Adres email
            </label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="twoj@email.pl"
              className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
              disabled={submitting}
            />
            {error && <p className="mt-1 text-sm text-red-500">{error}</p>}
          </div>

          <label className="flex items-start gap-3 cursor-pointer">
            <input
              type="checkbox"
              checked={marketingConsent}
              onChange={(e) => setMarketingConsent(e.target.checked)}
              className="mt-1 w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              disabled={submitting}
            />
            <span className="text-xs text-gray-500">
              Wyrażam zgodę na otrzymywanie informacji marketingowych na podany adres email.
              Zgoda jest dobrowolna i może być wycofana w dowolnym momencie.
            </span>
          </label>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 px-4 py-3 border border-gray-300 rounded-lg text-gray-700 hover:bg-gray-50 transition-colors"
              disabled={submitting}
            >
              Anuluj
            </button>
            <button
              type="submit"
              className="flex-1 px-4 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50"
              disabled={submitting}
            >
              {submitting ? 'Wysyłanie...' : 'Pobierz PDF'}
            </button>
          </div>
        </form>

        <p className="mt-4 text-xs text-gray-400 text-center">
          Twoje dane są przetwarzane zgodnie z polityką prywatności.
        </p>
      </div>
    </div>
  );
};

export default EmailGateModal;
