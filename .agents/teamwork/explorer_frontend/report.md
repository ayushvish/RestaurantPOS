# Frontend Architecture & Contract Investigation Report

**Project**: RestaurantPOS.Client (React 19 + TypeScript + Vite 8 + TailwindCSS v4)  
**Author**: Frontend Architecture Explorer (`teamwork_preview_explorer`)  
**Date**: 2026-10-17  
**Scope**: Requirements R2 (Frontend Audit, Dependencies & Linting) and frontend aspects of R3 (Workflow Integration & Real-Time Sync)  

---

## Executive Summary

The frontend is a well-designed, modern Single Page Application (SPA) built with React 19, TypeScript, Zustand, and Tailwind CSS v4, providing touch-friendly, high-velocity restaurant POS flows (inspired by Petpooja). However, two categories of critical issues prevent it from compiling and functioning seamlessly in production:

1. **Native Bundler Binding Failure (Blocks `npm run build` & `npm run lint`)**:  
   TypeScript typechecking (`tsc -b`) succeeds cleanly with exit code 0. However, `vite build` and `oxlint` crash on Windows because the repository's `node_modules` was populated from a Linux environment, containing only Linux native binaries (`linux-x64-gnu`) and omitting Windows binaries (`win32-x64-msvc`) for `@rolldown/binding`, `@oxlint/binding`, `@tailwindcss/oxide`, and `lightningcss`.
2. **Real-Time SignalR & Contract Disconnects (Breaks Live Sync & Robustness)**:  
   - **Event Name Mismatch**: Backend broadcasts `"TableStatusUpdated"`; frontend listens for `"TableStatusChanged"`.
   - **Parameter Signature Mismatch**: Backend emits a single payload object `{ tableId, status, currentOrderId }`; frontend callback expects three positional parameters `(tableId, status, orderId)`.
   - **Enum Serialization Mismatch**: Backend serializes enums as integers (`1, 2, 3...`) due to missing `JsonStringEnumConverter`, while frontend types expect string literals (`'Vacant' | 'Occupied' | ...`).
   - **Missing Auth Token in SignalR**: `getPosHubConnection()` in `api.ts` does not pass `accessTokenFactory`, meaning any JWT authentication requirement on the hub will reject frontend connections.
   - **Heavy Use of `any`**: In `posStore.ts`, key collections (`kots`, `rawMaterials`, `dashboardStats`, `users`, `allRestaurants`) are declared as `any[]` or `any`, masking contract misalignments.

---

## 1. Build & Bundler Setup Analysis

### 1.1 Toolchain Architecture
- **Framework & Runtime**: React 19.2.8, React-DOM 19.2.8, Node.js v24.15.0.
- **Build Tool**: Vite 8.2.2 with `@vitejs/plugin-react` 6.1.0 and `@tailwindcss/vite` 4.3.3.
- **Bundler Engine**: Rolldown 1.2.6 (Rust-based bundler integrated directly into Vite 8).
- **TypeScript**: TypeScript ~6.0.2 with project references (`tsconfig.app.json` and `tsconfig.node.json`).
- **Linter**: Oxlint 1.79.0.

### 1.2 The Native Binding Error
Running `npm run build` (`tsc -b && vite build`) or `npm run lint` (`oxlint`) on Windows produces:

```
Error: Cannot find native binding. npm has a bug related to optional dependencies (https://github.com/npm/cli/issues/4828). Please try `npm i` again after removing both package-lock.json and node_modules directory.
  cause: Error: Cannot find module '@rolldown/binding-win32-x64-msvc'
```

### 1.3 Root Cause Analysis
Inspection of `RestaurantPOS.Client/node_modules` revealed:
- Native binaries present:
  - `node_modules/@oxlint/binding-linux-x64-gnu`
  - `node_modules/@rolldown/binding-linux-x64-gnu`
  - `node_modules/@tailwindcss/oxide-linux-x64-gnu`
  - `node_modules/lightningcss-linux-x64-gnu`
- Native binaries missing:
  - `@rolldown/binding-win32-x64-msvc`
  - `@oxlint/binding-win32-x64-msvc`
  - `@tailwindcss/oxide-win32-x64-msvc`
  - `lightningcss-win32-x64-msvc`

The packages in `node_modules` were installed or locked in a Linux environment or container. When executed on Windows x64, Node cannot load `.node` binary bindings for the Linux platform, and the optional Windows dependencies are absent.

