# Backend Architecture Investigation Report: RestaurantPOS.API

**Explorer Role**: Backend Architecture Explorer (`explorer_backend`)  
**Target Codebase**: `D:\AyushV\RestaurantPOS\RestaurantPOS.API`  
**Date**: 2026-10-18  
**Scope**: Requirements R1 (Backend Audit & Code Quality) and backend aspects of R3 (End-to-End Workflow Integration & Real-Time Sync)

---

## 1. Executive Summary

A comprehensive, read-only architectural investigation was conducted across the `RestaurantPOS.API` ASP.NET Core project. The backend is built on .NET 10 (`net10.0`) using EF Core with PostgreSQL and SignalR for real-time kitchen/terminal updates.

### Core Discoveries
1. **10 Build Warnings Detected**: A clean build emits 4 NuGet warnings (2x `NU1510` and 2x `NU1903` regarding a known high-severity vulnerability in `Microsoft.AspNetCore.SignalR.Common`) and 6 C# compiler nullability warnings (`CS8604` in `AuthController.cs`, `CS8629` and 4x `CS8602` in `OrdersController.cs`).
2. **Redundant & Vulnerable Package**: `Microsoft.AspNetCore.SignalR.Common` (v10.0.11) is explicitly referenced in `RestaurantPOS.API.csproj`, which is automatically provided by `Microsoft.NET.Sdk.Web` (`Microsoft.AspNetCore.App`). Removing this reference eliminates all 4 NuGet warnings and the vulnerability advisory.
3. **SignalR Real-Time Sync Breakage**: A critical contract mismatch exists between the backend and frontend. The backend emits `"TableStatusUpdated"` with an anonymous object payload `{ tableId, status, currentOrderId }`, whereas the frontend client listens for `"TableStatusChanged"` expecting 3 separate arguments `(tableId, status, orderId)`. Furthermore, when orders are created or settled, table status updates are not broadcast via SignalR.
4. **Enum Serialization Inconsistency**: The API serializes enums as numeric integers by default (`JsonStringEnumConverter` is not configured in `Program.cs`), but the React client expects string literals (`'Vacant' | 'Occupied' | 'KotPlaced' | ...`), causing UI badge colors and states to fail.
5. **Double Settlement Inventory Deduction Bug**: `OrdersController.SettleOrder` lacks idempotency protection. If a paid order is settled again, inventory stocks are deducted a second time.
6. **Zero Automated Tests**: No test projects or test suites exist in the entire repository.

---

## 2. Project Dependencies & Build Configuration (`RestaurantPOS.API.csproj`)

### File Inspection
`RestaurantPOS.API.csproj` currently specifies:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.11" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.11" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="10.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.11">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
  </ItemGroup>
</Project>
```

### Dependency Audit
| Package | Version | Status | Action Required |
|---|---|---|---|
| `Microsoft.AspNetCore.SignalR.Common` | 10.0.11 | **Redundant & Vulnerable** (`NU1510`, `NU1903`, GHSA-334f-vvp4-7g6p) | **Remove completely**. The Web SDK already provides all SignalR libraries (`Microsoft.AspNetCore.SignalR`). |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.11 | Clean & Active | Keep |
| `Microsoft.AspNetCore.OpenApi` | 10.0.11 | Clean & Active | Keep |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.11 | Clean & Active | Keep (Design-time tooling for EF Core migrations) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | Clean & Active | Keep |

### Build Configuration Hardening
To prevent regressions and satisfy the acceptance criteria of 0 errors and 0 warnings:
- Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` under `<PropertyGroup>`.

---

## 3. Compiler Warnings & Nullability Audit

A clean rebuild (`dotnet build --no-incremental -v n`) reproduces 10 warnings:

### Warning Catalog & Exact Fixes

#### 1. NuGet Prune & Vulnerability Warnings (4 Warnings)
- **Warnings**:
  - `NU1510: PackageReference Microsoft.AspNetCore.SignalR.Common will not be pruned. This package is automatically available and does not need to be referenced explicitly. Remove the PackageReference item.` (Emitted during restore & resolve)
  - `NU1903: Package 'Microsoft.AspNetCore.SignalR.Common' 10.0.11 has a known high severity vulnerability, https://github.com/advisories/GHSA-334f-vvp4-7g6p` (Emitted during restore & resolve)
