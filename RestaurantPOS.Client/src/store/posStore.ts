import { create } from 'zustand';
import * as signalR from '@microsoft/signalr';
import type {
  CartItem,
  RestaurantTable,
  MenuItem,
  Category,
  Floor,
  TableStatus,
  KitchenTicket,
  RawMaterialStockItem,
  DashboardStats,
  User,
  RestaurantWithOutlets,
  OrderDetail,
  OrderDetailItem,
} from '../types/pos';
import { apiClient, getPosHubConnection } from '../services/api';

const normalizeTableStatus = (status: TableStatus | number | string): TableStatus => {
  if (typeof status === 'number') {
    switch (status) {
      case 1:
        return 'Vacant';
      case 2:
        return 'Occupied';
      case 3:
        return 'Billed';
      case 4:
        return 'Reserved';
      default:
        return 'Vacant';
    }
  }
  const s = String(status).toLowerCase();
  if (s === '1' || s === 'vacant') return 'Vacant';
  if (s === '2' || s === 'occupied') return 'Occupied';
  if (s === '3' || s === 'billed') return 'Billed';
  if (s === '4' || s === 'reserved') return 'Reserved';
  return 'Vacant';
};

interface PosState {
  activeTab: 'tables' | 'billing' | 'kds' | 'dashboard' | 'inventory' | 'users';
  selectedTable: RestaurantTable | null;
  cart: CartItem[];
  categories: Category[];
  floors: Floor[];
  customerPhone: string;
  customerName: string;
  discountPercent: number;
  discountAmount: number;
  gstPercent: number;
  selectedCategory: string | null;
  searchQuery: string;
  isConnectedToApi: boolean;
  isLoading: boolean;
  restaurantId: string | null;
  outletId: string | null;
  restaurantName: string | null;
  outletName: string | null;
  token: string | null;
  userRole: string | null;
  username: string | null;
  fullName: string | null;
  allRestaurants: RestaurantWithOutlets[];

  setActiveTab: (tab: 'tables' | 'billing' | 'kds' | 'dashboard' | 'inventory' | 'users') => void;
  setSelectedTable: (table: RestaurantTable | null) => void;
  addToCart: (item: MenuItem, variantName?: string, variantPrice?: number) => void;
  updateQuantity: (cartItemId: string, change: number) => void;
  removeFromCart: (cartItemId: string) => void;
  setItemNotes: (cartItemId: string, notes: string) => void;
  clearCart: () => void;
  setCustomerPhone: (phone: string) => void;
  setCustomerName: (name: string) => void;
  setDiscount: (percent: number) => void;
  setSelectedCategory: (categoryId: string | null) => void;
  setSearchQuery: (query: string) => void;

  // API & SignalR Actions
  fetchInitialData: () => Promise<void>;
  initializeSignalR: () => Promise<void>;
  kots: KitchenTicket[];
  rawMaterials: RawMaterialStockItem[];
  dashboardStats: DashboardStats | null;
  fetchKots: () => Promise<void>;
  toggleKotItemPrepared: (itemId: string) => Promise<void>;
  completeKot: (kotId: string) => Promise<void>;
  fetchRawMaterials: () => Promise<void>;
  addRawMaterial: (name: string, uom: string, cost: number, min: number) => Promise<void>;
  adjustStock: (rawMaterialId: string, quantityChange: number, reason: string) => Promise<void>;
  fetchDashboardStats: () => Promise<void>;
  fireKot: () => Promise<void>;
  settleOrder: (paymentMode: number) => Promise<void>;
  createMenuItem: (
    categoryId: string,
    name: string,
    shortCode: string,
    basePrice: number,
    isVegetarian: boolean,
    kitchenStation: string
  ) => Promise<void>;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  switchContext: (restaurantId: string, outletId: string) => Promise<void>;
  fetchAllRestaurants: () => Promise<void>;
  addRestaurant: (name: string, tagline?: string, outletName?: string, floorName?: string) => Promise<void>;
  users: User[];
  fetchUsers: () => Promise<void>;
  createUser: (fullName: string, username: string, password: string, role: number, pin?: string) => Promise<void>;
  registerRestaurant: (
    name: string,
    tagline: string,
    address: string,
    phone: string,
    adminFullName: string,
    adminUsername: string,
    adminPassword: string
  ) => Promise<void>;
}

