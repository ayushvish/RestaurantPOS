# Original User Request

## Initial Request — 2026-10-17T19:18:34Z

Comprehensive full-stack audit, bug fixes, and production hardening across the RestaurantPOS backend and frontend.

Working directory: D:\AyushV\RestaurantPOS
Integrity mode: development

## Requirements

### R1. Backend Audit & Code Quality
Conduct a complete inspection of the ASP.NET Core Web API project (RestaurantPOS.API). Eliminate all nullability and compiler warnings (including in OrdersController.cs and AuthController.cs), resolve redundant/vulnerable dependencies (such as Microsoft.AspNetCore.SignalR.Common), and ensure consistent error handling, input validation, and proper HTTP response contracts across all controllers.

### R2. Frontend Audit, Dependencies & Linting
Audit the React TypeScript client (RestaurantPOS.Client). Fix native build/bundler dependency resolution so npm run build succeeds cleanly without missing optional binding errors. Verify type definitions, state management stores (posStore.ts), and API contract alignments (api.ts).

### R3. End-to-End Workflow Integration & Real-Time Sync
Verify and ensure end-to-end integration across core restaurant workflows: authentication/token handling, table management & layout updates, order creation & KOT lifecycle, kitchen display sync via SignalR (PosHub.cs), and inventory stock deductions upon billing.

## Acceptance Criteria

### Backend Verification
- [ ] dotnet build RestaurantPOS.API/RestaurantPOS.API.csproj completes with 0 errors and 0 warnings.
- [ ] SignalR duplicate package reference and reported vulnerability warnings are resolved.
- [ ] All API controllers handle edge cases gracefully with appropriate HTTP status codes and structured problem responses.

### Frontend Verification
- [ ] npm run build (tsc -b && vite build) completes with exit code 0.
- [ ] Frontend API services and data contracts correspond 1:1 with backend DTOs and SignalR hub events without type mismatches.

### Integration Verification
- [ ] The application compiles cleanly, and key workflows (Auth, Orders/KOT, Kitchen Display Hub, Inventory deduction) pass automated or end-to-end sanity tests.
