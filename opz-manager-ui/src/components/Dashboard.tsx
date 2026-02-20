import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { authAPI, opzAPI, configAPI } from '../services/api';
import { useAuth } from '../hooks/useAuth';

const Dashboard: React.FC = () => {
  const { user } = useAuth();
  const [apiStatus, setApiStatus] = useState<string>('Sprawdzanie...');
  const [docCount, setDocCount] = useState<number>(0);
  const [llmStatus, setLlmStatus] = useState<string>('Sprawdzanie...');

  useEffect(() => {
    const checkStatus = async () => {
      // Check API status
      try {
        await authAPI.test();
        setApiStatus('Działa');
      } catch {
        setApiStatus('Niedostępne');
      }

      // Check document count
      try {
        const docs = await opzAPI.getOPZDocuments();
        setDocCount(docs.length);
      } catch {
        setDocCount(0);
      }

      // Check LLM status
      try {
        const status = await configAPI.getStatus();
        setLlmStatus(status.llmConnected ? 'Połączono' : 'Brak połączenia');
      } catch {
        setLlmStatus('Nieskonfigurowany');
      }
    };

    checkStatus();
  }, []);

  const featureCards = [
    {
      title: 'Analiza OPZ',
      description: 'Prześlij dokumenty OPZ do analizy i dopasowania sprzętu',
      to: '/admin/opz',
    },
    {
      title: 'Katalog sprzętu',
      description: 'Zarządzaj katalogiem sprzętu i dokumentacją techniczną',
      to: '/admin/equipment',
    },
    {
      title: 'Generator OPZ',
      description: 'Generuj dokumenty OPZ na podstawie wybranego sprzętu',
      to: '/admin/generator',
    },
  ];

  const adminCards = [
    {
      title: 'Dane treningowe',
      description: 'Generuj i zarządzaj danymi treningowymi dla modelu AI',
      to: '/admin/training',
    },
    {
      title: 'Konfiguracja',
      description: 'Konfiguruj połączenie z modelem Pllum i inne ustawienia',
      to: '/admin/config',
    },
  ];

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-900 mb-6">Panel główny</h2>

      {/* Status Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <div className={`w-8 h-8 rounded-full flex items-center justify-center ${apiStatus === 'Działa' ? 'bg-green-500' : 'bg-red-500'}`}>
                  <span className="text-white text-sm font-medium">API</span>
                </div>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Status API</dt>
                  <dd className="text-lg font-medium text-gray-900">{apiStatus}</dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center">
                  <span className="text-white text-sm font-medium">OPZ</span>
                </div>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Dokumenty OPZ</dt>
                  <dd className="text-lg font-medium text-gray-900">{docCount}</dd>
                </dl>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white overflow-hidden shadow rounded-lg">
          <div className="p-5">
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <div className={`w-8 h-8 rounded-full flex items-center justify-center ${llmStatus === 'Połączono' ? 'bg-green-500' : 'bg-purple-500'}`}>
                  <span className="text-white text-sm font-medium">AI</span>
                </div>
              </div>
              <div className="ml-5 w-0 flex-1">
                <dl>
                  <dt className="text-sm font-medium text-gray-500 truncate">Model Pllum</dt>
                  <dd className="text-lg font-medium text-gray-900">{llmStatus}</dd>
                </dl>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Feature Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {featureCards.map((card) => (
          <div key={card.to} className="bg-white overflow-hidden shadow rounded-lg hover:shadow-md transition-shadow">
            <div className="p-6">
              <h3 className="text-lg font-medium text-gray-900 mb-2">{card.title}</h3>
              <p className="text-sm text-gray-600 mb-4">{card.description}</p>
              <Link
                to={card.to}
                className="inline-block bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-md text-sm font-medium"
              >
                Przejdź
              </Link>
            </div>
          </div>
        ))}

        {user?.role === 'Admin' &&
          adminCards.map((card) => (
            <div key={card.to} className="bg-white overflow-hidden shadow rounded-lg hover:shadow-md transition-shadow">
              <div className="p-6">
                <h3 className="text-lg font-medium text-gray-900 mb-2">{card.title}</h3>
                <p className="text-sm text-gray-600 mb-4">{card.description}</p>
                <Link
                  to={card.to}
                  className="inline-block bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-md text-sm font-medium"
                >
                  Przejdź
                </Link>
              </div>
            </div>
          ))}
      </div>
    </div>
  );
};

export default Dashboard;
