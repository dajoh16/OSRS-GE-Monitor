<template>
  <div class="card">
    <div class="card__header">
      <h2>Watchlist</h2>
      <div class="header-actions">
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
        <div>
          <strong>{{ item.name }}</strong>
          <span class="muted">#{{ item.id }}</span>
        </div>
        <div class="item-actions">
          <router-link class="ghost" :to="`/history/${item.id}`">
            View history
          </router-link>
          <button class="ghost" @click="$emit('remove', item.id)">
            Remove
          </button>
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

defineEmits(['remove', 'bulk-add']);

const watchlistQuery = defineModel('watchlistQuery');
const watchlistDisplayLimit = defineModel('watchlistDisplayLimit');
const bulkNames = defineModel('bulkNames');
</script>
