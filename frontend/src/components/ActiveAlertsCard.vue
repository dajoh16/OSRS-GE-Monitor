<template>
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
            Drop: {{ alert.dropPercent }}% - Current price {{ alert.currentPrice }}
          </p>
        </div>
        <div class="action-group">
          <input
            v-model.number="alertQuantities[alert.id]"
            type="number"
            min="1"
            placeholder="Qty"
          />
          <button class="primary action-button" @click="$emit('acknowledge', alert)">
            Bought
          </button>
          <button class="secondary danger action-button" @click="$emit('remove', alert.id)">
            Remove
          </button>
        </div>
      </div>
    </div>
    <div v-else class="empty">
      No active alerts. We'll keep watching the market.
    </div>
  </div>
</template>

<script setup>
defineProps({
  alerts: Array,
  alertQuantities: Object
});

defineEmits(['acknowledge', 'remove']);
</script>
