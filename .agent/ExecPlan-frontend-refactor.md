# Refactor Vue Frontend Into Composables And Components

This ExecPlan is a living document. The sections `Progress`, `Surprises & Discoveries`, `Decision Log`, and `Outcomes & Retrospective` must be kept up to date as work proceeds.

Follow `/.agent/PLANS.md` from the repository root. This document must be maintained in accordance with it.

## Purpose / Big Picture

After this change, the Vue frontend is organized around composables and focused components instead of a single monolithic page file. A new contributor can open the home page and immediately see the layout and data flow, and users can still browse items, manage the watchlist, view alerts, notifications, and positions, and edit backend settings in the cogwheel menu with the OSRS-themed styling intact. You can see it working by running the frontend and verifying that all cards render, the watchlist bulk add works, and alerts/notifications/positions update as before.

## Progress

- [x] (2026-02-02 23:35Z) Created composables in `frontend/src/composables` for config, local settings, search, watchlist, alerts, notifications, positions, and catalog status.
- [x] (2026-02-02 23:35Z) Created card components in `frontend/src/components` plus `SettingsPanel.vue`.
- [x] (2026-02-02 23:58Z) Wire `frontend/src/pages/HomePage.vue` to the new composables and components, removing the old inlined logic and markup.
- [x] (2026-02-02 23:58Z) Move shared styling from the HomePage scoped block into `frontend/src/style.css` so card components render correctly.
- [ ] (2026-02-02 23:35Z) Validate the UI locally and confirm key flows still function (search, watchlist, alerts, notifications, positions, settings save).

## Surprises & Discoveries

- Observation: The existing `HomePage.vue` still contains the full pre-refactor template and scoped styles, which do not apply to the new components.
  Evidence: `frontend/src/pages/HomePage.vue` still renders sections for Browse Items, Watchlist, Notifications, Active Alerts, and Positions directly.

## Decision Log

- Decision: Keep the refactor in JavaScript (no conversion to TypeScript).
  Rationale: The current codebase uses JavaScript, and the user requested a refactor only, not a language migration.
  Date/Author: 2026-02-02 / Codex

- Decision: Move card styling to `frontend/src/style.css` so shared classes remain effective after splitting into components.
  Rationale: Scoped styles in the page would not apply to standalone components, causing loss of styling.
  Date/Author: 2026-02-02 / Codex

## Outcomes & Retrospective

Pending. This section will be updated after the refactor is complete with what was achieved, what remains, and lessons learned.

## Context and Orientation

The Vue frontend lives under `frontend/src`. The main page is `frontend/src/pages/HomePage.vue`, which currently contains a large template and all fetching logic. New composables under `frontend/src/composables` encapsulate API interactions and computed state for search, watchlist, alerts, notifications, positions, config, and local UI settings. New components under `frontend/src/components` represent the cards in the UI: `BrowseItemsCard.vue`, `WatchlistCard.vue`, `ActiveAlertsCard.vue`, `NotificationsCard.vue`, `PositionsCard.vue`, and the `SettingsPanel.vue` for the cogwheel menu. Shared theme styling is centralized in `frontend/src/style.css`.

## Plan of Work

First, update `frontend/src/pages/HomePage.vue` to import and use the new composables and card components. Replace the large template blocks with component tags and pass the necessary props and models (for example, `v-model:searchQuery` for search input, and `v-model:watchlistDisplayLimit` for the watchlist dropdown). Ensure the grid ordering places Active Alerts and Notifications in the first two-column section and Watchlist with Positions in the second, matching the requested layout swap. In the script, remove direct API calls and use composable functions for loading and mutations, while keeping the periodic refresh logic and error banner.

Second, move the styling rules from the HomePage scoped style block into `frontend/src/style.css`. Keep OSRS palette variables already defined there, and move only the layout and component class styles so that the new components continue to render with the same visual hierarchy. Remove the scoped style block from `HomePage.vue` once its contents are migrated.

Third, validate that the interface still behaves correctly by running the frontend and confirming key flows: search returns results, adding/removing from watchlist works, bulk add shows matched names, alerts can be acknowledged or removed, notifications can be acknowledged (single and all), positions can be removed, and settings save uses the cogwheel panel. Update this plan’s Progress and Outcomes sections with evidence from the validation.

## Concrete Steps

Work in `C:\dev\Freetime\OSRS-GE-Monitor`.

