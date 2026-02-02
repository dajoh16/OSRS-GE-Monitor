# Track Sold Positions, Profit/Loss, And P&L Charts

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

Follow `/.agent/PLANS.md` from the repository root. This document must be maintained in accordance with it.

## Purpose / Big Picture

After this change, you can mark a position as sold by entering a sell price, and the system will compute profit/loss (including GE tax) and persist it. You can then view historical P&L both overall and per item, and visualize it on charts. This enables ranking items by profitability and understanding which items are worth watching over time.

## Progress

- [ ] (2026-02-02 00:00Z) Define data model for sales and P&L (including GE tax).
- [ ] (2026-02-02 00:00Z) Persist sold positions and P&L to SQLite.
- [ ] (2026-02-02 00:00Z) Add API endpoints for selling a position and querying P&L summaries.
- [ ] (2026-02-02 00:00Z) Add frontend UI to sell positions and view profit metrics.
- [ ] (2026-02-02 00:00Z) Add charting for total P&L and per-item P&L.
- [ ] (2026-02-02 00:00Z) Validate persistence, calculations, and charts.

## Surprises & Discoveries

Pending.

## Decision Log

- Decision: Persist position sales and P&L in SQLite alongside the existing `price-history.db` file.
  Rationale: The project already uses SQLite and uses a shared DB file; this keeps persistence simple and reliable across restarts.
  Date/Author: 2026-02-02 / Codex

## Outcomes & Retrospective

Pending.

## Context and Orientation

Positions are currently stored in memory and not persisted. Positions are created when an alert is acknowledged (`InMemoryDataStore.AcknowledgeAlert`). Alerts and positions are surfaced via `/api/positions` and rendered in the frontend. There is already a SQLite DB (`price-history.db`) used for watchlist and time-series caching. This plan adds persistent storage for positions and sales plus new API endpoints and UI/graphing.

## Plan of Work

First, define the sales and P&L data model. We will extend `Position` with `SellPrice`, `SoldAt`, `TaxRateApplied`, and computed `Profit` fields. Profit should be calculated as `(sellPrice * quantity) - (buyPrice * quantity) - (sellPrice * quantity * taxRate)`. Store the tax rate (e.g., 2%) used for the sale.

Second, add SQLite persistence. Create a new SQLite table `Positions` to store buy and sell data. Add a `SqlitePositionStore` with methods for upsert, update with sell price, and read all positions (including unsold).

Third, update `InMemoryDataStore` so positions are loaded from SQLite at startup, and any position updates (buy and sell) are persisted immediately.

Fourth, add API endpoints:
  - `POST /api/positions/{id}/sell` with `{ sellPrice }` to mark as sold.
  - `GET /api/positions/summary` to return total P&L and per-item aggregates (count, total profit, average profit, win rate).
  - `GET /api/positions/history` to return a time series of profit totals by date for charting.

Fifth, update frontend:
  - In the Positions card, add a “Sell” action (input for sell price + confirm).
  - Add a Profit & Loss section that shows total profit, total tax paid, and rankings per item.
  - Add charts for overall P&L over time and per-item P&L, using the existing chart page or a new chart component.

Sixth, ensure persistence: stop/start backend and confirm sold positions and P&L remain.

## Concrete Steps

Work in `C:\dev\Freetime\OSRS-GE-Monitor`.

1. Add storage:
   - Create `backend/Services/SqlitePositionStore.cs` with CRUD and sell-update functions.
   - Add table `Positions` in SQLite with fields:
     `Id (TEXT)`, `ItemId (INT)`, `ItemName (TEXT)`, `Quantity (INT)`, `BuyPrice (REAL)`, `BoughtAt (TEXT)`,
     `SellPrice (REAL NULL)`, `SoldAt (TEXT NULL)`, `TaxRateApplied (REAL NULL)`, `Profit (REAL NULL)`.

2. Update models:
   - Extend `backend/Models/Position.cs` to include `SellPrice`, `SoldAt`, `TaxRateApplied`, `Profit`, and `IsSold`.

3. Update data store:
   - Load positions from SQLite into memory at startup.
   - Persist positions when created (on acknowledge).
   - Persist when sold and update profit fields.

4. Add endpoints:
   - `POST /api/positions/{id}/sell` (calculate profit, tax, store).
   - `GET /api/positions/summary` (aggregates).
   - `GET /api/positions/history` (daily cumulative P&L).

5. Frontend UI:
   - Add sell input + button in `PositionsCard.vue`.
   - Add P&L summary card on the home page or chart page.
   - Add charts for overall and per-item P&L.

## Validation and Acceptance

1. Create a position by acknowledging an alert.
2. Sell it using the new endpoint/UI with a sell price.
3. Verify profit calculation includes 2% tax and is stored in SQLite.
4. Restart backend and confirm positions and profits persist.
5. Check charts reflect totals and per-item series.

## Idempotence and Recovery

Changes are additive. If a migration fails, delete only the new Positions table and re-run the service to recreate it. If a sell price is wrong, allow editing or re-selling by updating the record.

## Artifacts and Notes

Key files to change/add:
  - `backend/Models/Position.cs`
  - `backend/Services/SqlitePositionStore.cs` (new)
  - `backend/Services/InMemoryDataStore.cs`
  - `backend/Controllers/PositionsController.cs`
  - `backend/Program.cs`
  - `frontend/src/components/PositionsCard.vue`
  - `frontend/src/api.js`
  - `frontend/src/pages/HomePage.vue`
  - Chart components (existing or new)

## Interfaces and Dependencies

Positions API:

  - `POST /api/positions/{id}/sell` body: `{ \"sellPrice\": number }`
  - `GET /api/positions/summary` returns:
      `{ totalProfit, totalTax, perItem: [{ itemId, itemName, count, totalProfit, avgProfit, winRate }] }`
  - `GET /api/positions/history` returns:
      `[{ date: \"2026-02-02\", totalProfit: 12345 }]`

Profit calculation:
  - `gross = sellPrice * quantity`
  - `tax = gross * 0.02`
  - `profit = gross - (buyPrice * quantity) - tax`

Change note: 2026-02-02 / Codex. Created initial ExecPlan for position sales and P&L tracking.