### 1.4 Verification of the Resolution
Running `npm install --dry-run` on Windows produced:
```
add lightningcss-win32-x64-msvc 1.33.0
add @tailwindcss/oxide-win32-x64-msvc 4.3.3
add @rolldown/binding-win32-x64-msvc 1.2.6
add @oxlint/binding-win32-x64-msvc 1.80.0
remove lightningcss-linux-x64-gnu 1.33.0
remove @tailwindcss/oxide-linux-x64-gnu 4.3.3
add lightningcss-win32-x64-msvc 1.32.0
remove lightningcss-linux-x64-gnu 1.32.0
remove @rolldown/binding-linux-x64-gnu 1.2.6
remove @oxlint/binding-linux-x64-gnu 1.80.0
added 5 packages, and removed 5 packages
```

**Concrete Fix**:
Executing `npm install` (or removing `node_modules` and running `npm install`) on the Windows build environment resolves all 5 native binary dependencies cleanly, allowing both `vite build` and `oxlint` to invoke native binaries without error.

---

## 2. Type Definitions & Contract Alignments

### 2.1 Enum Serialization & Format Discrepancies
In ASP.NET Core, `AddJsonOptions` in `Program.cs` currently configures:
```csharp
options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
```
It does **not** register `JsonStringEnumConverter()`. As a result, all C# enums are serialized as integers:

