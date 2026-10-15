import type { Category, Floor, KitchenTicket } from '../types/pos';

export const mockFloors: Floor[] = [
  {
    id: 'f1',
    outletId: 'out1',
    name: 'Ground Floor (AC)',
    tables: [
      { id: 't1', floorId: 'f1', tableNumber: 'T-01', seatingCapacity: 4, status: 'Occupied', posX: 0, posY: 0 },
      { id: 't2', floorId: 'f1', tableNumber: 'T-02', seatingCapacity: 2, status: 'Vacant', posX: 0, posY: 0 },
      { id: 't3', floorId: 'f1', tableNumber: 'T-03', seatingCapacity: 6, status: 'Billed', posX: 0, posY: 0 },
      { id: 't4', floorId: 'f1', tableNumber: 'T-04', seatingCapacity: 4, status: 'Vacant', posX: 0, posY: 0 },
      { id: 't5', floorId: 'f1', tableNumber: 'T-05', seatingCapacity: 4, status: 'Occupied', posX: 0, posY: 0 },
      { id: 't6', floorId: 'f1', tableNumber: 'T-06', seatingCapacity: 8, status: 'Vacant', posX: 0, posY: 0 },
    ],
  },
  {
    id: 'f2',
    outletId: 'out1',
    name: 'Rooftop Lounge',
    tables: [
      { id: 'rt1', floorId: 'f2', tableNumber: 'R-01', seatingCapacity: 4, status: 'Vacant', posX: 0, posY: 0 },
      { id: 'rt2', floorId: 'f2', tableNumber: 'R-02', seatingCapacity: 4, status: 'Occupied', posX: 0, posY: 0 },
      { id: 'rt3', floorId: 'f2', tableNumber: 'R-03', seatingCapacity: 2, status: 'Vacant', posX: 0, posY: 0 },
    ],
  },
];

export const mockCategories: Category[] = [
  {
    id: 'cat1',
    name: 'Starters',
    displayOrder: 1,
    menuItems: [
      { id: 'm1', categoryId: 'cat1', name: 'Paneer Tikka', shortCode: 'PNT', basePrice: 280, isVegetarian: true, isAvailable: true, kitchenStation: 'Tandoor' },
      { id: 'm2', categoryId: 'cat1', name: 'Chicken Seekh Kebab', shortCode: 'CSK', basePrice: 340, isVegetarian: false, isAvailable: true, kitchenStation: 'Tandoor' },
      { id: 'm3', categoryId: 'cat1', name: 'Crispy Corn Salt & Pepper', shortCode: 'CRC', basePrice: 220, isVegetarian: true, isAvailable: true, kitchenStation: 'Kitchen' },
      { id: 'm4', categoryId: 'cat1', name: 'Chilli Chicken Dry', shortCode: 'CCD', basePrice: 320, isVegetarian: false, isAvailable: true, kitchenStation: 'Kitchen' },
    ],
  },
  {
    id: 'cat2',
    name: 'Main Course',
    displayOrder: 2,
    menuItems: [
      { id: 'm5', categoryId: 'cat2', name: 'Butter Chicken', shortCode: 'BTC', basePrice: 420, isVegetarian: false, isAvailable: true, kitchenStation: 'Kitchen' },
      { id: 'm6', categoryId: 'cat2', name: 'Dal Makhani', shortCode: 'DLM', basePrice: 290, isVegetarian: true, isAvailable: true, kitchenStation: 'Kitchen' },
      { id: 'm7', categoryId: 'cat2', name: 'Paneer Butter Masala', shortCode: 'PBM', basePrice: 360, isVegetarian: true, isAvailable: true, kitchenStation: 'Kitchen' },
      { id: 'm8', categoryId: 'cat2', name: 'Kadai Murgh', shortCode: 'KDM', basePrice: 390, isVegetarian: false, isAvailable: true, kitchenStation: 'Kitchen' },
    ],
  },
  {
    id: 'cat3',
    name: 'Breads & Rice',
    displayOrder: 3,
    menuItems: [
      { id: 'm9', categoryId: 'cat3', name: 'Butter Naan', shortCode: 'BTN', basePrice: 60, isVegetarian: true, isAvailable: true, kitchenStation: 'Tandoor' },
      { id: 'm10', categoryId: 'cat3', name: 'Garlic Naan', shortCode: 'GLN', basePrice: 80, isVegetarian: true, isAvailable: true, kitchenStation: 'Tandoor' },
      { id: 'm11', categoryId: 'cat3', name: 'Jeera Rice', shortCode: 'JRR', basePrice: 180, isVegetarian: true, isAvailable: true, kitchenStation: 'Kitchen' },
      { id: 'm12', categoryId: 'cat3', name: 'Dum Biryani (Chicken)', shortCode: 'CKB', basePrice: 380, isVegetarian: false, isAvailable: true, kitchenStation: 'Kitchen' },
    ],
  },
  {
    id: 'cat4',
    name: 'Beverages & Mocktails',
    displayOrder: 4,
    menuItems: [
      { id: 'm13', categoryId: 'cat4', name: 'Virgin Mojito', shortCode: 'VMJ', basePrice: 160, isVegetarian: true, isAvailable: true, kitchenStation: 'Bar' },
      { id: 'm14', categoryId: 'cat4', name: 'Cold Coffee with Ice Cream', shortCode: 'CCF', basePrice: 190, isVegetarian: true, isAvailable: true, kitchenStation: 'Bar' },
      { id: 'm15', categoryId: 'cat4', name: 'Fresh Lime Soda (Sweet/Salt)', shortCode: 'FLS', basePrice: 90, isVegetarian: true, isAvailable: true, kitchenStation: 'Bar' },
    ],
  },
];

export const mockKots: KitchenTicket[] = [
  {
    id: 'kot-101',
    orderId: 'ord-1',
    orderNumber: 'ORD-20260827-001',
    tableNumber: 'T-01',
    kitchenStation: 'Kitchen',
    status: 'Pending',
    firedAt: '2026-08-27T20:45:00Z',
    items: [
      { id: 'ki1', itemName: 'Butter Chicken', quantity: 1, cookingNotes: 'Less spicy', isPrepared: false },
      { id: 'ki2', itemName: 'Butter Naan', quantity: 3, isPrepared: false },
    ],
  },
  {
    id: 'kot-102',
    orderId: 'ord-2',
    orderNumber: 'ORD-20260827-002',
    tableNumber: 'T-05',
    kitchenStation: 'Kitchen',
    status: 'Preparing',
    firedAt: '2026-08-27T20:38:00Z',
    items: [
      { id: 'ki3', itemName: 'Paneer Tikka', quantity: 2, isPrepared: true },
      { id: 'ki4', itemName: 'Crispy Corn Salt & Pepper', quantity: 1, isPrepared: false },
    ],
  },
];
