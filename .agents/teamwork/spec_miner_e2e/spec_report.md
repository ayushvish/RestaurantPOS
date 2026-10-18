# End-to-End Workflow & Integration Specification Report

**Project**: RestaurantPOS (ASP.NET Core Web API + React TypeScript POS Client)  
**Workspace**: `D:\AyushV\RestaurantPOS`  
**Report Location**: `D:\AyushV\RestaurantPOS\.agents\teamwork\spec_miner_e2e\spec_report.md`  
**Author**: E2E Workflow & Integration Spec Miner  
**Date**: 2026-10-17  
**Scope**: Full end-to-end specifications, interface contracts, DTO parity, SignalR real-time event sync, defect analysis, and automated integration test harness strategy for Requirement R3.

---

## 1. Executive Summary

RestaurantPOS is a full-stack restaurant point-of-sale, kitchen display, and stock inventory platform modeled after high-speed hospitality POS systems (such as Petpooja). It consists of:
- **Backend**: ASP.NET Core 10 Web API (`RestaurantPOS.API`) using Entity Framework Core with PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`), JWT Authentication, and ASP.NET Core SignalR (`PosHub.cs`).
- **Frontend**: Vite + React 19 + TypeScript SPA (`RestaurantPOS.Client`) using Tailwind CSS v4, Lucide Icons, Zustand (`posStore.ts`), Axios (`api.ts`), and `@microsoft/signalr`.

This specification report provides the authoritative blueprint for end-to-end workflows, API interface contracts, DTO parity, SignalR real-time synchronization, identified architectural gaps/discrepancies, and an executable automated testing strategy to satisfy acceptance criteria for R3.

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Auth | User Login | Authenticates user via username and SHA-256 hashed password; returns JWT. | `LoginRequest(Username, Password)` | `AuthResponse(Token, UserId, FullName, Role, RestaurantId, OutletId, RestaurantName, OutletName)` | 401 Unauthorized if invalid credentials; 500 NRE if `RestaurantId` is null in token generation. | `AuthController.cs:353`, `PosDtos.cs:5` |
| 2 | Auth | PIN Login | Authenticates staff via 4-digit PIN scoped to an outlet; returns JWT. | `PinLoginRequest(Pin, OutletId)` | `AuthResponse(...)` | 401 Unauthorized if PIN not found for outlet. | `AuthController.cs:370`, `PosDtos.cs:6` |
| 3 | Auth | Restaurant Self-Registration | Onboards new restaurant, owner user, default outlet ("Main Outlet"), ground floor, and 8 tables. | `CreateRestaurantRequest(...)` | `AuthResponse(...)` with owner JWT token | 400 BadRequest if username exists. | `AuthController.cs:280`, `PosDtos.cs:9` |
| 4 | Auth | SuperAdmin Create Restaurant | Platform admin creates restaurant, outlet, floor, 4 tables, and seeds starter menu items. | `SuperAdminCreateRestaurantRequest(Name, Tagline, OutletName, FloorName)` | `{ id, name, outlets: [...] }` | 400 BadRequest if restaurant name is empty. | `AuthController.cs:142`, `PosDtos.cs:83` |
| 5 | Auth | Staff User Provisioning | Creates staff account (Cashier, Waiter, Chef, Manager, InventoryUser). | `CreateUserRequest(RestaurantId, OutletId, FullName, Username, Password, Role, Pin)` | `{ message: "User created successfully." }` | 400 BadRequest if username exists. | `AuthController.cs:212`, `PosDtos.cs:84` |
| 6 | Auth | Multi-Tenant Restaurant Listing | Fetches restaurants and outlets accessible to caller (filtered by OwnerId if RestaurantUser). | None (JWT Claim extraction) | `List<{ id, name, outlets: [{ id, name }] }>` | Returns empty list if none accessible. | `AuthController.cs:115` |
| 7 | Auth | Staff Directory Query | Fetches users for restaurant, filtered by SuperAdmin vs RestaurantUser permissions. | Route: `restaurantId: Guid` | `List<{ id, fullName, username, role, pin, isActive, restaurantName }>` | Returns 200 with empty list if none found. | `AuthController.cs:238` |
| 8 | Floor & Tables | Floor & Table Layout Listing | Retrieves all active floors and active tables for an outlet ordered by `DisplayOrder`. | Route: `outletId: Guid` | `List<Floor>` with populated `Tables` collection | 200 with empty array if outlet has no floors. | `FloorController.cs:25` |
| 9 | Floor & Tables | Floor Creation | Creates a new floor section (e.g., "Rooftop", "AC Dining"). | `CreateFloorRequest(OutletId, Name, DisplayOrder)` | `Floor` entity | 200 with created Floor. | `FloorController.cs:37`, `PosDtos.cs:46` |
| 10 | Floor & Tables | Table Creation | Adds a table to a floor with seat capacity and designer canvas coordinates (PosX, PosY). | `CreateTableRequest(FloorId, TableNumber, SeatingCapacity, PosX, PosY)` | `RestaurantTable` entity | 200 with created Table. | `FloorController.cs:52`, `PosDtos.cs:47` |
| 11 | Floor & Tables | Table Status Mutation | Updates table state (`Vacant`, `Occupied`, `Billed`, `Reserved`); broadcasts SignalR event. | Route: `tableId: Guid`, Body: `TableStatus status` | `RestaurantTable` entity | 404 NotFound if table does not exist. | `FloorController.cs:70` |
| 12 | Orders | Order Creation | Creates Dine-In, Takeaway, or Delivery order; sets table to Occupied; emits `NewOrderCreated`. | `CreateOrderRequest(OutletId, OrderType, TableId, CustomerName, Phone, Address, GuestCount, Waiter, Notes, Items)` | `Order` entity with items and calculated totals | 404 NotFound if Outlet does not exist. | `OrdersController.cs:39`, `PosDtos.cs:59` |
| 13 | Orders | Active Orders Query | Queries all orders for an outlet that are not Paid and not Cancelled. | Route: `outletId: Guid` | `List<Order>` with items, table, and KOTs | 200 with array. | `OrdersController.cs:25` |
| 14 | Orders | Order Detail by ID | Retrieves single order with items, table, and kitchen order tickets. | Route: `orderId: Guid` | `Order` entity | 404 NotFound if orderId not found. | `OrdersController.cs:112` |
| 15 | Orders | Append Items to Running Order | Adds additional dishes/drinks to existing active order; marks items `IsFired = false`. | Route: `orderId: Guid`, Body: `List<OrderItemRequest>` | Updated `Order` entity | 404 NotFound if orderId not found. | `OrdersController.cs:126` |
| 16 | Orders & KOT | Fire KOT to Kitchen | Batches all un-fired, un-voided items into a `KitchenOrderTicket`; emits `KotReceived`. | Route: `orderId: Guid` | `KitchenOrderTicket` entity | 404 NotFound if order not found; 400 BadRequest if no new items to fire. | `OrdersController.cs:165` |
| 17 | Orders & Billing | Settle Order & Bill | Applies discount, service charge; sets Paid; releases table; deducts stock; emits `OrderSettled`. | Route: `orderId: Guid`, Body: `SettleOrderRequest(PaymentMode, Discount, Reason, ServiceCharge, TxnRef)` | Settled `Order` entity | 404 NotFound if orderId not found. | `OrdersController.cs:221`, `PosDtos.cs:73` |
| 18 | KDS | Query Active KOTs | Returns KOTs where status != Served and != Cancelled, ordered by `FiredAt`. | Route: `outletId: Guid` | `List<{ id, orderId, orderNumber, tableNumber, kotNumber, kitchenStation, status, firedAt, items }>` | 200 with list. | `OrdersController.cs:298` |
| 19 | KDS | Update KOT Status | Updates KOT status (Pending -> Preparing -> Ready -> Served); emits `KotStatusChanged`. | Route: `kotId: Guid`, Body: `UpdateKotStatusRequest(Status)` | `KitchenOrderTicket` entity | 404 NotFound if kotId not found. | `OrdersController.cs:329`, `PosDtos.cs:80` |
| 20 | KDS | Toggle KOT Item Prepared | Marks individual dish item as prepared (done); emits `KotItemToggled`. | Route: `itemId: Guid` | `KotItem` entity | 404 NotFound if itemId not found. | `OrdersController.cs:355` |
| 21 | Orders & Testing | Reset DB Orders | Clears all orders, KOTs, and resets all tables to Vacant (testing utility). | None | `{ message: "All orders cleared..." }` | 200 OK. | `OrdersController.cs:376` |
| 22 | Menu | Get Categories & Menu Tree | Returns active categories with available menu items, variants, modifier groups & options. | Route: `restaurantId: Guid` | `List<Category>` tree | 200 with categories. | `MenuController.cs:21` |
| 23 | Menu | Create Category | Creates menu category with display order. | Route: `restaurantId: Guid`, Body: `CreateCategoryRequest` | `Category` entity | 200 OK. | `MenuController.cs:37` |
| 24 | Menu | Create Menu Item | Creates dish with base price, variants, modifier groups, and kitchen station. | Route: `restaurantId: Guid`, Body: `CreateMenuItemRequest` | `MenuItem` entity | 200 OK. | `MenuController.cs:68` |
| 25 | Menu | Toggle 86ing Availability | Toggles `IsAvailable` flag on dish to instantly 86 (stock out) or restore dish. | Route: `itemId: Guid` | `{ itemId, isAvailable }` | 404 NotFound if item not found. | `MenuController.cs:128` |
| 26 | Inventory | Query Raw Materials | Fetches raw materials for restaurant along with current outlet stock levels. | Route: `restaurantId: Guid` | `List<RawMaterial>` with `Stocks` | 200 with list. | `InventoryController.cs:21` |
| 27 | Inventory | Create Raw Material | Creates ingredient/material with UOM, unit cost, and minimum threshold alert. | Route: `restaurantId: Guid`, Body: `CreateRawMaterialRequest` | `RawMaterial` entity | 200 OK. | `InventoryController.cs:32` |
| 28 | Inventory | Adjust Stock | Adjusts stock level (+ for delivery/restock, - for waste/damage) for an outlet. | Route: `outletId: Guid`, Body: `UpdateStockRequest(RawMaterialId, QuantityChange, Reason)` | `InventoryStock` entity | 200 with updated stock. | `InventoryController.cs:49` |
| 29 | Inventory | Set Recipe BOM | Configures Bill of Materials (recipe ingredients & quantities required per menu item). | Route: `menuItemId: Guid`, Body: `List<RecipeItem>` | `List<RecipeItem>` | 200 OK. Replaces existing recipe. | `InventoryController.cs:76` |
| 30 | Reports | Daily Dashboard Metrics | Aggregates today's total sales, direct material sales, covers/guests, ticket size, payments, top 5 items. | Route: `outletId: Guid` | Dashboard analytics JSON | 200 with stats. | `ReportsController.cs:20` |
| 31 | Reports | Sales Date Range Summary | Summarizes daily sales, taxes, discounts, order counts over a date range. | Route: `outletId: Guid`, Query: `fromDate`, `toDate` | `List<{ date, sales, tax, discounts, orderCount }>` | 200 with daily records. | `ReportsController.cs:67` |
| 32 | Real-Time Sync | Join Outlet Hub Group | SignalR client registers connection to receive outlet-specific real-time events. | SignalR Hub Invoke: `JoinOutletGroup(outletId)` | None (SignalR Group join) | Silent on hub failure. | `PosHub.cs:8` |
| 33 | Real-Time Sync | Leave Outlet Hub Group | SignalR client leaves outlet group when switching outlets or logging out. | SignalR Hub Invoke: `LeaveOutletGroup(outletId)` | None (SignalR Group leave) | Silent on hub failure. | `PosHub.cs:13` |
| 34 | Real-Time Sync | Join Kitchen Station Group | SignalR KDS screen joins station-specific channel (e.g. "Tandoor", "Bar"). | SignalR Hub Invoke: `JoinKitchenStation(outletId, station)` | None (SignalR Group join) | Silent on hub failure. | `PosHub.cs:18` |

---

## 3. Edge Cases & Observed System Behaviors

| # | Feature | Input / Condition | Observed Behavior | Root Cause & Risk |
|---|---------|-------------------|-------------------|-------------------|
| 1 | Auth: JWT Generation | Login as `superadmin` whose `RestaurantId` is `null` | `NullReferenceException` thrown during `GenerateJwtToken(User user)` line 403: `new Claim("RestaurantId", user.RestaurantId.ToString())`. | `user.RestaurantId` is nullable Guid; calling `.ToString()` unconditionally crashes the request with HTTP 500. Prevents platform admin access. |
| 2 | Table Management: Table Status Serialization | Fetching floors & tables (`GET /api/Floor/outlets/{outletId}/floors`) | Backend serializes `TableStatus` as integer `1, 2, 3, 4`. Frontend expects strings `'Vacant', 'Occupied', 'KotPlaced', 'Billed', 'Reserved'`. | `JsonStringEnumConverter` is missing in `Program.cs`. Equality check `table.status === 'Vacant'` evaluates to `false`, causing table visual styles to default. |
| 3 | Real-Time Sync: Table Status Broadcast | `UpdateTableStatus` in `FloorController.cs` | Backend emits `TableStatusUpdated` as single object `{ tableId, status, currentOrderId }`. Frontend listens to `TableStatusChanged` with 3 discrete arguments `(tableId, status, orderId)`. | Complete event name and argument signature mismatch. SignalR table status broadcast is never consumed by the client. |
| 4 | Real-Time Sync: Table Status on Order Creation | `CreateOrder` in `OrdersController.cs` | Backend sets `table.Status = TableStatus.Occupied` in DB and emits `NewOrderCreated`. Backend does NOT emit `TableStatusUpdated`. Frontend has NO listener for `NewOrderCreated`. | Other POS terminals do not see table turn red (Occupied) in real-time when an order is opened by another terminal. |
| 5 | Orders & KOT: Double Fire / Empty Fire | Clicking "KOT (F8)" when all cart items are already fired (`IsFired == true`) | Backend returns 400 Bad Request with `{ message: "No new items to fire to kitchen." }`. | Backend handles gracefully, but frontend catches error silently with `console.error` without notifying user why KOT didn't print. |
| 6 | Inventory: Insufficient Stock During Settlement | Settling order for dish whose required raw material stock is 0 or less than required | Backend decrements `stock.CurrentStockQuantity` into negative numbers (e.g., `-3.500 kg`). No validation or warning occurs. | Stock deduction is unconditional. Does not halt checkout (which is favorable in fast-paced restaurant operations), but allows negative inventory without audit alerts. |
| 7 | Inventory: Missing Stock Record on Settlement | Settling order where outlet does not have an `InventoryStock` record for that raw material | `FirstOrDefaultAsync` returns `null`. The `if (stock != null)` check evaluates to false, silently skipping deduction. | No stock row is auto-created or logged. Ingredient usage is lost from inventory audit trails. |
| 8 | Inventory: Stock Deduction Timing | Placing an order or firing a KOT | Inventory is NOT deducted at order placement or KOT firing. Stock deduction strictly occurs upon `POST /api/Orders/{orderId}/settle`. | If a customer places an order and cancels before billing, inventory is untouched. If items are consumed in kitchen but bill is unpaid, stock is not deducted until settled. |
| 9 | Billing: Tax Rate Inconsistency | Calling `AppendItems` or `SettleOrder` vs `CreateOrder` | `CreateOrder` uses `outlet.Restaurant?.DefaultGstPercent ?? 5.0m`. `AppendItems` hardcodes `5.0m`. `SettleOrder` hardcodes `0.05m`. | If a restaurant sets default GST to 18% or 12%, subsequent item additions and final settlement recalculate tax at hardcoded 5%, causing tax discrepancies on bills. |
| 10 | Security / Network: Expired or Revoked JWT | Frontend sends request with expired token after 7 days | Backend returns 401 Unauthorized. Frontend Axios response interceptor rejects the promise without clearing `localStorage` or navigating to login. | User remains on blank or broken screen; must manually clear localStorage to see the login screen again. |
| 11 | SignalR: Reconnection Group Re-subscription | Client network reconnects via `withAutomaticReconnect()` | SignalR re-establishes socket connection, but does NOT re-invoke `JoinOutletGroup(outletId)`. | Reconnected client stops receiving real-time KOT and order events until page is refreshed or outlet context is manually toggled. |
| 12 | Table Management: Table Coordinate Updates | Admin attempts to move or rearrange tables on the floor plan | There is no API route in `FloorController` (no `PUT` or `PATCH` table coordinates) and frontend `TableView.tsx` renders a CSS grid rather than coordinate-based layout. | Drag-and-drop floor designer specified in `FRONTEND_PLAN.md` has no backend persistence endpoint. |

---

## 4. End-to-End Workflow Specifications

### Workflow 1: Authentication & Token Handling

```
User/Terminal Staff
       │
       ▼