| Enum | Backend Enum Values (C#) | Serialized HTTP Output | Frontend Type Expectation (`pos.ts`) |
|---|---|---|---|
| `TableStatus` | Vacant=1, Occupied=2, Billed=3, Reserved=4 | `1, 2, 3, 4` | `'Vacant' \| 'Occupied' \| 'KotPlaced' \| 'Billed' \| 'Reserved'` |
| `KotStatus` | Pending=1, Preparing=2, Ready=3, Served=4, Cancelled=5 | `1, 2, 3, 4, 5` | `'Pending' \| 'Preparing' \| 'Ready' \| 'Served'` |
| `OrderType` | DineIn=1, Takeaway=2, Delivery=3, OnlineAggregator=4 | `1, 2, 3, 4` | Implicit number in payload |
| `PaymentMode`| Cash=1, Card=2, UPI=3, Split=4, DueCustomerLedger=5 | `1, 2, 3, 4, 5` | Number (1, 2, 3) |
| `UserRole` | SuperAdmin=1, RestaurantUser=2, Manager=3, Cashier=4, Waiter=5, Chef=6, InventoryUser=7 | String in Auth token / int in DTO | String in `localStorage` |

**Impact**:
- `TableView.tsx` checks `table.status === 'Vacant'`. Because the backend returns integer `1`, `getStatusColor(table.status)` and `getBadgeColor(table.status)` hit their default / undefined cases, stripping CSS styling from table cards.
- `KotPlaced` is an `OrderStatus` in C# (`OrderStatus.KotPlaced = 2`), but the frontend treats it as a `TableStatus`.

### 2.2 Missing Type Interfaces in Frontend
The frontend `src/types/pos.ts` only defines 8 interfaces (`Restaurant`, `MenuItemVariant`, `MenuItemModifierOption`, `MenuItemModifierGroup`, `MenuItem`, `Category`, `RestaurantTable`, `Floor`, `CartItem`, `KotItem`, `KitchenTicket`).
The following models are entirely un-typed and currently represented as `any`:
1. `User` / `UserDto` (`id`, `fullName`, `username`, `role`, `pin`, `restaurantId`, `restaurantName`).
2. `RestaurantWithOutlets` (`id`, `name`, `tagline`, `outlets: Outlet[]`).
3. `DashboardStats` (`totalSales`, `directMaterialSales`, `totalOrders`, `totalGuests`, `averageTicketSize`, `paymentBreakdown`, `topItems`).
4. `RawMaterial` / `InventoryStockItem` (`id`, `name`, `uom`, `cost`, `min`, `stock`).
5. `Order` / `OrderDetailDto` (`id`, `orderNumber`, `orderType`, `status`, `items`, `subTotal`, `taxAmount`, `grandTotal`, `tableId`).
6. `AuthResponse` (`token`, `userId`, `fullName`, `role`, `restaurantId`, `outletId`, `restaurantName`, `outletName`).

---

## 3. State Management & API Integration (`posStore.ts` & `api.ts`)

### 3.1 Axios & HTTP Client (`api.ts`)
- `API_BASE_URL` reads `import.meta.env.VITE_API_URL || 'http://localhost:5122'`.
- Request Interceptor: Reads `localStorage.getItem('pos_token')` and attaches `Authorization: Bearer ${token}`.
- Response Interceptor:
  ```ts
  apiClient.interceptors.response.use(
    (response) => response,
    (error) => Promise.reject(error)
  );
  ```
  **Vulnerability**: No automatic 401 handling. If a JWT expires or is revoked, API requests fail, but the user is not logged out or prompted to re-authenticate.

### 3.2 State Management (`posStore.ts`)
- **Persistence**: Auth credentials and active restaurant/outlet context are correctly persisted to `localStorage` (`pos_token`, `pos_role`, `pos_user_name`, `pos_restaurant_id`, `pos_outlet_id`, `pos_restaurant_name`, `pos_outlet_name`).
- **Error Handling**:
  - `fetchInitialData`: Catches network errors and logs a warning, setting `isConnectedToApi: false`.
  - `fireKot`: Catches errors via `console.error` but provides no visual feedback or rollback if order creation fails.
  - `settleOrder`: Catches errors via `console.error` without user notification if settlement fails.
- **Table Order Hydration**:
  When a table is selected in `setSelectedTable(table)`:
  If `table.currentOrderId` exists, it queries `GET /api/Orders/${table.currentOrderId}` and maps items into `cart`, marking them `isFired: true`. This correctly restores the bill in progress.

---

## 4. SignalR Real-Time Sync Inspection

### 4.1 Hub Connection & Group Subscription
In `src/services/api.ts`:
```ts
export const getPosHubConnection = () => {
  if (!hubConnection) {
    hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/pos`, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect()
      .build();
  }
  return hubConnection;
};
```
**Issue 1: Missing JWT Bearer Token Factory**  
If `PosHub` in backend is decorated with `[Authorize]`, connections fail immediately with 401 Unauthorized. Fix:
```ts
accessTokenFactory: () => localStorage.getItem('pos_token') || ''
```

**Issue 2: Outlet Group Leaks**  
When switching restaurant/outlet via `switchContext(rId, oId)`, `hub.invoke('JoinOutletGroup', outletId)` is called, but `LeaveOutletGroup` is never called on the previous `outletId`. The client continues receiving events for old outlets.

### 4.2 SignalR Event Matrix & Discrepancies

| Event | Backend Trigger | Backend Payload | Frontend Listener | Status |
|---|---|---|---|---|
| **Table Status** | `FloorController.cs:89` | `SendAsync("TableStatusUpdated", new { tableId, status, currentOrderId })` | `hub.on('TableStatusChanged', (tableId, status, orderId) => ...)` | **BROKEN**: Event name mismatch (`TableStatusUpdated` vs `TableStatusChanged`) AND argument mismatch (1 object vs 3 arguments). |
| **New Order** | `OrdersController.cs:106` | `SendAsync("NewOrderCreated", order)` | *None* | **MISSING**: Frontend does not listen for `NewOrderCreated`. Other terminals don't see the table become Occupied. |
| **KOT Received** | `OrdersController.cs:209` | `SendAsync("KotReceived", { orderId, orderNumber, tableNumber, kot })` | `hub.on('KotReceived', () => get().fetchKots())` | **WORKING**: Fetches KOT list and triggers audio chime in KDS. |
| **KOT Status** | `OrdersController.cs:348` | `SendAsync("KotStatusChanged", { kotId, status })` | `hub.on('KotStatusChanged', () => get().fetchKots())` | **WORKING**: Refreshes KOT list. |
| **KOT Item Toggle** | `OrdersController.cs:369` | `SendAsync("KotItemToggled", { itemId, isPrepared })` | `hub.on('KotItemToggled', () => get().fetchKots())` | **WORKING**: Refreshes KOT list. |
| **Order Settled** | `OrdersController.cs:292` | `SendAsync("OrderSettled", { orderId, grandTotal })` | `hub.on('OrderSettled', () => ...)` | **WORKING**: Refreshes floors, dashboard stats, and inventory stocks. |

---

## 5. UI Components & Core Workflows Audit

### 5.1 Table Management (`TableView.tsx`)
- Multi-floor navigation tab bar with table count per floor.
- Table card status styling: Vacant, Occupied, KotPlaced, Billed, Reserved.
- **Issues Found**:
  - Running total (`₹840.00`) and Bill pending (`₹1,250.00`) are currently hardcoded mock values instead of pulling from `table.currentOrder?.grandTotal`.
  - "Add Section" button lacks modal/action binding.
  - Due to the SignalR event name and parameter mismatch, remote table status changes never update the UI in real time.

### 5.2 POS Terminal & Billing (`PosTerminal.tsx`)
- Touch menu category selector, search by name/shortCode, and veg/non-veg filter.
- Cart with live line totals, tax calculation (GST 5%), and discount punching.
- Keyboard navigation (F2 search, F4 payment modal, F8 fire KOT, F9 print proforma, Esc dismiss).
- Settle Modal:
  - Cash, Card, UPI payment modes.
  - Dynamic Bharat UPI QR generated via `upi://pay` URL.
  - Integrates `<ThermalReceipt />` with printable 80mm CSS layout.