export const usePosStore = create<PosState>((set, get) => ({
  activeTab: 'tables',
  selectedTable: null,
  cart: [],
  categories: [],
  floors: [],
  customerPhone: '',
  customerName: '',
  discountPercent: 0,
  discountAmount: 0,
  gstPercent: 5.0,
  selectedCategory: null,
  searchQuery: '',
  isConnectedToApi: false,
  isLoading: false,
  restaurantId: localStorage.getItem('pos_restaurant_id') || null,
  outletId: localStorage.getItem('pos_outlet_id') || null,
  restaurantName: localStorage.getItem('pos_restaurant_name') || null,
  outletName: localStorage.getItem('pos_outlet_name') || null,
  token: localStorage.getItem('pos_token') || localStorage.getItem('token') || null,
  userRole: localStorage.getItem('pos_role') || null,
  username: null,
  fullName: localStorage.getItem('pos_user_name') || null,
  allRestaurants: [],
  kots: [],
  rawMaterials: [],
  dashboardStats: null,
  users: [],

  setActiveTab: (activeTab) => set({ activeTab }),
  setSelectedTable: async (table) => {
    set({ selectedTable: table });
    if (table && table.currentOrderId) {
      try {
        const res = await apiClient.get<OrderDetail>(`/Orders/${table.currentOrderId}`);
        if (res.data) {
          const order = res.data;
          const cartItems: CartItem[] = order.items.map((item: OrderDetailItem) => ({
            id: `${item.menuItemId || item.rawMaterialId}-${item.variantName || 'standard'}-fired`,
            menuItemId: item.menuItemId ?? null,
            rawMaterialId: item.rawMaterialId ?? null,
            name: item.itemName,
            variantName: item.variantName ?? undefined,
            unitPrice: item.unitPrice,
            quantity: item.quantity,
            totalPrice: item.totalPrice,
            isRawMaterial: !!item.rawMaterialId,
            isFired: item.isFired,
            notes: item.notes ?? undefined,
          }));
          set({ cart: cartItems });
        }
      } catch (err) {
        console.error('Error fetching table order:', err);
      }
    } else {
      set({ cart: [] });
    }
  },

  addToCart: (item, variantName, variantPrice) =>
    set((state) => {
      const price = variantPrice ?? item.basePrice;
      const isRaw = (item as unknown as { isRawMaterial?: boolean }).isRawMaterial ?? false;
      const key = `${item.id}-${variantName || 'standard'}-pending`;
      const existing = state.cart.find((i) => i.id === key);

      if (existing) {
        return {
          cart: state.cart.map((i) =>
            i.id === key
              ? { ...i, quantity: i.quantity + 1, totalPrice: (i.quantity + 1) * i.unitPrice }
              : i
          ),
        };
      }

      return {
        cart: [
          ...state.cart,
          {
            id: key,
            menuItemId: isRaw ? null : item.id,
            rawMaterialId: isRaw ? item.id : null,
            name: item.name,
            variantName: variantName,
            unitPrice: price,
            quantity: 1,
            totalPrice: price,
            isRawMaterial: isRaw,
            isFired: false,
          },
        ],
      };
    }),

  updateQuantity: (cartItemId, change) =>
    set((state) => {
      const item = state.cart.find((i) => i.id === cartItemId);
      if (item?.isFired) return state;
      return {
        cart: state.cart
          .map((i) => {
            if (i.id === cartItemId) {
              const newQty = i.quantity + change;
              return newQty > 0
                ? { ...i, quantity: newQty, totalPrice: newQty * i.unitPrice }
                : null;
            }
            return i;
          })
          .filter(Boolean) as CartItem[],
      };
    }),

  removeFromCart: (cartItemId) =>
    set((state) => {
      const item = state.cart.find((i) => i.id === cartItemId);
      if (item?.isFired) return state;
      return {
        cart: state.cart.filter((i) => i.id !== cartItemId),
      };
    }),

  setItemNotes: (cartItemId, notes) =>
    set((state) => ({
      cart: state.cart.map((i) => (i.id === cartItemId ? { ...i, notes } : i)),
    })),

  clearCart: () =>
    set({
      cart: [],
      selectedTable: null,
      customerName: '',
      customerPhone: '',
      discountPercent: 0,
    }),

  setCustomerPhone: (customerPhone) => set({ customerPhone }),
  setCustomerName: (customerName) => set({ customerName }),
  setDiscount: (percent) => set({ discountPercent: percent }),
  setSelectedCategory: (selectedCategory) => set({ selectedCategory }),
  setSearchQuery: (searchQuery) => set({ searchQuery }),

  fetchInitialData: async () => {
    const { token, restaurantId, outletId } = get();
    if (!token) return;

    set({ isLoading: true });
    try {
      set({ isConnectedToApi: true });

      // 2. Fetch categories & menu items from PostgreSQL
      if (restaurantId) {
        const catRes = await apiClient.get<Category[]>(`/Menu/categories/${restaurantId}`);
        set({ categories: catRes.data || [] });
      }

      // 3. Fetch floors & tables from PostgreSQL
      if (outletId) {
        const floorRes = await apiClient.get<Floor[]>(`/Floor/outlets/${outletId}/floors`);
        set({ floors: floorRes.data || [] });
      }

      // Fetch dashboard, KOT, and raw materials if relevant
      await get().fetchRawMaterials();
      await get().fetchKots();

      // 4. Initialize SignalR
      await get().initializeSignalR();
    } catch (error) {
      console.warn('[POS API] Backend unreachable or starting up. Using local cache/mock state.', error);
      set({ isConnectedToApi: false });
    } finally {
      set({ isLoading: false });
    }
  },

  initializeSignalR: async () => {
    try {
      const hub = getPosHubConnection();

      // Clear existing listeners to avoid duplicate subscriptions
      hub.off('TableStatusChanged');
      hub.off('TableStatusUpdated');
      hub.off('NewOrderCreated');
      hub.off('KotReceived');
      hub.off('KotStatusChanged');
      hub.off('KotItemToggled');
      hub.off('OrderSettled');

      // Shared handler supporting both (tableId, status, orderId) and ({ tableId, status, currentOrderId })
      const handleTableStatusChange = (
        arg1: string | { tableId: string; status: TableStatus | number | string; currentOrderId?: string; orderId?: string },
        arg2?: TableStatus | number | string,
        arg3?: string
      ) => {
        let tableId: string;
        let statusRaw: TableStatus | number | string;
        let orderId: string | undefined;

        if (typeof arg1 === 'object' && arg1 !== null) {
          tableId = arg1.tableId;
          statusRaw = arg1.status;
          orderId = arg1.currentOrderId ?? arg1.orderId;
        } else {
          tableId = arg1;
          statusRaw = arg2!;
          orderId = arg3;
        }

        const normalizedStatus = normalizeTableStatus(statusRaw);
        const effectiveOrderId = normalizedStatus === 'Vacant' ? undefined : orderId;

        set((state) => ({
          floors: state.floors.map((floor) => ({
            ...floor,
            tables: floor.tables.map((t) =>
              t.id === tableId
                ? {
                    ...t,
                    status: normalizedStatus,
                    currentOrderId: effectiveOrderId ?? (normalizedStatus === 'Vacant' ? undefined : t.currentOrderId),
                  }
                : t
            ),
          })),
          selectedTable:
            state.selectedTable?.id === tableId
              ? {
                  ...state.selectedTable,
                  status: normalizedStatus,
                  currentOrderId: effectiveOrderId ?? (normalizedStatus === 'Vacant' ? undefined : state.selectedTable.currentOrderId),
                }
              : state.selectedTable,
        }));
      };

      // Listen for table status updates from backend
      hub.on('TableStatusChanged', handleTableStatusChange);
      hub.on('TableStatusUpdated', handleTableStatusChange);

      // Listen for newly created orders to update table occupancy
      hub.on('NewOrderCreated', (order: { id: string; tableId?: string }) => {
        if (order?.tableId) {
          const tableId = order.tableId;
          set((state) => ({
            floors: state.floors.map((floor) => ({
              ...floor,
              tables: floor.tables.map((t) =>
                t.id === tableId
                  ? { ...t, status: 'Occupied', currentOrderId: order.id }
                  : t
              ),
            })),
            selectedTable:
              state.selectedTable?.id === tableId
                ? { ...state.selectedTable, status: 'Occupied', currentOrderId: order.id }
                : state.selectedTable,
          }));
        }
      });

      // Listen for KOT events
      hub.on('KotReceived', async () => {
        await get().fetchKots();
      });

      hub.on('KotStatusChanged', async () => {
        await get().fetchKots();
      });

      hub.on('KotItemToggled', async () => {
        await get().fetchKots();
      });

      hub.on('OrderSettled', async (payload?: { orderId?: string; grandTotal?: number }) => {
        const { outletId, selectedTable } = get();
        if (outletId) {
          try {
            const res = await apiClient.get<Floor[]>(`/Floor/outlets/${outletId}/floors`);
            if (res.data) set({ floors: res.data });
          } catch (err) {
            console.error('[SignalR] Error refreshing floors on OrderSettled:', err);
          }
        }
        if (selectedTable && payload?.orderId && selectedTable.currentOrderId === payload.orderId) {
          set({
            selectedTable: { ...selectedTable, status: 'Vacant', currentOrderId: undefined },
            cart: [],
          });
        }
        await get().fetchDashboardStats();
        await get().fetchRawMaterials();
      });

      // Reconnection lifecycle handling
      hub.onreconnected(async (connectionId) => {
        console.log('[SignalR] Reconnected. Connection ID:', connectionId);
        const { outletId } = get();
        if (outletId) {
          try {
            await hub.invoke('JoinOutletGroup', outletId);
          } catch (err) {
            console.error('[SignalR] Error re-joining outlet group after reconnection:', err);
          }
        }
      });

      if (hub.state === signalR.HubConnectionState.Disconnected) {
        await hub.start();
        console.log('[SignalR] Connected to Real-time POS Hub');

        const { outletId } = get();
        if (outletId) {
          await hub.invoke('JoinOutletGroup', outletId);
        }
      }
    } catch (err) {
      console.warn('[SignalR Connection Warning]:', err);
    }
  },

  fetchKots: async () => {
    const { outletId } = get();
    if (!outletId) return;
    try {
      const res = await apiClient.get<KitchenTicket[]>(`/Orders/outlet/${outletId}/kots`);
      set({ kots: res.data || [] });
    } catch (err) {
      console.error('Error fetching KOTs:', err);
    }
  },

  toggleKotItemPrepared: async (itemId) => {
    try {
      await apiClient.post(`/Orders/kots/items/${itemId}/toggle`);
      await get().fetchKots();
    } catch (err) {
      console.error('Error toggling KOT item prepared state:', err);
    }
  },

  completeKot: async (kotId) => {
    try {
      await apiClient.post(`/Orders/kots/${kotId}/status`, { status: 4 }); // 4 is Served / Completed
      await get().fetchKots();
    } catch (err) {
      console.error('Error completing KOT:', err);
    }
  },

  fetchRawMaterials: async () => {
    const { restaurantId, outletId } = get();
    if (!restaurantId || !outletId) return;
    try {
      const res = await apiClient.get<
        Array<{
          id: string;
          name: string;
          unitOfMeasure: string;
          costPerUnit: number;
          minThresholdAlert: number;
          stocks?: Array<{ outletId: string; currentStockQuantity: number }>;
        }>
      >(`/Inventory/materials/${restaurantId}`);

      const materials: RawMaterialStockItem[] = (res.data || []).map((m) => {
        const stock = m.stocks?.find((s) => s.outletId === outletId);
        return {
          id: m.id,
          name: m.name,
          uom: m.unitOfMeasure,
          cost: m.costPerUnit,
          min: m.minThresholdAlert,
          stock: stock ? stock.currentStockQuantity : 0,
        };
      });
      set({ rawMaterials: materials });
    } catch (err) {
      console.error('Error fetching raw materials:', err);
    }
  },

  addRawMaterial: async (name, uom, cost, min) => {
    const { restaurantId } = get();
    if (!restaurantId) return;
    try {
      await apiClient.post(`/Inventory/materials/${restaurantId}`, {
        name,
        unitOfMeasure: uom,
        costPerUnit: cost,
        minThresholdAlert: min,
      });
      await get().fetchRawMaterials();
    } catch (err) {
      console.error('Error adding raw material:', err);
    }
  },

  adjustStock: async (rawMaterialId, quantityChange, reason) => {
    const { outletId } = get();
    if (!outletId) return;
    try {
      await apiClient.post(`/Inventory/stock/${outletId}/adjust`, {
        rawMaterialId,
        quantityChange,
        reason,
      });
      await get().fetchRawMaterials();
    } catch (err) {
      console.error('Error adjusting stock:', err);
    }
  },

  fetchDashboardStats: async () => {
    const { outletId } = get();
    if (!outletId) return;
    try {
      const res = await apiClient.get<DashboardStats>(`/Reports/outlet/${outletId}/dashboard`);
      set({ dashboardStats: res.data || null });
    } catch (err) {
      console.error('Error fetching dashboard stats:', err);
    }
  },

  fireKot: async () => {
    const { cart, selectedTable, outletId } = get();
    if (cart.length === 0 || !outletId) return;

    try {
      let orderId = selectedTable?.currentOrderId;
      const newItems = cart.filter((item) => !item.isFired);

      if (!orderId) {
        // Create order first
        const orderRes = await apiClient.post<OrderDetail>('/Orders', {
          outletId,
          orderType: 1, // DineIn
          tableId: selectedTable?.id,
          customerName: get().customerName || 'Table Client',
          customerPhone: get().customerPhone,
          guestCount: 2,
          items: cart.map((item) => ({
            menuItemId: item.menuItemId,
            rawMaterialId: item.rawMaterialId,
            itemName: item.name,
            variantName: item.variantName,
            quantity: item.quantity,
            unitPrice: item.unitPrice,
            notes: item.notes,
          })),
        });
        orderId = orderRes.data.id;
      } else if (newItems.length > 0) {
        // Append new items to existing order
        await apiClient.post(
          `/Orders/${orderId}/items`,
          newItems.map((item) => ({
            menuItemId: item.menuItemId,
            rawMaterialId: item.rawMaterialId,
            itemName: item.name,
            variantName: item.variantName,
            quantity: item.quantity,
            unitPrice: item.unitPrice,
            notes: item.notes,
          }))
        );
      }

      await apiClient.post(`/Orders/${orderId}/fire-kot`);

      // Refresh floor layout
      const floorRes = await apiClient.get<Floor[]>(`/Floor/outlets/${outletId}/floors`);
      if (floorRes.data && floorRes.data.length > 0) {
        set({ floors: floorRes.data });
      }

      // Re-load table to show updated KOT items as fired in the cart
      if (selectedTable) {
        const updatedTable = floorRes.data
          .flatMap((f) => f.tables)
          .find((t) => t.id === selectedTable.id);
        if (updatedTable) {
          await get().setSelectedTable(updatedTable);
        }
      }
    } catch (err) {
      console.error('Error firing KOT:', err);
    }
  },

  settleOrder: async (paymentMode) => {
    const { cart, selectedTable, outletId, discountPercent } = get();
    if (!outletId) return;

    try {
      let orderId = selectedTable?.currentOrderId;
      const newItems = cart.filter((item) => !item.isFired);

      if (!orderId && cart.length > 0) {
        const orderRes = await apiClient.post<OrderDetail>('/Orders', {
          outletId,
          orderType: 1,
          tableId: selectedTable?.id,
          customerName: get().customerName || 'Walk-In Customer',
          customerPhone: get().customerPhone,
          guestCount: 1,
          items: cart.map((item) => ({
            menuItemId: item.menuItemId,
            rawMaterialId: item.rawMaterialId,
            itemName: item.name,
            variantName: item.variantName,
            quantity: item.quantity,
            unitPrice: item.unitPrice,
            notes: item.notes,
          })),
        });
        orderId = orderRes.data.id;
      } else if (orderId && newItems.length > 0) {
        await apiClient.post(
          `/Orders/${orderId}/items`,
          newItems.map((item) => ({
            menuItemId: item.menuItemId,
            rawMaterialId: item.rawMaterialId,
            itemName: item.name,
            variantName: item.variantName,
            quantity: item.quantity,
            unitPrice: item.unitPrice,
            notes: item.notes,
          }))
        );
      }

      if (!orderId) return;

      const subTotal = cart.reduce((acc, item) => acc + item.totalPrice, 0);
      const discountAmount = (subTotal * discountPercent) / 100;

      await apiClient.post(`/Orders/${orderId}/settle`, {
        paymentMode,
        discountAmount,
        discountReason: discountPercent > 0 ? 'Loyalty/Special' : null,
        serviceChargeAmount: 0,
        paymentTransactionRef: 'TXN-' + Date.now(),
      });

      const floorRes = await apiClient.get<Floor[]>(`/Floor/outlets/${outletId}/floors`);
      if (floorRes.data && floorRes.data.length > 0) {
        set({ floors: floorRes.data });
      }
      get().clearCart();
    } catch (err) {
      console.error('Error settling order:', err);
    }
  },

  createMenuItem: async (categoryId, name, shortCode, basePrice, isVegetarian, kitchenStation) => {
    const { restaurantId } = get();
    if (!restaurantId) return;
    try {
      await apiClient.post(`/Menu/items/${restaurantId}`, {
        categoryId,
        name,
        shortCode,
        basePrice,
        isVegetarian,
        gstPercent: 5.0,
        kitchenStation,
        variants: [],
        modifierGroups: [],
      });
      const catRes = await apiClient.get<Category[]>(`/Menu/categories/${restaurantId}`);
      if (catRes.data && catRes.data.length > 0) {
        set({ categories: catRes.data });
      }
    } catch (err) {
      console.error('Error creating menu item:', err);
    }
  },

  login: async (username, password) => {
    set({ isLoading: true });
    try {
      const res = await apiClient.post('/Auth/login', { username, password });
      if (res.data && res.data.token) {
        const { token, role, fullName, restaurantId, outletId, restaurantName, outletName } = res.data;

        localStorage.setItem('pos_token', token);
        localStorage.setItem('token', token);
        localStorage.setItem('pos_role', role);
        localStorage.setItem('pos_user_name', fullName);
        localStorage.setItem('pos_restaurant_id', restaurantId);
        localStorage.setItem('pos_outlet_id', outletId || '');
        localStorage.setItem('pos_restaurant_name', restaurantName || '');
        localStorage.setItem('pos_outlet_name', outletName || '');

        set({
          token,
          userRole: role,
          fullName,
          restaurantId,
          outletId,
          restaurantName,
          outletName,
          isConnectedToApi: true,
        });

        if (role === 'InventoryUser') {
          set({ activeTab: 'inventory' });
        } else if (role === 'SuperAdmin') {
          set({ activeTab: 'users' });
        } else {
          set({ activeTab: 'tables' });
        }

        await get().fetchInitialData();
        return true;
      }
      return false;
    } catch (err) {
      console.error('Login failed:', err);
      return false;
    } finally {
      set({ isLoading: false });
    }
  },

  logout: () => {
    localStorage.removeItem('pos_token');
    localStorage.removeItem('token');
    localStorage.removeItem('pos_role');
    localStorage.removeItem('pos_user_name');
    localStorage.removeItem('pos_restaurant_id');
    localStorage.removeItem('pos_outlet_id');
    localStorage.removeItem('pos_restaurant_name');
    localStorage.removeItem('pos_outlet_name');

    set({
      token: null,
      userRole: null,
      fullName: null,
      restaurantId: null,
      outletId: null,
      restaurantName: null,
      outletName: null,
      isConnectedToApi: false,
      floors: [],
      categories: [],
      cart: [],
      kots: [],
      rawMaterials: [],
    });
  },

  switchContext: async (restaurantId, outletId) => {
    const previousOutletId = get().outletId;

    localStorage.setItem('pos_restaurant_id', restaurantId);
    localStorage.setItem('pos_outlet_id', outletId);

    const restaurantObj = get().allRestaurants.find((r) => r.id === restaurantId);
    const outletObj = restaurantObj?.outlets.find((o) => o.id === outletId);
    const rName = restaurantObj?.name || 'Restaurant';
    const oName = outletObj?.name || 'Outlet';

    localStorage.setItem('pos_restaurant_name', rName);
    localStorage.setItem('pos_outlet_name', oName);

    set({
      restaurantId,
      outletId,
      restaurantName: rName,
      outletName: oName,
      floors: [],
      categories: [],
      cart: [],
      kots: [],
    });

    try {
      const catRes = await apiClient.get<Category[]>(`/Menu/categories/${restaurantId}`);
      set({ categories: catRes.data || [] });

      if (outletId) {
        const floorRes = await apiClient.get<Floor[]>(`/Floor/outlets/${outletId}/floors`);
        set({ floors: floorRes.data || [] });
      }

      // Re-register SignalR Hub connection with new outlet group
      const hub = getPosHubConnection();
      if (hub.state === signalR.HubConnectionState.Connected) {
        if (previousOutletId && previousOutletId !== outletId) {
          try {
            await hub.invoke('LeaveOutletGroup', previousOutletId);
          } catch (e) {
            console.warn('[SignalR] Failed leaving previous outlet group:', e);
          }
        }
        if (outletId) {
          try {
            await hub.invoke('JoinOutletGroup', outletId);
          } catch (e) {
            console.warn('[SignalR] Failed joining new outlet group:', e);
          }
        }
      }

      await get().fetchRawMaterials();
      await get().fetchKots();
      await get().fetchDashboardStats();
    } catch (err) {
      console.error('Error switching restaurant context:', err);
    }
  },

  fetchAllRestaurants: async () => {
    try {
      const res = await apiClient.get<RestaurantWithOutlets[]>('/Auth/restaurants');
      if (res.data) {
        set({ allRestaurants: res.data });
      }
    } catch (err) {
      console.error('Error fetching all restaurants:', err);
    }
  },

  addRestaurant: async (name, tagline, outletName, floorName) => {
    try {
      await apiClient.post('/Auth/restaurants/admin', { name, tagline, outletName, floorName });
      await get().fetchAllRestaurants();
    } catch (err) {
      console.error('Error creating restaurant:', err);
      throw err;
    }
  },

  fetchUsers: async () => {
    const { restaurantId } = get();
    if (!restaurantId) return;
    try {
      const res = await apiClient.get<User[]>(`/Auth/users/${restaurantId}`);
      if (res.data) {
        set({ users: res.data });
      }
    } catch (err) {
      console.error('Error fetching users:', err);
    }
  },

  createUser: async (fullName, username, password, role, pin) => {
    const { restaurantId, outletId } = get();
    if (!restaurantId) return;
    try {
      await apiClient.post('/Auth/users', {
        restaurantId,
        outletId,
        fullName,
        username,
        password,
        role,
        pin,
      });
      await get().fetchUsers();
    } catch (err) {
      console.error('Error creating user:', err);
      throw err;
    }
  },

  registerRestaurant: async (
    name,
    tagline,
    address,
    phone,
    adminFullName,
    adminUsername,
    adminPassword
  ) => {
    try {
      await apiClient.post('/Auth/register', {
        name,
        tagline,
        address,
        phone,
        adminFullName,
        adminUsername,
        adminPassword,
      });
    } catch (err) {
      console.error('Error registering restaurant:', err);
      throw err;
    }
  },
}));
