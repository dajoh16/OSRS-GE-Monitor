<template>
  <div class="app">
    <header class="hero">
      <div>
        <h1>OSRS GE Monitor</h1>
        <p>Track price drops, manage your watchlist, and log buys when alerts fire.</p>
      </div>
      <div class="status" v-if="lastUpdated">
        Last updated: {{ lastUpdated }}
      </div>
    </header>

    <section class="card">
      <div class="card__header">
        <h2>Global Configuration</h2>
        <span v-if="configError" class="error">{{ configError }}</span>
      </div>
      <div class="grid">
        <label class="field">
          Standard deviation threshold
          <input
            v-model.number="threshold"
            type="number"
            min="0"
            step="0.1"
          />
        </label>
        <label class="field">
          Recovery threshold
          <input
            v-model.number="recoveryThreshold"
            type="number"
            min="0"
            step="0.1"
          />
        </label>
        <label class="field">
          Rolling window size
          <input
            v-model.number="rollingWindowSize"
            type="number"
            min="5"
            step="1"
          />
        </label>
        <label class="field">
          Fetch interval (seconds)
          <input
            v-model.number="fetchInterval"
            type="number"
            min="10"
            step="5"
          />
        </label>
        <button class="primary" :disabled="configLoading" @click="saveConfig">
          {{ configLoading ? 'Saving...' : 'Save Config' }}
        </button>
      </div>
    </section>

    <section class="card">
      <div class="card__header">
        <h2>Browse Items</h2>
        <span v-if="searchError" class="error">{{ searchError }}</span>
      </div>
      <div class="grid">
        <label class="field search">
          Search items
          <input v-model.trim="searchQuery" placeholder="Rune scimitar, shark..." />
        </label>
        <button class="secondary" :disabled="searchLoading" @click="runSearch">
          {{ searchLoading ? 'Searching...' : 'Search' }}
        </button>
      </div>
      <div v-if="searchResults.length" class="list">
        <div v-for="item in searchResults" :key="item.id" class="list__item">
          <div>
            <strong>{{ item.name }}</strong>
            <span class="muted">#{{ item.id }}</span>
          </div>
          <button class="primary" @click="addToWatchlist(item.id)">
            Add to watchlist
          </button>
        </div>
      </div>
      <div v-else class="empty">
        Search for items to add them to your watchlist.
      </div>
    </section>

    <section class="grid two-col">
      <div class="card">
        <div class="card__header">
          <h2>Watchlist</h2>
          <span class="muted">{{ watchlist.length }} items</span>
        </div>
        <div v-if="watchlist.length" class="list">
          <div v-for="item in watchlist" :key="item.id" class="list__item">
            <div>
              <strong>{{ item.name }}</strong>
              <span class="muted">#{{ item.id }}</span>
            </div>
            <button class="ghost" @click="removeFromWatchlist(item.id)">
              Remove
            </button>
          </div>
        </div>
        <div v-else class="empty">
          No items in your watchlist yet.
        </div>
      </div>

      <div class="card">
        <div class="card__header">
          <h2>Notifications</h2>
          <span class="muted">{{ notifications.length }} alerts</span>
        </div>
        <div v-if="notifications.length" class="notification-panel">
          <div
            v-for="note in notifications"
            :key="note.id"
            class="notification"
            :class="note.type"
          >
            <div>
              <strong>{{ note.title }}</strong>
              <p>{{ note.message }}</p>
            </div>
            <span class="muted">{{ formatTimestamp(note.createdAt) }}</span>
          </div>
        </div>
        <div v-else class="empty">
          Price drop and recovery notifications will appear here.
        </div>
      </div>
    </section>

    <section class="grid two-col">
      <div class="card">
        <div class="card__header">
          <h2>Active Alerts</h2>
          <span class="muted">{{ alerts.length }} open</span>
        </div>
        <div v-if="alerts.length" class="list">
          <div v-for="alert in alerts" :key="alert.id" class="list__item">
            <div>
              <strong>{{ alert.itemName }}</strong>
              <p class="muted">
                Drop: {{ alert.dropPercent }}% · Current price {{ alert.currentPrice }}
              </p>
            </div>
            <div class="action-group">
              <input
                v-model.number="alertQuantities[alert.id]"
                type="number"
                min="1"
                placeholder="Qty"
              />
              <button class="primary" @click="acknowledge(alert)">
                Bought
              </button>
            </div>
          </div>
        </div>
        <div v-else class="empty">
          No active alerts. We'll keep watching the market.
        </div>
      </div>

      <div class="card">
        <div class="card__header">
          <h2>Positions</h2>
          <span class="muted">{{ positions.length }} tracked</span>
        </div>
        <div v-if="positions.length" class="list">
          <div v-for="position in positions" :key="position.id" class="list__item">
            <div>
              <strong>{{ position.itemName }}</strong>
              <p class="muted">
                Bought {{ position.quantity }} @ {{ position.buyPrice }} · Recovery {{
                  position.recoveryPrice
                }}
              </p>
            </div>
            <span class="pill" :class="position.status">
              {{ position.statusLabel }}
            </span>
          </div>
        </div>
        <div v-else class="empty">
          Acknowledge an alert to start tracking a bought item.
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue';
import {
  acknowledgeAlert,
  addWatchlistItem,
  getAlerts,
  getConfig,
  getNotifications,
  getPositions,
  getWatchlist,
  removeWatchlistItem,
  searchItems,
  updateConfig
} from './api';

