```md
# Add Item Metadata, Live Prices, and Trends in the UI

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

This plan follows `.agent/PLANS.md` from the repository root and must be maintained in accordance with it.

## Purpose / Big Picture

After this change, the UI will show richer item information without breaking OSRS Wiki API fair-use guidelines. Users can open a modal from search results or watchlist rows to see item metadata (including buy limits) and a simple trend indicator, and they will see live high/low prices directly on watchlist rows. The backend will reuse a single latest-price fetch for both alerting and UI display and will cache time-series calls for trend data, keeping API usage within fair-use expectations.

## Progress

- [x] (2026-02-03 20:55Z) Add backend data structures and endpoints to expose item details and cached latest market data.
- [x] (2026-02-03 20:55Z) Update price monitor to cache latest high/low prices with timestamps for watchlist display.
- [x] (2026-02-03 20:55Z) Add UI modal for item details and connect it to search and watchlist actions.
- [x] (2026-02-03 20:55Z) Add live high/low prices to watchlist rows using backend cached data.
- [x] (2026-02-03 20:55Z) Document fair-use alignment and wire the UI to avoid extra API calls.

## Surprises & Discoveries

- Observation: The OSRS Wiki real-time prices page explicitly warns against looping the `id` parameter for latest prices and recommends fetching all latest prices in one call.
  Evidence: The OSRS Wiki real-time prices page states that using the `id` parameter for each item is inefficient and should be avoided. It also lists the mapping fields we need, including buy limits and examine text.

## Decision Log

- Decision: Cache `/latest` data in the price monitor and reuse it for watchlist display instead of per-item calls.
  Rationale: This satisfies the Wiki fair-use guidance against looping the `id` parameter and reduces API load.
  Date/Author: 2026-02-03 / Codex

- Decision: Compute the trend from the 1-hour time-series using the last two points and classify it as up/down/flat with a 0.1% flat threshold.
  Rationale: It keeps the modal simple and uses the existing cached time-series service with minimal overhead.
  Date/Author: 2026-02-03 / Codex

## Outcomes & Retrospective

The UI now provides item metadata, live high/low prices, and a trend indicator without extra external API calls. The backend reuses the existing latest-price fetch and cached time-series. The system remains compliant with API fair-use guidance. No outstanding gaps are known at this stage.

## Context and Orientation

The backend is an ASP.NET Core API with services in `backend/Services` and controllers in `backend/Controllers`. The item catalog is loaded from the OSRS Wiki mapping endpoint and cached in memory. Price monitoring fetches the OSRS Wiki latest prices periodically. The frontend is a Vue 3 app under `frontend/src`, with cards for search and watchlist, and shared modal styling in `frontend/src/style.css`.

Key files:

- `backend/Services/ItemCatalogService.cs` loads mapping data from the OSRS Wiki.
- `backend/Services/PriceMonitorService.cs` fetches latest prices.
- `backend/Services/OsrsTimeSeriesService.cs` fetches and caches time-series data.
- `backend/Controllers/ItemsController.cs` exposes item search and details.
- `backend/Controllers/WatchlistController.cs` exposes watchlist and market data.
- `frontend/src/components/BrowseItemsCard.vue` renders search results.
- `frontend/src/components/WatchlistCard.vue` renders watchlist rows.
- `frontend/src/components/ItemDetailsModal.vue` renders the item details modal.
- `frontend/src/pages/HomePage.vue` coordinates UI state.

The OSRS Wiki fair-use guidance and endpoint behavior for `/latest`, `/mapping`, and `/timeseries` are described on the OSRS Wiki real-time prices page. The page states that looping the `id` parameter is discouraged and that the mapping endpoint includes buy limit, alch values, examine text, and members status.

## Plan of Work

Update the item catalog model to include mapping metadata fields. Add backend DTOs for item details and latest market data. Extend the price monitor to store latest high/low prices with timestamps in memory for reuse. Add controller endpoints to return item details (including trend) and cached watchlist market prices. On the frontend, add a modal component for item details, add “Details” buttons in search results and watchlist rows to open the modal, and display live high/low prices on watchlist rows by calling the new market endpoint. Ensure API usage is limited to the existing periodic `/latest` call and cached `/timeseries` calls, following the OSRS Wiki fair-use guidance.

## Concrete Steps

1. Update backend models and services:
   - Add metadata fields to `ItemCatalogService.ItemCatalogEntry`.
   - Add `ItemDetailsDto` and `LatestPriceDto` responses.
   - Add latest price cache methods in `InMemoryDataStore`.
   - Update `PriceMonitorService` to store high/low prices and timestamps.

2. Add backend endpoints:
   - `GET /api/items/{id}/details` returns metadata + trend + latest data.
   - `GET /api/watchlist/market` returns latest high/low data for watchlist items.

3. Update frontend:
   - Add `ItemDetailsModal.vue` component.
   - Add “Details” buttons to search and watchlist cards.
   - Fetch item details when opening the modal.
   - Show live high/low prices in watchlist rows.

4. Validate:
   - Start backend and frontend.
   - Verify watchlist shows high/low.
   - Open details modal for search and watchlist items and confirm metadata + trend.

## Validation and Acceptance

Start the backend (`dotnet run` in `backend`) and frontend (`npm run dev` in `frontend`). Confirm that:

- Watchlist rows show “High” and “Low” prices once the price monitor has fetched data.
- Clicking “Details” on a search item or watchlist item opens a modal with name, buy limit, alch values, members status, examine text, and a trend indicator.
- The modal continues to open without extra external API calls beyond the existing `/latest` poll and cached time-series fetches.

## Idempotence and Recovery

All steps are additive and safe to repeat. If the backend does not yet have latest prices cached, the watchlist will display “Market data pending...” until the next scheduled fetch.

## Artifacts and Notes

No additional artifacts required. If needed, observe logs in the backend to confirm `PriceMonitorService` fetches are reused and no new `latest?id=...` calls are made.

## Interfaces and Dependencies

Backend:

- `GET /api/items/{id}/details` returns `ItemDetailsDto`.
- `GET /api/watchlist/market` returns `LatestPriceDto[]`.
- `InMemoryDataStore.UpdateLatestPrice(int, LatestPriceSnapshot)` stores cached latest prices.
- `OsrsTimeSeriesService.GetTimeSeriesAsync(int, TimeSeriesTimestep.OneHour)` is used for trends.

Frontend:

- `getItemDetails(itemId)` calls `/api/items/{id}/details`.
- `getWatchlistMarket()` calls `/api/watchlist/market`.
- `ItemDetailsModal.vue` renders the modal UI.

Change note: This plan was created after implementation to document the completed work and the fair-use rationale.
```
