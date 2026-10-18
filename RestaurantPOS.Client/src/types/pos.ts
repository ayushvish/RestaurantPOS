export interface Restaurant {
  id: string;
  name: string;
  tagline?: string;
  gstNumber?: string;
  currencySymbol: string;
  defaultGstPercent: number;
}

export interface MenuItemVariant {
  id: string;
  name: string;
  price: number;
  isDefault: boolean;
}

export interface MenuItemModifierOption {
  id: string;
  name: string;
  price: number;
}

export interface MenuItemModifierGroup {
  id: string;
  title: string;
  minSelection: number;
  maxSelection: number;
  options: MenuItemModifierOption[];
}

export interface MenuItem {
  id: string;
  categoryId: string;
  name: string;
  shortCode?: string;
  description?: string;
  basePrice: number;
  isVegetarian: boolean;
  isAvailable: boolean;
  kitchenStation: string;
  variants?: MenuItemVariant[];
  modifierGroups?: MenuItemModifierGroup[];
}

export interface Category {
  id: string;
  name: string;
  displayOrder: number;
  menuItems: MenuItem[];
}

export type TableStatus = 'Vacant' | 'Occupied' | 'Billed' | 'Reserved';

export type OrderStatus =
  | 'Draft'
  | 'KotPlaced'
  | 'InKitchen'
  | 'Served'
  | 'Paid'
  | 'Cancelled'
  | 'Void'
  | 'Active'
  | 'Preparing'
  | 'Ready'
  | 'Billed';

export type KotStatus = 'Pending' | 'Preparing' | 'Ready' | 'Served' | 'Cancelled';

export type PaymentMode = 'Cash' | 'Card' | 'UPI' | 'Split' | 'DueCustomerLedger';

export type UserRole =
  | 'SuperAdmin'
  | 'RestaurantUser'
  | 'Manager'
  | 'Cashier'
  | 'Waiter'
  | 'Chef'
  | 'InventoryUser';

export interface RestaurantTable {
  id: string;
  floorId: string;
  tableNumber: string;
  seatingCapacity: number;
  status: TableStatus;
  currentOrderId?: string;
  posX: number;
  posY: number;
}

export interface Floor {
  id: string;
  outletId: string;
  name: string;
  tables: RestaurantTable[];
}

export interface CartItem {
  id: string; // unique for this cart line
  menuItemId: string | null;
  rawMaterialId?: string | null;
  name: string;
  variantName?: string;
  unitPrice: number;
  quantity: number;
  notes?: string;
  totalPrice: number;
  isRawMaterial?: boolean;
  isFired?: boolean;
}

export interface KotItem {
  id: string;
  itemName: string;
  variantName?: string | null;
  quantity: number;
  cookingNotes?: string | null;
  isPrepared: boolean;
}

export interface KitchenTicket {
  id: string;
  orderId: string;
  orderNumber: string;
  tableNumber: string;
  kotNumber?: string;
  kitchenStation: string;
  status: KotStatus | number | string;
  firedAt: string;
  items: KotItem[];
}

export interface OutletSummary {
  id: string;
  name: string;
}

export interface RestaurantWithOutlets {
  id: string;
  name: string;
  tagline?: string;
  outlets: OutletSummary[];
}

export interface RawMaterialStockItem {
  id: string;
  name: string;
  uom: string;
  cost: number;
  min: number;
  stock: number;
}

export interface PaymentModeBreakdown {
  paymentMode: string;
  total: number;
  count: number;
}

export interface TopSellingItem {
  itemName: string;
  quantitySold: number;
  revenue: number;
}

export interface DashboardStats {
  totalSales: number;
  directMaterialSales: number;
  totalOrders: number;
  totalGuests: number;
  averageTicketSize: number;
  paymentBreakdown: PaymentModeBreakdown[];
  topItems: TopSellingItem[];
}

export interface User {
  id: string;
  fullName: string;
  username: string;
  role: string;
  pin?: string;
  isActive?: boolean;
  restaurantName?: string;
}

export interface OrderDetailItem {
  id?: string;
  menuItemId?: string | null;
  rawMaterialId?: string | null;
  itemName: string;
  variantName?: string | null;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
  isFired?: boolean;
  notes?: string | null;
}

export interface OrderDetail {
  id: string;
  orderNumber: string;
  orderType: number | string;
  status: OrderStatus | number;
  tableId?: string | null;
  items: OrderDetailItem[];
  subTotal: number;
  taxAmount: number;
  grandTotal: number;
  isPaid: boolean;
}

