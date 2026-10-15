import React, { useState, useEffect } from 'react';
import { Package, AlertTriangle, Plus, RefreshCw, X } from 'lucide-react';
import { usePosStore } from '../store/posStore';

export const Inventory: React.FC = () => {
  const { rawMaterials, fetchRawMaterials, addRawMaterial, adjustStock } = usePosStore();
  
  const [showAddModal, setShowAddModal] = useState(false);
  const [newName, setNewName] = useState('');
  const [newUom, setNewUom] = useState('kg');
  const [newCost, setNewCost] = useState('0');
  const [newMin, setNewMin] = useState('0');

  const [showAdjustModal, setShowAdjustModal] = useState(false);
  const [selectedMaterial, setSelectedMaterial] = useState<any>(null);
  const [qtyChange, setQtyChange] = useState('0');
  const [adjustReason, setAdjustReason] = useState('Manual Audit');

  useEffect(() => {
    fetchRawMaterials();
  }, [fetchRawMaterials]);

  const handleAddMaterial = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName) return;
    await addRawMaterial(newName, newUom, parseFloat(newCost), parseFloat(newMin));
    setShowAddModal(false);
    setNewName('');
    setNewCost('0');
    setNewMin('0');
  };

  const handleAdjustStock = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedMaterial) return;
    await adjustStock(selectedMaterial.id, parseFloat(qtyChange), adjustReason);
    setShowAdjustModal(false);
    setQtyChange('0');
  };

  return (
    <div className="flex-1 flex flex-col p-6 overflow-y-auto bg-slate-900 space-y-6 relative">
      <div className="flex items-center justify-between pb-6 border-b border-slate-800">
        <div>
          <h2 className="text-xl font-black text-white">Raw Material & Inventory Stock</h2>
          <p className="text-xs text-slate-400 mt-0.5">Automated recipe deduction on order settlement</p>
        </div>

        <div className="flex items-center gap-3">
          <button 
            onClick={() => fetchRawMaterials()}
            className="flex items-center gap-2 px-4 py-2 bg-slate-800 hover:bg-slate-700 text-white rounded-xl text-xs font-bold transition cursor-pointer"
          >
            <RefreshCw className="w-3.5 h-3.5" /> Sync Data
          </button>
          <button 
            onClick={() => setShowAddModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-orange-600 hover:bg-orange-500 text-white rounded-xl text-xs font-bold transition shadow-lg shadow-orange-600/20 cursor-pointer"
          >
            <Plus className="w-3.5 h-3.5" /> Add Raw Material
          </button>
        </div>
      </div>

      {/* Stock Table */}
      <div className="bg-slate-950 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
        <table className="w-full text-left text-sm">
          <thead className="bg-slate-900 text-slate-400 text-xs uppercase font-semibold border-b border-slate-800">
            <tr>
              <th className="p-4">Raw Material</th>
              <th className="p-4">Current Stock</th>
              <th className="p-4">Unit</th>
              <th className="p-4">Unit Cost</th>
              <th className="p-4">Threshold Alert</th>
              <th className="p-4">Status</th>
              <th className="p-4 text-right">Action</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800/60">
            {rawMaterials.map((item, idx) => {
              const isLow = item.stock <= item.min;
              return (
                <tr key={idx} className="hover:bg-slate-900/40 transition">
                  <td className="p-4 font-bold text-white flex items-center gap-2">
                    <Package className="w-4 h-4 text-slate-500" />
                    {item.name}
                  </td>
                  <td className="p-4 font-black text-base text-slate-100">{item.stock}</td>
                  <td className="p-4 text-slate-400">{item.uom}</td>
                  <td className="p-4 text-slate-300">₹{item.cost.toFixed(2)}</td>
                  <td className="p-4 text-slate-400">{item.min} {item.uom}</td>
                  <td className="p-4">
                    {isLow ? (
                      <span className="inline-flex items-center gap-1 bg-rose-500/20 text-rose-400 text-xs px-2.5 py-0.5 rounded-full border border-rose-500/30 font-semibold">
                        <AlertTriangle className="w-3 h-3" /> Low Stock
                      </span>
                    ) : (
                      <span className="inline-flex items-center gap-1 bg-emerald-500/20 text-emerald-400 text-xs px-2.5 py-0.5 rounded-full border border-emerald-500/30 font-semibold">
                        Adequate
                      </span>
                    )}
                  </td>
                  <td className="p-4 text-right">
                    <button 
                      onClick={() => {
                        setSelectedMaterial(item);
                        setShowAdjustModal(true);
                      }}
                      className="text-xs text-orange-400 hover:text-orange-300 font-bold px-2.5 py-1 rounded bg-orange-500/10 border border-orange-500/20 cursor-pointer"
                    >
                      Adjust
                    </button>
                  </td>
                </tr>
              );
            })}
            {rawMaterials.length === 0 && (
              <tr>
                <td colSpan={7} className="p-8 text-center text-slate-500">
                  No raw materials found. Add one to get started!
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Add Modal */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 bg-slate-950/80 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-md overflow-hidden shadow-2xl">
            <div className="p-5 border-b border-slate-800 flex items-center justify-between">
              <h3 className="text-base font-bold text-white">Add New Raw Material</h3>
              <button onClick={() => setShowAddModal(false)} className="text-slate-400 hover:text-white cursor-pointer">
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleAddMaterial} className="p-5 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1">Name</label>
                <input 
                  type="text" 
                  value={newName} 
                  onChange={e => setNewName(e.target.value)} 
                  required
                  placeholder="e.g. Fresh Tomatoes"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-orange-500"
                />
              </div>
              <div className="grid grid-cols-3 gap-3">
                <div className="col-span-1">
                  <label className="block text-xs font-semibold text-slate-400 mb-1">UOM</label>
                  <input 
                    type="text" 
                    value={newUom} 
                    onChange={e => setNewUom(e.target.value)} 
                    required
                    placeholder="kg, ltr, pcs"
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-orange-500"
                  />
                </div>
                <div className="col-span-1">
                  <label className="block text-xs font-semibold text-slate-400 mb-1">Cost/Unit</label>
                  <input 
                    type="number" 
                    step="0.01"
                    value={newCost} 
                    onChange={e => setNewCost(e.target.value)} 
                    required
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-orange-500"
                  />
                </div>
                <div className="col-span-1">
                  <label className="block text-xs font-semibold text-slate-400 mb-1">Min Alert</label>
                  <input 
                    type="number" 
                    step="0.01"
                    value={newMin} 
                    onChange={e => setNewMin(e.target.value)} 
                    required
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-orange-500"
                  />
                </div>
              </div>
              <button 
                type="submit" 
                className="w-full py-3 bg-orange-600 hover:bg-orange-500 text-white rounded-xl text-sm font-bold transition shadow-lg shadow-orange-600/20 cursor-pointer mt-2"
              >
                Create Material
              </button>
            </form>
          </div>
        </div>
      )}

      {/* Adjust Modal */}
      {showAdjustModal && selectedMaterial && (
        <div className="fixed inset-0 z-50 bg-slate-950/80 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-md overflow-hidden shadow-2xl">
            <div className="p-5 border-b border-slate-800 flex items-center justify-between">
              <h3 className="text-base font-bold text-white">Adjust Stock: {selectedMaterial.name}</h3>
              <button onClick={() => setShowAdjustModal(false)} className="text-slate-400 hover:text-white cursor-pointer">
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleAdjustStock} className="p-5 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1">Quantity Change ({selectedMaterial.uom})</label>
                <input 
                  type="number" 
                  step="0.01"
                  value={qtyChange} 
                  onChange={e => setQtyChange(e.target.value)} 
                  required
                  placeholder="Use negative numbers to deduct"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-orange-500"
                />
                <span className="text-[10px] text-slate-500 mt-1 block">Current stock: {selectedMaterial.stock} {selectedMaterial.uom}</span>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-400 mb-1">Reason</label>
                <input 
                  type="text" 
                  value={adjustReason} 
                  onChange={e => setAdjustReason(e.target.value)} 
                  required
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-orange-500"
                />
              </div>
              <button 
                type="submit" 
                className="w-full py-3 bg-orange-600 hover:bg-orange-500 text-white rounded-xl text-sm font-bold transition shadow-lg shadow-orange-600/20 cursor-pointer mt-2"
              >
                Apply Adjustment
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
