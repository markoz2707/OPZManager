import React from 'react';
import { Link, useLocation } from 'react-router-dom';

const PublicHeader: React.FC = () => {
  const location = useLocation();

  const navItems = [
    { path: '/', label: 'Strona główna' },
    { path: '/verify', label: 'Weryfikuj OPZ' },
    { path: '/generate', label: 'Generuj OPZ' },
  ];

  return (
    <header className="bg-white border-b border-gray-200 sticky top-0 z-40">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          <Link to="/" className="flex items-center gap-2">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-sm">OPZ</span>
            </div>
            <span className="text-lg font-bold text-gray-900">OPZManager</span>
          </Link>

          <nav className="hidden md:flex items-center gap-1">
            {navItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                  location.pathname === item.path
                    ? 'bg-blue-50 text-blue-700'
                    : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
                }`}
              >
                {item.label}
              </Link>
            ))}
          </nav>

          <Link
            to="/login"
            className="text-sm text-gray-500 hover:text-gray-700 transition-colors"
          >
            Panel administracyjny
          </Link>
        </div>
      </div>
    </header>
  );
};

export default PublicHeader;