- **Remediation**: Remove line 12 from `RestaurantPOS.API.csproj`:
  ```xml
  <!-- DELETE THIS LINE: -->
  <PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="10.0.11" />
  ```

#### 2. AuthController.cs(403,39): Warning CS8604
- **Warning**: `Possible null reference argument for parameter 'value' in 'Claim.Claim(string type, string value)'.`
- **Root Cause**: `user.RestaurantId` is nullable `Guid?` (SuperAdmin users have `RestaurantId = null`). Calling `.ToString()` on a null `Guid?` evaluates to null, which is invalid for the `Claim` value parameter.
- **Location**: `RestaurantPOS.API/Controllers/AuthController.cs`, Line 403:
  ```csharp
  // BEFORE:
  new Claim("RestaurantId", user.RestaurantId.ToString()),
  
  // AFTER (FIX):
  new Claim("RestaurantId", user.RestaurantId?.ToString() ?? string.Empty),
  ```

#### 3. OrdersController.cs(254,96): Warning CS8629
- **Warning**: `Nullable value type may be null.`
- **Root Cause**: `i.MenuItemId` is `Guid?`. Even though filtered by `.Where(i => !i.IsVoided && i.MenuItemId.HasValue)`, the compiler's flow analysis does not infer non-nullability inside `.Select(i => i.MenuItemId.Value)`.
- **Location**: `RestaurantPOS.API/Controllers/OrdersController.cs`, Line 254:
  ```csharp
  // BEFORE:
  var itemIds = order.Items.Where(i => !i.IsVoided && i.MenuItemId.HasValue).Select(i => i.MenuItemId.Value).ToList();
  
  // AFTER (FIX):
  var itemIds = order.Items.Where(i => !i.IsVoided && i.MenuItemId.HasValue).Select(i => i.MenuItemId!.Value).ToList();
  ```

#### 4. OrdersController.cs(303,35) & (304,25): Warning CS8602 (x2)
- **Warning**: `Dereference of a possibly null reference.`
- **Root Cause**: In `KitchenOrderTicket`, navigation property `public Order? Order { get; set; }` is nullable. Line 303 `.ThenInclude(o => o.Table)` and Line 304 `k.Order.OutletId` dereference `k.Order` without checking for null.
- **Location**: `RestaurantPOS.API/Controllers/OrdersController.cs`, Lines 300–306:
  ```csharp
  // BEFORE:
  var kots = await _context.KitchenOrderTickets
      .Include(k => k.Items)
      .Include(k => k.Order)
          .ThenInclude(o => o.Table)
      .Where(k => k.Order.OutletId == outletId && k.Status != KotStatus.Served && k.Status != KotStatus.Cancelled)
      .OrderBy(k => k.FiredAt)
      .ToListAsync();

  // AFTER (FIX):
  var kots = await _context.KitchenOrderTickets
      .Include(k => k.Items)
      .Include(k => k.Order)
          .ThenInclude(o => o!.Table)
      .Where(k => k.Order != null && k.Order.OutletId == outletId && k.Status != KotStatus.Served && k.Status != KotStatus.Cancelled)
      .OrderBy(k => k.FiredAt)
      .ToListAsync();
  ```

#### 5. OrdersController.cs(311,27): Warning CS8602
- **Warning**: `Dereference of a possibly null reference.`
- **Root Cause**: In the LINQ projection inside `GetActiveKots`, `k.Order.OrderNumber` dereferences nullable `k.Order`.
- **Location**: `RestaurantPOS.API/Controllers/OrdersController.cs`, Line 311:
  ```csharp
  // BEFORE:
  orderNumber = k.Order.OrderNumber,
  tableNumber = k.Order.Table != null ? k.Order.Table.TableNumber : "Takeaway",

  // AFTER (FIX):
  orderNumber = k.Order != null ? k.Order.OrderNumber : string.Empty,
  tableNumber = k.Order?.Table != null ? k.Order.Table.TableNumber : "Takeaway",
  ```

#### 6. OrdersController.cs(359,35): Warning CS8602
- **Warning**: `Dereference of a possibly null reference.`
- **Root Cause**: In `KotItem`, navigation property `public KitchenOrderTicket? KitchenOrderTicket { get; set; }` is nullable.
- **Location**: `RestaurantPOS.API/Controllers/OrdersController.cs`, Line 359:
  ```csharp
  // BEFORE:
  var item = await _context.KotItems
      .Include(i => i.KitchenOrderTicket)
          .ThenInclude(k => k.Order)
      .FirstOrDefaultAsync(i => i.Id == itemId);

  // AFTER (FIX):
  var item = await _context.KotItems
      .Include(i => i.KitchenOrderTicket)
          .ThenInclude(k => k!.Order)
      .FirstOrDefaultAsync(i => i.Id == itemId);
  ```

