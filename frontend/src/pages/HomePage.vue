<template>
  <div class="app">
    <header class="hero">
      <div>
        <h1>OSRS GE Monitor</h1>
        <p>Track price drops, manage your watchlist, and log buys when alerts fire.</p>
      </div>
      <div class="hero-actions">
        <div class="status" v-if="lastUpdated">
          Last updated: {{ lastUpdated }}
        </div>
        <button class="icon-button" type="button" @click="toggleSettings">
          &#9881;
        </button>
      </div>
    </header>

    <ToastBar :toasts="toasts" @dismiss="removeToast" />

    <SettingsPanel
      :open="settingsOpen"
      :config-loading="configLoading"
      :config-error="configError"
      :is-user-agent-valid="isUserAgentValid"
      v-model:threshold="threshold"
      v-model:profitTargetPercent="profitTargetPercent"
      v-model:discordNotificationsEnabled="discordNotificationsEnabled"
      v-model:discordWebhookUrl="discordWebhookUrl"
      v-model:recoveryThreshold="recoveryThreshold"
      v-model:rollingWindowSize="rollingWindowSize"
      v-model:fetchInterval="fetchInterval"
      v-model:userAgent="userAgent"
      v-model:alertGraceMinutes="alertGraceMinutes"
      @close="toggleSettings"
      @save="handleSaveConfig"
      @test-discord="handleSendDiscordTest"
    />

    <div v-if="refreshError" class="error-banner">
      {{ refreshError }}
    </div>

    <BrowseItemsCard
      v-model:searchQuery="searchQuery"
      v-model:searchPage="searchPage"
      v-model:searchPageSize="searchPageSize"
      :catalog-status-label="catalogStatusLabel"
      :catalog-status-class="catalogStatusClass"
      :search-error="searchError"
      :search-loading="searchLoading"
      :paged-search-results="pagedSearchResults"
      :search-results-length="searchResults.length"
      :search-total-pages="searchTotalPages"
      :search-page-options="searchPageOptions"
      @search="runSearch"
      @add="handleAddToWatchlist"
      @details="handleOpenDetails"
    />

    <section class="grid two-col">
      <ActiveAlertsCard
        :alerts="alerts"
        :alert-quantities="alertQuantities"
        @acknowledge="handleAcknowledge"
        @remove="handleRemoveAlert"
      />
      <NotificationsCard
        v-model:notificationDisplayLimit="notificationDisplayLimit"
        :notifications="notifications"
        :displayed-notifications="displayedNotifications"
        :notification-limit-options="notificationLimitOptions"
        :suppressed-open="suppressedOpen"
        :suppressed-items="suppressedItems"
        :suppressed-loading="suppressedLoading"
        @acknowledge="handleAcknowledgeNotification"
        @acknowledge-all="handleAcknowledgeAllNotifications"
        @open-suppressed="openSuppressed"
        @close-suppressed="closeSuppressed"
        @unsuppress="handleUnsuppress"
      />
    </section>

    <section class="grid two-col">
      <WatchlistCard
        v-model:watchlistQuery="watchlistQuery"
        v-model:watchlistDisplayLimit="watchlistDisplayLimit"
        v-model:bulkNames="bulkNames"
        v-model:watchlistSortField="watchlistSortField"
        v-model:watchlistSortDirection="watchlistSortDirection"
        :displayed-watchlist="displayedWatchlist"
        :filtered-count="filteredWatchlist.length"
        :bulk-result="bulkResult"
        :bulk-error="bulkError"
        :bulk-loading="bulkLoading"
        :watchlist-limit-options="watchlistLimitOptions"
        @remove="handleRemoveFromWatchlist"
        @bulk-add="addBulkToWatchlist"
        @details="handleOpenDetails"
      />
      <PositionsCard
        v-model:positionDisplayLimit="positionDisplayLimit"
        v-model:sellPrices="sellPrices"
        v-model:buyPrices="buyPrices"
        v-model:manualName="manualName"
        :displayed-positions="displayedPositions"
        :positions-count="openPositions.length"
        :position-limit-options="positionLimitOptions"
        :manual-match="manualMatch"
        :manual-match-loading="manualMatchLoading"
        @remove="handleRemovePosition"
        @sell="handleSellPosition"
        @market="handleMarketPrice"
        @update-buy="handleUpdateBuyPrice"
        @manual-add="handleManualPosition"
      />
    </section>

    <ItemDetailsModal
      :open="detailsOpen"
      :loading="detailsLoading"
      :error="detailsError"
      :item="detailsItem"
      @close="closeDetails"
    />
  </div>
