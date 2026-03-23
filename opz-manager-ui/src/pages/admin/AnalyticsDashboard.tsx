import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import api from '../../services/api';

interface DashboardStats {
  totalDocuments: number;
  documentsLast30Days: number;
  totalVerifications: number;
  verificationsLast30Days: number;
  totalUsers: number;
  usersLast30Days: number;
  totalLeads: number;
  leadsLast30Days: number;
  conversionRate: number;
  totalEquipmentModels: number;
  totalKnowledgeDocs: number;
}

interface ActivityData {
  date: string;
  documents: number;
  verifications: number;
  registrations: number;
  leads: number;
}

interface PopularEquipment {
  modelId: number;
  modelName: string;
  manufacturerName: string;
  typeName: string;
  matchCount: number;
}

const StatCard: React.FC<{ label: string; value: number | string; sub?: string; color?: string }> = ({ label, value, sub, color = 'blue' }) => (
  <div className="bg-white rounded-lg shadow-sm border p-4">
    <p className="text-sm text-gray-500">{label}</p>
    <p className={`text-2xl font-bold text-${color}-600 mt-1`}>{value}</p>
    {sub && <p className="text-xs text-gray-400 mt-1">{sub}</p>}
  </div>
);

const AnalyticsDashboard: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [activity, setActivity] = useState<ActivityData[]>([]);
  const [popular, setPopular] = useState<PopularEquipment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [statsRes, activityRes, popularRes] = await Promise.all([
          api.get('/analytics/dashboard'),
          api.get('/analytics/activity?days=30'),
          api.get('/analytics/popular-equipment?limit=5'),
        ]);
        setStats(statsRes.data);
        setActivity(activityRes.data);
        setPopular(popularRes.data);
      } catch (err) {
        console.error('Failed to load analytics', err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  if (loading) return <div className="p-8 text-center text-gray-500">Ładowanie analityki...</div>;
  if (!stats) return <div className="p-8 text-center text-red-500">Nie udało się załadować danych.</div>;

  const maxActivity = Math.max(...activity.map(a => a.documents + a.verifications + a.leads), 1);

  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Panel analityczny</h1>

      {/* Stats grid */}
      <div className="grid grid-cols-2 lg:grid-cols-4 xl:grid-cols-6 gap-4">
        <StatCard label="Dokumenty OPZ" value={stats.totalDocuments} sub={`+${stats.documentsLast30Days} (30 dni)`} />
        <StatCard label="Weryfikacje" value={stats.totalVerifications} sub={`+${stats.verificationsLast30Days} (30 dni)`} color="green" />
        <StatCard label="Użytkownicy" value={stats.totalUsers} sub={`+${stats.usersLast30Days} (30 dni)`} color="indigo" />
        <StatCard label="Leady" value={stats.totalLeads} sub={`+${stats.leadsLast30Days} (30 dni)`} color="yellow" />
        <StatCard label="Konwersja" value={`${stats.conversionRate}%`} color="green" />
        <StatCard label="Modele sprzętu" value={stats.totalEquipmentModels} sub={`${stats.totalKnowledgeDocs} dokumentów KB`} color="gray" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Activity chart (simple bar) */}
        <div className="bg-white rounded-lg shadow-sm border p-4">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Aktywność (ostatnie 30 dni)</h2>
          <div className="space-y-1">
            {activity.slice(-14).map(day => {
              const total = day.documents + day.verifications + day.leads;
              const width = Math.max((total / maxActivity) * 100, 2);
              return (
                <div key={day.date} className="flex items-center gap-2 text-xs">
                  <span className="w-16 text-gray-500 text-right">{day.date.slice(5)}</span>
                  <div className="flex-1 bg-gray-100 rounded h-4 overflow-hidden">
                    <div className="h-full bg-indigo-500 rounded" style={{ width: `${width}%` }}
                      title={`Dokumenty: ${day.documents}, Weryfikacje: ${day.verifications}, Leady: ${day.leads}`} />
                  </div>
                  <span className="w-8 text-gray-600">{total}</span>
                </div>
              );
            })}
          </div>
        </div>

        {/* Popular equipment */}
        <div className="bg-white rounded-lg shadow-sm border p-4">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Najpopularniejsze modele</h2>
          {popular.length === 0 ? (
            <p className="text-gray-500 text-sm">Brak danych o dopasowaniach.</p>
          ) : (
            <div className="space-y-3">
              {popular.map((eq, i) => (
                <div key={eq.modelId} className="flex items-center gap-3">
                  <span className="text-lg font-bold text-gray-300 w-6">{i + 1}</span>
                  <div className="flex-1">
                    <Link to={`/admin/equipment/${eq.modelId}`} className="text-sm font-medium text-gray-900 hover:text-indigo-600">
                      {eq.modelName}
                    </Link>
                    <p className="text-xs text-gray-500">{eq.manufacturerName} - {eq.typeName}</p>
                  </div>
                  <span className="text-sm font-semibold text-indigo-600">{eq.matchCount} dopasowań</span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Quick links */}
      <div className="flex gap-4">
        <Link to="/admin/leads" className="text-sm text-indigo-600 hover:underline">Zobacz wszystkie leady</Link>
        <Link to="/admin/users" className="text-sm text-indigo-600 hover:underline">Zarządzaj użytkownikami</Link>
      </div>
    </div>
  );
};

export default AnalyticsDashboard;