---

## 4. API Controllers, Error Handling, Validation, and HTTP Contracts

### Controller Endpoint Inventory
The API exposes 6 controllers mapped to `api/[controller]`:

| Controller | HTTP Method & Route | Request Body / Params | Return Type | Status Codes | Auth Required? |
|---|---|---|---|---|---|
| **AuthController** | `POST api/Auth/login` | `LoginRequest` | `AuthResponse` | 200, 401 | Anonymous |
| | `POST api/Auth/pin-login` | `PinLoginRequest` | `AuthResponse` | 200, 401 | Anonymous |
| | `POST api/Auth/register` | `CreateRestaurantRequest` | `AuthResponse` | 200, 400 | Anonymous |
| | `GET api/Auth/restaurants` | None | Anonymous list | 200 | Missing `[Authorize]` (reads `User.Claims`) |
| | `POST api/Auth/restaurants/admin` | `SuperAdminCreateRestaurantRequest` | Anonymous object | 200, 400 | Missing `[Authorize]` |
| | `POST api/Auth/users` | `CreateUserRequest` | `{ message }` | 200, 400 | Missing `[Authorize]` |
| | `GET api/Auth/users/{restaurantId}` | `Guid restaurantId` | Anonymous list | 200 | Missing `[Authorize]` (reads `User.Claims`) |
| **FloorController** | `GET api/Floor/outlets/{outletId}/floors` | `Guid outletId` | `List<Floor>` | 200 | None |
| | `POST api/Floor/floors` | `CreateFloorRequest` | `Floor` | 200 | None |
| | `POST api/Floor/tables` | `CreateTableRequest` | `RestaurantTable` | 200 | None |
| | `PATCH api/Floor/tables/{tableId}/status`| `Guid tableId`, `[FromBody] TableStatus` | `RestaurantTable` | 200, 404 | None |
| **InventoryController**| `GET api/Inventory/materials/{restaurantId}` | `Guid restaurantId` | `List<RawMaterial>` | 200 | None |
| | `POST api/Inventory/materials/{restaurantId}`| `CreateRawMaterialRequest` | `RawMaterial` | 200 | None |
| | `POST api/Inventory/stock/{outletId}/adjust` | `UpdateStockRequest` | `InventoryStock` | 200 | None |
| | `POST api/Inventory/recipes/{menuItemId}` | `List<RecipeItem>` | `List<RecipeItem>` | 200 | None |
| **MenuController** | `GET api/Menu/categories/{restaurantId}` | `Guid restaurantId` | `List<Category>` | 200 | None |
| | `POST api/Menu/categories/{restaurantId}`| `CreateCategoryRequest` | `Category` | 200 | None |
| | `GET api/Menu/items/{restaurantId}` | `Guid restaurantId` | `List<MenuItem>` | 200 | None |
| | `POST api/Menu/items/{restaurantId}` | `CreateMenuItemRequest` | `MenuItem` | 200 | None |
| | `PATCH api/Menu/items/{itemId}/toggle-availability`| `Guid itemId` | `{ itemId, isAvailable }`| 200, 404 | None |
| **OrdersController** | `GET api/Orders/outlet/{outletId}/active` | `Guid outletId` | `List<Order>` | 200 | None |
| | `POST api/Orders` | `CreateOrderRequest` | `Order` | 200, 404 | None |
| | `GET api/Orders/{orderId}` | `Guid orderId` | `Order` | 200, 404 | None |
| | `POST api/Orders/{orderId}/items` | `List<OrderItemRequest>` | `Order` | 200, 404 | None |
| | `POST api/Orders/{orderId}/fire-kot` | `Guid orderId` | `KitchenOrderTicket` | 200, 400, 404 | None |
| | `POST api/Orders/{orderId}/settle` | `SettleOrderRequest` | `Order` | 200, 404 | None |
| | `GET api/Orders/outlet/{outletId}/kots` | `Guid outletId` | Anonymous list | 200 | None |
| | `POST api/Orders/kots/{kotId}/status` | `UpdateKotStatusRequest` | `KitchenOrderTicket` | 200, 404 | None |
| | `POST api/Orders/kots/items/{itemId}/toggle` | `Guid itemId` | `KotItem` | 200, 404 | None |
| | `POST api/Orders/reset-db` | None | `{ message }` | 200 | None (Destructive dev action) |
| **ReportsController** | `GET api/Reports/outlet/{outletId}/dashboard` | `Guid outletId` | Anonymous stats object | 200 | None |
| | `GET api/Reports/outlet/{outletId}/sales-summary` | `outletId`, `fromDate`, `toDate` | Anonymous list | 200 | None |