</template>

<script setup>
import { onMounted, onUnmounted, ref, watch } from 'vue';
import SettingsPanel from '../components/SettingsPanel.vue';
import BrowseItemsCard from '../components/BrowseItemsCard.vue';
import ActiveAlertsCard from '../components/ActiveAlertsCard.vue';
import NotificationsCard from '../components/NotificationsCard.vue';
import WatchlistCard from '../components/WatchlistCard.vue';
import PositionsCard from '../components/PositionsCard.vue';
import ToastBar from '../components/ToastBar.vue';
import ItemDetailsModal from '../components/ItemDetailsModal.vue';
import { useConfig } from '../composables/useConfig';
import { useLocalSettings } from '../composables/useLocalSettings';
import { useCatalogStatus } from '../composables/useCatalogStatus';
import { useSearch } from '../composables/useSearch';
import { useWatchlist } from '../composables/useWatchlist';
import { useAlerts } from '../composables/useAlerts';
import { useNotifications } from '../composables/useNotifications';
import { usePositions } from '../composables/usePositions';
import { useToasts } from '../composables/useToasts';
import {
  sendDiscordTest,
  sellPosition,
  updateBuyPrice,
  getLatestPrice,
  addManualPosition,
  searchItems,
  getSuppressedNotifications,
  removeSuppressedNotification,
  getItemDetails
} from '../api';

const settingsOpen = ref(false);
const refreshError = ref('');
const lastUpdated = ref('');
let refreshTimer = null;
const sellPrices = ref({});
const buyPrices = ref({});
const manualName = ref('');
const manualMatch = ref('');
const manualMatchLoading = ref(false);
let manualMatchToken = 0;
let manualMatchTimer = null;
const suppressedOpen = ref(false);
const suppressedLoading = ref(false);
const suppressedItems = ref([]);
const detailsOpen = ref(false);
const detailsLoading = ref(false);
const detailsError = ref('');
const detailsItem = ref(null);

const { toasts, pushToast, removeToast } = useToasts();

const {
  configLoading,
  configError,
  threshold,
  profitTargetPercent,
  discordNotificationsEnabled,
  discordWebhookUrl,
  recoveryThreshold,
  rollingWindowSize,
  fetchInterval,
  userAgent,
  isUserAgentValid,
  loadConfig,
  saveConfig,
  alertGraceMinutes
} = useConfig();

const {
  load: loadLocalSettings,
  searchPageSize,
  watchlistDisplayLimit,
  notificationDisplayLimit,
  positionDisplayLimit,
  searchPageOptions,
  watchlistLimitOptions,
  notificationLimitOptions,
  positionLimitOptions
} = useLocalSettings();

const { catalogStatusLabel, catalogStatusClass, loadCatalogStatus } = useCatalogStatus();

const {
  searchQuery,
  searchResults,
  searchLoading,
  searchError,
  searchPage,
  pagedSearchResults,
  searchTotalPages,
  runSearch
} = useSearch(searchPageSize);

const {
  watchlist,
  watchlistQuery,
  filteredWatchlist,
  displayedWatchlist,
  bulkNames,
  bulkResult,
  bulkError,
  bulkLoading,
  watchlistSortField,
  watchlistSortDirection,
  loadWatchlist,
  addToWatchlist,
  removeFromWatchlist,
  addBulkToWatchlist
} = useWatchlist(watchlistDisplayLimit);

const { alerts, alertQuantities, loadAlerts, acknowledge, removeAlertItem } = useAlerts();

const {
  notifications,
  displayedNotifications,
  loadNotifications,
  clearAllNotifications,
  removeNotificationItem
} = useNotifications(notificationDisplayLimit);

const { openPositions, displayedPositions, loadPositions, removePositionItem } = usePositions(
  positionDisplayLimit
);

