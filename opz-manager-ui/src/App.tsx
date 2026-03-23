import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './contexts/AuthContext';
import { SessionProvider } from './contexts/SessionContext';
import ErrorBoundary from './components/common/ErrorBoundary';
import ProtectedRoute from './components/auth/ProtectedRoute';
import AdminRoute from './components/auth/AdminRoute';
import AppLayout from './components/layout/AppLayout';
import PublicLayout from './components/layout/PublicLayout';
import Login from './components/Login';
import RegisterPage from './pages/auth/RegisterPage';
import LandingPage from './pages/public/LandingPage';
import VerifyOPZPage from './pages/public/VerifyOPZPage';
import GenerateOPZPage from './pages/public/GenerateOPZPage';
import CatalogPage from './pages/public/CatalogPage';
import ComparisonPage from './pages/public/ComparisonPage';
import SWZCreatorPage from './pages/public/SWZCreatorPage';
import OPZListPage from './pages/opz/OPZListPage';
import OPZUploadPage from './pages/opz/OPZUploadPage';
import OPZDetailPage from './pages/opz/OPZDetailPage';
import EquipmentCatalogPage from './pages/equipment/EquipmentCatalogPage';
import EquipmentModelDetailPage from './pages/equipment/EquipmentModelDetailPage';
import OPZGeneratorPage from './pages/generator/OPZGeneratorPage';
import AnalyticsDashboard from './pages/admin/AnalyticsDashboard';
import LeadsPage from './pages/admin/LeadsPage';
import UsersPage from './pages/admin/UsersPage';
import TrainingDataPage from './pages/admin/TrainingDataPage';
import ConfigurationPage from './pages/admin/ConfigurationPage';
import LlmLogsPage from './pages/admin/LlmLogsPage';

function App() {
  return (
    <ErrorBoundary>
      <AuthProvider>
        <SessionProvider>
          <BrowserRouter>
            <Routes>
              {/* Public routes (no auth required) */}
              <Route element={<PublicLayout />}>
                <Route path="/" element={<LandingPage />} />
                <Route path="/verify" element={<VerifyOPZPage />} />
                <Route path="/verify/:id" element={<VerifyOPZPage />} />
                <Route path="/generate" element={<GenerateOPZPage />} />
                <Route path="/catalog" element={<CatalogPage />} />
                <Route path="/catalog/compare" element={<ComparisonPage />} />
                <Route path="/swz" element={<SWZCreatorPage />} />
              </Route>

              {/* Auth */}
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<RegisterPage />} />

              {/* Protected admin routes */}
              <Route element={<ProtectedRoute />}>
                <Route element={<AppLayout />}>
                  <Route path="/admin" element={<AnalyticsDashboard />} />
                  <Route path="/admin/opz" element={<OPZListPage />} />
                  <Route path="/admin/opz/upload" element={<OPZUploadPage />} />
                  <Route path="/admin/opz/:id" element={<OPZDetailPage />} />
                  <Route path="/admin/equipment" element={<EquipmentCatalogPage />} />
                  <Route path="/admin/equipment/:id" element={<EquipmentModelDetailPage />} />
                  <Route path="/admin/generator" element={<OPZGeneratorPage />} />

                  {/* Admin-only routes */}
                  <Route element={<AdminRoute />}>
                    <Route path="/admin/leads" element={<LeadsPage />} />
                    <Route path="/admin/users" element={<UsersPage />} />
                    <Route path="/admin/training" element={<TrainingDataPage />} />
                    <Route path="/admin/config" element={<ConfigurationPage />} />
                    <Route path="/admin/llm-logs" element={<LlmLogsPage />} />
                  </Route>
                </Route>
              </Route>

              {/* Catch-all */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </BrowserRouter>
          <Toaster position="top-right" />
        </SessionProvider>
      </AuthProvider>
    </ErrorBoundary>
  );
}

export default App;
