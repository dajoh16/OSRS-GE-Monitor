<template>
  <section class="card">
    <div class="card__header">
      <div class="status-row">
        <h2>Browse Items</h2>
        <span class="catalog-status" :class="catalogStatusClass">
          <span class="catalog-dot" :class="catalogStatusClass"></span>
          {{ catalogStatusLabel }}
        </span>
      </div>
      <div class="header-actions">
        <label class="inline-field">
          Page size
          <select v-model.number="searchPageSize">
            <option v-for="size in searchPageOptions" :key="size" :value="size">
              {{ size }}
            </option>
          </select>
        </label>
        <span v-if="searchError" class="error">{{ searchError }}</span>
      </div>
    </div>
    <div class="grid">
      <label class="field search">
        Search items
        <input v-model.trim="searchQuery" placeholder="Rune scimitar, shark..." />
      </label>
      <button class="secondary" :disabled="searchLoading" @click="$emit('search')">
        {{ searchLoading ? 'Searching...' : 'Search' }}
      </button>
    </div>
    <div v-if="pagedSearchResults.length" class="list list--spaced">
      <div v-for="item in pagedSearchResults" :key="item.id" class="list__item">
        <div>
          <strong>{{ item.name }}</strong>
          <span class="muted">#{{ item.id }}</span>
        </div>
        <button class="primary" @click="$emit('add', item.id)">
          Add to watchlist
        </button>
      </div>
    </div>
    <div v-if="searchResultsLength" class="pagination">
      <button class="ghost" :disabled="searchPage === 1" @click="searchPage -= 1">
        Prev
      </button>
      <span class="muted">Page {{ searchPage }} of {{ searchTotalPages }}</span>
      <button class="ghost" :disabled="searchPage >= searchTotalPages" @click="searchPage += 1">
        Next
      </button>
    </div>
    <div v-else class="empty">
      Search for items to add them to your watchlist.
    </div>
  </section>
</template>

<script setup>
defineProps({
  catalogStatusLabel: String,
  catalogStatusClass: String,
  searchError: String,
  searchLoading: Boolean,
  pagedSearchResults: Array,
  searchResultsLength: Number,
  searchTotalPages: Number,
  searchPageOptions: Array
});

defineEmits(['search', 'add']);

const searchQuery = defineModel('searchQuery');
const searchPage = defineModel('searchPage');
const searchPageSize = defineModel('searchPageSize');
</script>
