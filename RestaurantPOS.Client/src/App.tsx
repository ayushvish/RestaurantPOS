import React, { useEffect } from 'react';
import { Header } from './components/Header';
import { TableView } from './components/TableView';
import { PosTerminal } from './components/PosTerminal';
import { KitchenDisplay } from './components/KitchenDisplay';
import { Dashboard } from './components/Dashboard';
import { Inventory } from './components/Inventory';
import { Login } from './components/Login';
import { UserManagement } from './components/UserManagement';
import { usePosStore } from './store/posStore';

export const App: React.FC = () => {
  const { activeTab, token, fetchInitialData, userRole, setActiveTab } = usePosStore();

  useEffect(() => {
    if (token) {
      fetchInitialData();
    }
  }, [token, fetchInitialData]);

  useEffect(() => {
    if (userRole === 'SuperAdmin' && activeTab === 'tables') {
      setActiveTab('users');
    }
  }, [userRole, activeTab, setActiveTab]);

  if (!token) {
    return <Login />;
  }

  return (
    <div className="h-screen w-screen flex flex-col overflow-hidden bg-slate-900 text-slate-100">
      <Header />
      <main className="flex-1 flex overflow-hidden">
        {activeTab === 'tables' && <TableView />}
        {activeTab === 'billing' && <PosTerminal />}
        {activeTab === 'kds' && <KitchenDisplay />}
        {activeTab === 'dashboard' && <Dashboard />}
        {activeTab === 'inventory' && <Inventory />}
        {activeTab === 'users' && (userRole === 'SuperAdmin' || userRole === 'RestaurantUser') && <UserManagement />}
      </main>
    </div>
  );
};

export default App;