[Enter Credentials / PIN]
       │
       ├─► POST /api/Auth/login ───► Validate SHA-256(password + Salt) ──► Generate JWT (7d expiry)
       ├─► POST /api/Auth/pin-login ─► Validate OutletId + PIN ──────────► Generate JWT (7d expiry)
       └─► POST /api/Auth/register ──► Create Restaurant + Owner User ───► Generate JWT (7d expiry)
                                                  │
                                                  ▼
                                      AuthResponse Payload
                             { token, role, restaurantId, outletId, ... }
                                                  │
                                                  ▼
                                      Persist to localStorage
                                   ['pos_token', 'pos_role', ...]
                                                  │
                                                  ▼
                                  Zustand Store Hydration (posStore)
                                                  │
                                                  ▼
                             Axios Request Interceptor: Bearer Token
```

- **Login Flow**:
  1. Frontend presents `Login.tsx` with username/password inputs and "Demo Accounts" quick fill (`superadmin` / `Password123!`).
  2. Dispatches `POST /api/Auth/login` with `LoginRequest { Username, Password }`.
  3. Server hashes `password + "RestaurantPOS_Salt_2026"` using SHA-256 and compares to `User.PasswordHash`.
  4. Upon success, server generates a JWT containing `ClaimTypes.NameIdentifier`, `ClaimTypes.Name`, `ClaimTypes.Role`, `RestaurantId`, and `OutletId`.
  5. Response returns `AuthResponse` with token and user/restaurant metadata.
  6. Frontend stores token and metadata in `localStorage` (`pos_token`, `pos_role`, `pos_restaurant_id`, `pos_outlet_id`, etc.) and sets state in `posStore`.
  7. Client dispatches `fetchInitialData()` to fetch categories, menu, floor plan, and initiates SignalR hub connection.
- **PIN Login Flow**:
  1. Quick punch pad for waiters/cashiers: `POST /api/Auth/pin-login` with `{ pin: string, outletId: Guid }`.
  2. Authenticates user matching `OutletId` and `Pin`, issuing JWT scoped to that staff member.
- **Request Interception**:
  - `apiClient` Axios instance adds `Authorization: Bearer ${token}` on every outgoing request.
- **Defects & Gaps**:
  - `GenerateJwtToken` in `AuthController.cs:403` throws `NullReferenceException` if `user.RestaurantId` is null (occurs for SuperAdmin).
  - No 401 response interceptor exists to clear `localStorage` and redirect to `<Login />`.

---

### Workflow 2: Table Management & Layout Updates

```
Floor View (TableView.tsx)
       │
       ▼
