<template>
  <div class="card">
    <div class="card__header">
      <h2>Positions</h2>
      <div class="header-actions">
        <label class="inline-field">
          Show
          <select v-model.number="positionDisplayLimit">
            <option v-for="size in positionLimitOptions" :key="size" :value="size">
              {{ size }}
            </option>
          </select>
        </label>
        <span class="muted">{{ displayedPositions.length }} of {{ positionsCount }}</span>
      </div>
    </div>
    <div v-if="displayedPositions.length" class="list">
      <div v-for="position in displayedPositions" :key="position.id" class="list__item">
        <div>
          <strong>{{ position.itemName }}</strong>
          <p class="muted">
            Bought {{ position.quantity }} @ {{ position.buyPrice }} - Recovery {{
              position.recoveryPrice
            }}
          </p>
        </div>
        <div class="item-actions">
          <span class="pill" :class="position.status">
            {{ position.statusLabel }}
          </span>
          <button class="ghost" @click="$emit('remove', position.id)">
            Remove
          </button>
        </div>
      </div>
    </div>
    <div v-else class="empty">
      Acknowledge an alert to start tracking a bought item.
    </div>
  </div>
</template>

<script setup>
defineProps({
  displayedPositions: Array,
  positionsCount: Number,
  positionLimitOptions: Array
});

defineEmits(['remove']);

const positionDisplayLimit = defineModel('positionDisplayLimit');
</script>
