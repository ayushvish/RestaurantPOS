import React, { useEffect, useState } from 'react';
import { usePosStore } from '../store/posStore';
import { Utensils, LayoutGrid, ChefHat, BarChart3, Package, Bell, RefreshCw, Plus, X, Users } from 'lucide-react';

export const Header: React.FC = () => {
  const { 
    activeTab, 
    setActiveTab, 
    isConnectedToApi, 
    fetchInitialData, 
    userRole,
    fullName,
    logout,
    allRestaurants,
    fetchAllRestaurants,
    switchContext,
    restaurantName,
    outletName
  } = usePosStore();

  const [showAddModal, setShowAddModal] = useState(false);
  const [newRestName, setNewRestName] = useState('');
  const [newTagline, setNewTagline] = useState('');
  const [newOutletName, setNewOutletName] = useState('');
  const [newFloorName, setNewFloorName] = useState('');

  useEffect(() => {
    if (userRole === 'SuperAdmin' || userRole === 'RestaurantUser') {
      fetchAllRestaurants();
    }
  }, [userRole, fetchAllRestaurants]);

  const showTables = userRole !== 'InventoryUser';
  const showKds = userRole !== 'InventoryUser';
  const showAnalytics = true;

  const currentVal = `${usePosStore.getState().restaurantId || ''}:${usePosStore.getState().outletId || ''}`;

  return (
    <header className="h-16 bg-slate-950/80 backdrop-blur-md border-b border-slate-900 px-6 flex items-center justify-between relative z-30">
      <div className="flex items-center gap-6">
        <div className="flex items-center gap-3">
          <img src="/logo.jpg" alt="GustoFlow Logo" className="h-10 w-10 rounded-xl object-cover shadow-lg shadow-orange-600/15 hover:scale-105 transition" />
          {(userRole === 'SuperAdmin' || userRole === 'RestaurantUser') ? (
            <div className="flex items-end gap-2">
              <div className="flex flex-col gap-0.5 text-left">
                <span className="text-[9px] font-bold text-orange-400 uppercase tracking-widest">Restaurant Control</span>
                <select
                  value={currentVal}
                  onChange={(e) => {
                    const [rId, oId] = e.target.value.split(':');
                    switchContext(rId, oId);
                  }}
                  className="bg-slate-900/90 border border-slate-800/80 text-xs font-semibold text-slate-200 rounded-xl px-2.5 py-1.5 outline-none focus:border-orange-500 cursor-pointer transition-all duration-200"
                >
                  {allRestaurants.map((r) =>
                    r.outlets.map((o: any) => (
                      <option key={o.id} value={`${r.id}:${o.id}`}>
                        {r.name} • {o.name}
                      </option>
                    ))
                  )}
                </select>
              </div>
              <button
                onClick={() => setShowAddModal(true)}
                className="bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-400 hover:to-orange-500 text-white font-bold p-1.5 rounded-xl text-xs flex items-center justify-center cursor-pointer h-8 w-8 btn-interactive shadow-md shadow-orange-500/10"
                title="Add New Restaurant"
              >
                <Plus className="w-4 h-4" />
              </button>
            </div>
          ) : (
            <div>
              <h1 className="text-sm font-bold tracking-tight text-white m-0 p-0 leading-tight">GustoFlow</h1>
              <p className="text-[10px] text-orange-400 font-medium">
                {restaurantName || 'Restaurant'} • {outletName || 'Main Outlet'}
              </p>
            </div>
          )}
        </div>

        <nav className="flex items-center gap-1.5 bg-slate-900/60 p-1 rounded-2xl border border-slate-800/60">
          {userRole !== 'SuperAdmin' && showTables && (
            <button
              onClick={() => setActiveTab('tables')}
              className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-bold transition-all duration-200 btn-interactive ${
                activeTab === 'tables'
                  ? 'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-md shadow-orange-500/10 border border-orange-400/10'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/40'
              }`}
            >
              <LayoutGrid className="w-3.5 h-3.5" />
              Tables / Dine-In
            </button>
          )}

          {userRole !== 'SuperAdmin' && (
            <button
              onClick={() => setActiveTab('billing')}
              className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-bold transition-all duration-200 btn-interactive ${
                activeTab === 'billing'
                  ? 'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-md shadow-orange-500/10 border border-orange-400/10'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/40'
              }`}
            >
              <Utensils className="w-3.5 h-3.5" />
              POS Terminal
            </button>
          )}

          {userRole !== 'SuperAdmin' && showKds && (
            <button
              onClick={() => setActiveTab('kds')}
              className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-bold transition-all duration-200 btn-interactive ${
                activeTab === 'kds'
                  ? 'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-md shadow-orange-500/10 border border-orange-400/10'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/40'
              }`}
            >
              <ChefHat className="w-3.5 h-3.5" />
              Kitchen Display (KDS)
            </button>
          )}

          {userRole !== 'SuperAdmin' && (
            <button
              onClick={() => setActiveTab('inventory')}
              className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-bold transition-all duration-200 btn-interactive ${
                activeTab === 'inventory'
                  ? 'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-md shadow-orange-500/10 border border-orange-400/10'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/40'
              }`}
            >
              <Package className="w-3.5 h-3.5" />
              Inventory & Stock
            </button>
          )}

          {userRole !== 'SuperAdmin' && showAnalytics && (
            <button
              onClick={() => setActiveTab('dashboard')}
              className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-bold transition-all duration-200 btn-interactive ${
                activeTab === 'dashboard'
                  ? 'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-md shadow-orange-500/10 border border-orange-400/10'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/40'
              }`}
            >
              <BarChart3 className="w-3.5 h-3.5" />
              Live Analytics
            </button>
          )}

          {(userRole === 'SuperAdmin' || userRole === 'RestaurantUser') && (
            <button
              onClick={() => setActiveTab('users')}
              className={`flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-bold transition-all duration-200 btn-interactive ${
                activeTab === 'users'
                  ? 'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-md shadow-orange-500/10 border border-orange-400/10'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/40'
              }`}
            >
              <Users className="w-3.5 h-3.5" />
              Users
            </button>
          )}

        </nav>
      </div>

      <div className="flex items-center gap-4">
        <div className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-full bg-slate-900 border border-slate-800/80 text-[10px] shadow-sm">
          <span className={`w-2 h-2 rounded-full ${isConnectedToApi ? 'bg-emerald-500 shadow-sm shadow-emerald-500' : 'bg-amber-500 animate-pulse'}`}></span>
          <span className="text-slate-300 font-bold tracking-wider">
            {isConnectedToApi ? 'POSTGRESQL API' : 'OFFLINE MODE'}
          </span>
        </div>

        <button 
          onClick={() => fetchInitialData()}
          title="Refresh Data from Backend"
          className="p-2 rounded-xl bg-slate-900 border border-slate-800 hover:border-slate-700 text-slate-400 hover:text-white transition duration-200 btn-interactive"
        >
          <RefreshCw className="w-4 h-4" />
        </button>

        <button className="relative p-2 rounded-xl bg-slate-900 border border-slate-800 hover:border-slate-700 text-slate-400 hover:text-white transition duration-200 btn-interactive">
          <Bell className="w-4 h-4" />
          <span className="absolute top-1 right-1 w-2 h-2 rounded-full bg-orange-500 animate-pulse"></span>
        </button>

        <div className="flex items-center gap-3 pl-4 border-l border-slate-900">
          <div className="h-9 w-9 rounded-full bg-gradient-to-br from-slate-800 to-slate-900 border border-slate-800 flex items-center justify-center font-bold text-sm text-slate-200 uppercase">
            {fullName ? fullName.substring(0, 2) : 'US'}
          </div>
          <div className="text-left mr-2">
            <p className="text-xs font-semibold text-white m-0 leading-tight">{fullName || 'User'}</p>
            <span className="text-[9px] text-emerald-400 font-bold uppercase tracking-wider">{userRole?.replace(/([A-Z])/g, ' $1').trim() || 'Role'}</span>
          </div>
          <button
            onClick={() => logout()}
            className="px-3 py-1.5 bg-slate-900/60 hover:bg-rose-950/40 border border-slate-850 hover:border-rose-900/60 rounded-xl text-xs font-bold text-slate-400 hover:text-rose-400 transition duration-200 cursor-pointer btn-interactive"
          >
            Logout
          </button>
        </div>
      </div>

      {showAddModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="glass-panel glass-panel-glow rounded-3xl w-full max-w-md p-6 space-y-4 shadow-2xl">
            <div className="flex items-center justify-between border-b border-slate-900 pb-3">
              <h3 className="text-sm font-bold text-white uppercase tracking-wider">Add New Restaurant</h3>
              <button 
                onClick={() => setShowAddModal(false)} 
                className="text-slate-400 hover:text-white transition duration-200 btn-interactive"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="space-y-3">
              <div>
                <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1 text-left">
                  Restaurant Name *
                </label>
                <input
                  type="text"
                  placeholder="e.g. GustoFlow Sizzlers"
                  value={newRestName}
                  onChange={(e) => setNewRestName(e.target.value)}
                  className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                />
              </div>

              <div>
                <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1 text-left">
                  Tagline (Optional)
                </label>
                <input
                  type="text"
                  placeholder="e.g. Savor the Flavor"
                  value={newTagline}
                  onChange={(e) => setNewTagline(e.target.value)}
                  className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                />
              </div>

              <div>
                <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1 text-left">
                  Main Outlet Name *
                </label>
                <input
                  type="text"
                  placeholder="e.g. Outlet 1"
                  value={newOutletName}
                  onChange={(e) => setNewOutletName(e.target.value)}
                  className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                />
              </div>

              <div>
                <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1 text-left">
                  Default Floor Section Name
                </label>
                <input
                  type="text"
                  placeholder="e.g. Ground Floor"
                  value={newFloorName}
                  onChange={(e) => setNewFloorName(e.target.value)}
                  className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                />
              </div>
            </div>

            <div className="flex gap-3 pt-2 border-t border-slate-900">
              <button
                onClick={() => setShowAddModal(false)}
                className="flex-1 bg-slate-950/60 hover:bg-slate-900 border border-slate-850 text-slate-400 font-bold py-2.5 rounded-xl text-xs transition duration-200 btn-interactive"
              >
                Cancel
              </button>
              <button
                onClick={async () => {
                  if (!newRestName.trim()) return;
                  try {
                    await usePosStore.getState().addRestaurant(
                      newRestName, 
                      newTagline, 
                      newOutletName || "Outlet 1", 
                      newFloorName || "Ground Floor"
                    );
                    setShowAddModal(false);
                    setNewRestName('');
                    setNewTagline('');
                    setNewOutletName('');
                    setNewFloorName('');
                  } catch (e) {
                    alert("Failed to add restaurant.");
                  }
                }}
                className="flex-1 bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-400 hover:to-orange-500 text-white font-bold py-2.5 rounded-xl text-xs transition duration-200 btn-interactive shadow-lg shadow-orange-500/10"
              >
                Create Restaurant
              </button>
            </div>
          </div>
        </div>
      )}

    </header>
  );
};