GET /api/Floor/outlets/{outletId}/floors
       │
       ├─► Returns Floors with nested Tables (Status, Capacity, PosX, PosY, CurrentOrderId)
       │
       ▼
Table Selection / Status Transition:
       │
       ├─► Click Vacant Table ──────► Sets selectedTable, opens POS Terminal for order punching
       ├─► Order Created (POST /Orders) ──► Table.Status = Occupied, Table.CurrentOrderId = order.Id
       ├─► Order Settled (POST /settle) ─► Table.Status = Vacant, Table.CurrentOrderId = null
       └─► Manual PATCH /tables/{id}/status ──► Broadcasts TableStatusUpdated via SignalR
```

- **Table States**:
  - Backend `TableStatus`: `Vacant (1)`, `Occupied (2)`, `Billed (3)`, `Reserved (4)`.
  - Frontend `RestaurantTable.status`: `'Vacant' | 'Occupied' | 'KotPlaced' | 'Billed' | 'Reserved'`.
- **Transitions**:
  - `Vacant -> Occupied`: Automatically applied when `POST /api/Orders` includes `tableId`. Table's `CurrentOrderId` is linked to the new order.
  - `Occupied -> Vacant`: Automatically applied when `POST /api/Orders/{orderId}/settle` completes. Table's `CurrentOrderId` is cleared to `null`.
  - Manual Status Override: `PATCH /api/Floor/tables/{tableId}/status` updates table status directly and clears `CurrentOrderId` if status is set to `Vacant`.
- **Real-Time Synchronization**:
  - Server emits SignalR `TableStatusUpdated` (`{ tableId, status, currentOrderId }`) to `$"outlet-{outletId}"`.
  - Server emits SignalR `OrderSettled` upon bill settlement. Client handles `OrderSettled` by re-fetching `/Floor/outlets/{outletId}/floors`.
- **Defects & Gaps**:
  - Event name mismatch: Server emits `TableStatusUpdated`, but client listens to `TableStatusChanged`.
  - Signature mismatch: Server emits single object payload `{ tableId, status, currentOrderId }`, but client listener expects 3 arguments `(tableId, status, orderId)`.
  - Enum serialization: C# integers `1, 2, 3, 4` fail strict string comparisons in React UI (`table.status === 'Vacant'`).
  - Coordinate updates: No endpoint exists to persist drag-and-drop table positions (`PosX`, `PosY`).

---

### Workflow 3: Order Creation & KOT Lifecycle

```
Cart (PosTerminal.tsx)
       │
       ▼