### Key Issues in Contracts & Validation

1. **Inconsistent Error Response Format**:
   - `OrdersController` lines 131, 173, 228, 335, 362 return empty `NotFound()` (HTTP 404 without body).
   - `OrdersController` line 45 returns `NotFound(new { message = "Outlet not found." })`.
   - `AuthController` returns `BadRequest(new { message = "..." })` and `Unauthorized(new { message = "..." })`.
   - **Recommendation**: Standardize on RFC 7807 `ProblemDetails` via ASP.NET Core `Problem(detail: ..., statusCode: ...)` or register `builder.Services.AddProblemDetails()` in `Program.cs`.

2. **Enum Serialization (Number vs String)**:
   - In `Program.cs`:
     ```csharp
     builder.Services.AddControllers()
         .AddJsonOptions(options => {
             options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
             // MISSING:
             options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
         });
     ```
   - In frontend TypeScript (`types/pos.ts` line 57), `status: 'Vacant' | 'Occupied' | 'KotPlaced' | 'Billed' | 'Reserved'`. Without `JsonStringEnumConverter`, the backend sends integers `1, 2, 3...`, causing frontend UI status matches to fail.

3. **Input Validation Deficiencies**:
   - DTOs in `DTOs/PosDtos.cs` are raw C# records without validation attributes (`[Required]`, `[Range]`, `[MinLength]`).
   - `CreateOrderRequest`: If `Items` is null or empty, `CreateOrder` iterates without guard and generates a 0-value order or throws `NullReferenceException`.
   - Foreign key validation: In `CreateMenuItem`, `CategoryId` is assigned directly without verifying existence in DB. If an invalid GUID is supplied, EF Core throws a foreign key constraint exception resulting in an unhandled 500 error instead of a 400 Bad Request.

4. **Security & Authorization Gaps**:
   - Endpoints inspecting `HttpContext.User.Claims` (`GetRestaurants`, `CreateRestaurantSuperAdmin`, `GetUsers`) lack `[Authorize]`. If called unauthenticated, claims evaluate to null and bypass tenant scoping or behave unpredictably.

---

## 5. SignalR Hub (`PosHub.cs`) & Real-Time Sync Inspection

### Hub Definition
`PosHub.cs` exposes:
```csharp
public class PosHub : Hub
{
    public async Task JoinOutletGroup(string outletId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"outlet-{outletId}");
    }
    public async Task LeaveOutletGroup(string outletId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"outlet-{outletId}");
    }
    public async Task JoinKitchenStation(string outletId, string station)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"station-{outletId}-{station}");
    }
}
```

### SignalR Event Matrix: Backend vs Frontend
| Event Name (Backend) | Emission Location | Target Group | Event Name (Frontend) | Listener Signature (Frontend) | Contract Status |
|---|---|---|---|---|---|
| `"TableStatusUpdated"` | `FloorController.cs:89` | `$"outlet-{floor.OutletId}"` | `"TableStatusChanged"` | `(tableId: string, status: RestaurantTable['status'], orderId?: string)` | **BROKEN (Name & Arg Mismatch)** |
| *(None emitted)* | `OrdersController.cs:106` | — | *(None)* | — | Table status set to `Occupied` in DB, but table status change is **NOT broadcast** to other terminals |
| *(None emitted)* | `OrdersController.cs:247` | — | *(None)* | — | Table released to `Vacant` on settle in DB, but table status change is **NOT broadcast** |
| `"NewOrderCreated"` | `OrdersController.cs:106` | `$"outlet-{req.OutletId}"` | *(None)* | *(Unused on client)* | Backend emits, client doesn't listen |
| `"KotReceived"` | `OrdersController.cs:209` | `$"outlet-{order.OutletId}"` | `"KotReceived"` | `() => get().fetchKots()` | **Matched** |
| `"OrderSettled"` | `OrdersController.cs:292` | `$"outlet-{order.OutletId}"` | `"OrderSettled"` | `() => { refetchFloors(); refetchStats(); }` | **Matched** |
| `"KotStatusChanged"` | `OrdersController.cs:348` | `$"outlet-{kot.Order.OutletId}"` | `"KotStatusChanged"` | `() => get().fetchKots()` | **Matched** |
| `"KotItemToggled"` | `OrdersController.cs:369` | `$"outlet-{item.Kot.Order.OutletId}"` | `"KotItemToggled"` | `() => get().fetchKots()` | **Matched** |