const updateTimestamp = () => {
  lastUpdated.value = new Date().toLocaleTimeString();
};

const reportRefreshError = (error, fallbackMessage) => {
  const message = error?.message || fallbackMessage;
  refreshError.value = message;
};

const safeCall = async (fn, fallbackMessage) => {
  try {
    await fn();
  } catch (error) {
    reportRefreshError(error, fallbackMessage);
  }
};

const refreshAll = async () => {
  refreshError.value = '';
  await Promise.allSettled([
    safeCall(loadWatchlist, 'Failed to load watchlist.'),
    safeCall(loadAlerts, 'Failed to load alerts.'),
    safeCall(loadPositions, 'Failed to load positions.'),
    safeCall(loadNotifications, 'Failed to load notifications.'),
    safeCall(loadCatalogStatus, 'Failed to load catalog status.')
  ]);
  updateTimestamp();
};

const startRefreshTimer = () => {
  const seconds = Math.max(5, Number(fetchInterval.value) || 30);
  if (refreshTimer) {
    clearInterval(refreshTimer);
  }
  refreshTimer = setInterval(refreshAll, seconds * 1000);
};

const resolveItemName = (itemId) => {
  const fromSearch = searchResults.value.find((item) => item.id === itemId);
  if (fromSearch) {
    return fromSearch.name;
  }
  const fromWatchlist = watchlist.value.find((item) => item.id === itemId);
  return fromWatchlist?.name ?? `Item #${itemId}`;
};

const handleAddToWatchlist = async (itemId) => {
  try {
    await addToWatchlist(itemId);
    pushToast(`Added ${resolveItemName(itemId)} to watchlist.`, 'success');
  } catch (error) {
    reportRefreshError(error, 'Failed to add to watchlist.');
    pushToast('Could not add to watchlist.', 'error');
  }
};

const handleRemoveFromWatchlist = async (itemId) => {
  const name = resolveItemName(itemId);
  try {
    await removeFromWatchlist(itemId);
    pushToast(`Removed ${name} from watchlist.`, 'success');
  } catch (error) {
    reportRefreshError(error, 'Failed to remove from watchlist.');
    pushToast('Could not remove from watchlist.', 'error');
  }
};

const handleAcknowledge = async (alert) => {
  try {
    await acknowledge(alert);
    pushToast(`Bought ${alert.itemName}.`, 'success');
  } catch (error) {
    reportRefreshError(error, 'Failed to acknowledge alert.');
    pushToast('Could not acknowledge alert.', 'error');
    return;
  }
  await Promise.allSettled([
    safeCall(loadAlerts, 'Failed to load alerts.'),
    safeCall(loadPositions, 'Failed to load positions.')
  ]);
};

const handleRemoveAlert = async (alertId) => {
  try {
    await removeAlertItem(alertId);
  } catch (error) {
    reportRefreshError(error, 'Failed to remove alert.');
  }
  await safeCall(loadAlerts, 'Failed to load alerts.');
};

const handleRemovePosition = async (positionId) => {
  try {
    await removePositionItem(positionId);
  } catch (error) {
    reportRefreshError(error, 'Failed to remove position.');
  }
};

const handleSellPosition = async ({ id, price }) => {
  try {
    await sellPosition(id, price);
    await loadPositions();
    pushToast('Position sold and P&L recorded.', 'success');
  } catch (error) {
    reportRefreshError(error, 'Failed to sell position.');
    pushToast('Failed to sell position.', 'error');
  }
};

const handleMarketPrice = async (id) => {
  try {
    const latest = await getLatestPrice(id);
    if (latest?.price) {
      sellPrices.value[id] = Math.round(latest.price);
    }
  } catch (error) {
    reportRefreshError(error, 'Failed to load latest price.');
    pushToast('Failed to load latest price.', 'error');
  }
};

const handleUpdateBuyPrice = async ({ id, price }) => {
  try {
    await updateBuyPrice(id, price);
    await loadPositions();
    pushToast('Buy price updated.', 'success');
  } catch (error) {
    reportRefreshError(error, 'Failed to update buy price.');
    pushToast('Failed to update buy price.', 'error');
  }
};

