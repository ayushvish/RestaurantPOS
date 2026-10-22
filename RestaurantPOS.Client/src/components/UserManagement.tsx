import React, { useEffect, useState } from 'react';
import { usePosStore } from '../store/posStore';
import { UserPlus, Shield, Key, Loader2, AlertCircle } from 'lucide-react';

export const UserManagement: React.FC = () => {
  const { users, fetchUsers, createUser, restaurantId, userRole } = usePosStore();
  const [isLoading, setIsLoading] = useState(false);
  const [showAddModal, setShowAddModal] = useState(false);
  
  // Create user form state
  const [fullName, setFullName] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [pin, setPin] = useState('1111');
  const [role, setRole] = useState<number>(2); // Default to RestaurantUser (2)
  
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let ignore = false;
    if (restaurantId || userRole === 'SuperAdmin') {
      void Promise.resolve().then(() => {
        if (!ignore) setIsLoading(true);
      });
      fetchUsers().finally(() => {
        if (!ignore) setIsLoading(false);
      });
    }
    return () => {
      ignore = true;
    };
  }, [restaurantId, userRole, fetchUsers]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!fullName || !username || !password) {
      setError('Please fill in all required fields.');
      return;
    }
    
    setIsLoading(true);
    try {
      await createUser(fullName, username, password, role, pin);
      setShowAddModal(false);
      setFullName('');
      setUsername('');
      setPassword('');
      setPin('1111');
      setRole(3);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to create user account. Username may exist.');
    } finally {
      setIsLoading(false);
    }
  };

  const getRoleBadgeClass = (roleStr: string) => {
    switch (roleStr) {
      case 'SuperAdmin':
        return 'bg-red-500/10 border-red-500/20 text-red-400';
      case 'RestaurantUser':
        return 'bg-indigo-500/10 border-indigo-500/20 text-indigo-400';
      case 'Manager':
        return 'bg-amber-500/10 border-amber-500/20 text-amber-400';
      case 'Cashier':
        return 'bg-orange-500/10 border-orange-500/20 text-orange-400';
      case 'Waiter':
        return 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400';
      case 'Chef':
        return 'bg-blue-500/10 border-blue-500/20 text-blue-400';
      case 'InventoryUser':
        return 'bg-purple-500/10 border-purple-500/20 text-purple-400';
      default:
        return 'bg-slate-500/10 border-slate-500/20 text-slate-400';
    }
  };

  return (
    <div className="flex-1 flex flex-col bg-slate-950 p-6 overflow-y-auto select-none">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h2 className="text-xl font-black text-white flex items-center gap-2">
            <Shield className="w-5 h-5 text-orange-500" />
            User Accounts & Permission Roles
          </h2>
          <p className="text-slate-500 text-xs mt-1">Create restaurant users, inventory users, and terminal staff credentials.</p>
        </div>

        <button
          onClick={() => setShowAddModal(true)}
          className="flex items-center gap-2 px-4 py-2 bg-orange-600 hover:bg-orange-700 text-white rounded-xl text-xs font-bold transition shadow-lg shadow-orange-600/10 cursor-pointer btn-interactive"
        >
          <UserPlus className="w-4 h-4" />
          Add User Account
        </button>
      </div>

      {isLoading && users.length === 0 ? (
        <div className="flex-1 flex flex-col items-center justify-center gap-2 text-slate-500 text-sm">
          <Loader2 className="w-6 h-6 animate-spin text-orange-500" />
          Loading user records...
        </div>
      ) : (
        <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-slate-800 text-slate-400 text-[10px] font-bold uppercase tracking-wider bg-slate-900/60">
                <th className="px-6 py-4">Full Name</th>
                <th className="px-6 py-4">Username</th>
                {userRole === 'SuperAdmin' && <th className="px-6 py-4">Restaurant</th>}
                <th className="px-6 py-4">Role Badge</th>
                <th className="px-6 py-4">Terminal PIN</th>
                <th className="px-6 py-4">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60 text-xs">
              {users.length === 0 ? (
                <tr>
                  <td colSpan={userRole === 'SuperAdmin' ? 6 : 5} className="px-6 py-12 text-center text-slate-500">
                    <p className="font-semibold text-sm text-slate-400">No users found</p>
                    <p className="text-xs text-slate-500 mt-1">Create a restaurant using the "+" button in the top bar or add a user account.</p>
                  </td>
                </tr>
              ) : (
                users.map((u) => (
                  <tr key={u.id} className="hover:bg-slate-800/30 text-slate-300">
                    <td className="px-6 py-4 font-bold text-white">{u.fullName}</td>
                    <td className="px-6 py-4 font-medium text-slate-400">{u.username}</td>
                    {userRole === 'SuperAdmin' && (
                      <td className="px-6 py-4 font-semibold text-orange-400">{u.restaurantName || 'Platform'}</td>
                    )}
                    <td className="px-6 py-4">
                      <span className={`px-2.5 py-1 border rounded-full text-[10px] font-semibold uppercase tracking-wider ${getRoleBadgeClass(u.role)}`}>
                        {u.role.replace(/([A-Z])/g, ' $1').trim()}
                      </span>
                    </td>
                    <td className="px-6 py-4 font-mono font-bold text-slate-400 flex items-center gap-1.5">
                      <Key className="w-3 h-3 text-slate-600" />
                      {u.pin || 'None'}
                    </td>
                    <td className="px-6 py-4">
                      <span className="flex items-center gap-1.5 text-emerald-400 font-bold">
                        <span className="w-1.5 h-1.5 bg-emerald-500 rounded-full"></span>
                        Active
                      </span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {showAddModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-3xl w-full max-w-md p-6 space-y-5 shadow-2xl relative">
            <div className="flex justify-between items-center border-b border-slate-800 pb-3">
              <h3 className="text-sm font-black text-white flex items-center gap-1.5">
                <UserPlus className="w-4 h-4 text-orange-500" />
                Register New User Account
              </h3>
              <button 
                onClick={() => setShowAddModal(false)}
                className="text-xs text-slate-400 hover:text-white cursor-pointer"
              >
                ✕
              </button>
            </div>

            {error && (
              <div className="flex items-center gap-2 bg-rose-500/10 border border-rose-500/20 text-rose-400 p-3 rounded-xl text-xs font-semibold">
                <AlertCircle className="w-4 h-4" />
                <span>{error}</span>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-[10px] font-bold text-slate-400 mb-1 uppercase tracking-wider">Full Name *</label>
                <input
                  type="text"
                  placeholder="e.g. Samuel Jackson"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-orange-500"
                  required
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-[10px] font-bold text-slate-400 mb-1 uppercase tracking-wider">Username *</label>
                  <input
                    type="text"
                    placeholder="e.g. sam123"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-orange-500"
                    required
                  />
                </div>
                <div>
                  <label className="block text-[10px] font-bold text-slate-400 mb-1 uppercase tracking-wider">Password *</label>
                  <input
                    type="password"
                    placeholder="••••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-orange-500"
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-[10px] font-bold text-slate-400 mb-1 uppercase tracking-wider">Role *</label>
                  <select
                    value={role}
                    onChange={(e) => setRole(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-slate-300 focus:outline-none focus:border-orange-500"
                  >
                    {userRole === 'SuperAdmin' && <option value={2}>Restaurant User</option>}
                    <option value={7}>Inventory User</option>
                    <option value={3}>Manager</option>
                    <option value={4}>Cashier</option>
                    <option value={5}>Waiter</option>
                    <option value={6}>Chef</option>
                  </select>
                </div>
                <div>
                  <label className="block text-[10px] font-bold text-slate-400 mb-1 uppercase tracking-wider">Quick PIN *</label>
                  <input
                    type="text"
                    maxLength={4}
                    value={pin}
                    onChange={(e) => setPin(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-orange-500 text-center font-mono font-bold"
                    required
                  />
                </div>
              </div>

              <div className="flex gap-3 pt-3 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setShowAddModal(false)}
                  className="flex-1 bg-slate-950 hover:bg-slate-800 border border-slate-800 text-slate-400 font-bold py-2 rounded-xl text-xs transition cursor-pointer"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isLoading}
                  className="flex-1 bg-orange-600 hover:bg-orange-700 text-white font-bold py-2 rounded-xl text-xs transition cursor-pointer flex items-center justify-center gap-1.5"
                >
                  {isLoading ? 'Creating...' : 'Register User'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
