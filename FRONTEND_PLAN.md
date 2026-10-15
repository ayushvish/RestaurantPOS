# Restaurant POS & Management System - Frontend Architecture & Implementation Plan

## 1. Overview
A high-speed, touch-optimized, responsive Web & Desktop POS application modeled after **Petpooja**. Designed for restaurant cashiers, waiters/captains, kitchen display operators (KDS), and restaurant managers.

---

## 2. Tech Stack
- **Framework**: Next.js (React 19 / TypeScript) / Vite + React SPA
- **Styling & UI**: Tailwind CSS, Shadcn UI, Lucide Icons, Framer Motion (for smooth transitions)
- **State Management**: Zustand / Redux Toolkit (Offline cart, Table states, Active order caching)
- **Real-Time Client**: `@microsoft/signalr` (Instant Table status, Live KOT updates, KDS alerts with sound notifications)
- **Hardware / Printing**: Browser Thermal Print integration (ESC/POS emulation & 80mm/58mm receipt layout)

---

## 3. Core Frontend Modules & Screen Architecture

```
src/
├── app/ or pages/
│   ├── (auth)/
│   │   ├── login/                 # Username/Password Login & PIN Punch Pad (4-digit quick waiter/cashier switch)
│   │   └── register/              # New Restaurant & Outlet Onboarding Wizard
│   ├── (pos)/
│   │   ├── pos-terminal/          # Main High-Speed Billing Terminal (Search, Categories, Modifiers, Cart, Split, Settlement)
│   │   ├── table-view/            # Interactive 2D Floor & Table Layout (Vacant/Occupied/Billed with Timer)
│   │   └── kds/                   # Kitchen Display System (Live Ticket Cards, Station Filter, Audio Alerts)
│   ├── (admin)/
│   │   ├── dashboard/             # Real-time Sales, Covers, Hourly graph, Top selling items
│   │   ├── menu-management/       # Categories, Items, Variants, Modifiers, 86ing (Stock-out toggle)
│   │   ├── floor-designer/        # Drag-and-drop 2D Table & Floor layout builder
│   │   ├── inventory/             # Stock list, Recipe BOM mapping, Purchase records, Low stock alerts
│   │   └── reports/               # Daily sales summary, Tax breakdown, Payment reconciliations
├── components/
│   ├── pos/
│   │   ├── MenuGrid.tsx           # Category tabs + Search + Food items cards with veg/non-veg tags
│   │   ├── OrderCart.tsx          # Running cart, qty modifiers, custom notes, item voiding
│   │   ├── TableCard.tsx          # Dynamic color-coded table box with running total & time elapsed
│   │   ├── VariantModal.tsx       # Popup for Half/Full, Add-ons & Modifiers
│   │   ├── PaymentModal.tsx       # Cash, UPI QR code generator, Card, Split payment & Discount punch
│   │   └── ThermalReceipt.tsx     # 80mm/58mm printable bill receipt
│   └── kds/
│       └── KotCard.tsx            # Digital KOT card with prep timer, item ready checkboxes & finish button
├── hooks/
│   ├── usePosHub.ts               # SignalR hook for real-time table sync and KOT events
│   └── useKeyboardShortcuts.ts    # F1-F12 shortcuts for fast cashier keyboard-only billing
└── stores/
    ├── useCartStore.ts            # Local active order state, modifiers, discounts
    └── useAuthStore.ts            # JWT session, active outlet & active user
```

---

## 4. Key UI & User Experience Highlights
1. **High-Speed POS Billing Terminal**:
   - Touch-friendly category tabs with high-contrast veg/non-veg indicators.
   - Quick search by item name or short code (e.g. `BTR` for Butter Chicken).
   - Keyboard shortcuts (`F2` for search, `F4` for payment, `F8` to fire KOT, `F9` for quick print).
2. **Visual Floor & Table Map**:
   - Visual floor plan showing real-time color status:
     - 🟢 **Vacant**
     - 🔴 **Occupied** (shows running amount & elapsed dining time)
     - 🟡 **KOT Placed / In Prep**
     - 🔵 **Bill Printed / Pending Payment**
   - Click to open running table order, transfer table, or merge tables.
3. **Kitchen Display System (KDS)**:
   - Full-screen auto-updating board showing pending KOT tickets sorted by elapsed time.
   - Color shifts from Green -> Yellow -> Red as ticket age exceeds 15+ minutes.
   - Audio chime upon receiving new KOT.
4. **Thermal Bill Printing**:
   - Instant 80mm thermal receipt pop-up with restaurant logo, GST details, FSSAI number, itemized charges, and UPI QR code.

---

## 5. Phased Frontend Development Plan
* **Phase 1: Project Setup & Design System** (`D:\AyushV\RestaurantPOS\RestaurantPOS.Client`)
  - Initialize Next.js / React + Tailwind CSS + Lucide Icons + SignalR.
  - Setup authentication and layout shells (POS Header, Floor Tab, KDS Switcher).
* **Phase 2: Table Layout & Floor View**
  - Interactive table grid with real-time occupancy indicators.
* **Phase 3: High-Speed POS Terminal & Cart Engine**
  - Menu grid, variant & add-on popups, order cart, and keyboard shortcuts.
* **Phase 4: Real-time KDS & KOT Hub**
  - KOT fire action, SignalR connection, and interactive Kitchen Display screen.
* **Phase 5: Payment Settlement & Thermal Receipt**
  - Split payments, discount reasons, UPI payment simulation, and thermal print template.
* **Phase 6: Admin Dashboard & Inventory Management**
  - Real-time revenue analytics, category performance, recipe BOM setup, and inventory alerts.
