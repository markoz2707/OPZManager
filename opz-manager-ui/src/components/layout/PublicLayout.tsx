import React from 'react';
import { Outlet } from 'react-router-dom';
import PublicHeader from './PublicHeader';

const PublicLayout: React.FC = () => {
  return (
    <div className="min-h-screen bg-gray-50">
      <PublicHeader />
      <main>
        <Outlet />
      </main>
      <footer className="bg-white border-t border-gray-200 mt-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="flex flex-col md:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-2">
              <div className="w-6 h-6 bg-blue-600 rounded flex items-center justify-center">
                <span className="text-white font-bold text-xs">OPZ</span>
              </div>
              <span className="text-sm text-gray-500">OPZManager - Weryfikacja i generowanie dokumentów OPZ</span>
            </div>
            <div className="text-sm text-gray-400">
              &copy; {new Date().getFullYear()} OPZManager. Wszelkie prawa zastrzeżone.
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default PublicLayout;
