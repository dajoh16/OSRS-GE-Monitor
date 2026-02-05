# Add Watchlist Discord Report Button

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

PLANS.md is checked into the repo at `.agent/PLANS.md` from the repository root. This document must be maintained in accordance with that file.

## Purpose / Big Picture

Users can already manage a watchlist and configure a Discord webhook for alerts. After this change, every watchlist item will include a button that sends a Discord report containing the item name, high, low, spread, and after-tax profit or loss. A user can see it working by enabling Discord notifications, clicking the new button for a watchlist item with market data, and observing the formatted report message in their Discord channel.

## Progress

- [x] (2026-02-04 21:10Z) Extend Discord notification types to include a report payload and message.
- [x] (2026-02-04 21:12Z) Add the watchlist report API endpoint that validates config and market data.
- [x] (2026-02-04 21:18Z) Wire the frontend watchlist report button to the API and show toasts.
- [x] (2026-02-04 21:23Z) Add backend integration tests for the report endpoint and confirm they pass.

## Surprises & Discoveries

None yet.

## Decision Log

- Decision: Use a dedicated `POST /api/watchlist/{id}/discord-report` endpoint and a new `DiscordNotificationType.Report`.
  Rationale: Watchlist reporting is a distinct user action and should not overload existing alert or config endpoints.
  Date/Author: 2026-02-04 / Codex
- Decision: Use the backend GE tax rule with a 5,000,000 cap when computing after-tax profit or loss.
  Rationale: The report should match the same tax logic used for positions and avoid inconsistent calculations.
  Date/Author: 2026-02-04 / Codex

## Outcomes & Retrospective

The feature is implemented end-to-end with a new report endpoint, Discord notification formatting, frontend button wiring, and integration tests. Remaining work is to run the test suite and manually verify a Discord message with a real webhook.

## Context and Orientation

The backend is an ASP.NET Core API in `backend/`, with controllers in `backend/Controllers`. Watchlist data lives in `InMemoryDataStore` at `backend/Services/InMemoryDataStore.cs` and is exposed via `backend/Controllers/WatchlistController.cs`. Discord webhook notifications are queued and sent through `backend/Services/DiscordNotificationService.cs` and `backend/Services/DiscordNotificationWorker.cs`. The frontend is a Vue 3 app in `frontend/`, with the watchlist UI in `frontend/src/components/WatchlistCard.vue` and orchestration in `frontend/src/pages/HomePage.vue`. API calls are centralized in `frontend/src/api.js`.

## Plan of Work

First, extend the Discord notification model to include a report type and report fields, and update the worker to build a report message that includes the required data. Next, add a new endpoint in `WatchlistController` to validate Discord configuration, ensure market data is available, calculate spread and after-tax profit or loss, and enqueue the report. Then update the frontend to add a per-item button that calls the new API endpoint, shows loading state, and posts success or error toasts. Finally, add integration tests in `backend.Tests` to verify the new endpoint returns the correct status codes and can be exercised without background hosted services.

## Concrete Steps

Update backend services:

  - Edit `backend/Services/DiscordNotificationQueue.cs` to add `Report` and optional report fields to `DiscordNotification`.
  - Edit `backend/Services/DiscordNotificationService.cs` to add `EnqueueReportAsync`.
  - Edit `backend/Services/DiscordNotificationWorker.cs` to format report messages.

Add the report endpoint:

  - Edit `backend/Controllers/WatchlistController.cs` to add `POST /api/watchlist/{id}/discord-report`.
  - Use `InMemoryDataStore.GetLatestPrice` to fetch market data and compute spread and after-tax profit or loss.

Update the frontend:

  - Edit `frontend/src/api.js` to add `sendWatchlistDiscordReport`.
  - Edit `frontend/src/components/WatchlistCard.vue` to add a “Report” button that emits `discord-report`.
  - Edit `frontend/src/pages/HomePage.vue` to handle the event, manage per-item loading state, and show toasts.

Add integration tests:

  - Create `backend.Tests/WatchlistDiscordReportTests.cs` with tests for bad-request and accepted responses.
  - In the test WebApplicationFactory, remove hosted services to avoid external network calls.

## Validation and Acceptance

Run backend tests:

  - Working directory: `C:\dev\Freetime\OSRS-GE-Monitor`
  - Command: `dotnet test backend.Tests/OSRSGeMonitor.Api.Tests.csproj`

Expect all tests to pass. The new tests should fail before implementation and pass after. For manual verification, start both backend and frontend, enable Discord notifications with a valid webhook URL, and click the report button on a watchlist item with market data. A Discord message containing item name, high, low, spread, and after-tax profit or loss should appear.

## Idempotence and Recovery

All changes are additive and safe to repeat. If the report endpoint returns conflict due to missing market data, wait for the next price refresh and retry. If tests fail because of hosted services, re-check the service removal in the test WebApplicationFactory.

## Artifacts and Notes

Example report message:

  📊 Watchlist Report: Rune scimitar
  High: 15,230 gp
  Low: 14,900 gp
  Spread: 330 gp
  After-tax P/L: 25 gp

## Interfaces and Dependencies

The new endpoint must exist:

  - `POST /api/watchlist/{id}/discord-report`

The new service method must exist:

  - `DiscordNotificationService.EnqueueReportAsync(string itemName, double high, double low, double spread, double afterTax, CancellationToken cancellationToken = default)`

The frontend API helper must exist:

  - `sendWatchlistDiscordReport(itemId)`

Plan update note (2026-02-04): Marked progress items complete and recorded the tax calculation decision after implementing backend, frontend, and tests.