### Detailed Remediation for Real-Time Sync
1. In `FloorController.cs`:
   Change the broadcast from:
   ```csharp
   await _hubContext.Clients.Group($"outlet-{table.Floor.OutletId}")
       .SendAsync("TableStatusUpdated", new { tableId = table.Id, status = table.Status, currentOrderId = table.CurrentOrderId });
   ```
   To emit both or align with the frontend:
   ```csharp
   await _hubContext.Clients.Group($"outlet-{table.Floor.OutletId}")
       .SendAsync("TableStatusChanged", table.Id.ToString(), table.Status.ToString(), table.CurrentOrderId?.ToString());
   ```
2. In `OrdersController.CreateOrder`:
   After setting `table.Status = TableStatus.Occupied`, broadcast `TableStatusChanged`:
   ```csharp
   if (req.TableId.HasValue && table != null)
   {
       await _hubContext.Clients.Group($"outlet-{req.OutletId}")
           .SendAsync("TableStatusChanged", table.Id.ToString(), TableStatus.Occupied.ToString(), order.Id.ToString());
   }
   ```
3. In `OrdersController.SettleOrder`:
   After setting `order.Table.Status = TableStatus.Vacant`, broadcast `TableStatusChanged`:
   ```csharp
   if (order.TableId.HasValue && order.Table != null)
   {
       await _hubContext.Clients.Group($"outlet-{order.OutletId}")
           .SendAsync("TableStatusChanged", order.Table.Id.ToString(), TableStatus.Vacant.ToString(), (string?)null);
   }
   ```

---

## 6. Business Logic & Core Workflows

### 1. Order Creation & Numbering
- Sequence format: `$"ORD-{DateTime.UtcNow:yyyyMMdd}-{(orderCountToday + 1):D3}"`.
- Subtotal is computed by summing `item.UnitPrice * item.Quantity`.
- Tax calculation: Retrieves `outlet.Restaurant?.DefaultGstPercent` (fallback 5.0%), calculates tax, and sums to `GrandTotal`.
- **Finding**: Table occupancy collision is not prevented. If a table is already occupied, `CreateOrder` silently overwrites `table.CurrentOrderId`. It should validate `table.Status == TableStatus.Vacant`.

### 2. KOT Lifecycle
- Sequence format: `$"KOT-{(order.Kots.Count + 1):D2}"`.
- Partial firing supported: filters `order.Items.Where(i => !i.IsVoided && !i.IsFired)`.
- Updates `order.Status = OrderStatus.KotPlaced`.
- Cook can toggle items: `POST /api/Orders/kots/items/{itemId}/toggle`.
- Cook can update ticket status: `POST /api/Orders/kots/{kotId}/status`.
- **Finding**: While `TableStatus` enum has a `KotPlaced` state (3), `FireKot` only sets `order.Status = OrderStatus.KotPlaced`, leaving `table.Status` as `Occupied`.

### 3. Billing, Settlement & Inventory Deduction
- Cashier triggers settlement via `POST /api/Orders/{orderId}/settle`.
- Updates `DiscountAmount`, `ServiceChargeAmount`, `PaymentMode`, `SettledAt`, and sets `Status = OrderStatus.Paid`.
- Round-off computed via `Math.Round(total, 0, MidpointRounding.AwayFromZero)`.
- **Inventory Deduction Logic**:
  - Direct raw material items: decrements `stock.CurrentStockQuantity -= orderItem.Quantity`.
  - Recipe items: queries `RecipeItems` for each `MenuItemId`, decrements `stock.CurrentStockQuantity -= (recipe.QuantityRequired * orderItem.Quantity)`.
