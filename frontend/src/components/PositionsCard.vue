<template>
  <div class="card">
    <div class="card__header">
      <h2>Positions</h2>
      <div class="header-actions">
        <router-link class="ghost" to="/profit">View P&L charts</router-link>
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
        <span class="pill status-pill" :class="position.status">
          {{ position.statusLabel }}
        </span>
        <div>
          <strong>{{ position.itemName }}</strong>
          <p class="muted">
            Bought {{ position.quantity }} @ {{ position.buyPrice }} - Recovery {{
              position.recoveryPrice
            }}
          </p>
          <p v-if="position.soldAt" class="muted">
            Sold @ {{ position.sellPrice }} - Tax {{ position.taxPaid }} - Profit
            <span :class="['profit', position.profit >= 0 ? 'positive' : 'negative']">
              {{ position.profit }}
            </span>
          </p>
        </div>
        <div class="item-actions">
          <div v-if="!position.soldAt" class="sell-controls">
            <input
              v-model.number="sellPrices[position.id]"
              type="number"
              min="1"
              placeholder="Sell price"
            />
            <input
              v-model.number="sellQuantities[position.id]"
              type="number"
              min="1"
              :max="position.quantity"
              placeholder="Sell qty"
            />
            <button
              class="primary action-button"
              :disabled="!canSellAll(position.id)"
              @click="emitSellAll(position.id)"
            >
              Sell All
            </button>
            <button
              class="primary action-button"
              :disabled="!canSellQuantity(position.id, position.quantity)"
              @click="emitSellQuantity(position.id, position.quantity)"
            >
              Sell Qty
            </button>
          </div>
          <div class="sell-controls">
            <input
              v-model.number="buyPrices[position.id]"
              type="number"
              min="1"
              placeholder="Edit buy"
            />
            <input
              v-model.number="addQuantities[position.id]"
              type="number"
              min="1"
              step="1"
              placeholder="Add qty"
            />
            <button
              class="secondary action-button"
              :disabled="!canUpdatePosition(position.id)"
              @click="emitPositionUpdate(position.id, position.buyPrice)"
            >
              Update
            </button>
          </div>
          <button class="secondary danger action-button" @click="$emit('remove', position.id)">
            Remove
          </button>
        </div>
      </div>
    </div>
    <div v-else class="empty">
      Acknowledge an alert to start tracking a bought item.
    </div>
    <div class="manual-entry">
      <h3>Manual entry</h3>
      <div class="manual-entry__fields">
        <label class="field">
          Item name
          <input v-model.trim="manualName" type="text" placeholder="Yew logs" />
        </label>
        <label class="field">
          Quantity
          <input v-model.number="manualQuantity" type="number" min="1" step="1" />
        </label>
        <label class="field">
          Buy price
          <input v-model.number="manualBuyPrice" type="number" min="1" step="1" />
        </label>
      </div>
      <p v-if="manualName" class="muted">
        <span v-if="manualMatchLoading">Matching item...</span>
        <span v-else-if="manualMatch">Best match: {{ manualMatch }}</span>
        <span v-else>No catalog match yet.</span>
      </p>
      <button class="primary" type="button" :disabled="!canSubmitManual" @click="submitManual">
        Add manual position
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue';

defineProps({
  displayedPositions: Array,
  positionsCount: Number,
  positionLimitOptions: Array,
  manualMatch: String,
  manualMatchLoading: Boolean
});

const emit = defineEmits(['remove', 'sell', 'update-position', 'manual-add']);

const positionDisplayLimit = defineModel('positionDisplayLimit');
const sellPrices = defineModel('sellPrices');
const sellQuantities = defineModel('sellQuantities');
const buyPrices = defineModel('buyPrices');
const addQuantities = defineModel('addQuantities');
const manualName = defineModel('manualName');
const manualQuantity = ref(1);
const manualBuyPrice = ref(null);

const canSubmitManual = computed(() => {
  return manualName.value.trim().length > 0 && Number(manualQuantity.value) > 0 && Number(manualBuyPrice.value) > 0;
});
const getSellPrice = (id) => Number(sellPrices.value[id]);
const getSellQuantity = (id) => Number(sellQuantities.value[id]);

const canSellAll = (id) => {
  const price = getSellPrice(id);
  return Number.isFinite(price) && price > 0;
};

const canSellQuantity = (id, maxQuantity) => {
  const price = getSellPrice(id);
  const quantity = getSellQuantity(id);
  return (
    Number.isFinite(price) &&
    price > 0 &&
    Number.isInteger(quantity) &&
    quantity > 0 &&
    quantity <= maxQuantity
  );
};

const emitSellAll = (id) => {
  if (!canSellAll(id)) {
    return;
  }
  const price = getSellPrice(id);
  emit('sell', { id, price, quantity: null });
  sellPrices.value[id] = null;
  sellQuantities.value[id] = null;
};

const emitSellQuantity = (id, maxQuantity) => {
  if (!canSellQuantity(id, maxQuantity)) {
    return;
  }
  const price = getSellPrice(id);
  const quantity = getSellQuantity(id);
  emit('sell', { id, price, quantity });
  sellPrices.value[id] = null;
  sellQuantities.value[id] = null;
};

const getBuyPriceUpdate = (id) => Number(buyPrices.value[id]);
const getAddQuantity = (id) => Number(addQuantities.value[id]);

const canUpdatePosition = (id) => {
  const price = getBuyPriceUpdate(id);
  const addQuantity = getAddQuantity(id);
  const hasPrice = Number.isFinite(price) && price > 0;
  const hasQuantity = Number.isInteger(addQuantity) && addQuantity > 0;
  return hasPrice || hasQuantity;
};

const emitPositionUpdate = (id, currentBuyPrice) => {
  if (!canUpdatePosition(id)) {
    return;
  }
  const price = getBuyPriceUpdate(id);
  const addQuantity = getAddQuantity(id);
  emit('update-position', {
    id,
    buyPrice: Number.isFinite(price) && price > 0 ? price : null,
    addQuantity: Number.isInteger(addQuantity) && addQuantity > 0 ? addQuantity : null,
    currentBuyPrice
  });
  buyPrices.value[id] = null;
  addQuantities.value[id] = null;
};

const submitManual = () => {
  if (!canSubmitManual.value) {
    return;
  }
  emit('manual-add', {
    name: manualName.value.trim(),
    quantity: Number(manualQuantity.value),
    buyPrice: Number(manualBuyPrice.value)
  });
  manualName.value = '';
  manualQuantity.value = 1;
  manualBuyPrice.value = null;
};
</script>