const threshold = ref(0);
const recoveryThreshold = ref(0);
const rollingWindowSize = ref(30);
const fetchInterval = ref(60);
const configLoading = ref(false);
const configError = ref('');
const searchQuery = ref('');
const searchResults = ref([]);
const searchLoading = ref(false);
const searchError = ref('');
const watchlist = ref([]);
const alerts = ref([]);
const positions = ref([]);
const notifications = ref([]);
const alertQuantities = reactive({});
const lastUpdated = ref('');

const updateTimestamp = () => {
  lastUpdated.value = new Date().toLocaleTimeString();
};

const loadConfig = async () => {
  configError.value = '';
  try {
    const data = await getConfig();
    threshold.value = data.standardDeviationThreshold ?? 0;
    recoveryThreshold.value = data.recoveryStandardDeviationThreshold ?? 0;
    rollingWindowSize.value = data.rollingWindowSize ?? 30;
    fetchInterval.value = data.fetchIntervalSeconds ?? 60;
  } catch (error) {
    configError.value = error.message;
  }
};

const saveConfig = async () => {
  configLoading.value = true;
  configError.value = '';
  try {
    await updateConfig({
      standardDeviationThreshold: threshold.value,
      recoveryStandardDeviationThreshold: recoveryThreshold.value,
      rollingWindowSize: rollingWindowSize.value,
      fetchIntervalSeconds: fetchInterval.value
    });
    updateTimestamp();
  } catch (error) {
    configError.value = error.message;
  } finally {
    configLoading.value = false;
  }
};

const runSearch = async () => {
  if (!searchQuery.value) {
    searchResults.value = [];
    return;
  }
  searchLoading.value = true;
  searchError.value = '';
  try {
    searchResults.value = await searchItems(searchQuery.value);
  } catch (error) {
    searchError.value = error.message;
  } finally {
    searchLoading.value = false;
  }
};

const loadWatchlist = async () => {
  watchlist.value = await getWatchlist();
};

const addToWatchlist = async (itemId) => {
  await addWatchlistItem(itemId);
  await loadWatchlist();
};

const removeFromWatchlist = async (itemId) => {
  await removeWatchlistItem(itemId);
  await loadWatchlist();
};

