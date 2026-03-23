import React, { useState, useEffect } from 'react';
import api, { User } from '../../services/api';
import toast from 'react-hot-toast';

interface UserExtended extends User {
  fullName?: string;
  company?: string;
  phone?: string;
  createdAt: string;
}

const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<UserExtended[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const response = await api.get('/auth/users');
      setUsers(response.data);
    } catch {
      toast.error('Nie udało się załadować użytkowników.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchUsers(); }, []);

  const updateRole = async (userId: number, newRole: string) => {
    try {
      await api.put(`/auth/users/${userId}/role`, { role: newRole });
      toast.success('Rola została zmieniona.');
      fetchUsers();
    } catch {
      toast.error('Nie udało się zmienić roli.');
    }
  };

  const deleteUser = async (userId: number, username: string) => {
    if (!window.confirm(`Czy na pewno chcesz usunąć użytkownika "${username}"?`)) return;
    try {
      await api.delete(`/auth/users/${userId}`);
      toast.success('Użytkownik został usunięty.');
      fetchUsers();
    } catch {
      toast.error('Nie udało się usunąć użytkownika.');
    }
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Użytkownicy</h1>
        <p className="text-sm text-gray-500">{users.length} użytkowników</p>
      </div>

      <div className="bg-white rounded-lg shadow-sm border overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Użytkownik</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Firma</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Rola</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Data rejestracji</th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Akcje</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {loading ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-500">Ładowanie...</td></tr>
            ) : users.map(user => (
              <tr key={user.id} className="hover:bg-gray-50">
                <td className="px-4 py-3">
                  <div className="text-sm font-medium text-gray-900">{user.username}</div>
                  {user.fullName && <div className="text-xs text-gray-500">{user.fullName}</div>}
                </td>
                <td className="px-4 py-3 text-sm text-gray-600">{user.email}</td>
                <td className="px-4 py-3 text-sm text-gray-600">{user.company || '-'}</td>
                <td className="px-4 py-3">
                  <select value={user.role} onChange={e => updateRole(user.id, e.target.value)}
                    className="text-sm border rounded px-2 py-1">
                    <option value="User">User</option>
                    <option value="Admin">Admin</option>
                  </select>
                </td>
                <td className="px-4 py-3 text-sm text-gray-500">
                  {new Date(user.createdAt).toLocaleDateString('pl-PL')}
                </td>
                <td className="px-4 py-3">
                  <button onClick={() => deleteUser(user.id, user.username)}
                    className="text-sm text-red-600 hover:text-red-800">
                    Usuń
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default UsersPage;