const handleManualMatch = (name) => {
  if (manualMatchTimer) {
    clearTimeout(manualMatchTimer);
    manualMatchTimer = null;
  }

  const trimmed = name.trim();
  if (!trimmed) {
    manualMatch.value = '';
    manualMatchLoading.value = false;
    return;
  }

  manualMatchLoading.value = true;
  const token = (manualMatchToken += 1);
  manualMatchTimer = setTimeout(async () => {
    try {
      const results = await searchItems(trimmed);
      if (token !== manualMatchToken) {
        return;
      }
      const query = trimmed.toLowerCase();
      const exact = results?.find((item) => item.name.toLowerCase() === query);
      const starts = results?.find((item) => item.name.toLowerCase().startsWith(query));
      manualMatch.value = exact?.name ?? starts?.name ?? results?.[0]?.name ?? '';
    } catch (error) {
      if (token !== manualMatchToken) {
        return;
      }
      manualMatch.value = '';
    } finally {
      if (token === manualMatchToken) {
        manualMatchLoading.value = false;
      }
    }
  }, 350);
};

const handleManualPosition = async ({ name, quantity, buyPrice }) => {
  try {
    const position = await addManualPosition({
      itemName: name,
      quantity,
      buyPrice
    });
    await loadPositions();
    pushToast(`Manual position added for ${position.itemName}.`, 'success');
  } catch (error) {
    reportRefreshError(error, 'Failed to add manual position.');
    pushToast('Failed to add manual position.', 'error');
  }
};

const handleOpenDetails = async (itemId) => {
  detailsOpen.value = true;
  detailsLoading.value = true;
  detailsError.value = '';
  detailsItem.value = null;
  try {
    detailsItem.value = await getItemDetails(itemId);
  } catch (error) {
    detailsError.value = error.message || 'Failed to load item details.';
  } finally {
    detailsLoading.value = false;
  }
};

const closeDetails = () => {
  detailsOpen.value = false;
};

const handleAcknowledgeNotification = async (notificationId) => {
  try {
    await removeNotificationItem(notificationId);
  } catch (error) {
    reportRefreshError(error, 'Failed to acknowledge notification.');
  }
};

const handleAcknowledgeAllNotifications = async () => {
  try {
    await clearAllNotifications();
  } catch (error) {
    reportRefreshError(error, 'Failed to acknowledge notifications.');
  }
};

const loadSuppressed = async () => {
  suppressedLoading.value = true;
  try {
    suppressedItems.value = await getSuppressedNotifications();
  } catch (error) {
    reportRefreshError(error, 'Failed to load suppressed items.');
  } finally {
    suppressedLoading.value = false;
  }
};

const openSuppressed = async () => {
  suppressedOpen.value = true;
  await loadSuppressed();
};

const closeSuppressed = () => {
  suppressedOpen.value = false;
};

const handleUnsuppress = async (itemId) => {
  try {
    await removeSuppressedNotification(itemId);
    await loadSuppressed();
  } catch (error) {
    reportRefreshError(error, 'Failed to remove suppression.');
  }
};

const toggleSettings = () => {
  settingsOpen.value = !settingsOpen.value;
};

const handleSaveConfig = async () => {
  await saveConfig();
  updateTimestamp();
};

const handleSendDiscordTest = async () => {
  try {
    await sendDiscordTest();
    pushToast('Sent Discord test alert.', 'success');
  } catch (error) {
    reportRefreshError(error, 'Failed to send Discord test alert.');
    pushToast('Failed to send Discord test alert.', 'error');
  }
};

onMounted(async () => {
  await loadConfig();
  loadLocalSettings();
  await refreshAll();
  startRefreshTimer();
});

onUnmounted(() => {
  if (refreshTimer) {
    clearInterval(refreshTimer);
    refreshTimer = null;
  }
});

watch(fetchInterval, () => {
  startRefreshTimer();
});

watch([searchResults, searchPageSize], () => {
  if (searchPage.value > searchTotalPages.value) {
    searchPage.value = searchTotalPages.value;
  }
});

watch(manualName, (value) => {
  handleManualMatch(value ?? '');
});
</script>