Add Menu Items / Variants / Direct Raw Materials
       │
       ▼
Click "KOT (F8)" or "Pay (F4)"
       │
       ├─► If order not yet created (orderId is null):
       │      POST /api/Orders ──► Creates Order (ORD-yyyyMMdd-NNN), status Active
       │                           Table status set to Occupied
       │                           Emits SignalR "NewOrderCreated"
       │
       ├─► If order exists and new items added:
       │      POST /api/Orders/{orderId}/items ──► Appends OrderItems (IsFired = false)
       │
       ▼
Click "KOT (F8)":
       │
       ▼
POST /api/Orders/{orderId}/fire-kot
       │
       ├─► Filters items where !IsVoided && !IsFired
       ├─► Creates KitchenOrderTicket (KOT-01, KOT-02...), status Pending
       ├─► Copies items into KotItems (Name, Variant, Qty, CookingNotes)
       ├─► Marks OrderItems as IsFired = true
       ├─► Sets Order.Status = KotPlaced
       └─► Emits SignalR "KotReceived" ({ orderId, orderNumber, tableNumber, kot })
```

- **Order Creation Contract**:
  - Route: `POST /api/Orders`
  - Body: `CreateOrderRequest`:
    ```json
    {
      "outletId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "orderType": 1,
      "tableId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "customerName": "John Doe",
      "customerPhone": "9876543210",
      "customerAddress": null,
      "guestCount": 2,
      "waiterName": "Captain",
      "specialNotes": "No spicy",
      "items": [
        {
          "menuItemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "rawMaterialId": null,
          "itemName": "Paneer Tikka",
          "variantName": "Full",
          "quantity": 2,
          "unitPrice": 280.0,
          "notes": "Extra crispy"
        }
      ]
    }
    ```
- **KOT Fire Contract**:
  - Route: `POST /api/Orders/{orderId}/fire-kot`
  - Validates un-fired items exist; creates ticket with timestamp `FiredAt = DateTime.UtcNow`.
  - Sets `order.Status = OrderStatus.KotPlaced`.
  - Emits SignalR event `KotReceived` to outlet group.
- **Defects & Gaps**:
  - Voiding / Item cancellation: No backend endpoint exists to void an individual fired item or cancel an order.

---

### Workflow 4: Kitchen Display System (KDS) Sync

```
Kitchen Station View (KitchenDisplay.tsx)
       │
       ▼
GET /api/Orders/outlet/{outletId}/kots (Status != Served && != Cancelled)
       │
       ▼
SignalR Real-Time Listener:
       │
       ├─► Event "KotReceived" ──────► Audio Chime (Web Audio API) + fetchKots()
       ├─► Event "KotStatusChanged" ──► Re-fetches active tickets
       └─► Event "KotItemToggled" ────► Re-fetches active tickets
       │
       ▼