const loadAlerts = async () => {
  const data = await getAlerts();
  alerts.value = data.map((alert) => {
    const currentPrice = alert.currentPrice ?? alert.triggerPrice ?? 0;
    const dropPercent =
      alert.mean && currentPrice
        ? (((alert.mean - currentPrice) / alert.mean) * 100).toFixed(1)
        : '0.0';
    return {
      ...alert,
      currentPrice,
      dropPercent
    };
  });
};

const acknowledge = async (alert) => {
  const quantity = alertQuantities[alert.id] ?? 1;
  await acknowledgeAlert(alert.id, quantity);
  await Promise.all([loadAlerts(), loadPositions()]);
};

const loadPositions = async () => {
  const data = await getPositions();
  positions.value = data.map((position) => {
    const isRecovered = Boolean(position.recoveredAt || position.recoveryPrice);
    return {
      ...position,
      recoveryPrice: position.recoveryPrice ?? '—',
      status: isRecovered ? 'recovered' : 'open',
      statusLabel: isRecovered ? 'Recovered' : 'Open'
    };
  });
};

const loadNotifications = async () => {
  notifications.value = await getNotifications();
};

const refreshAll = async () => {
  await Promise.all([
    loadWatchlist(),
    loadAlerts(),
    loadPositions(),
    loadNotifications()
  ]);
  updateTimestamp();
};

const formatTimestamp = (timestamp) => {
  if (!timestamp) {
    return 'just now';
  }
  return new Date(timestamp).toLocaleString();
};

onMounted(async () => {
  await loadConfig();
  await refreshAll();
  setInterval(refreshAll, 30000);
});
</script>

<style scoped>
.app {
  max-width: 1200px;
  margin: 0 auto;
  padding: 32px 20px 60px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.hero {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
}

.hero h1 {
  margin: 0 0 8px;
  font-size: 2.2rem;
}

.status {
  background: #e2e8f0;
  padding: 8px 12px;
  border-radius: 999px;
  font-size: 0.85rem;
}

.card {
  background: white;
  border-radius: 16px;
  padding: 20px 24px;
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
}

.card__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.grid {
  display: grid;
  gap: 16px;
}

.two-col {
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-weight: 600;
}

.field input {
  padding: 10px 12px;
  border-radius: 10px;
  border: 1px solid #cbd5f5;
}

.search input {
  min-width: 220px;
}

.primary,
.secondary,
.ghost {
  border: none;
  border-radius: 10px;
  padding: 10px 14px;
  cursor: pointer;
}

.primary {
  background: #2563eb;
  color: white;
}

.secondary {
  background: #e2e8f0;
  color: #0f172a;
}

.ghost {
  background: transparent;
  color: #2563eb;
}

.list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.list__item {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  padding: 12px 14px;
  border-radius: 12px;
  background: #f8fafc;
}

.list__item p {
  margin: 4px 0 0;
}

.action-group {
  display: flex;
  gap: 8px;
  align-items: center;
}

.action-group input {
  width: 80px;
  padding: 8px;
  border-radius: 8px;
  border: 1px solid #cbd5f5;
}

.notification-panel {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.notification {
  padding: 12px 14px;
  border-radius: 12px;
  background: #eff6ff;
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

.notification.recovery {
  background: #dcfce7;
}

.notification.drop {
  background: #fee2e2;
}

.pill {
  padding: 6px 10px;
  border-radius: 999px;
  font-size: 0.8rem;
  background: #e2e8f0;
}

.pill.recovered {
  background: #dcfce7;
  color: #166534;
}

.pill.open {
  background: #fee2e2;
  color: #991b1b;
}

.muted {
  color: #64748b;
  font-size: 0.85rem;
}

.error {
  color: #b91c1c;
  font-size: 0.85rem;
}

.empty {
  color: #94a3b8;
  font-style: italic;
}

@media (max-width: 720px) {
  .hero {
    flex-direction: column;
  }

  .list__item {
    flex-direction: column;
    align-items: flex-start;
  }

  .action-group {
    width: 100%;
  }

  .action-group input {
    flex: 1;
  }
}
</style>
