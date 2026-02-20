import React from 'react';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  message?: string;
}

const sizeClasses = {
  sm: 'h-6 w-6',
  md: 'h-10 w-10',
  lg: 'h-16 w-16',
};

const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ size = 'md', message }) => (
  <div className="flex flex-col items-center justify-center py-12">
    <div className={`animate-spin rounded-full border-b-2 border-indigo-600 ${sizeClasses[size]}`} />
    {message && <p className="mt-4 text-sm text-gray-500">{message}</p>}
  </div>
);

export default LoadingSpinner;
