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

export interface RestaurantTable {
  id: string;
  floorId: string;
  tableNumber: string;
  seatingCapacity: number;
  status: 'Vacant' | 'Occupied' | 'Billed' | 'Reserved';
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
  variantName?: string;
  quantity: number;
  cookingNotes?: string;
  isPrepared: boolean;
}

export interface KitchenTicket {
  id: string;
  orderId: string;
  orderNumber: string;
  tableNumber: string;
  kitchenStation: string;
  status: 'Pending' | 'Preparing' | 'Ready' | 'Served';
  firedAt: string;
  items: KotItem[];
}
