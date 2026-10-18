## 2026-10-17T19:35:56Z
You are the Frontend Implementation Worker (teamwork_preview_worker).
Your working directory is D:\AyushV\RestaurantPOS\.agents\teamwork\worker_frontend_m2.
Create your working directory and store your logs/metadata there.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Authoritative source of truth:
Read D:\AyushV\RestaurantPOS\.agents\teamwork\ORIGINAL_REQUEST.md first.
Also read the investigation findings in:
D:\AyushV\RestaurantPOS\.agents\teamwork\explorer_frontend\report.md

Exclusive Write Ownership:
You own `RestaurantPOS.Client/**` exclusively. Do NOT touch `RestaurantPOS.API/**`.

Tasks for Milestone 2 (Frontend Bundler Resolution, Types, State & Real-Time Sync):
1. Native Build & Bundler Resolution:
   - In `RestaurantPOS.Client`, run `npm install` to install native Windows bindings (`@rolldown/binding-win32-x64-msvc`, `@oxlint/binding-win32-x64-msvc`, `@tailwindcss/oxide-win32-x64-msvc`, etc.).
   - Verify `npm run build` (`tsc -b && vite build`) executes cleanly with exit code 0.
   - Verify `npm run lint` (`oxlint`) passes.
2. SignalR Hub Connection & Auth:
   - In `RestaurantPOS.Client/src/services/api.ts`:
     - In `getPosHubConnection()`: configure `accessTokenFactory` to retrieve token from `localStorage.getItem('token')` or `useAuthStore.getState().token`.
     - Ensure connection lifecycle handles reconnection cleanly.
3. SignalR Event Alignment & Store Contracts:
   - In `RestaurantPOS.Client/src/store/posStore.ts`:
     - Verify and ensure listeners for `"TableStatusChanged"` match backend parameter format `(tableId: string, status: string, orderId?: string)`.
     - Update local table status and currentOrderId in `tables` state when `"TableStatusChanged"` fires.
     - Ensure listeners for `"KotReceived"`, `"KotStatusChanged"`, `"KotItemToggled"`, and `"OrderSettled"` are properly wired and update state.
4. Type Definitions & Enum Parity:
   - In `RestaurantPOS.Client/src/types/pos.ts`:
     - Align `TableStatus` with backend: `'Vacant' | 'Occupied' | 'Billed' | 'Reserved'`.
     - Ensure OrderStatus aligns with backend: `'Draft' | 'KotPlaced' | 'InKitchen' | 'Served' | 'Paid' | 'Cancelled' | 'Void'`.
     - Add missing DTO type definitions for KOT, inventory, and stats to eliminate loose `any` types in `posStore.ts`.
5. Verification:
   - Run `npm run build` in `RestaurantPOS.Client` and verify exit code 0.
   - Run `npm run lint` and verify no errors.

Deliver a complete handoff report to `D:\AyushV\RestaurantPOS\.agents\teamwork\worker_frontend_m2\handoff.md` and message the orchestrator when done.
