# Send Alert Notifications To Discord Via Webhook

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

Follow `/.agent/PLANS.md` from the repository root. This document must be maintained in accordance with it.

## Purpose / Big Picture

After this change, the backend can push price-drop and recovery notifications to a specific Discord server channel using a Discord webhook URL. This makes alerts visible in Discord without keeping the UI open. You will be able to set the webhook URL in the frontend settings, restart the app without losing that setting, trigger a test alert, and see a formatted Discord message appear in the configured channel.

## Progress

- [ ] (2026-02-02 00:00Z) Decide config inputs (webhook URL + enable flag) and validate assumptions with the user.
- [ ] (2026-02-02 00:00Z) Implement Discord webhook sender in backend services.
- [ ] (2026-02-02 00:00Z) Wire alert/recovery notifications to send Discord messages.
- [ ] (2026-02-02 00:00Z) Add config endpoints + UI settings for Discord.
- [ ] (2026-02-02 00:00Z) Persist Discord config so it survives restarts.
- [ ] (2026-02-02 00:00Z) Validate with a test alert and document expected output.

## Surprises & Discoveries

Pending.

## Decision Log

- Decision: Use Discord webhooks (single URL) instead of bot tokens.
  Rationale: Webhooks are simpler to configure, do not require OAuth or bot permissions, and are sufficient for one-channel alert posting.
  Date/Author: 2026-02-02 / Codex

## Outcomes & Retrospective

Pending.

## Context and Orientation

The backend lives in `backend/`. Alerts and notifications are created in `backend/Services/InMemoryDataStore.cs` and the monitoring loop is in `backend/Services/PriceMonitorService.cs`. Configuration is stored in `backend/Models/GlobalConfig.cs` and updated via `backend/Models/Requests/UpdateConfigRequest.cs` and `backend/Services/InMemoryDataStore.cs`. The API controller for config is `backend/Controllers/ConfigController.cs`. The frontend settings UI is in `frontend/src/components/SettingsPanel.vue` and the config composable is in `frontend/src/composables/useConfig.js`. This change will add Discord webhook config to GlobalConfig, persist it to disk, and implement a new service that posts to the Discord webhook URL.

## Plan of Work

First, extend config models to include Discord webhook settings: `DiscordWebhookUrl` and `DiscordNotificationsEnabled` (bool). Update the config update request and the in-memory config update logic with validation: trim strings, allow empty URL when disabled, and require a valid-looking HTTPS Discord webhook URL when enabled.

Second, implement a new backend service `DiscordNotificationService` (e.g., in `backend/Services/DiscordNotificationService.cs`). It should take `IHttpClientFactory`, `InMemoryDataStore`, and `ILogger`. Provide a single method `SendAlertAsync(Alert alert, NotificationType type, CancellationToken ct)` that posts a message to Discord using the webhook URL with a JSON body `{ "content": "..." }` and optionally `embeds`. Keep it minimal to avoid rate limits. Add guard clauses: if disabled or missing webhook URL, do nothing and log once at startup or per call with debug-level.

Third, wire the Discord sender into alert creation and recovery. The simplest place is in `InMemoryDataStore.AddAlert` (for drop) and `TryRecoverAlert` (for recovery). Call the Discord service from those methods or publish an event from the data store and have a background worker send messages. Because the data store is currently synchronous, prefer injecting the Discord service and using a queue + worker to avoid blocking the price monitor loop. Implement a small `DiscordNotificationQueue` service and `DiscordNotificationWorker` hosted service to consume the queue and call the webhook URL.

Fourth, update the config API responses to include Discord fields and update the frontend settings UI to allow configuring the webhook URL and an enable toggle.

Fifth, persist the config so it survives restarts, using a small SQLite-backed config store (similar to watchlist/time-series cache) or a JSON file in the backend data folder. Load persisted config at startup and save on update.

Finally, validate by setting config, triggering a manual alert (by lowering thresholds temporarily or using a test endpoint), and verifying the Discord message arrives. Restart the backend and confirm the webhook config is still present.

