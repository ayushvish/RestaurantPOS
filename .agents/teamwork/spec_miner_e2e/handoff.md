# Handoff Report — E2E Workflow & Integration Spec Mining

## 1. Observation
- Inspected the codebase using `codebase-memory-mcp` (913 nodes, 1536 edges, 20 routes, 6 SignalR channels) and direct file inspections.
- Authoritative source `ORIGINAL_REQUEST.md` specifies R3: End-to-End Workflow Integration & Real-Time Sync across 5 key workflows:
  1. Authentication & Token Handling
  2. Table Management & Layout Updates
  3. Order Creation & KOT Lifecycle
  4. Kitchen Display System (KDS) Sync
  5. Inventory Stock Deduction upon billing
- Key observations in code:
  - `RestaurantPOS.API/Controllers/AuthController.cs:403`: `new Claim("RestaurantId", user.RestaurantId.ToString())` throws `NullReferenceException` when `user.RestaurantId` is null (e.g., when logging in as `superadmin`).
  - `RestaurantPOS.API/Program.cs:14`: ASP.NET Core JSON serializer does not register `JsonStringEnumConverter()`. As a result, C# enums serialize as integers (`1, 2, 3`), whereas `RestaurantPOS.Client/src/types/pos.ts` defines string unions (e.g. `'Vacant' | 'Occupied' | 'KotPlaced' | 'Billed' | 'Reserved'`).
  - `RestaurantPOS.API/Controllers/FloorController.cs:88-89`: Emits `TableStatusUpdated` with single object `{ tableId, status, currentOrderId }`.
  - `RestaurantPOS.Client/src/store/posStore.ts:260`: Listens for `TableStatusChanged` with 3 discrete arguments `(tableId, status, orderId)`.
  - `RestaurantPOS.API/Controllers/OrdersController.cs:106`: Emits `NewOrderCreated` upon order creation; `posStore.ts` has no listener for `NewOrderCreated`, and `OrdersController` does not emit `TableStatusUpdated` when occupying a table.
  - `RestaurantPOS.API/Controllers/OrdersController.cs:253-288`: Stock deduction occurs strictly during `SettleOrder`, never at order placement or KOT fire. Stock is decremented unconditionally (can go negative).
  - Tax calculations: `OrdersController.cs:156, 237` hardcodes 5% GST instead of reading `outlet.Restaurant.DefaultGstPercent`.
  - Comprehensive findings documented in `D:\AyushV\RestaurantPOS\.agents\teamwork\spec_miner_e2e\spec_report.md`.

## 2. Logic Chain
1. *Authentication*: Since SuperAdmin has `RestaurantId == null`, calling `.ToString()` causes HTTP 500 on login, blocking initial admin operations. This must be guarded with `?.ToString() ?? string.Empty`.
2. *Real-Time Table Updates*: Because the server broadcasts `TableStatusUpdated` while the client subscribes to `TableStatusChanged` with mismatched arguments, real-time table transitions fail silently over WebSockets. Aligning the name and payload restores live multi-terminal updates.
3. *Table Status Rendering*: Because backend serializes enums as numbers without `JsonStringEnumConverter`, frontend strict string checks (`table.status === 'Vacant'`) evaluate to false, breaking UI badge rendering and table interaction colors.
4. *Inventory & Settlement*: Stock deduction correctly traces recipe BOM links (`RecipeItems`) and direct raw material items, deducting from `InventoryStock` upon billing. Because it permits negative values and omits notifications, low stock warnings are evaluated client-side via `MinThresholdAlert`.
5. *E2E Test Execution*: With no pre-existing test suite, a 5-step automated integration test harness (HTTP API + SignalR client) is required to validate all 5 workflows in a deterministic pipeline.

## 3. Caveats
- No code modifications were implemented; this agent is strictly a read-only specification miner.
- Table coordinate updates (`PosX`, `PosY` drag-and-drop floor designer) currently lack a persistence endpoint; `TableView.tsx` currently renders a responsive CSS grid.
- Split payments are defined in `PaymentMode.Split`, but the settlement endpoint currently records a single payment transaction reference and mode.

## 4. Conclusion
- The specifications for all 5 core workflows, route contracts, DTO mappings, and real-time SignalR channels are completely mined and documented.
- 4 critical integration defects (SuperAdmin JWT crash, enum serialization mismatch, SignalR table event mismatch, missing NewOrderCreated listener) were pinpointed with exact file locations and remediation steps.
- A comprehensive specification and automated test strategy has been written to `D:\AyushV\RestaurantPOS\.agents\teamwork\spec_miner_e2e\spec_report.md`.

## 5. Verification Method
- Review `D:\AyushV\RestaurantPOS\.agents\teamwork\spec_miner_e2e\spec_report.md` for full contract parity tables, workflow diagrams, defect log, and E2E test plan.
- Verify exact source code citations in:
  - `RestaurantPOS.API/Controllers/AuthController.cs` (lines 353-416)
  - `RestaurantPOS.API/Controllers/FloorController.cs` (lines 69-93)
  - `RestaurantPOS.API/Controllers/OrdersController.cs` (lines 38-109, 164-218, 220-295)
  - `RestaurantPOS.Client/src/services/api.ts` (lines 1-41)
  - `RestaurantPOS.Client/src/store/posStore.ts` (lines 247-299, 396-523)
  - `RestaurantPOS.Client/src/types/pos.ts` (lines 52-61)
- Verify test script against Section 8 of `spec_report.md`.
