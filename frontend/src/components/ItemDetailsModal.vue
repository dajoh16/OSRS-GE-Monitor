<template>
  <div v-if="open" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-card">
      <div class="modal-header">
        <h3>Item details</h3>
        <button class="ghost" type="button" @click="$emit('close')">Close</button>
      </div>
      <div v-if="loading" class="muted">Loading...</div>
      <div v-else-if="error" class="error">{{ error }}</div>
      <div v-else-if="!item" class="empty">No item selected.</div>
      <div v-else class="details-grid">
        <div class="details-summary">
          <strong>{{ item.name }}</strong>
          <span class="muted">#{{ item.id }}</span>
          <p v-if="item.examine" class="muted">{{ item.examine }}</p>
        </div>
        <div class="details-meta">
          <p><span class="muted">Members:</span> {{ item.members ? 'Yes' : 'No' }}</p>
          <p><span class="muted">Buy limit:</span> {{ item.buyLimit ?? 'Unknown' }}</p>
          <p><span class="muted">Low alch:</span> {{ item.lowAlch ?? 'Unknown' }}</p>
          <p><span class="muted">High alch:</span> {{ item.highAlch ?? 'Unknown' }}</p>
          <p><span class="muted">Value:</span> {{ item.value ?? 'Unknown' }}</p>
        </div>
        <div class="details-trend">
          <p class="muted">Trend ({{ item.trend?.window ?? '1h' }})</p>
          <p v-if="item.trend" :class="['trend', item.trend.direction]">
            {{ trendLabel(item.trend.direction) }}
            <span v-if="item.trend.percentChange !== null">
              {{ item.trend.percentChange > 0 ? '+' : '' }}{{ item.trend.percentChange }}%
            </span>
          </p>
          <p v-else class="muted">Not enough data.</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  open: Boolean,
  loading: Boolean,
  error: String,
  item: Object
});

defineEmits(['close']);

const trendLabel = (direction) => {
  switch (direction) {
    case 'up':
      return 'Trending up';
    case 'down':
      return 'Trending down';
    default:
      return 'Flat';
  }
};
</script>
