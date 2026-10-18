## 2026-10-17T19:35:56Z
You are the Backend Implementation Worker (teamwork_preview_worker).
Your working directory is D:\AyushV\RestaurantPOS\.agents\teamwork\worker_backend_m1.
Create your working directory and store your logs/metadata there.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Authoritative source of truth:
Read D:\AyushV\RestaurantPOS\.agents\teamwork\ORIGINAL_REQUEST.md first.
Also read the investigation findings in:
D:\AyushV\RestaurantPOS\.agents\teamwork\explorer_backend\report.md

Exclusive Write Ownership:
You own `RestaurantPOS.API/**` exclusively. Do NOT touch `RestaurantPOS.Client/**`.

Tasks for Milestone 1 (Backend Audit, Warnings, Packages, Error Contracts & Workflows):
1. Dependencies & Warnings configuration:
   - Edit `RestaurantPOS.API/RestaurantPOS.API.csproj`:
     - Delete the redundant and vulnerable `<PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="10.0.11" />`.
     - Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` under `<PropertyGroup>`.
2. Eliminate all 6 compiler warnings:
   - `RestaurantPOS.API/Controllers/AuthController.cs`:
     - Line 403: Fix CS8604 null reference argument: use `new Claim("RestaurantId", user.RestaurantId?.ToString() ?? string.Empty)`.
     - Refactor `EnsureDefaultUsers()`: Remove the synchronous call from `AuthController`'s constructor. Instead, make it an internal/public method or extension method called once during application startup in `Program.cs`.
   - `RestaurantPOS.API/Controllers/OrdersController.cs`:
     - Line 254: Fix CS8629 by using `i.MenuItemId!.Value`.
     - Lines 303, 304: Fix CS8602 by ensuring `k.Order != null && k.Order.OutletId == outletId` and `.ThenInclude(o => o!.Table)`.
     - Line 311: Fix CS8602 by checking `k.Order != null ? k.Order.OrderNumber : string.Empty` and `k.Order?.Table != null ? k.Order.Table.TableNumber : "Takeaway"`.
     - Line 359: Fix CS8602 by `.ThenInclude(k => k!.Order)`.
3. SignalR Hub & Event Sync:
   - In `RestaurantPOS.API/Controllers/FloorController.cs`:
     - In `UpdateTableStatus`, align event broadcast with frontend contract: emit `"TableStatusChanged"` with parameters `(table.Id.ToString(), table.Status.ToString(), table.CurrentOrderId?.ToString())`.
   - In `RestaurantPOS.API/Controllers/OrdersController.cs`:
     - In `CreateOrder`: When a table is assigned and marked `Occupied`, broadcast `TableStatusChanged` to group `$"outlet-{req.OutletId}"` with parameters `(table.Id.ToString(), TableStatus.Occupied.ToString(), order.Id.ToString())`.
     - In `SettleOrder`: When order is settled and `order.Table` is marked `Vacant`, broadcast `TableStatusChanged` to group `$"outlet-{order.OutletId}"` with parameters `(order.Table.Id.ToString(), TableStatus.Vacant.ToString(), (string?)null)`.
4. Serialization & Error Contracts:
   - In `RestaurantPOS.API/Program.cs`:
     - Configure JSON serialization to use `JsonStringEnumConverter`:
       `options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());`
     - Register `builder.Services.AddProblemDetails();`.
     - Call default user seeding on startup in `Program.cs`.
   - In `RestaurantPOS.API/Controllers/OrdersController.cs`:
     - In `SettleOrder`: Add idempotency check to prevent double settlement and double inventory deduction:
       `if (order.Status == OrderStatus.Paid || order.IsPaid) return BadRequest(new ProblemDetails { Title = "Order already settled", Detail = "This order has already been paid and settled." });`
     - Standardize error responses to return `Problem(detail: "...", statusCode: ...)` or `BadRequest(new ProblemDetails { ... })` and `NotFound(new ProblemDetails { ... })`.
5. Verification:
   - Run `dotnet build RestaurantPOS.API/RestaurantPOS.API.csproj`.
   - Confirm it builds with 0 errors and 0 warnings (with TreatWarningsAsErrors enabled).

Deliver a complete handoff report to `D:\AyushV\RestaurantPOS\.agents\teamwork\worker_backend_m1\handoff.md` and message the orchestrator when done.


## 2026-10-17T19:44:23Z
**Context**: Milestone 1 Backend Implementation & Test Integration
**Content**: The E2E Test Writer is creating `RestaurantPOS.API.Tests` with `WebApplicationFactory<Program>`. Please ensure you append `public partial class Program { }` at the bottom of `RestaurantPOS.API/Program.cs` (outside the top-level statements), so that `WebApplicationFactory<Program>` can reference the Program type.
**Action**: Include `public partial class Program { }` in `Program.cs` during your edits.