Kitchen Staff Actions:
       │
       ├─► Toggle Item Prepared ──► POST /api/Orders/kots/items/{itemId}/toggle
       │                            Inverts KotItem.IsPrepared
       │                            Emits SignalR "KotItemToggled"
       │
       └─► Complete Ticket ───────► POST /api/Orders/kots/{kotId}/status ({ status: 4 })
                                    Sets Kot.Status = Served, CompletedAt = UtcNow
                                    Emits SignalR "KotStatusChanged"
                                    Ticket disappears from KDS board
```

- **Ticket Age & Visual Urgency**:
  - `< 10 mins`: Normal green (`text-emerald-400 bg-emerald-500/10`).
  - `10 - 15 mins`: Warning yellow (`text-amber-400 bg-amber-500/15`).
  - `> 15 mins`: Overdue red pulsing alert (`text-rose-400 bg-rose-500/20 animate-pulse`).
- **Station Filtering**:
  - KDS header allows filtering tickets by station: `All`, `Kitchen`, `Tandoor`, `Bar`.
  - Backend stores `KitchenStation` on both `MenuItem` and `KitchenOrderTicket`.
- **Item-Level Preparation Tracking**:
  - Cooks tap each item as it is plated; `KotItem.IsPrepared` toggles to `true` with strike-through styling.
  - When all items in a ticket are marked prepared, ticket highlights green (`border-emerald-500/60`), prompting the "Mark Order Ready & Serve" button.
- **Defects & Gaps**:
  - Frontend calls `get().fetchKots()` on every item toggle rather than updating state locally, causing redundant HTTP roundtrips during busy kitchen rush hours.

---

### Workflow 5: Inventory Stock Deduction

```
POS Billing Checkout (PosTerminal.tsx)
       │
       ▼
Click "Confirm Settle (F4)"
       │
       ▼
POST /api/Orders/{orderId}/settle
       │
       ├─► Financials: SubTotal - Discount + GST (5%) + ServiceCharge = GrandTotal
       ├─► Order.IsPaid = true, Order.Status = Paid, SettledAt = UtcNow
       ├─► Release Table: Table.Status = Vacant, Table.CurrentOrderId = null
       │
       ▼
Inventory Deduction Engine (Backend Loop):
       │
       ├─► Non-voided items with RawMaterialId (Direct Inventory Sale):
       │      stock = InventoryStock(OutletId, RawMaterialId)
       │      stock.CurrentStockQuantity -= orderItem.Quantity
       │
       ├─► Non-voided items with MenuItemId (Recipe-based Deduction):
       │      Query RecipeItems for MenuItemId
       │      For each recipeItem:
       │          stock = InventoryStock(OutletId, recipeItem.RawMaterialId)
       │          stock.CurrentStockQuantity -= (recipeItem.QuantityRequired * orderItem.Quantity)
       │
       ▼
SaveChangesAsync()
       │
       ▼
Broadcast SignalR "OrderSettled" ({ orderId, grandTotal })
       │
       ▼
