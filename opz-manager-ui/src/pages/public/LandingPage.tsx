import React from 'react';
import { Link } from 'react-router-dom';

const LandingPage: React.FC = () => {
  return (
    <div>
      {/* Hero Section */}
      <section className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-800 text-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 md:py-28">
          <div className="text-center max-w-3xl mx-auto">
            <h1 className="text-4xl md:text-5xl font-bold leading-tight mb-6">
              Weryfikacja i generowanie dokumentów OPZ
            </h1>
            <p className="text-lg md:text-xl text-blue-100 mb-10">
              Sprawdź jakość swojego OPZ lub wygeneruj nowy dokument
              dopasowany do wybranego sprzętu IT. Bez rejestracji, za darmo.
            </p>
            <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
              <Link
                to="/verify"
                className="w-full sm:w-auto px-8 py-4 bg-white text-blue-700 rounded-xl font-semibold text-lg hover:bg-blue-50 transition-colors shadow-lg"
              >
                Weryfikuj istniejący OPZ
              </Link>
              <Link
                to="/generate"
                className="w-full sm:w-auto px-8 py-4 bg-blue-500 bg-opacity-30 text-white border-2 border-white border-opacity-30 rounded-xl font-semibold text-lg hover:bg-opacity-40 transition-colors"
              >
                Generuj nowy OPZ
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-20">
        <h2 className="text-3xl font-bold text-gray-900 text-center mb-12">Co oferujemy?</h2>
        <div className="grid md:grid-cols-3 gap-8">
          <FeatureCard
            icon={
              <svg className="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            }
            title="Weryfikacja OPZ"
            description="Prześlij dokument PDF i otrzymaj szczegółową analizę jakości: kompletność, zgodność z normami, specyfikacja techniczna i analiza braków."
          />
          <FeatureCard
            icon={
              <svg className="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
            }
            title="Generowanie OPZ"
            description="Wybierz typ sprzętu i modele, a system wygeneruje profesjonalny dokument OPZ zgodny z wymogami Prawa Zamówień Publicznych."
          />
          <FeatureCard
            icon={
              <svg className="w-8 h-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
            }
            title="Dopasowanie sprzętu"
            description="Automatyczne porównanie wymagań OPZ z katalogiem sprzętu IT od czołowych producentów: DELL, HPE, IBM."
          />
        </div>
      </section>

      {/* How it works */}
      <section className="bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-20">
          <h2 className="text-3xl font-bold text-gray-900 text-center mb-12">Jak to działa?</h2>
          <div className="grid md:grid-cols-2 gap-12">
            {/* Verify flow */}
            <div className="bg-blue-50 rounded-2xl p-8">
              <h3 className="text-xl font-bold text-blue-800 mb-6">Weryfikacja OPZ</h3>
              <div className="space-y-4">
                <StepItem number={1} text="Prześlij dokument OPZ w formacie PDF" />
                <StepItem number={2} text="System automatycznie analizuje dokument" />
                <StepItem number={3} text="Otrzymaj ocenę A-F z szczegółowym raportem" />
                <StepItem number={4} text="Pobierz raport weryfikacji jako PDF" />
              </div>
            </div>
            {/* Generate flow */}
            <div className="bg-indigo-50 rounded-2xl p-8">
              <h3 className="text-xl font-bold text-indigo-800 mb-6">Generowanie OPZ</h3>
              <div className="space-y-4">
                <StepItem number={1} text="Wybierz typ sprzętu IT" color="indigo" />
                <StepItem number={2} text="Zaznacz modele sprzętu do uwzględnienia" color="indigo" />
                <StepItem number={3} text="Przejrzyj i edytuj wygenerowaną treść" color="indigo" />
                <StepItem number={4} text="Podaj email i pobierz gotowy PDF" color="indigo" />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <div className="bg-gradient-to-r from-blue-600 to-indigo-700 rounded-2xl p-10 text-center text-white">
          <h2 className="text-2xl md:text-3xl font-bold mb-4">Gotowy, aby sprawdzić swój OPZ?</h2>
          <p className="text-blue-100 mb-8 max-w-xl mx-auto">
            Rozpocznij za darmo - bez rejestracji, bez zobowiązań.
            Prześlij dokument i otrzymaj wyniki w kilka sekund.
          </p>
          <Link
            to="/verify"
            className="inline-block px-8 py-4 bg-white text-blue-700 rounded-xl font-semibold text-lg hover:bg-blue-50 transition-colors shadow-lg"
          >
            Rozpocznij weryfikację
          </Link>
        </div>
      </section>
    </div>
  );
};

const FeatureCard: React.FC<{ icon: React.ReactNode; title: string; description: string }> = ({
  icon, title, description,
}) => (
  <div className="bg-white border border-gray-200 rounded-2xl p-6 hover:shadow-lg transition-shadow">
    <div className="w-14 h-14 bg-blue-100 rounded-xl flex items-center justify-center text-blue-600 mb-4">
      {icon}
    </div>
    <h3 className="text-lg font-bold text-gray-900 mb-2">{title}</h3>
    <p className="text-gray-600 text-sm leading-relaxed">{description}</p>
  </div>
);

const StepItem: React.FC<{ number: number; text: string; color?: string }> = ({ number, text, color = 'blue' }) => (
  <div className="flex items-center gap-4">
    <div className={`w-8 h-8 bg-${color}-600 text-white rounded-full flex items-center justify-center text-sm font-bold flex-shrink-0`}>
      {number}
    </div>
    <p className="text-gray-700">{text}</p>
  </div>
);

export default LandingPage;
