import React, { useEffect } from 'react';
import { DollarSign, ShoppingBag, Users, TrendingUp, ArrowUpRight, RefreshCw, AlertTriangle, Package, Activity } from 'lucide-react';
import { usePosStore } from '../store/posStore';

export const Dashboard: React.FC = () => {
  const { dashboardStats, fetchDashboardStats, userRole, rawMaterials, fetchRawMaterials } = usePosStore();

  useEffect(() => {
    fetchDashboardStats();
    fetchRawMaterials();
  }, [fetchDashboardStats, fetchRawMaterials]);

  const isInventory = userRole === 'InventoryUser';

  const sales = isInventory ? (dashboardStats?.directMaterialSales ?? 0) : (dashboardStats?.totalSales ?? 0);
  const orders = dashboardStats?.totalOrders ?? 0;
  const guests = dashboardStats?.totalGuests ?? 0;
  const ticketSize = dashboardStats?.averageTicketSize ?? 0;
  const payments = dashboardStats?.paymentBreakdown ?? [];
  const topItems = dashboardStats?.topItems ?? [];

  // Inventory stats
  const totalValuation = rawMaterials.reduce((acc, rm) => acc + ((rm.stock || 0) * (rm.cost || 0)), 0);
  const lowStockItems = rawMaterials.filter(rm => (rm.stock || 0) <= (rm.min || 0));

  const totalPaymentVal = payments.reduce((acc: number, curr: any) => acc + curr.total, 0) || 1;

  return (
    <div className="flex-1 flex flex-col p-6 overflow-y-auto bg-slate-900 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between pb-4 border-b border-slate-800">
        <div>
          <h2 className="text-xl font-black text-white">
            {isInventory ? '📦 Inventory Operations Dashboard' : '👑 Restaurant Analytics Dashboard'}
          </h2>
          <p className="text-xs text-slate-400 mt-0.5">
            {isInventory 
              ? 'Real-time stock valuation, low stock warnings, and direct material sales summary' 
              : 'Real-time sales performance metrics, ticket size, and dining transactions'}
          </p>
        </div>
        <button
          onClick={() => {
            fetchDashboardStats();
            fetchRawMaterials();
          }}
          className="flex items-center gap-2 px-4 py-2 bg-slate-800 hover:bg-slate-700 text-white rounded-xl text-xs font-bold transition cursor-pointer"
        >
          <RefreshCw className="w-3.5 h-3.5" /> Refresh Dashboard
        </button>
      </div>

      {/* Top Metrics Row */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-5">
        <div className="bg-slate-950 border border-slate-800 p-5 rounded-2xl flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              {isInventory ? "Direct Material Sales" : "Today's Total Sales"}
            </span>
            <h3 className="text-2xl font-black text-white mt-1">₹{sales.toFixed(2)}</h3>
            <span className="text-xs text-emerald-400 font-medium flex items-center gap-0.5 mt-2">
              <ArrowUpRight className="w-3.5 h-3.5" /> Live tracking
            </span>
          </div>
          <div className="p-3.5 rounded-2xl bg-orange-500/10 text-orange-400 border border-orange-500/20">
            <DollarSign className="w-6 h-6" />
          </div>
        </div>

        <div className="bg-slate-950 border border-slate-800 p-5 rounded-2xl flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              {isInventory ? "Total Items Value" : "Total Orders"}
            </span>
            <h3 className="text-2xl font-black text-white mt-1">
              {isInventory ? `₹${totalValuation.toFixed(2)}` : `${orders} Bills`}
            </h3>
            <span className="text-xs text-slate-400 font-medium mt-2 block">
              {isInventory ? "Valuation of current stock" : "Settled today"}
            </span>
          </div>
          <div className="p-3.5 rounded-2xl bg-sky-500/10 text-sky-400 border border-sky-500/20">
            {isInventory ? <Package className="w-6 h-6" /> : <ShoppingBag className="w-6 h-6" />}
          </div>
        </div>

        <div className="bg-slate-950 border border-slate-800 p-5 rounded-2xl flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              {isInventory ? "Low Stock Materials" : "Covers / Guests"}
            </span>
            <h3 className={`text-2xl font-black mt-1 ${isInventory && lowStockItems.length > 0 ? 'text-amber-400' : 'text-white'}`}>
              {isInventory ? `${lowStockItems.length} Materials` : `${guests} Guests`}
            </h3>
            <span className="text-xs text-slate-400 font-medium mt-2 block">
              {isInventory ? "Below alert threshold" : "Dine-in traffic today"}
            </span>
          </div>
          <div className={`p-3.5 rounded-2xl border ${
            isInventory && lowStockItems.length > 0
              ? 'bg-amber-500/10 text-amber-400 border-amber-500/20 animate-pulse'
              : 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20'
          }`}>
            {isInventory ? <AlertTriangle className="w-6 h-6" /> : <Users className="w-6 h-6" />}
          </div>
        </div>

        <div className="bg-slate-950 border border-slate-800 p-5 rounded-2xl flex items-center justify-between">
          <div>
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              {isInventory ? "Total Unique SKUs" : "Avg. Ticket Size"}
            </span>
            <h3 className="text-2xl font-black text-white mt-1">
              {isInventory ? `${rawMaterials.length} Items` : `₹${ticketSize.toFixed(2)}`}
            </h3>
            <span className="text-xs text-slate-400 font-medium mt-2 block">
              {isInventory ? "Registered in inventory" : "Average spend per bill"}
            </span>
          </div>
          <div className="p-3.5 rounded-2xl bg-amber-500/10 text-amber-400 border border-amber-500/20">
            {isInventory ? <Activity className="w-6 h-6" /> : <TrendingUp className="w-6 h-6" />}
          </div>
        </div>
      </div>

      {/* Analytics Breakdown Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left Column Section */}
        {isInventory ? (
          /* Low Stock Alerts Detail for Inventory Mode */
          <div className="bg-slate-950 border border-slate-800 p-6 rounded-2xl space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-base font-bold text-white flex items-center gap-2">
                <AlertTriangle className="w-4 h-4 text-amber-400" /> Low Stock Alerts
              </h3>
              <span className="text-xs text-amber-400 font-bold px-2 py-0.5 bg-amber-500/10 rounded-full border border-amber-500/20">
                Action Required
              </span>
            </div>
            <div className="space-y-3 max-h-[350px] overflow-y-auto pr-1">
              {lowStockItems.map((item, idx) => (
                <div key={idx} className="flex items-center justify-between p-3 rounded-xl bg-slate-900 border border-slate-800">
                  <div className="text-left">
                    <span className="text-sm font-semibold text-slate-200">{item.name}</span>
                    <span className="text-[10px] text-slate-500 block">UOM: {item.uom}</span>
                  </div>
                  <div className="text-right">
                    <span className="text-sm font-bold text-rose-400">{item.stock.toFixed(2)} left</span>
                    <span className="text-[10px] text-slate-500 block">Min Threshold: {item.min}</span>
                  </div>
                </div>
              ))}
              {lowStockItems.length === 0 && (
                <p className="text-sm text-slate-500 text-center py-8">All stock quantities are currently healthy.</p>
              )}
            </div>
          </div>
        ) : (
          /* Payment Mode Breakdown for Restaurant Mode */
          <div className="bg-slate-950 border border-slate-800 p-6 rounded-2xl space-y-4">
            <h3 className="text-base font-bold text-white">Payment Mode Breakdown</h3>
            <div className="space-y-4">
              {payments.map((p: any, i: number) => {
                const percent = ((p.total / totalPaymentVal) * 100).toFixed(1);
                return (
                  <div key={i}>
                    <div className="flex justify-between text-xs font-semibold mb-1">
                      <span className="text-slate-300">{p.paymentMode}</span>
                      <span className="text-white">₹{p.total.toFixed(2)} ({percent}%)</span>
                    </div>
                    <div className="w-full h-2 bg-slate-900 rounded-full overflow-hidden">
                      <div className="bg-orange-500 h-full rounded-full" style={{ width: `${percent}%` }}></div>
                    </div>
                  </div>
                );
              })}
              {payments.length === 0 && (
                <p className="text-sm text-slate-500 text-center py-4">No settled transaction records found today.</p>
              )}
            </div>
          </div>
        )}

        {/* Right Column Section */}
        {isInventory ? (
          /* Inventory Valuation Breakdown */
          <div className="bg-slate-950 border border-slate-800 p-6 rounded-2xl space-y-4">
            <h3 className="text-base font-bold text-white">Material Valuation Breakdown</h3>
            <div className="space-y-3 max-h-[350px] overflow-y-auto pr-1">
              {rawMaterials.map((item, idx) => {
                const val = (item.stock || 0) * (item.cost || 0);
                return (
                  <div key={idx} className="flex items-center justify-between p-3 rounded-xl bg-slate-900/60 border border-slate-850">
                    <div className="text-left">
                      <span className="text-sm font-semibold text-slate-200">{item.name}</span>
                      <span className="text-[10px] text-slate-400 block">Stock: {item.stock} {item.uom}</span>
                    </div>
                    <div className="text-right">
                      <span className="text-sm font-black text-white">₹{val.toFixed(2)}</span>
                      <span className="text-[10px] text-slate-500 block">Cost: ₹{item.cost}/{item.uom}</span>
                    </div>
                  </div>
                );
              })}
              {rawMaterials.length === 0 && (
                <p className="text-sm text-slate-500 text-center py-4">No items registered in inventory.</p>
              )}
            </div>
          </div>
        ) : (
          /* Top Selling Dishes for Restaurant Mode */
          <div className="bg-slate-950 border border-slate-800 p-6 rounded-2xl space-y-4">
            <h3 className="text-base font-bold text-white">Top Best-Selling Dishes</h3>
            <div className="space-y-3">
              {topItems.map((dish: any, i: number) => (
                <div key={i} className="flex items-center justify-between p-2.5 rounded-xl bg-slate-900/60 border border-slate-850">
                  <div className="flex items-center gap-3">
                    <span className="w-6 h-6 rounded-lg bg-orange-600/20 text-orange-400 font-bold text-xs flex items-center justify-center">
                      #{i + 1}
                    </span>
                    <span className="text-sm font-semibold text-slate-200">{dish.itemName}</span>
                  </div>
                  <div className="text-right">
                    <span className="text-sm font-black text-white">₹{dish.revenue.toFixed(2)}</span>
                    <span className="text-[11px] text-slate-400 block">{dish.quantitySold} sold</span>
                  </div>
                </div>
              ))}
              {topItems.length === 0 && (
                <p className="text-sm text-slate-500 text-center py-4">No orders placed today.</p>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