Client handles "OrderSettled":
       ├─► Re-fetch floors (clears table occupancy)
       ├─► Re-fetch dashboard stats (updates today's revenue)
       └─► Re-fetch raw materials (updates stock table & alerts)
```

- **Recipe Mapping Rules**:
  - Configured via `POST /api/Inventory/recipes/{menuItemId}` with `List<RecipeItem>`.
  - `RecipeItem.QuantityRequired` represents fractional consumption per unit sold (e.g., `0.200` kg paneer per 1 Paneer Tikka).
  - Direct sale raw materials deduct 1:1 based on `orderItem.Quantity`.
- **Threshold Alerts**:
  - In `Inventory.tsx`: If `stock <= minThresholdAlert`, renders warning badge `<AlertTriangle /> Low Stock`.
  - In `Dashboard.tsx`: Counts total materials below threshold and displays highlighted warning card.
- **Defects & Gaps**:
  - Negative Stock: No safety check prevents stock from going below 0.
  - Missing Stock Record: If `InventoryStock` has never been initialized for an outlet, deduction is silently skipped.
  - Hardcoded GST: `SettleOrder` recalculates tax using hardcoded `0.05m` rather than the restaurant's configured tax rate.

---

## 5. Interface Contracts & DTO Parity Matrix

### HTTP API Endpoints

| # | Controller | Route | Method | Backend Request DTO | Backend Response | Frontend Service Call | Parity Status | Discrepancy / Action Required |
|---|------------|-------|--------|---------------------|------------------|-----------------------|---------------|-------------------------------|
| 1 | `AuthController` | `/api/Auth/login` | POST | `LoginRequest` | `AuthResponse` | `posStore.login` | MATCH | Fix NRE on `user.RestaurantId` in JWT generator. |
| 2 | `AuthController` | `/api/Auth/pin-login` | POST | `PinLoginRequest` | `AuthResponse` | None (UI uses login) | UNUSED IN UI | Backend works; Frontend PIN punch UI not yet wired. |
| 3 | `AuthController` | `/api/Auth/register` | POST | `CreateRestaurantRequest` | `AuthResponse` | `posStore.registerRestaurant` | MISMATCH | Frontend omits `CurrencySymbol`, `DefaultGstPercent`, `ServiceChargePercent`. Pass defaults in DTO. |
| 4 | `AuthController` | `/api/Auth/restaurants` | GET | None | `List<RestaurantSummary>` | `posStore.fetchAllRestaurants` | MATCH | Full parity. |
| 5 | `AuthController` | `/api/Auth/restaurants/admin` | POST | `SuperAdminCreateRestaurantRequest` | `RestaurantSummary` | `posStore.addRestaurant` | MATCH | Full parity. |
| 6 | `AuthController` | `/api/Auth/users` | POST | `CreateUserRequest` | `{ message }` | `posStore.createUser` | MISMATCH | Frontend sends role as integer; ensure string/int enum binding works cleanly. |
| 7 | `AuthController` | `/api/Auth/users/{restaurantId}` | GET | Route: `restaurantId` | `List<UserDto>` | `posStore.fetchUsers` | MATCH | Full parity. |
| 8 | `FloorController` | `/api/Floor/outlets/{outletId}/floors` | GET | Route: `outletId` | `List<Floor>` | `posStore.fetchInitialData` | ENUM MISMATCH | Add `JsonStringEnumConverter` so `TableStatus` serializes as string. |
| 9 | `FloorController` | `/api/Floor/floors` | POST | `CreateFloorRequest` | `Floor` | None | UNUSED IN UI | API functional. |
| 10 | `FloorController` | `/api/Floor/tables` | POST | `CreateTableRequest` | `RestaurantTable` | None | UNUSED IN UI | API functional. |
| 11 | `FloorController` | `/api/Floor/tables/{tableId}/status` | PATCH | Body: `TableStatus` | `RestaurantTable` | None | BROKEN SIGNALR | Fix broadcast event name and payload shape. |
| 12 | `OrdersController` | `/api/Orders/outlet/{outletId}/active` | GET | Route: `outletId` | `List<Order>` | None | UNUSED IN UI | API functional. |
| 13 | `OrdersController` | `/api/Orders` | POST | `CreateOrderRequest` | `Order` | `posStore.fireKot / settleOrder` | MATCH | Emits `NewOrderCreated`. Add client listener. |
| 14 | `OrdersController` | `/api/Orders/{orderId}` | GET | Route: `orderId` | `Order` | `posStore.setSelectedTable` | MATCH | Full parity. |
| 15 | `OrdersController` | `/api/Orders/{orderId}/items` | POST | `List<OrderItemRequest>` | `Order` | `posStore.fireKot / settleOrder` | MATCH | Full parity. |
| 16 | `OrdersController` | `/api/Orders/{orderId}/fire-kot` | POST | None | `KitchenOrderTicket` | `posStore.fireKot` | MATCH | Full parity. |
| 17 | `OrdersController` | `/api/Orders/{orderId}/settle` | POST | `SettleOrderRequest` | `Order` | `posStore.settleOrder` | MATCH | Fix hardcoded 5% GST to use restaurant default. |
| 18 | `OrdersController` | `/api/Orders/outlet/{outletId}/kots` | GET | Route: `outletId` | `List<KotSummary>` | `posStore.fetchKots` | MATCH | Full parity. |
| 19 | `OrdersController` | `/api/Orders/kots/{kotId}/status` | POST | `UpdateKotStatusRequest` | `KitchenOrderTicket` | `posStore.completeKot` | MATCH | Full parity. |
| 20 | `OrdersController` | `/api/Orders/kots/items/{itemId}/toggle` | POST | None | `KotItem` | `posStore.toggleKotItemPrepared` | MATCH | Full parity. |
| 21 | `OrdersController` | `/api/Orders/reset-db` | POST | None | `{ message }` | None | UTILITY | Clears database orders and tables. |
| 22 | `MenuController` | `/api/Menu/categories/{restaurantId}` | GET | Route: `restaurantId` | `List<Category>` | `posStore.fetchInitialData` | MATCH | Full parity. |
| 23 | `MenuController` | `/api/Menu/categories/{restaurantId}` | POST | `CreateCategoryRequest` | `Category` | None | UNUSED IN UI | API functional. |
| 24 | `MenuController` | `/api/Menu/items/{restaurantId}` | GET | Route: `restaurantId` | `List<MenuItem>` | None | UNUSED IN UI | Categories endpoint includes items. |
| 25 | `MenuController` | `/api/Menu/items/{restaurantId}` | POST | `CreateMenuItemRequest` | `MenuItem` | `posStore.createMenuItem` | MATCH | Full parity. |
| 26 | `MenuController` | `/api/Menu/items/{itemId}/toggle-availability` | PATCH | None | `{ itemId, isAvailable }` | None | UNUSED IN UI | API functional. |
| 27 | `InventoryController` | `/api/Inventory/materials/{restaurantId}` | GET | Route: `restaurantId` | `List<RawMaterial>` | `posStore.fetchRawMaterials` | MATCH | Full parity. |
| 28 | `InventoryController` | `/api/Inventory/materials/{restaurantId}` | POST | `CreateRawMaterialRequest` | `RawMaterial` | `posStore.addRawMaterial` | MATCH | Full parity. |
| 29 | `InventoryController` | `/api/Inventory/stock/{outletId}/adjust` | POST | `UpdateStockRequest` | `InventoryStock` | `posStore.adjustStock` | MATCH | Full parity. |
| 30 | `InventoryController` | `/api/Inventory/recipes/{menuItemId}` | POST | `List<RecipeItem>` | `List<RecipeItem>` | None | UNUSED IN UI | API functional. |
| 31 | `ReportsController` | `/api/Reports/outlet/{outletId}/dashboard` | GET | Route: `outletId` | `DashboardStats` | `posStore.fetchDashboardStats` | MATCH | Full parity. |
| 32 | `ReportsController` | `/api/Reports/outlet/{outletId}/sales-summary` | GET | Query params | `List<SalesSummary>` | None | UNUSED IN UI | API functional. |

---

## 6. SignalR Hub Events & Real-Time Sync Specification

### Hub Definition: `PosHub.cs`
- **Path**: `/hubs/pos`
- **Transport**: WebSockets, LongPolling fallback.
- **Client Methods Callable**:
  1. `JoinOutletGroup(string outletId)` -> Adds connection to `$"outlet-{outletId}"`
  2. `LeaveOutletGroup(string outletId)` -> Removes connection from `$"outlet-{outletId}"`
  3. `JoinKitchenStation(string outletId, string station)` -> Adds connection to `$"station-{outletId}-{station}"`

### Events Emitted & Received

```
┌──────────────────────────────────────────────────────────────────────────────────────────┐
│                                 SERVER-TO-CLIENT BROADCASTS                              │
├─────────────────────┬──────────────────────────┬─────────────────────────────────────────┤
│ Server Method       │ SignalR Event Name       │ Payload Schema                          │
├─────────────────────┼──────────────────────────┼─────────────────────────────────────────┤
│ UpdateTableStatus   │ TableStatusUpdated [!]   │ { tableId: Guid,                        │
│                     │                          │   status: TableStatus,                  │
│                     │                          │   currentOrderId: Guid? }               │
├─────────────────────┼──────────────────────────┼─────────────────────────────────────────┤
│ CreateOrder         │ NewOrderCreated          │ Order entity (full JSON tree)           │
├─────────────────────┼──────────────────────────┼─────────────────────────────────────────┤
│ FireKot             │ KotReceived              │ { orderId: Guid,                        │
│                     │                          │   orderNumber: string,                  │
│                     │                          │   tableNumber: string,                  │
│                     │                          │   kot: KitchenOrderTicket }             │
├─────────────────────┼──────────────────────────┼─────────────────────────────────────────┤
│ UpdateKotStatus     │ KotStatusChanged         │ { kotId: Guid, status: int }            │
├─────────────────────┼──────────────────────────┼─────────────────────────────────────────┤
│ ToggleKotItemPrepared│ KotItemToggled          │ { itemId: Guid, isPrepared: bool }      │
├─────────────────────┼──────────────────────────┼─────────────────────────────────────────┤
│ SettleOrder         │ OrderSettled             │ { orderId: Guid, grandTotal: decimal }  │
└─────────────────────┴──────────────────────────┴─────────────────────────────────────────┘
```

### SignalR Parity Gaps & Fix Specifications

1. **Table Status Event Name & Payload Alignment**:
   - **Current Backend** (`FloorController.cs:88`):
     ```csharp
     await _hubContext.Clients.Group($"outlet-{table.Floor.OutletId}")
         .SendAsync("TableStatusUpdated", new { tableId = table.Id, status = table.Status, currentOrderId = table.CurrentOrderId });
     ```
   - **Current Frontend** (`posStore.ts:260`):
     ```typescript
     hub.on('TableStatusChanged', (tableId: string, status: RestaurantTable['status'], orderId?: string) => { ... });
     ```
   - **Required Fix**: Align on single standard.
     - Option A: Change frontend listener to:
       ```typescript
       hub.on('TableStatusUpdated', (payload: { tableId: string; status: any; currentOrderId?: string }) => {
         set((state) => ({
           floors: state.floors.map((floor) => ({
             ...floor,
             tables: floor.tables.map((t) =>
               t.id === payload.tableId ? { ...t, status: payload.status, currentOrderId: payload.currentOrderId } : t
             ),
           })),
         }));
       });
       ```
     - And emit `TableStatusUpdated` from `OrdersController.CreateOrder` so table turns Occupied in real-time across all connected clients!

2. **Handle `NewOrderCreated`**:
   - Add frontend listener in `posStore.ts`:
     ```typescript
     hub.on('NewOrderCreated', (order: any) => {
       if (order.tableId) {
         set((state) => ({
           floors: state.floors.map((floor) => ({
             ...floor,
             tables: floor.tables.map((t) =>
               t.id === order.tableId ? { ...t, status: 'Occupied', currentOrderId: order.id } : t
             ),
           })),
         }));
       }
     });
     ```

3. **Re-connection Group Rejoining**:
   - In `api.ts` or `posStore.ts`, register `hub.onreconnected(async () => { const { outletId } = get(); if (outletId) await hub.invoke('JoinOutletGroup', outletId); })`.

---

## 7. Discrepancies, Gaps, and Defect Log

| Defect ID | Severity | Layer | Component / File | Description | Impact | Fix Recommendation |
|-----------|----------|-------|------------------|-------------|--------|--------------------|
| **DEF-01** | CRITICAL | Backend | `AuthController.cs:403` | `user.RestaurantId.ToString()` throws NRE when `user.RestaurantId` is null (SuperAdmin login). | SuperAdmin cannot log in; returns HTTP 500. | Change to `new Claim("RestaurantId", user.RestaurantId?.ToString() ?? string.Empty)`. |
| **DEF-02** | HIGH | Full-Stack | `Program.cs:14`, `types/pos.ts:57` | Enums serialized as numbers (`1, 2, 3, 4`) instead of strings (`"Vacant"`, `"Occupied"`). | Table status styling in `TableView.tsx` breaks; status badges fail to render. | Add `options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());` in `Program.cs`. |
| **DEF-03** | HIGH | Full-Stack | `FloorController.cs:89`, `posStore.ts:260` | Event name mismatch (`TableStatusUpdated` vs `TableStatusChanged`) and signature mismatch (object vs 3 params). | Real-time table status updates are completely lost. | Update `posStore.ts` to listen to `TableStatusUpdated` with object `{ tableId, status, currentOrderId }`. |
| **DEF-04** | MEDIUM | Full-Stack | `OrdersController.cs:106`, `posStore.ts:250` | `CreateOrder` emits `NewOrderCreated`, but frontend does not listen to `NewOrderCreated`. | Other terminals don't see table status turn to Occupied upon order creation until manual refresh. | Add `hub.on('NewOrderCreated')` in `posStore.ts` or emit `TableStatusUpdated` on order creation. |
| **DEF-05** | MEDIUM | Frontend | `posStore.ts:726` | `registerRestaurant` request omits required non-nullable fields `currencySymbol`, `defaultGstPercent`, `serviceChargePercent`. | Default Gst and currency can become 0 or null on registered restaurants. | Include default values `{ currencySymbol: '₹', defaultGstPercent: 5.0, serviceChargePercent: 0.0 }` in the POST payload. |
| **DEF-06** | MEDIUM | Backend | `OrdersController.cs:156, 237` | Tax rate hardcoded to 5% in `AppendItems` and `SettleOrder`, ignoring `outlet.Restaurant.DefaultGstPercent`. | Restaurants with 12% or 18% GST will have incorrect tax amounts calculated on settlements. | Query restaurant default GST rate in `AppendItems` and `SettleOrder`. |
| **DEF-07** | LOW | Frontend | `api.ts:21` | Axios response interceptor does not handle 401 Unauthorized errors to clear token and prompt re-login. | Expired token leaves app in broken state with blank screens. | Add 401 interceptor that clears localStorage and updates state. |
| **DEF-08** | LOW | Backend | `OrdersController.cs:268, 282` | Stock deduction does not validate if stock is sufficient or alert if stock goes negative. | Inventory can drift into negative balances without notification. | Add low stock warning flag or audit entry when stock becomes negative. |

---

## 8. Verification & Test Strategy

To verify the 5 end-to-end workflows in an automated and repeatable manner for R3 acceptance, the following multi-tiered test strategy must be implemented.

### Tier 1: Backend Integration Test Harness (`RestaurantPOS.Tests`)
A dedicated xUnit/NUnit integration test project targeting `RestaurantPOS.API` using `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`):
- **Dependencies**:
  - `Microsoft.AspNetCore.Mvc.Testing`
  - `Microsoft.EntityFrameworkCore.InMemory` or SQLite/Testcontainers
  - `Microsoft.AspNetCore.SignalR.Client`
  - `FluentAssertions`
  - `xunit` / `xunit.runner.visualstudio`
- **Execution**: Run via `dotnet test` with 0 manual intervention.

### Tier 2: Automated Node.js / TypeScript E2E Sanity Harness
A lightweight script executing against the running API (HTTP + WebSocket SignalR):
- Executes the entire restaurant customer lifecycle in under 5 seconds.
- Connects a real SignalR WebSocket client to `/hubs/pos`.

### Concrete Step-by-Step E2E Test Suite Specification

```
================================================================================
E2E SANITY INTEGRATION PIPELINE (5 CORE WORKFLOWS)
================================================================================

