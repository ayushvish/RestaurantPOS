import React, { useState } from 'react';
import { usePosStore } from '../store/posStore';
import { Users, Clock, PlusCircle } from 'lucide-react';
import type { RestaurantTable } from '../types/pos';

export const TableView: React.FC = () => {
  const { setSelectedTable, setActiveTab, floors } = usePosStore();
  const [selectedFloorId, setSelectedFloorId] = useState<string>(floors[0]?.id || '');

  const activeFloor = floors.find((f) => f.id === selectedFloorId) || floors[0];

  if (!activeFloor) {
    return (
      <div className="flex-1 flex flex-col items-center justify-center p-6 bg-slate-900 text-slate-400">
        <span className="text-3xl mb-2 animate-pulse">🍽️</span>
        <p className="text-sm font-medium">Loading floor layouts...</p>
      </div>
    );
  }

  const handleSelectTable = (table: RestaurantTable) => {
    setSelectedTable(table);
    setActiveTab('billing');
  };

  const getStatusColor = (status: RestaurantTable['status']) => {
    switch (status) {
      case 'Vacant':
        return 'bg-emerald-500/5 border-emerald-500/20 text-emerald-400 hover:border-emerald-400 hover:bg-emerald-500/10 hover:shadow-lg hover:shadow-emerald-500/5';
      case 'Occupied':
        return 'bg-rose-500/5 border-rose-500/20 text-rose-400 hover:border-rose-400 hover:bg-rose-500/10 hover:shadow-lg hover:shadow-rose-500/5';
      case 'KotPlaced':
        return 'bg-amber-500/10 border-amber-500/40 text-amber-300 hover:border-amber-400 hover:bg-amber-500/20 hover:shadow-lg hover:shadow-amber-500/10 animate-pulse';
      case 'Billed':
        return 'bg-sky-500/5 border-sky-500/20 text-sky-400 hover:border-sky-400 hover:bg-sky-500/10 hover:shadow-lg hover:shadow-sky-500/5';
      case 'Reserved':
        return 'bg-purple-500/5 border-purple-500/20 text-purple-400 hover:border-purple-400 hover:bg-purple-500/10 hover:shadow-lg hover:shadow-purple-500/5';
    }
  };

  const getBadgeColor = (status: RestaurantTable['status']) => {
    switch (status) {
      case 'Vacant':
        return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'Occupied':
        return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      case 'KotPlaced':
        return 'bg-amber-500/20 text-amber-300 border-amber-500/40';
      case 'Billed':
        return 'bg-sky-500/10 text-sky-400 border-sky-500/20';
      case 'Reserved':
        return 'bg-purple-500/10 text-purple-400 border-purple-500/20';
    }
  };

  return (
    <div className="flex-1 flex flex-col p-6 overflow-hidden bg-slate-950/20">
      {/* Floor Selector & Quick Stats */}
      <div className="flex items-center justify-between pb-6 border-b border-slate-900">
        <div className="flex items-center gap-2">
          {floors.map((floor) => (
            <button
              key={floor.id}
              onClick={() => setSelectedFloorId(floor.id)}
              className={`px-5 py-2.5 rounded-xl font-bold text-xs transition-all duration-200 btn-interactive ${
                (selectedFloorId === floor.id || (!selectedFloorId && floor === activeFloor))
                  ? 'bg-slate-900 text-white border border-slate-800 shadow-md shadow-slate-900/40'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/30'
              }`}
            >
              {floor.name} ({floor.tables.length})
            </button>
          ))}
          <button className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl border border-dashed border-slate-800 hover:border-slate-700 text-slate-400 hover:text-slate-200 text-xs font-bold transition duration-200 btn-interactive">
            <PlusCircle className="w-4 h-4" /> Add Section
          </button>
        </div>

        {/* Legend */}
        <div className="flex items-center gap-4 bg-slate-950/60 px-4 py-2 rounded-xl border border-slate-900 text-xs font-bold">
          <div className="flex items-center gap-2">
            <span className="w-2.5 h-2.5 rounded-full bg-emerald-500 shadow-sm shadow-emerald-500/50"></span>
            <span className="text-slate-400 font-semibold">Vacant</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="w-2.5 h-2.5 rounded-full bg-rose-500 shadow-sm shadow-rose-500/50"></span>
            <span className="text-slate-400 font-semibold">Occupied</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="w-2.5 h-2.5 rounded-full bg-amber-400 animate-ping shadow-sm shadow-amber-400/50"></span>
            <span className="text-slate-400 font-semibold">KOT Placed</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="w-2.5 h-2.5 rounded-full bg-sky-500 shadow-sm shadow-sky-500/50"></span>
            <span className="text-slate-400 font-semibold">Billed</span>
          </div>
        </div>
      </div>

      {/* Tables Grid */}
      <div className="flex-1 overflow-y-auto py-6">
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-5">
          {activeFloor.tables.map((table) => (
            <div
              key={table.id}
              onClick={() => handleSelectTable(table)}
              className={`p-5 rounded-2xl border-2 transition-all duration-200 cursor-pointer flex flex-col justify-between h-44 shadow-md backdrop-blur-md btn-interactive ${getStatusColor(
                table.status
              )}`}
            >
              <div className="flex items-center justify-between">
                <span className="text-2xl font-black tracking-tight">{table.tableNumber}</span>
                <span
                  className={`text-[10px] font-bold px-2 py-0.5 rounded-full border ${getBadgeColor(
                    table.status
                  )}`}
                >
                  {table.status}
                </span>
              </div>

              <div className="space-y-2 text-left">
                <div className="flex items-center justify-between text-[11px] text-slate-400 font-bold">
                  <div className="flex items-center gap-1">
                    <Users className="w-3.5 h-3.5" />
                    <span>{table.seatingCapacity} Seats</span>
                  </div>
                  {table.status === 'Occupied' && (
                    <div className="flex items-center gap-1 text-rose-400">
                      <Clock className="w-3.5 h-3.5" />
                      <span>24m</span>
                    </div>
                  )}
                </div>

                {table.status === 'Occupied' && (
                  <div className="pt-2 border-t border-rose-500/20 flex items-center justify-between">
                    <span className="text-[10px] font-bold text-rose-300/80">Running Total</span>
                    <span className="text-xs font-black text-rose-300">₹840.00</span>
                  </div>
                )}

                {table.status === 'KotPlaced' && (
                  <div className="pt-2 border-t border-amber-500/30 flex items-center justify-between">
                    <span className="text-[10px] font-bold text-amber-300/90 animate-pulse">🍳 In Kitchen</span>
                    <span className="text-xs font-black text-amber-300">Active KOT</span>
                  </div>
                )}

                {table.status === 'Billed' && (
                  <div className="pt-2 border-t border-sky-500/20 flex items-center justify-between">
                    <span className="text-[10px] font-bold text-sky-300/80">Bill Pending</span>
                    <span className="text-xs font-black text-sky-300">₹1,250.00</span>
                  </div>
                )}

                {table.status === 'Vacant' && (
                  <div className="pt-2 border-t border-emerald-500/20 text-center">
                    <span className="text-[10px] font-bold text-emerald-400/80">Tap to Punch Order</span>
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