- **Critical Vulnerability - Double Settlement**:
  Line 228 of `OrdersController`:
  ```csharp
  if (order == null) return NotFound();
  // MISSING CHECK:
  if (order.Status == OrderStatus.Paid || order.IsPaid)
  {
      return BadRequest(new ProblemDetails { Title = "Order already settled", Detail = "This order has already been paid and settled." });
  }
  ```
  Without this check, settling a paid order again will re-deduct stock and re-apply roundoff.
- **N+1 Query Issue**:
  Lines 263 and 277 execute `await _context.InventoryStocks.FirstOrDefaultAsync(...)` inside nested loops over order items and recipes. For orders with numerous items, this results in excessive round trips to PostgreSQL. Batch loading relevant stocks beforehand is recommended.
- **Missing Stock Record Handling**:
  If `stock == null`, the deduction is silently ignored without logging or creating an initial stock record.

### 4. Database Context & Migrations
- Provider: PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Database name configured in `appsettings.json`: `"Host=localhost;Database=ResturantPOS;Username=postgres;Password=1212"`.
- 8 migrations exist in `RestaurantPOS.API/Migrations/` up to `20260911153523_DropSubscriptionsTable`. ModelSnapshot is synchronized.
- **Architectural Anti-Pattern**:
  `AuthController` runs `EnsureDefaultUsers()` synchronously in its constructor on every HTTP request instantiation:
  ```csharp
  public AuthController(RestaurantPosContext context, IConfiguration configuration)
  {
      _context = context;
      _configuration = configuration;
      EnsureDefaultUsers(); // Synchronous DB query on every controller instantiation!
  }
  ```
  This causes unnecessary database queries on every auth-related request. Data seeding should occur once on application startup in `Program.cs`.

---

## 7. Existing Tests & Test Project Status

### Test Audit
- **Backend Test Projects**: None exist. There is no `RestaurantPOS.Tests.csproj` or test runner in the solution.
- **Frontend Test Suite**: None configured in `package.json` (no Vitest or Jest scripts).
- **Current Test Coverage**: **0%**.

### Recommended Test Architecture
To fulfill the acceptance criteria ("The application compiles cleanly, and key workflows ... pass automated or end-to-end sanity tests"), a backend test project should be added:
1. Target: `RestaurantPOS.API.Tests/RestaurantPOS.API.Tests.csproj`.
2. Packages: `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.InMemory` (or SQLite in-memory), `FluentAssertions`.
3. Test Scope:
   - `OrderWorkflowTests`: Create DineIn order -> Fire KOT -> Settle Order -> Verify table released to Vacant.
   - `InventoryDeductionTests`: Verify raw material and recipe stock deductions upon settle; verify double-settle rejection.
   - `SignalRContractTests`: Verify `PosHub` group join/leave and event dispatch.
   - `CompilerCleanlinessTest`: Build validation ensuring 0 warnings.

---

## 8. Summary of Action Items for Implementation Phase

1. **Clean Dependencies**:
   - Delete `Microsoft.AspNetCore.SignalR.Common` from `RestaurantPOS.API.csproj`.
   - Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
2. **Eliminate All 6 Compiler Nullability Warnings**:
   - `AuthController.cs:403`: null-safe `user.RestaurantId?.ToString() ?? string.Empty`.
   - `OrdersController.cs:254`: non-null assertion on filtered value `i.MenuItemId!.Value`.
   - `OrdersController.cs:303, 304, 311`: null checks on `k.Order` and null-forgiving on EF navigation `o!.Table`.
   - `OrdersController.cs:359`: null-forgiving on EF navigation `k!.Order`.
3. **Configure Enum String Serialization**:
   - Add `options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());` in `Program.cs`.
4. **Fix Real-Time SignalR Event Alignment**:
   - Align `FloorController` table update event name to `"TableStatusChanged"` and parameter signature to match frontend listener.
   - Add `TableStatusChanged` broadcast to `CreateOrder` and `SettleOrder`.
5. **Business Logic Hardening**:
   - Add double-settlement guard (`if (order.IsPaid) return BadRequest(...)`) in `SettleOrder`.
   - Validate table vacancy in `CreateOrder`.
   - Standardize error responses to RFC 7807 `ProblemDetails`.
   - Move `EnsureDefaultUsers()` out of `AuthController` constructor into a startup hosted service or extension method in `Program.cs`.
6. **Add Integration / Sanity Tests**:
   - Create `RestaurantPOS.API.Tests` project to cover order lifecycle and inventory deduction.