1. Edit `frontend/src/pages/HomePage.vue`:
   - Replace the template sections with `<BrowseItemsCard>`, `<ActiveAlertsCard>`, `<NotificationsCard>`, `<WatchlistCard>`, `<PositionsCard>`, and `<SettingsPanel>`.
   - Import the new components and composables and wire up props, emits, and models.
   - Keep the refresh timer, but call `load*` functions from composables and update `lastUpdated`.
   - Remove the old API imports and local state no longer needed.

2. Move styles:
   - Copy the `.app`, `.hero`, `.card`, `.list`, `.notification`, `.pagination`, `.pill`, and related CSS rules from the `HomePage.vue` style block into `frontend/src/style.css`.
   - Remove the `<style scoped>` block from `HomePage.vue`.

3. Run the frontend to validate:
   - From `frontend`, run `npm install` (if not already done) and `npm run dev`.
   - Open the local dev URL and confirm each card functions and is styled.

Expected evidence:

  - The page shows a hero header, a Browse Items card, a first row with Active Alerts and Notifications, and a second row with Watchlist and Positions.
  - Buttons and inputs are styled with the OSRS theme.
  - The watchlist bulk add displays matched names and status counts.

## Validation and Acceptance

Start the frontend from `frontend` with `npm run dev` and verify:
  - Search results render and pagination updates when page size changes.
  - Watchlist shows search filter input, supports bulk add, and the list updates after add/remove.
  - Active alerts allow acknowledging with a quantity and removing alerts.
  - Notifications allow acknowledging one and acknowledging all; the counts update.
  - Positions show status pills and allow removal.
  - Settings cogwheel opens the settings panel and saving updates backend config.
Acceptance is satisfied when all cards render in the correct order and the interactions above work without console errors.

## Idempotence and Recovery

The edits are safe to apply repeatedly. If a component is miswired, revert only the `HomePage.vue` changes and re-apply the wiring. If styling looks incorrect, recheck `frontend/src/style.css` for missing rules. No data migrations or destructive commands are required.

## Artifacts and Notes

Key files:
  - `frontend/src/pages/HomePage.vue`
  - `frontend/src/components/SettingsPanel.vue`
  - `frontend/src/components/BrowseItemsCard.vue`
  - `frontend/src/components/WatchlistCard.vue`
  - `frontend/src/components/ActiveAlertsCard.vue`
  - `frontend/src/components/NotificationsCard.vue`
  - `frontend/src/components/PositionsCard.vue`
  - `frontend/src/composables/useConfig.js`
  - `frontend/src/composables/useLocalSettings.js`
  - `frontend/src/composables/useSearch.js`
  - `frontend/src/composables/useWatchlist.js`
  - `frontend/src/composables/useAlerts.js`
  - `frontend/src/composables/useNotifications.js`
  - `frontend/src/composables/usePositions.js`
  - `frontend/src/composables/useCatalogStatus.js`
  - `frontend/src/style.css`

## Interfaces and Dependencies

The `frontend/src/api` module remains the interface for backend calls. The composables expose functions used by `HomePage.vue`:
  - `useConfig()` provides `loadConfig` and `saveConfig`, plus refs for the standard deviation thresholds, rolling window size, fetch interval, and user agent.
  - `useLocalSettings()` provides local-only dropdown controls (`searchPageSize`, `watchlistDisplayLimit`, `notificationDisplayLimit`, `positionDisplayLimit`) and their option arrays.
  - `useSearch(searchPageSize)` provides the search state and pagination.
  - `useWatchlist(watchlistDisplayLimit)` provides watchlist state, filter query, and bulk add.
  - `useAlerts()` provides alerts state and acknowledge/remove actions.
  - `useNotifications(notificationDisplayLimit)` provides notifications state and acknowledge/clear actions.
  - `usePositions(positionDisplayLimit)` provides positions state and remove actions.
  - `useCatalogStatus()` provides catalog status labels for the Browse Items card.

At the end of the refactor, `HomePage.vue` should only orchestrate these composables and render the components with props and emits.

Change note: 2026-02-02 / Codex. Created the initial ExecPlan based on the current state and outlined the remaining refactor work.

Change note: 2026-02-02 / Codex. Marked wiring and styling steps complete after refactoring `HomePage.vue` to use composables/components and moving shared styles into `style.css`.
