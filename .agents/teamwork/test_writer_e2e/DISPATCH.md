## 2026-10-17T19:36:11Z
You are the E2E Test Suite Creator (teamwork_preview_test_writer).
Your working directory is D:\AyushV\RestaurantPOS\.agents\teamwork\test_writer_e2e.
Create your working directory and store your metadata/reports there.

Authoritative source of truth:
Read D:\AyushV\RestaurantPOS\.agents\teamwork\ORIGINAL_REQUEST.md first.
Also read the detailed specifications and test harness design in:
D:\AyushV\RestaurantPOS\.agents\teamwork\spec_miner_e2e\spec_report.md

Mission:
Build a comprehensive, production-grade automated integration test project (`RestaurantPOS.API.Tests`) covering all 5 core workflows defined in ORIGINAL_REQUEST.md and spec_report.md.

Scope & Test Architecture:
1. Create `RestaurantPOS.API.Tests/RestaurantPOS.API.Tests.csproj` targeting `net10.0` with:
   - `xunit` (v2.9.3 or compatible)
   - `xunit.runner.visualstudio`
   - `Microsoft.NET.Test.Sdk`
   - `Microsoft.AspNetCore.Mvc.Testing`
   - `Microsoft.EntityFrameworkCore.InMemory` (or Sqlite)
   - Reference `..\RestaurantPOS.API\RestaurantPOS.API.csproj`.
   - Add the test project to the solution (`RestaurantPOS.sln`) if feasible.
2. Implement a custom `CustomWebApplicationFactory<Program>` that configures an in-memory/isolated database with seeded users, restaurants, outlets, floors, tables, menu categories, menu items, raw materials, and recipes.
3. Implement 4 tiers of automated test suites:
   - **Tier 1: Feature Coverage** (>=5 test cases per feature across Auth, Floor/Table, Order/KOT, KDS/SignalR, Inventory/Billing):
     - Auth: Login valid, PIN login valid, SuperAdmin login, Register restaurant, Token validation.
     - Table: Get tables, Update table status, Table layout.
     - Order & KOT: Create dine-in order, Add items, Fire KOT, Toggle KOT item, Update KOT status.
     - SignalR / Hub: Connect to hub, Join group, Event verification.
     - Inventory: Get raw materials, Stock adjustment, Settle order & stock deduction.
   - **Tier 2: Boundary & Corner Cases** (>=5 per feature):
     - Invalid credentials & PIN.
     - SuperAdmin null RestaurantId token claim handling.
     - Non-existent table/outlet/order IDs returning 404 ProblemDetails.
     - Empty items order creation.
     - Double settlement attempt returning 400 ProblemDetails (idempotency).
     - Stock deduction when recipe ingredients don't exist.
   - **Tier 3: Cross-Feature Combinations**:
     - Full order lifecycle: Table occupied -> Order created -> KOT fired -> Items toggled -> Bill settled -> Table marked vacant -> Inventory stock deducted.
     - Multiple orders on separate tables without interference.
   - **Tier 4: Real-World Scenarios**:
     - Multi-course dining scenario with multiple KOTs fired sequentially before final bill settlement.
     - Takeaway order without table assignment.
4. Verify tests pass cleanly by running `dotnet test RestaurantPOS.API.Tests/RestaurantPOS.API.Tests.csproj`.
5. When all tests pass, generate `TEST_READY.md` at project root `D:\AyushV\RestaurantPOS\TEST_READY.md` summarizing the test suite, test count per tier, execution command, and exit status.

Deliver a complete handoff report to `D:\AyushV\RestaurantPOS\.agents\teamwork\test_writer_e2e\handoff.md` and message the orchestrator when done.

## 2026-10-17T19:44:27Z
Backend Worker has been instructed to append `public partial class Program { }` to `Program.cs`. Backend warning fixes in `AuthController.cs` and `OrdersController.cs` are currently being applied.
Action: Proceed with test suite creation and wiring.
