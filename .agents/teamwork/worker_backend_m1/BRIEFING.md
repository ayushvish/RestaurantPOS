# BRIEFING — 2026-10-18T01:25:00Z

## Mission
Execute Milestone 1 tasks for RestaurantPOS.API: Dependencies, warnings removal, TreatWarningsAsErrors, AuthController/OrdersController warning fixes & seeding refactor, SignalR hub event synchronization, JSON serialization & error contracts.

## 🔒 My Identity
- Archetype: teamwork_preview_worker
- Roles: implementer, qa, specialist
- Working directory: D:\AyushV\RestaurantPOS\.agents\teamwork\worker_backend_m1
- Original parent: 1e8a1d12-8d7a-49ce-9d47-19164d34effe
- Milestone: Milestone 1 - Backend Audit, Warnings, Packages, Error Contracts & Workflows

## 🔒 Key Constraints
- DO NOT CHEAT. All implementations must be genuine.
- Exclusive Write Ownership: RestaurantPOS.API/** only. Do NOT touch RestaurantPOS.Client/**.
- TreatWarningsAsErrors must be enabled and dotnet build must succeed with 0 errors and 0 warnings.

## Current Parent
- Conversation ID: 1e8a1d12-8d7a-49ce-9d47-19164d34effe
- Updated: 2026-10-18T01:25:00Z

## Task Summary
- **What to build**:
  1. `RestaurantPOS.API.csproj`: Removed `Microsoft.AspNetCore.SignalR.Common` (v10.0.11), added `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
  2. `AuthController.cs`: Fixed CS8604 null reference argument on RestaurantId claim, refactored `EnsureDefaultUsers(RestaurantPosContext context)` to a public static method and made `HashPassword` static, removed constructor invocation.
  3. `FloorController.cs`: Emitted `"TableStatusChanged"` event with `(table.Id.ToString(), table.Status.ToString(), table.CurrentOrderId?.ToString())` and standardized NotFound response.
  4. `OrdersController.cs`: Fixed compiler warnings CS8629 (`i.MenuItemId!.Value`) and CS8602 (null checks on `k.Order` and null-forgiving on `o!.Table`, `k!.Order`), added idempotency check on `SettleOrder` (`order.Status == OrderStatus.Paid || order.IsPaid`), broadcast `TableStatusChanged` on table occupation in `CreateOrder` and vacancy in `SettleOrder`, standardized error responses to RFC 7807 `ProblemDetails`.
  5. `Program.cs`: Configured `JsonStringEnumConverter`, registered `builder.Services.AddProblemDetails()`, invoked `AuthController.EnsureDefaultUsers(context)` on startup in a scoped container, appended `public partial class Program { }` to expose for `WebApplicationFactory<Program>`.
- **Success criteria**:
  - `dotnet build RestaurantPOS.API/RestaurantPOS.API.csproj --no-incremental` builds with 0 errors and 0 warnings (with TreatWarningsAsErrors enabled).
  - `dotnet test RestaurantPOS.API.Tests/RestaurantPOS.API.Tests.csproj` executes and passes (4/4 tests).
- **Interface contracts**: D:\AyushV\RestaurantPOS\.agents\teamwork\ORIGINAL_REQUEST.md & explorer_backend\report.md

## Change Tracker
- **Files modified**:
  - `RestaurantPOS.API/RestaurantPOS.API.csproj`: Removed SignalR.Common package reference, added TreatWarningsAsErrors.
  - `RestaurantPOS.API/Controllers/AuthController.cs`: Refactored EnsureDefaultUsers and HashPassword to static, fixed claim nullability.
  - `RestaurantPOS.API/Controllers/FloorController.cs`: Aligned TableStatusChanged event broadcast signature, added ProblemDetails.
  - `RestaurantPOS.API/Controllers/OrdersController.cs`: Fixed nullability warnings CS8629 & CS8602, added TableStatusChanged broadcast to CreateOrder & SettleOrder, added idempotency guard on SettleOrder, standardized ProblemDetails error responses.
  - `RestaurantPOS.API/Program.cs`: Added AddProblemDetails, JsonStringEnumConverter, startup default user seeding, and public partial class Program.
- **Build status**: Pass (0 errors, 0 warnings with TreatWarningsAsErrors=true)
- **Pending issues**: None

## Quality Status
- **Build/test result**: Pass (Build: 0 errors, 0 warnings; Tests: 4 passed, 0 failed)
- **Lint status**: Clean (TreatWarningsAsErrors enabled)
- **Tests added/modified**: Test integration validated via RestaurantPOS.API.Tests

## Loaded Skills
- None specified.

## Key Decisions Made
- `HashPassword` in AuthController made public static so `EnsureDefaultUsers` can be safely invoked during Program.cs application startup.
- ProblemDetails standardized across all NotFound and BadRequest paths in OrdersController and FloorController.
- TableStatusChanged broadcast includes tableId, status, and orderId matching frontend posStore listener.

## Artifact Index
- D:\AyushV\RestaurantPOS\.agents\teamwork\worker_backend_m1\progress.md
- D:\AyushV\RestaurantPOS\.agents\teamwork\worker_backend_m1\handoff.md
