# Allow Increasing Existing Position Quantity at Same Buy Price

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repo at `.agent/PLANS.md` from the repository root. This document must be maintained in accordance with that file.

## Purpose / Big Picture

Today, positions are created per buy event, and there is no direct way to add quantity to an existing open position when you buy more at the same price. After this change, a user can select an existing open position and increase its quantity for the same buy price. They can see it working by adding a position, then using the new “Add quantity” action to increase it, and verifying the updated quantity and P&L calculations.

## Progress

- [x] (2026-02-04 22:05Z) Add backend endpoint and data store method to increment quantity on open positions with the same buy price.
- [x] (2026-02-04 22:11Z) Add frontend UI controls and API call to increase a position’s quantity.
- [x] (2026-02-04 22:16Z) Add integration tests for the new endpoint behavior.

## Surprises & Discoveries

None yet.

## Decision Log

- Decision: The increment endpoint will only accept open (unsold) positions and will require the provided buy price to match the existing position’s buy price.
  Rationale: This keeps the feature focused on “buy more at the same price” and avoids accidental averaging logic.
  Date/Author: 2026-02-04 / Codex
- Decision: Frontend will reuse the existing “Update” flow in the positions UI. If the buy price input is empty, do not change price; if the add-quantity input is empty, only update price. Frontend will decide whether to call the buy-price endpoint, the increase-quantity endpoint, or both.
  Rationale: This matches the UI mental model you described and keeps backend endpoints focused and composable.
  Date/Author: 2026-02-04 / Codex

## Outcomes & Retrospective

Implemented the increase-quantity endpoint, frontend Update flow changes, and integration tests. Remaining work is to run the test suite and validate the combined Update behavior manually.

## Context and Orientation

Positions are stored in memory in `backend/Services/InMemoryDataStore.cs` and persisted via `SqlitePositionStore`. The API surface for positions is in `backend/Controllers/PositionsController.cs`. The frontend shows positions in `frontend/src/components/PositionsCard.vue` and orchestrates interactions in `frontend/src/pages/HomePage.vue`. API calls are defined in `frontend/src/api.js`, and position data is managed in `frontend/src/composables/usePositions.js`.

An “open position” is a position that has not been sold (its `SoldAt` is null). This feature only applies to open positions and does not change tax or profit calculations because those are computed at sell time.

## Plan of Work

First, add a backend endpoint that increments quantity on an open position when the buy price matches exactly. Implement a new request DTO with the additional quantity and buy price. The handler should validate that quantity is positive, the position exists, the position is not sold, and the buy price matches. If valid, increment quantity and persist. If invalid, return a clear 400 error.

Next, adjust the frontend positions UI to reuse the existing “Update” action. Add an “add quantity” input alongside the buy price input. The Update handler will:

If buy price is provided but add-quantity is empty, call the existing buy-price update endpoint only.
If add-quantity is provided but buy price is empty, call the new increase-quantity endpoint only.
If both are provided, call both endpoints in a safe order (update price first or increase quantity first, but consistent), and then refresh positions. Provide loading and error toasts, and clear inputs on success.

Finally, add integration tests for the new endpoint, including:
- Returns 404 for unknown position.
- Returns 400 for sold position.
- Returns 400 for buy price mismatch.
- Returns 200/202 and updated quantity for a valid increment.

## Concrete Steps

Backend:

  - Edit `backend/Models/Requests` to add `IncreasePositionQuantityRequest` with `Quantity` and `BuyPrice`.
  - Edit `backend/Services/InMemoryDataStore.cs` to add `IncreasePositionQuantityAsync(Guid id, int quantity, double buyPrice, CancellationToken cancellationToken)` that validates and persists.
  - Edit `backend/Controllers/PositionsController.cs` to add `POST /api/positions/{id}/increase` (or similar) using the new request.

Frontend:

  - Edit `frontend/src/api.js` to add `increasePositionQuantity(positionId, buyPrice, quantity)`.
  - Edit `frontend/src/components/PositionsCard.vue` to add an “add quantity” input and wire it into the existing Update button flow.
  - Edit `frontend/src/pages/HomePage.vue` to decide whether to call the buy-price update endpoint, the increase-quantity endpoint, or both based on which inputs are present.

Tests:

  - Add `backend.Tests/PositionIncreaseTests.cs` to validate endpoint behavior using `WebApplicationFactory`.

## Validation and Acceptance

Run backend tests:

  - Working directory: `C:\dev\Freetime\OSRS-GE-Monitor`
  - Command: `dotnet test backend.Tests/OSRSGeMonitor.Api.Tests.csproj`

Expect all tests to pass, including the new increase quantity tests.

Manual verification:

  - Start the backend and frontend.
  - Add a manual position or acknowledge an alert to create a position.
  - Leave buy price blank, enter add-quantity, and click Update. Observe quantity increases and price unchanged.
  - Enter buy price, leave add-quantity blank, and click Update. Observe price changes and quantity unchanged.
  - Enter both buy price and add-quantity and click Update. Observe both changes applied.

## Idempotence and Recovery

All steps are additive and safe to repeat. If the increase endpoint returns a validation error, the client should show the error and allow retry after correcting the input.

## Artifacts and Notes

Expected endpoint behavior:

  - 200 OK with updated position when quantity is increased successfully.
  - 400 Bad Request when the buy price does not match.
  - 400 Bad Request when quantity is invalid or position is sold.
  - 404 Not Found when the position id does not exist.

## Interfaces and Dependencies

New endpoint:

  - `POST /api/positions/{id}/increase`

New request DTO:

  - `IncreasePositionQuantityRequest(double BuyPrice, int Quantity)`

New frontend API helper:

  - `increasePositionQuantity(positionId, buyPrice, quantity)`

Plan update note (2026-02-04): Updated the frontend design to reuse the existing Update button and documented the conditional endpoint calls based on which inputs are provided.
Plan update note (2026-02-04): Marked progress complete after implementing backend, frontend, and tests.