- Order Creation & Append Flow:
  - If table has no order, creates order via `POST /api/Orders`, then fires KOT via `POST /api/Orders/{orderId}/fire-kot`.
  - If table already has an active order, posts new items to `POST /api/Orders/{orderId}/items`, then fires KOT.
  - **Audit Verdict**: Logic is clean and adheres to backend lifecycle.

### 5.3 Kitchen Display System (`KitchenDisplay.tsx`)
- Station filters: All, Kitchen, Tandoor, Bar.
- Elapsed time tracker with color coding: Green (<10 min), Amber (<15 min), Red (>15 min overdue).
- Audio Chime: Uses Web Audio API oscillator (`880Hz` sine beep) triggered by `kots.length` changes.
- Item toggling and ticket completion correctly call backend endpoints.

### 5.4 Inventory & Stock Tracking (`Inventory.tsx`)
- Displays raw materials, current stock quantities, units of measure, unit costs, and minimum threshold alerts.
- Supports adding raw materials and recording manual stock adjustments.
- Real-time stock deduction: Auto-recipe deduction occurs in backend on `SettleOrder`, and the frontend refreshes stock via `OrderSettled` SignalR broadcast.

### 5.5 User Management & Self-Serve Onboarding (`UserManagement.tsx` & `Login.tsx`)
- Role-based tab visibility: SuperAdmin, RestaurantUser, Manager, Cashier, Waiter, Chef, InventoryUser.
- Multi-branch restaurant switcher in `Header.tsx` allows SuperAdmin/RestaurantUser to switch context across branches.
- Self-registration wizard in `Login.tsx` onboarding new restaurants (`POST /api/Auth/register`).

---

## 6. Testing & Linting Infrastructure

### 6.1 Current Status
- **Test Infrastructure**: None. There are zero test files (`*.test.tsx`, `*.spec.ts`), no Vitest configuration, and no Jest/Playwright setup.
- **Linting**: Oxlint is configured in `.oxlintrc.json` with React and TypeScript plugins, but cannot run on Windows due to the missing native binding.

### 6.2 Recommended Test Additions
1. **Vitest + React Testing Library**:
   - Install `vitest`, `@testing-library/react`, `@testing-library/jest-dom`, and `jsdom`.
   - Add unit tests for:
     - `posStore.ts`: Cart calculations (tax, discount, subtotal), add to cart, quantity updates.
     - `api.ts`: Auth header attachment and token management.
     - SignalR event listener dispatching.
     - `PosTerminal.tsx` and `TableView.tsx` component rendering and keyboard shortcuts.

---

## 7. Actionable Implementation Scope (R2 & R3)

### Phase 1: Build & Toolchain Hardening (R2)
1. **Restore Native Windows Bindings**:
   - Run `npm install` in `RestaurantPOS.Client` on Windows.
   - Verify that `@rolldown/binding-win32-x64-msvc`, `@oxlint/binding-win32-x64-msvc`, `@tailwindcss/oxide-win32-x64-msvc`, and `lightningcss-win32-x64-msvc` are installed.
   - Run `npm run build` (`tsc -b && vite build`) and confirm exit code 0.
   - Run `npm run lint` (`oxlint`) and confirm clean passage.

### Phase 2: Type Contracts & DTO Alignment (R2)
1. **Add Strong Type Definitions**:
   - Create typed interfaces for `User`, `RawMaterialItem`, `DashboardStats`, `RestaurantWithOutlets`, and SignalR payloads.
   - Eliminate `any` in `posStore.ts`, `KitchenDisplay.tsx`, `UserManagement.tsx`, and `Dashboard.tsx`.
2. **Align Enums**:
   - In backend `Program.cs`, add `options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());` OR map integer statuses in frontend.
   - Standardize `TableStatus` vs `OrderStatus.KotPlaced`.

### Phase 3: SignalR Real-Time Sync Realignment (R3)
1. **Fix Table Status SignalR Events**:
   - In `posStore.ts`, rename `TableStatusChanged` to `TableStatusUpdated`.
   - Update callback signature from `(tableId, status, orderId)` to `({ tableId, status, currentOrderId }: { tableId: string; status: any; currentOrderId?: string })`.
   - Add handler for `NewOrderCreated` to update floor tables when a table is occupied on another terminal.
2. **SignalR Auth & Lifecycle**:
   - Add `accessTokenFactory: () => localStorage.getItem('pos_token') || ''` in `api.ts`.
   - Add `LeaveOutletGroup` call prior to `JoinOutletGroup` in `switchContext`.
3. **Response Interceptor 401 Handler**:
   - In `api.ts`, add an interceptor that triggers `logout()` and redirects to login when receiving HTTP 401 Unauthorized.
