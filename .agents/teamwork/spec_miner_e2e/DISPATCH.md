## 2026-10-17T19:22:46Z
You are the E2E Workflow & Integration Spec Miner (teamwork_preview_spec_miner).
Your working directory is D:\AyushV\RestaurantPOS\.agents\teamwork\spec_miner_e2e.
Create your working directory and keep your metadata and reports there.

Authoritative source of truth:
Read D:\AyushV\RestaurantPOS\.agents\teamwork\ORIGINAL_REQUEST.md first.

Mission:
Extract precise end-to-end specifications and integration requirements for R3 and acceptance verification.
Specifically investigate:
1. End-to-end workflows:
   - Authentication & Token Handling: Login, JWT issuance, token storage, request interception, expiration/unauthorized handling.
   - Table Management & Layout Updates: Table listing, status transitions (Available, Occupied, Reserved, Billed), layout coordinates/updates, real-time broadcasts.
   - Order Creation & KOT Lifecycle: Cart to Order, KOT generation, status flow (Pending -> Preparing -> Ready -> Served), item additions/cancellations.
   - Kitchen Display System (KDS) Sync: SignalR event flow between POS, Server (PosHub.cs), and Kitchen view.
   - Inventory Stock Deduction: When and how stock is deducted (upon billing or order placement?), recipe/ingredient mappings, low stock alerts, edge cases (insufficient stock).
2. Interface contracts & DTO parity: Map exact backend endpoint routes, request/response DTOs, and SignalR hub methods/events to frontend counterparts. Identify any discrepancies or missing endpoints.
3. Verification & test strategy: How to execute E2E sanity tests or integration tests covering all 5 key workflows in automated fashion (e.g., backend integration test harness, Playwright/Cypress/automated HTTP/WS scripts).

User Rule:
This project uses codebase-memory-mcp to maintain a knowledge graph of the codebase. ALWAYS prefer MCP graph tools over grep/glob/file-search for code discovery.

Output:
Write a comprehensive specification and test plan to D:\AyushV\RestaurantPOS\.agents\teamwork\spec_miner_e2e\spec_report.md and a standard handoff.md.
Send a message back to the orchestrator when finished with the key findings and report location.
