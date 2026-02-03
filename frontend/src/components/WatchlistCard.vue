<template>
  <div class="card">
    <div class="card__header">
      <h2>Watchlist</h2>
      <div class="header-actions">
        <label class="inline-field">
          Sort by
          <select v-model="watchlistSortField">
            <option value="name">Name</option>
            <option value="id">Id</option>
            <option value="high">High</option>
            <option value="low">Low</option>
            <option value="limit">Limit</option>
            <option value="afterTax">After-tax</option>
          </select>
        </label>
        <button class="secondary action-button" type="button" @click="toggleSortDirection">
          {{ watchlistSortDirection === 'asc' ? 'Asc' : 'Desc' }}
        </button>
        <label class="inline-field">
          Show
          <select v-model.number="watchlistDisplayLimit">
            <option v-for="size in watchlistLimitOptions" :key="size" :value="size">
              {{ size }}
            </option>
          </select>
        </label>
        <span class="muted">
          Showing {{ displayedWatchlist.length }} of {{ filteredCount }}
        </span>
      </div>
    </div>
    <div class="grid">
      <label class="field search">
        Filter watchlist
        <input v-model.trim="watchlistQuery" placeholder="Search watchlist..." />
      </label>
    </div>
    <div v-if="displayedWatchlist.length" class="list list--spaced">
      <div v-for="item in displayedWatchlist" :key="item.id" class="list__item">
        <div class="watchlist-row">
          <div class="watchlist-row__header">
            <strong>{{ item.name }}</strong>
            <span class="muted">#{{ item.id }}</span>
          </div>
          <div class="watchlist-row__market">
            <span v-if="item.marketHigh || item.marketLow" class="muted">
              High {{ item.marketHigh ?? '—' }} / Low {{ item.marketLow ?? '—' }} · Limit
              {{ item.buyLimit ?? '—' }}
            </span>
            <span v-else class="muted">Market data pending...</span>
          </div>
          <div v-if="item.marketHigh || item.marketLow" class="watchlist-row__market">
            <span class="muted">
              Spread {{ formatNumber(spread(item)) }} · After tax
              <span :class="['profit', afterTaxProfit(item) >= 0 ? 'positive' : 'negative']">
                {{ formatNumber(afterTaxProfit(item)) }}
              </span>
            </span>
          </div>
          <div class="watchlist-row__actions">
            <button class="secondary action-button" @click="$emit('details', item.id)">
              Details
            </button>
            <router-link class="secondary action-button button-link" :to="`/history/${item.id}`">
              View history
            </router-link>
            <button class="secondary danger action-button" @click="$emit('remove', item.id)">
              Remove
            </button>
          </div>
        </div>
      </div>
    </div>
    <div v-else class="empty">
      No items in your watchlist yet.
    </div>
    <div class="bulk-add">
      <label class="field">
        Bulk add by name (one per line or comma-separated)
        <textarea
          v-model="bulkNames"
          rows="4"
          placeholder="Rune scimitar&#10;Shark&#10;Dragon scimitar"
        ></textarea>
      </label>
      <button class="secondary" :disabled="bulkLoading" @click="$emit('bulk-add')">
        {{ bulkLoading ? 'Adding...' : 'Add list' }}
      </button>
      <span v-if="bulkError" class="error">{{ bulkError }}</span>
      <div v-if="bulkResult" class="bulk-result muted">
        Added {{ bulkResult.added.length }},
        duplicates {{ bulkResult.duplicates.length }},
        not found {{ bulkResult.notFound.length }}.
      </div>
      <div v-if="bulkResult?.matched?.length" class="bulk-matches">
        <div v-for="match in bulkResult.matched" :key="`${match.inputName}-${match.itemId}`">
          {{ match.inputName }} -> {{ match.matchedName }} (#{{ match.itemId }})
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  displayedWatchlist: Array,
  filteredCount: Number,
  bulkResult: Object,
  bulkError: String,
  bulkLoading: Boolean,
  watchlistLimitOptions: Array
});

defineEmits(['remove', 'bulk-add', 'details']);

const watchlistQuery = defineModel('watchlistQuery');
const watchlistDisplayLimit = defineModel('watchlistDisplayLimit');
const bulkNames = defineModel('bulkNames');
const watchlistSortField = defineModel('watchlistSortField');
const watchlistSortDirection = defineModel('watchlistSortDirection');

const formatNumber = (value) => {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return '—';
  }
  return Math.round(value).toLocaleString();
};

const spread = (item) => {
  if (item.marketHigh == null || item.marketLow == null) {
    return null;
  }
  return item.marketHigh - item.marketLow;
};

const afterTaxProfit = (item) => {
  if (item.marketHigh == null || item.marketLow == null) {
    return null;
  }
  const tax = item.marketHigh < 100 ? 0 : Math.floor(item.marketHigh * 0.02);
  return item.marketHigh - item.marketLow - tax;
};

const toggleSortDirection = () => {
  watchlistSortDirection.value = watchlistSortDirection.value === 'asc' ? 'desc' : 'asc';
};
</script>
