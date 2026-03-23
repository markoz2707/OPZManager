import React, { useState } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { authAPI } from '../../services/api';
import { useAuth } from '../../hooks/useAuth';

interface RegisterForm {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  fullName: string;
  company: string;
  position: string;
  phone: string;
  nip: string;
  address: string;
  marketingConsent: boolean;
  termsAccepted: boolean;
}

const RegisterPage: React.FC = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const returnTo = searchParams.get('returnTo');

  const [form, setForm] = useState<RegisterForm>({
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    fullName: '',
    company: '',
    position: '',
    phone: '',
    nip: '',
    address: '',
    marketingConsent: false,
    termsAccepted: false,
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setForm(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const validate = (): string | null => {
    if (!form.fullName.trim()) return 'Imię i nazwisko jest wymagane.';
    if (!form.company.trim()) return 'Nazwa firmy jest wymagana.';
    if (!form.email.trim()) return 'Adres email jest wymagany.';
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) return 'Nieprawidłowy format adresu email.';
    if (!form.phone.trim()) return 'Numer telefonu jest wymagany.';
    if (!form.username.trim() || form.username.length < 3) return 'Nazwa użytkownika musi mieć co najmniej 3 znaki.';
    if (form.password.length < 8) return 'Hasło musi mieć co najmniej 8 znaków.';
    if (form.password !== form.confirmPassword) return 'Hasła nie są identyczne.';
    if (!form.marketingConsent) return 'Zgoda na przetwarzanie danych jest wymagana.';
    if (!form.termsAccepted) return 'Akceptacja regulaminu jest wymagana.';
    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await authAPI.register({
        username: form.username,
        email: form.email,
        password: form.password,
        fullName: form.fullName,
        company: form.company,
        position: form.position || undefined,
        phone: form.phone,
        nip: form.nip || undefined,
        address: form.address || undefined,
        marketingConsent: form.marketingConsent,
      });
      login(response.token, response.user);
      navigate(returnTo || '/admin', { replace: true });
    } catch (err: any) {
      setError(err.response?.data?.message || 'Rejestracja nie powiodła się. Spróbuj ponownie.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-lg w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Rejestracja w OPZ Manager
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Utwórz konto, aby uzyskać pełny dostęp do systemu
          </p>
        </div>

        <form className="mt-8 space-y-4" onSubmit={handleSubmit}>
          {/* Contact info */}
          <div className="bg-white p-4 rounded-lg shadow-sm border border-gray-200 space-y-3">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Dane kontaktowe</h3>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label htmlFor="fullName" className="block text-sm font-medium text-gray-700">
                  Imię i nazwisko *
                </label>
                <input id="fullName" name="fullName" type="text" required value={form.fullName}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                />
              </div>
              <div>
                <label htmlFor="company" className="block text-sm font-medium text-gray-700">
                  Firma *
                </label>
                <input id="company" name="company" type="text" required value={form.company}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label htmlFor="email" className="block text-sm font-medium text-gray-700">
                  Email *
                </label>
                <input id="email" name="email" type="email" required value={form.email}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                />
              </div>
              <div>
                <label htmlFor="phone" className="block text-sm font-medium text-gray-700">
                  Telefon *
                </label>
                <input id="phone" name="phone" type="tel" required value={form.phone}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label htmlFor="position" className="block text-sm font-medium text-gray-700">
                  Stanowisko
                </label>
                <input id="position" name="position" type="text" value={form.position}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                />
              </div>
              <div>
                <label htmlFor="nip" className="block text-sm font-medium text-gray-700">
                  NIP
                </label>
                <input id="nip" name="nip" type="text" value={form.nip}
                  onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                />
              </div>
            </div>

            <div>
              <label htmlFor="address" className="block text-sm font-medium text-gray-700">
                Adres
              </label>
              <input id="address" name="address" type="text" value={form.address}
                onChange={handleChange}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              />
            </div>
          </div>

          {/* Account info */}
          <div className="bg-white p-4 rounded-lg shadow-sm border border-gray-200 space-y-3">
            <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Dane konta</h3>

            <div>
              <label htmlFor="username" className="block text-sm font-medium text-gray-700">
                Nazwa użytkownika *
              </label>
              <input id="username" name="username" type="text" required value={form.username}
                onChange={handleChange}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              />
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700">
                Hasło * (min. 8 znaków)
              </label>
              <div className="relative">
                <input id="password" name="password" type={showPassword ? 'text' : 'password'} required
                  value={form.password} onChange={handleChange}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                />
                <button type="button" onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-sm text-gray-500 hover:text-gray-700">
                  {showPassword ? 'Ukryj' : 'Pokaż'}
                </button>
              </div>
            </div>

            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700">
                Powtórz hasło *
              </label>
              <input id="confirmPassword" name="confirmPassword" type="password" required
                value={form.confirmPassword} onChange={handleChange}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              />
            </div>
          </div>

          {/* Consents */}
          <div className="bg-white p-4 rounded-lg shadow-sm border border-gray-200 space-y-3">
            <div className="flex items-start">
              <input id="marketingConsent" name="marketingConsent" type="checkbox"
                checked={form.marketingConsent} onChange={handleChange}
                className="h-4 w-4 text-indigo-600 border-gray-300 rounded focus:ring-indigo-500 mt-0.5"
              />
              <label htmlFor="marketingConsent" className="ml-2 text-sm text-gray-700">
                Wyrażam zgodę na przetwarzanie moich danych osobowych w celach marketingowych *
              </label>
            </div>

            <div className="flex items-start">
              <input id="termsAccepted" name="termsAccepted" type="checkbox"
                checked={form.termsAccepted} onChange={handleChange}
                className="h-4 w-4 text-indigo-600 border-gray-300 rounded focus:ring-indigo-500 mt-0.5"
              />
              <label htmlFor="termsAccepted" className="ml-2 text-sm text-gray-700">
                Akceptuję regulamin serwisu i politykę prywatności *
              </label>
            </div>
          </div>

          {error && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md text-sm">
              {error}
            </div>
          )}

          <button type="submit" disabled={loading}
            className="w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50">
            {loading ? 'Rejestracja...' : 'Zarejestruj się'}
          </button>

          <div className="text-center">
            <p className="text-sm text-gray-600">
              Masz już konto?{' '}
              <Link to="/login" className="font-medium text-indigo-600 hover:text-indigo-500">
                Zaloguj się
              </Link>
            </p>
          </div>
        </form>
      </div>
    </div>
  );
};

export default RegisterPage;
