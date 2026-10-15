import React, { useState } from 'react';
import { usePosStore } from '../store/posStore';
import { Search, Plus, Minus, Trash2, Send, CreditCard, User, Phone, Tag, X } from 'lucide-react';

export const PosTerminal: React.FC = () => {
  const {
    cart,
    categories,
    rawMaterials,
    userRole,
    selectedTable,
    selectedCategory,
    searchQuery,
    customerName,
    customerPhone,
    discountPercent,
    gstPercent,
    addToCart,
    updateQuantity,
    removeFromCart,
    setSelectedCategory,
    setSearchQuery,
    setCustomerName,
    setCustomerPhone,
    setDiscount,
    clearCart,
    fireKot,
    settleOrder,
    createMenuItem,
  } = usePosStore();

  const [showAddModal, setShowAddModal] = useState(false);
  const [newItemName, setNewItemName] = useState('');
  const [newItemShortCode, setNewItemShortCode] = useState('');
  const [newItemPrice, setNewItemPrice] = useState('0');
  const [newItemCategory, setNewItemCategory] = useState('');
  const [newItemIsVeg, setNewItemIsVeg] = useState(true);
  const [newItemStation, setNewItemStation] = useState('Kitchen');

  const handleAddItem = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newItemName || !newItemCategory) return;
    await createMenuItem(
      newItemCategory,
      newItemName,
      newItemShortCode,
      parseFloat(newItemPrice),
      newItemIsVeg,
      newItemStation
    );
    setShowAddModal(false);
    setNewItemName('');
    setNewItemShortCode('');
    setNewItemPrice('0');
    setNewItemCategory('');
    setNewItemIsVeg(true);
    setNewItemStation('Kitchen');
  };

  const isInventoryUser = userRole === 'InventoryUser';

  const itemsToDisplay = isInventoryUser
    ? rawMaterials.map(rm => ({
        id: rm.id,
        categoryId: 'raw-mat',
        name: rm.name,
        shortCode: rm.uom,
        basePrice: rm.cost || 0,
        isVegetarian: true,
        isAvailable: true,
        kitchenStation: 'Inventory Item',
        isRawMaterial: true
      }))
    : (() => {
        const active = categories.find((c) => c.id === selectedCategory);
        return active ? active.menuItems : categories.flatMap((c) => c.menuItems);
      })();

  const filteredItems = itemsToDisplay.filter((item) =>
    item.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    (item.shortCode && item.shortCode.toLowerCase().includes(searchQuery.toLowerCase()))
  );

  const subTotal = cart.reduce((acc, item) => acc + item.totalPrice, 0);
  const discountAmount = (subTotal * discountPercent) / 100;
  const taxableAmount = subTotal - discountAmount;
  const taxAmount = (taxableAmount * gstPercent) / 100;
  const grandTotal = Math.round(taxableAmount + taxAmount);

  return (
    <div className="flex-1 flex overflow-hidden bg-slate-950/20">
      {/* Left: Category Sidebar */}
      <div className="w-56 bg-slate-950/80 border-r border-slate-900 flex flex-col p-3 gap-2 overflow-y-auto z-10">
        {isInventoryUser ? (
          <button
            className="w-full text-left px-4 py-3 rounded-xl font-semibold text-sm transition-all bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-lg shadow-orange-600/10 btn-interactive"
          >
            📦 Raw Materials
            <span className="block text-xs font-normal opacity-70">{rawMaterials.length} items</span>
          </button>
        ) : (
          <>
            <button
              onClick={() => setSelectedCategory(null)}
              className={`w-full text-left px-4 py-3 rounded-xl font-bold text-xs transition-all duration-200 btn-interactive ${
                selectedCategory === null
                  ? 'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-md shadow-orange-500/10 border border-orange-400/10'
                  : 'text-slate-400 hover:text-white hover:bg-slate-900/60'
              }`}
            >
              All Categories
            </button>

            {categories.map((cat) => (
              <button
                key={cat.id}
                onClick={() => setSelectedCategory(cat.id)}
                className={`w-full text-left px-4 py-3 rounded-xl font-bold text-xs transition-all duration-200 btn-interactive ${
                  selectedCategory === cat.id
                    ? 'bg-gradient-to-r from-orange-500 to-orange-600 text-white shadow-md shadow-orange-500/10 border border-orange-400/10'
                    : 'text-slate-400 hover:text-white hover:bg-slate-900/60'
                }`}
              >
                {cat.name}
                <span className="block text-[10px] font-normal opacity-60 mt-0.5">{cat.menuItems.length} items</span>
              </button>
            ))}
          </>
        )}
      </div>

      {/* Middle: Menu Items Grid */}
      <div className="flex-1 flex flex-col p-6 overflow-hidden">
        {/* Search & Fast Filter Bar */}
        <div className="flex items-center gap-4 mb-6">
          <div className="flex-1 relative">
            <Search className="w-5 h-5 absolute left-4 top-1/2 -translate-y-1/2 text-slate-450" />
            <input
              type="text"
              placeholder="Search by Dish Name or Shortcode (F2)..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full bg-slate-950/85 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl pl-12 pr-4 py-3.5 text-sm text-white placeholder-slate-500 focus:outline-none transition-all duration-200"
            />
          </div>

          {!isInventoryUser && (
            <>
              <div className="flex items-center gap-1.5 bg-slate-950/60 p-1 rounded-xl border border-slate-900 text-xs font-bold shadow-inner">
                <button className="px-3 py-1.5 rounded-lg bg-slate-800 text-white shadow-sm btn-interactive">All</button>
                <button className="px-3 py-1.5 rounded-lg text-emerald-400 hover:bg-slate-900/60 btn-interactive">🟢 Veg Only</button>
                <button className="px-3 py-1.5 rounded-lg text-rose-400 hover:bg-slate-900/60 btn-interactive">🔴 Non-Veg</button>
              </div>

              <button 
                onClick={() => {
                  if (categories.length > 0) {
                    setNewItemCategory(categories[0].id);
                  }
                  setShowAddModal(true);
                }}
                className="flex items-center gap-2 px-4 py-3.5 bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-400 hover:to-orange-500 text-white rounded-xl text-xs font-bold transition shadow-lg shadow-orange-500/10 cursor-pointer btn-interactive"
              >
                <Plus className="w-4 h-4" /> Add Item
              </button>
            </>
          )}
        </div>

        {/* Menu Grid */}
        <div className="flex-1 overflow-y-auto grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 pr-2">
          {filteredItems.map((item) => (
            <div
              key={item.id}
              onClick={() => addToCart(item)}
              className="bg-slate-950/80 border border-slate-800/80 hover:border-orange-500/40 hover:bg-slate-900 p-4 rounded-2xl cursor-pointer flex flex-col justify-between transition-all duration-200 group shadow-md btn-interactive"
            >
              <div>
                <div className="flex items-center justify-between mb-2">
                  {!isInventoryUser ? (
                    <span
                      className={`w-3.5 h-3.5 border rounded-sm flex items-center justify-center ${
                        item.isVegetarian ? 'border-emerald-500' : 'border-rose-500'
                      }`}
                    >
                      <span
                        className={`w-1.5 h-1.5 rounded-full ${
                          item.isVegetarian ? 'bg-emerald-500' : 'bg-rose-500'
                        }`}
                      ></span>
                    </span>
                  ) : (
                    <span className="text-[9px] uppercase font-bold text-orange-400 bg-orange-500/10 px-2 py-0.5 rounded-md">
                      Loose Stock
                    </span>
                  )}
                  {item.shortCode && (
                    <span className="text-[9px] font-mono bg-slate-850 px-1.5 py-0.5 rounded text-slate-300">
                      {isInventoryUser ? `Unit: ${item.shortCode}` : item.shortCode}
                    </span>
                  )}
                </div>
                <h2 className="font-bold text-sm text-slate-100 group-hover:text-orange-400 transition leading-snug text-left">
                  {item.name}
                </h2>
                <span className="text-[10px] text-slate-400 mt-1 block text-left">
                  {isInventoryUser ? 'Direct Inventory Sale' : item.kitchenStation}
                </span>
              </div>

              <div className="flex items-center justify-between mt-4 pt-3 border-t border-slate-800/60">
                <span className="text-sm font-black text-white">₹{item.basePrice.toFixed(2)}</span>
                <button className="p-2 rounded-xl bg-orange-600/10 text-orange-400 group-hover:bg-orange-600 group-hover:text-white transition duration-200">
                  <Plus className="w-4 h-4" />
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Right: Running Cart & Billing Panel */}
      <div className="w-96 bg-slate-950/80 border-l border-slate-900 flex flex-col justify-between z-10">
        {/* Order Header / Table Info */}
        <div className="p-4 border-b border-slate-900 bg-slate-900/20">
          <div className="flex items-center justify-between mb-3 text-left">
            <div>
              <span className="text-[10px] text-slate-500 uppercase tracking-widest font-bold">Current Order</span>
              <h2 className="text-lg font-black text-white">
                {selectedTable ? `Table ${selectedTable.tableNumber}` : 'Dine-In / Quick Bill'}
              </h2>
            </div>
            {cart.length > 0 && (
              <button
                onClick={clearCart}
                className="text-[10px] text-rose-400 hover:text-rose-350 font-bold px-2.5 py-1 rounded-xl bg-rose-500/10 border border-rose-500/20 transition btn-interactive"
              >
                Clear
              </button>
            )}
          </div>

          {/* Quick Customer Info */}
          <div className="grid grid-cols-2 gap-2 text-left">
            <div className="relative">
              <Phone className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                type="text"
                placeholder="Phone..."
                value={customerPhone}
                onChange={(e) => setCustomerPhone(e.target.value)}
                className="w-full bg-slate-900/90 border border-slate-800/80 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/10 rounded-xl pl-9 pr-2 py-2 text-xs text-white placeholder-slate-500 focus:outline-none transition duration-200"
              />
            </div>
            <div className="relative">
              <User className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                type="text"
                placeholder="Name..."
                value={customerName}
                onChange={(e) => setCustomerName(e.target.value)}
                className="w-full bg-slate-900/90 border border-slate-800/80 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/10 rounded-xl pl-9 pr-2 py-2 text-xs text-white placeholder-slate-500 focus:outline-none transition duration-200"
              />
            </div>
          </div>
        </div>

        {/* Cart Item List */}
        <div className="flex-1 overflow-y-auto p-4 space-y-3">
          {cart.length === 0 ? (
            <div className="h-full flex flex-col items-center justify-center text-slate-550 text-center">
              <span className="text-3xl mb-2 animate-pulse">🍽️</span>
              <p className="text-xs font-bold text-slate-400">Cart is Empty</p>
              <p className="text-[10px] text-slate-500 max-w-[200px] mx-auto mt-1 leading-normal">Select dishes from the menu to build order</p>
            </div>
          ) : (
            cart.map((item) => (
              <div key={item.id} className="bg-slate-900/40 p-3 rounded-xl border border-slate-800/60 space-y-2 text-left">
                <div className="flex items-center justify-between">
                  <div className="flex flex-col gap-0.5 text-left">
                    <h4 className="font-bold text-xs text-slate-200 leading-snug">{item.name}</h4>
                    {item.isFired ? (
                      <span className="text-[8px] font-bold uppercase tracking-wider text-emerald-400 bg-emerald-500/10 px-1.5 py-0.5 rounded w-max">
                        🍳 Sent to Kitchen
                      </span>
                    ) : (
                      <span className="text-[8px] font-bold uppercase tracking-wider text-orange-400 bg-orange-500/10 px-1.5 py-0.5 rounded w-max">
                        ⏳ Pending Fire
                      </span>
                    )}
                  </div>
                  <span className="font-black text-xs text-white">₹{item.totalPrice.toFixed(2)}</span>
                </div>

                <div className="flex items-center justify-between pt-1">
                  <span className="text-[10px] text-slate-500">₹{item.unitPrice.toFixed(2)} each</span>
                  <div className="flex items-center gap-2 bg-slate-950 px-2 py-1 rounded-lg border border-slate-850">
                    {item.isFired ? (
                      <span className="text-[9px] font-bold text-slate-500 px-1.5 py-0.5">
                        Qty: {item.quantity} (Locked)
                      </span>
                    ) : (
                      <>
                        <button
                          onClick={() => updateQuantity(item.id, -1)}
                          className="text-slate-400 hover:text-white cursor-pointer transition duration-155"
                        >
                          <Minus className="w-3 h-3" />
                        </button>
                        <span className="text-xs font-bold text-white px-1">{item.quantity}</span>
                        <button
                          onClick={() => updateQuantity(item.id, 1)}
                          className="text-slate-400 hover:text-white cursor-pointer transition duration-155"
                        >
                          <Plus className="w-3 h-3" />
                        </button>
                        <button
                          onClick={() => removeFromCart(item.id)}
                          className="text-rose-450 hover:text-rose-350 pl-1.5 ml-1.5 border-l border-slate-800 cursor-pointer transition duration-155"
                        >
                          <Trash2 className="w-3 h-3" />
                        </button>
                      </>
                    )}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Pricing Summary & Action Buttons */}
        <div className="p-4 bg-slate-900/30 border-t border-slate-900 space-y-3">
          {/* Quick Discount Selector */}
          <div className="flex items-center justify-between text-xs font-bold">
            <span className="text-slate-500 flex items-center gap-1">
              <Tag className="w-3.5 h-3.5" /> Discount
            </span>
            <div className="flex gap-1">
              {[0, 5, 10, 15].map((pct) => (
                <button
                  key={pct}
                  onClick={() => setDiscount(pct)}
                  className={`px-2 py-0.5 rounded-lg text-[10px] font-black transition duration-200 btn-interactive ${
                    discountPercent === pct
                      ? 'bg-orange-600 text-white'
                      : 'bg-slate-900 text-slate-450 hover:bg-slate-800'
                  }`}
                >
                  {pct}%
                </button>
              ))}
            </div>
          </div>

          <div className="space-y-1.5 text-xs pt-2 border-t border-slate-900">
            <div className="flex justify-between text-slate-500 font-semibold">
              <span>Subtotal</span>
              <span>₹{subTotal.toFixed(2)}</span>
            </div>
            {discountPercent > 0 && (
              <div className="flex justify-between text-emerald-450 font-bold">
                <span>Discount ({discountPercent}%)</span>
                <span>-₹{discountAmount.toFixed(2)}</span>
              </div>
            )}
            <div className="flex justify-between text-slate-500 font-semibold">
              <span>GST (5%)</span>
              <span>₹{taxAmount.toFixed(2)}</span>
            </div>
            <div className="flex justify-between text-sm font-black text-white pt-2 border-t border-slate-900">
              <span>Grand Total</span>
              <span className="text-orange-400">₹{grandTotal.toFixed(2)}</span>
            </div>
          </div>

          {/* Action Triggers */}
          <div className="grid grid-cols-2 gap-2 pt-2">
            <button
              onClick={() => fireKot()}
              disabled={cart.length === 0}
              className="flex items-center justify-center gap-2 bg-slate-900 hover:bg-slate-850 disabled:opacity-50 text-slate-350 py-3.5 rounded-xl font-bold text-xs transition duration-200 cursor-pointer btn-interactive border border-slate-850"
            >
              <Send className="w-4 h-4 text-orange-450" />
              Fire KOT (F8)
            </button>
            <button
              onClick={() => settleOrder(3)} // UPI
              disabled={cart.length === 0 && !selectedTable?.currentOrderId}
              className="flex items-center justify-center gap-2 bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-400 hover:to-orange-500 disabled:opacity-50 text-white py-3.5 rounded-xl font-bold text-xs transition duration-200 shadow-lg shadow-orange-600/10 cursor-pointer btn-interactive"
            >
              <CreditCard className="w-4 h-4" />
              Settle Bill (F4)
            </button>
          </div>
        </div>
      </div>

      {/* Add MenuItem Modal */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="glass-panel glass-panel-glow rounded-3xl w-full max-w-md overflow-hidden shadow-2xl text-left">
            <div className="p-5 border-b border-slate-900 flex items-center justify-between">
              <h3 className="text-sm font-bold text-white uppercase tracking-wider">Add New Menu Item</h3>
              <button onClick={() => setShowAddModal(false)} className="text-slate-400 hover:text-white cursor-pointer transition duration-200 btn-interactive">
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleAddItem} className="p-5 space-y-4">
              <div>
                <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1">Item Name</label>
                <input 
                  type="text" 
                  value={newItemName} 
                  onChange={e => setNewItemName(e.target.value)} 
                  required
                  placeholder="e.g. Masala Dosa"
                  className="w-full bg-slate-950 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-4 py-2.5 text-xs text-white focus:outline-none transition-all duration-200"
                />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1">Short Code</label>
                  <input 
                    type="text" 
                    value={newItemShortCode} 
                    onChange={e => setNewItemShortCode(e.target.value)} 
                    placeholder="e.g. MSD"
                    className="w-full bg-slate-950 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-4 py-2.5 text-xs text-white focus:outline-none transition-all duration-200"
                  />
                </div>
                <div>
                  <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1">Base Price (₹)</label>
                  <input 
                    type="number" 
                    step="0.01"
                    value={newItemPrice} 
                    onChange={e => setNewItemPrice(e.target.value)} 
                    required
                    className="w-full bg-slate-950 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-4 py-2.5 text-xs text-white focus:outline-none transition-all duration-200"
                  />
                </div>
              </div>
              <div>
                <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1">Category</label>
                <select 
                  value={newItemCategory} 
                  onChange={e => setNewItemCategory(e.target.value)}
                  required
                  className="w-full bg-slate-950 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-4 py-2.5 text-xs text-white focus:outline-none transition-all duration-200 cursor-pointer"
                >
                  {categories.map(cat => (
                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                  ))}
                </select>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-1">Kitchen Station</label>
                  <input 
                    type="text" 
                    value={newItemStation} 
                    onChange={e => setNewItemStation(e.target.value)} 
                    required
                    className="w-full bg-slate-950 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-4 py-2.5 text-xs text-white focus:outline-none transition-all duration-200"
                  />
                </div>
                <div className="flex items-center gap-2 pt-5">
                  <input 
                    type="checkbox" 
                    id="isVegCheckbox"
                    checked={newItemIsVeg} 
                    onChange={e => setNewItemIsVeg(e.target.checked)} 
                    className="w-4 h-4 rounded border-slate-800 bg-slate-950 accent-orange-600 focus:outline-none focus:ring-0 cursor-pointer"
                  />
                  <label htmlFor="isVegCheckbox" className="text-[10px] font-bold text-slate-400 uppercase tracking-wider cursor-pointer select-none">Vegetarian</label>
                </div>
              </div>
              <button 
                type="submit" 
                className="w-full py-3 bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-400 hover:to-orange-500 text-white rounded-xl text-xs font-bold transition shadow-lg shadow-orange-600/10 cursor-pointer mt-2 btn-interactive"
              >
                Create Menu Item
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