Step 1: Auth & Provisioning (Workflow 1)
  1.1 POST /api/Auth/login with 'superadmin' / 'Password123!'
      -> Assert status 200 OK
      -> Assert token is non-empty string
      -> Assert role is 'SuperAdmin'
  1.2 POST /api/Auth/register (New Restaurant: "Tasty Bistro", Admin: "bistro_admin")
      -> Assert status 200 OK
      -> Assert AuthResponse contains restaurantId, outletId, and JWT token

Step 2: Real-time Socket & Floor Layout (Workflow 2 & SignalR)
  2.1 Connect SignalR client to /hubs/pos with Bearer token
      -> Assert connection state == 'Connected'
  2.2 Invoke 'JoinOutletGroup(outletId)'
      -> Register event handlers: 'TableStatusUpdated', 'KotReceived', 'KotItemToggled', 'OrderSettled'
  2.3 GET /api/Floor/outlets/{outletId}/floors
      -> Assert status 200 OK
      -> Assert floors.length >= 1
      -> Assert floor.tables.length >= 4
      -> Assert table[0].status == 'Vacant'
  2.4 PATCH /api/Floor/tables/{table[0].id}/status with 'Reserved'
      -> Assert status 200 OK
      -> Assert SignalR client receives 'TableStatusUpdated' with status == 'Reserved'

