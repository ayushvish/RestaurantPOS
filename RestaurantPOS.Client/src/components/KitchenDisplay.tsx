import React, { useState, useEffect } from 'react';
import { usePosStore } from '../store/posStore';
import { Clock, CheckCircle2, AlertCircle } from 'lucide-react';

export const KitchenDisplay: React.FC = () => {
  const { kots, fetchKots, toggleKotItemPrepared, completeKot } = usePosStore();
  const [activeStation, setActiveStation] = useState<string>('All');

  useEffect(() => {
    fetchKots();
  }, [fetchKots]);

  const handleToggleItem = (itemId: string) => {
    toggleKotItemPrepared(itemId);
  };

  const handleCompleteTicket = (ticketId: string) => {
    completeKot(ticketId);
  };

  const playChimeAudio = () => {
    try {
      const ctx = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.type = 'sine';
      osc.frequency.setValueAtTime(880, ctx.currentTime);
      gain.gain.setValueAtTime(0.25, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.6);
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.start();
      osc.stop(ctx.currentTime + 0.6);
    } catch {
      // AudioContext fallback
    }
  };

  useEffect(() => {
    if (kots.length > 0) {
      playChimeAudio();
    }
  }, [kots.length]);

  const getTicketAge = (firedAt: string) => {
    if (!firedAt) return { minutes: 0, label: 'Just now', color: 'text-emerald-400 bg-emerald-500/10 border-emerald-500/20' };
    const diff = Math.floor((new Date().getTime() - new Date(firedAt).getTime()) / 60000);
    const mins = Math.max(0, diff);
    if (mins < 10) {
      return { minutes: mins, label: `${mins}m ago`, color: 'text-emerald-400 bg-emerald-500/10 border-emerald-500/20' };
    } else if (mins < 15) {
      return { minutes: mins, label: `${mins}m ago (Warning)`, color: 'text-amber-400 bg-amber-500/15 border-amber-500/30' };
    } else {
      return { minutes: mins, label: `${mins}m (Overdue!)`, color: 'text-rose-400 bg-rose-500/20 border-rose-500/40 animate-pulse' };
    }
  };

  const filteredTickets = activeStation === 'All'
    ? kots
    : kots.filter((t: any) => t.kitchenStation === activeStation);

  return (
    <div className="flex-1 flex flex-col p-6 overflow-hidden bg-slate-900">
      {/* KDS Header & Station Filter */}
      <div className="flex items-center justify-between pb-6 border-b border-slate-800">
        <div className="flex items-center gap-3">
          <span className="text-xl font-black text-white">Live Kitchen Order Tickets (KOT)</span>
          <span className="bg-orange-500/20 text-orange-400 font-bold text-xs px-3 py-1 rounded-full border border-orange-500/30">
            {kots.length} Active Orders
          </span>
        </div>

        <div className="flex items-center gap-2 bg-slate-950 p-1.5 rounded-xl border border-slate-800 text-xs font-semibold">
          {['All', 'Kitchen', 'Tandoor', 'Bar'].map((station) => (
            <button
              key={station}
              onClick={() => setActiveStation(station)}
              className={`px-4 py-2 rounded-lg transition ${
                activeStation === station
                  ? 'bg-orange-600 text-white shadow-md'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {station}
            </button>
          ))}
        </div>
      </div>

      {/* Ticket Cards Grid */}
      <div className="flex-1 overflow-y-auto py-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {filteredTickets.map((ticket: any) => {
          const allPrepared = ticket.items.every((i: any) => i.isPrepared);

          return (
            <div
              key={ticket.id}
              className={`rounded-2xl border-2 flex flex-col justify-between overflow-hidden shadow-xl transition-all ${
                allPrepared
                  ? 'bg-emerald-950/20 border-emerald-500/60'
                  : 'bg-slate-950 border-slate-800'
              }`}
            >
              {/* Ticket Header */}
              <div className="p-4 bg-slate-900 border-b border-slate-800 flex items-center justify-between">
                <div>
                  <span className="text-xs font-bold text-orange-400 uppercase tracking-wider block">
                    {ticket.kitchenStation}
                  </span>
                  <h3 className="text-xl font-black text-white">{ticket.tableNumber}</h3>
                </div>

                <div className="text-right">
                  {(() => {
                    const age = getTicketAge(ticket.firedAt);
                    return (
                      <div className={`flex items-center gap-1 text-[11px] font-bold px-2 py-0.5 rounded-full border ${age.color}`}>
                        <Clock className="w-3 h-3" />
                        <span>{age.label}</span>
                      </div>
                    );
                  })()}
                  <span className="text-[11px] font-mono text-slate-400 block mt-1">{ticket.orderNumber}</span>
                </div>
              </div>

              {/* Items List */}
              <div className="p-4 flex-1 space-y-3 overflow-y-auto">
                {ticket.items.map((item: any) => (
                  <div
                    key={item.id}
                    onClick={() => handleToggleItem(item.id)}
                    className={`p-3 rounded-xl border transition-all cursor-pointer flex items-center justify-between ${
                      item.isPrepared
                        ? 'bg-emerald-900/20 border-emerald-500/40 opacity-60 line-through text-slate-400'
                        : 'bg-slate-900 border-slate-800 text-slate-100 hover:border-slate-700'
                    }`}
                  >
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-black text-base text-orange-400">{item.quantity}x</span>
                        <span className="font-bold text-sm">{item.itemName}</span>
                      </div>
                      {item.cookingNotes && (
                        <span className="text-xs text-amber-400 font-medium flex items-center gap-1 mt-1">
                          <AlertCircle className="w-3 h-3" /> {item.cookingNotes}
                        </span>
                      )}
                    </div>

                    <CheckCircle2
                      className={`w-5 h-5 ${
                        item.isPrepared ? 'text-emerald-400' : 'text-slate-600'
                      }`}
                    />
                  </div>
                ))}
              </div>

              {/* Ticket Footer Action */}
              <div className="p-4 bg-slate-900/60 border-t border-slate-800">
                <button
                  onClick={() => handleCompleteTicket(ticket.id)}
                  className={`w-full py-3 rounded-xl font-bold text-sm flex items-center justify-center gap-2 transition ${
                    allPrepared
                      ? 'bg-emerald-600 hover:bg-emerald-500 text-white shadow-lg shadow-emerald-600/30'
                      : 'bg-slate-800 hover:bg-slate-700 text-slate-300'
                  }`}
                >
                  <CheckCircle2 className="w-4 h-4" />
                  {allPrepared ? 'Mark Order Ready & Serve' : 'Ready All Items'}
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