## Concrete Steps

Work in `C:\dev\Freetime\OSRS-GE-Monitor`.

1. Add config fields:
   - `backend/Models/GlobalConfig.cs`: add `DiscordWebhookUrl`, `DiscordNotificationsEnabled`.
   - `backend/Models/Requests/UpdateConfigRequest.cs`: add nullable equivalents.
   - `backend/Services/InMemoryDataStore.cs`: update config update logic.

2. Add Discord queue + worker:
   - Create `backend/Services/DiscordNotificationQueue.cs` containing a `Channel<DiscordNotification>` with `Enqueue` and `DequeueAsync`.
   - Create `backend/Services/DiscordNotificationWorker.cs` as `BackgroundService` that reads from the queue and posts via the Discord webhook URL.
   - Create `backend/Services/DiscordNotificationService.cs` that formats messages and enqueues them.
   - Register these services in `backend/Program.cs`.

3. Wire alert/recovery events:
   - In `InMemoryDataStore.AddAlert` call `DiscordNotificationService.EnqueueDrop(alert)`.
   - In `InMemoryDataStore.TryRecoverAlert` call `DiscordNotificationService.EnqueueRecovery(alert)`.
   - Ensure failures are logged but do not break alerts.

4. UI:
   - Update `frontend/src/composables/useConfig.js` to surface Discord webhook settings.
   - Update `frontend/src/components/SettingsPanel.vue` to add inputs and a toggle.
   - Update `frontend/src/pages/HomePage.vue` to bind the new models.

5. Persist config:
   - Add `backend/Services/SqliteConfigStore.cs` (or a JSON file store) to save `GlobalConfig` values.
   - On startup, load persisted config into `InMemoryDataStore.Config`.
   - On every config update, write the new config to the store.

## Validation and Acceptance

1. Set Discord config via UI or API:
   - `PUT /api/config` with `discordNotificationsEnabled: true` and `discordWebhookUrl`.
2. Trigger an alert:
   - Temporarily lower thresholds or use a test endpoint if added.
3. Acceptance:
   - A Discord message appears in the specified channel for both drop and recovery notifications.
   - The backend does not crash if Discord is unreachable, and logs failures.
   - Restarting the backend retains the Discord webhook configuration.

## Idempotence and Recovery

These changes are safe to rerun. If Discord settings are misconfigured, the worker should log errors and continue. You can disable Discord notifications by setting `discordNotificationsEnabled` to false, which should stop all outgoing messages without code changes.

## Artifacts and Notes

Key files to change/add:
  - `backend/Models/GlobalConfig.cs`
  - `backend/Models/Requests/UpdateConfigRequest.cs`
  - `backend/Services/InMemoryDataStore.cs`
  - `backend/Services/DiscordNotificationQueue.cs` (new)
  - `backend/Services/DiscordNotificationWorker.cs` (new)
  - `backend/Services/DiscordNotificationService.cs` (new)
  - `backend/Services/SqliteConfigStore.cs` (new) or JSON store
  - `backend/Program.cs`
  - `frontend/src/composables/useConfig.js`
  - `frontend/src/components/SettingsPanel.vue`
  - `frontend/src/pages/HomePage.vue`

## Interfaces and Dependencies

Use the Discord webhook endpoint with a simple JSON payload:

    { "content": "Message text" }

Optionally include an embed payload:

    {
      "content": "",
      "embeds": [
        { "title": "Price Drop", "description": "...", "color": 15105570 }
      ]
    }

The `DiscordNotificationService` should expose:

    public Task EnqueueDropAsync(Alert alert, CancellationToken ct);
    public Task EnqueueRecoveryAsync(Alert alert, CancellationToken ct);

The worker reads from the queue and posts messages. All failures should be caught and logged with the alert ID and item name.

Change note: 2026-02-02 / Codex. Rewrote plan to use webhooks and added config persistence and UI requirements.