Step 3: Menu & Inventory Setup (Workflow 5 Pre-requisite)
  3.1 POST /api/Inventory/materials/{restaurantId}
      -> Create Raw Material: "Paneer Block", UOM: "kg", Cost: 250.00, MinThreshold: 2.00
      -> Save rawMaterialId
  3.2 POST /api/Inventory/stock/{outletId}/adjust
      -> Adjust stock: +10.000 kg
      -> Assert status 200 OK, stock == 10.000
  3.3 POST /api/Menu/items/{restaurantId}
      -> Create Menu Item: "Shahi Paneer", BasePrice: 300.00
      -> Save menuItemId
  3.4 POST /api/Inventory/recipes/{menuItemId}
      -> Link recipe: 0.250 kg "Paneer Block" per "Shahi Paneer"
      -> Assert status 200 OK

Step 4: Order Creation & KOT Lifecycle (Workflows 3 & 4)
  4.1 POST /api/Orders
      -> Body: outletId, tableId: table[0].id, guestCount: 2, items: [ { menuItemId, qty: 2, price: 300 } ]
      -> Assert status 200 OK
      -> Assert order.orderNumber starts with "ORD-"
      -> Assert order.subTotal == 600.00, grandTotal == 630.00 (with 5% GST)
      -> Assert table[0] status is now 'Occupied' in DB
  4.2 POST /api/Orders/{orderId}/fire-kot
      -> Assert status 200 OK
      -> Assert kot.kotNumber == "KOT-01"
      -> Assert kot.status == 'Pending'
      -> Assert SignalR client received 'KotReceived' with orderNumber and tableNumber
  4.3 GET /api/Orders/outlet/{outletId}/kots
      -> Assert status 200 OK
      -> Assert active KOTs list contains KOT-01
  4.4 POST /api/Orders/kots/items/{kot.items[0].id}/toggle
      -> Assert status 200 OK, isPrepared == true
      -> Assert SignalR client received 'KotItemToggled'
  4.5 POST /api/Orders/kots/{kot.id}/status with status: 4 (Served)
      -> Assert status 200 OK
      -> GET /api/Orders/outlet/{outletId}/kots -> Assert KOT-01 is no longer in active list

Step 5: Settlement & Stock Deduction (Workflow 5)
  5.1 POST /api/Orders/{orderId}/settle
      -> Body: paymentMode: 1 (Cash), discountAmount: 30.00, serviceChargeAmount: 0
      -> Assert status 200 OK
      -> Assert order.isPaid == true, order.status == 'Paid'
      -> Assert SignalR client received 'OrderSettled'
  5.2 Verify Table Release
      -> GET /api/Floor/outlets/{outletId}/floors
      -> Assert table[0].status == 'Vacant' and table[0].currentOrderId == null
  5.3 Verify Inventory Deduction
      -> GET /api/Inventory/materials/{restaurantId}
      -> Initial stock was 10.000 kg
      -> 2 Shahi Paneer sold * 0.250 kg = 0.500 kg deducted
      -> Assert current stock == 9.500 kg
  5.4 Verify Dashboard Reports
      -> GET /api/Reports/outlet/{outletId}/dashboard
      -> Assert totalSales >= 598.50, totalOrders >= 1, totalGuests >= 2

================================================================================
ALL 5 WORKFLOWS PASS VERIFICATION
================================================================================
```

---

## 9. Conclusion & Recommendations

The RestaurantPOS codebase exhibits a well-architected domain model with clear separation of concerns across floor tables, orders, kitchen order tickets, menu configuration, and inventory recipes.

To achieve production hardening and complete acceptance for Requirement R3:
1. **Apply the 4 critical bug fixes**:
   - Fix nullability crash in `AuthController.GenerateJwtToken` for `user.RestaurantId`.
   - Add `JsonStringEnumConverter` in `Program.cs`.
   - Harmonize SignalR event names and payload signatures between `FloorController.UpdateTableStatus` and `posStore.ts`.
   - Add listener for `NewOrderCreated` in `posStore.ts` or emit `TableStatusUpdated` upon order placement.
2. **Implement the automated E2E sanity test harness** as defined in Section 8 to verify that all 5 workflows function seamlessly in automated test runs.
